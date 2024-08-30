using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Services.SqlBuilders;
using DataAccess.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace DataAccess;

public class SimpleSaveStrategy(IDbConnectionManager connectionManager, ISequenceGenerator sequenceGenerator) : ISaveStrategy {
    public async Task<SaveResponse> SaveAsync(IEnumerable<IDataChange> dataChanges) {
        //Implements simplistic approach to ordering the data changes to ensure referential integrity
        //  i.e. parent table is inserted before child table
        //  Each table has a priority which can be set in the TableInfo
        using var conn = connectionManager.CreateConnection();
        using var dbTransaction = conn.BeginTransaction();
        var sql = "";
        ITableInfo? tableInfo = null;
        var sb = new SaveResponseBuilder();
        try {
            var changes = dataChanges
                .OrderBy(dc => dc.TableInfo.Priority)
                .ThenBy(dc => dc.TableInfo.TableName)
                .ToList();
            foreach (var dataChange in changes) {
                tableInfo = dataChange.TableInfo;
                sql = new TableSqlBuilder(dataChange.TableInfo).GetWriteSql(dataChange);
                if (dataChange.DataChangeKind == DataChangeKind.Insert) {
                    if (dataChange.IsCollection) {
                        var collection = (ICollection)dataChange.Entity;
                        if (dataChange.TableInfo.IsIdentity) {
                            foreach (var item in collection) {
                                var id = await conn.QuerySingleAsync<IdPk>($"{sql}", item).ConfigureAwait(false);
                                dataChange.TableInfo.SetPrimaryKeyValue(item, id);
                                sb.Add(tableInfo.TableName, insertedIds: id.ItemAsEnumerable());
                            }
                        }
                        else {
                            if (conn is SqlConnection sqlConn && dbTransaction is SqlTransaction sqlTransaction) {
                                var idsNeeded = collection.Cast<object>()
                                    .Count(item => (IdPk)dataChange.TableInfo.GetPrimaryKeyValue(item) == 0);
                                var id = await sequenceGenerator.GetSequencesAsync(dataChange.TableInfo, idsNeeded).ConfigureAwait(false);
                                foreach (var item in collection) {
                                    if ((IdPk)tableInfo.GetPrimaryKeyValue(item) == 0) tableInfo.SetPrimaryKeyValue(item, id);
                                    sb.Add(tableInfo.TableName, insertedIds: id.ItemAsEnumerable());
                                    id++;
                                }

                                await bulkInsert(sqlConn, tableName: tableInfo.TableName, collection, sqlTransaction).ConfigureAwait(false);
                            }
                        }
                    }
                    else if (dataChange.SqlShouldReturnPk) {
                        var id = await conn.ExecuteScalarAsync<IdPk>($"{sql}", dataChange.Entity, dbTransaction).ConfigureAwait(false);
                        tableInfo.SetPrimaryKeyValue(dataChange.Entity, id);
                        sb.Add(tableInfo.TableName, insertedIds: id.ItemAsEnumerable());
                    }
                    else if (dataChange.TableInfo.PrimaryKeyType == typeof(IdPk)) {
                        await conn.ExecuteScalarAsync<int>($"{sql}", dataChange.Entity, dbTransaction).ConfigureAwait(false);
                        var id = (IdPk)dataChange.TableInfo.GetPrimaryKeyValue(dataChange.Entity);
                        sb.Add(tableInfo.TableName, insertedIds: id.ItemAsEnumerable());
                    }
                }
                else {
                    var ct = dataChange.DataChangeKind == DataChangeKind.StoredProcedure
                        ? CommandType.StoredProcedure
                        : CommandType.Text;
                    var rows = await conn.ExecuteAsync(sql, dataChange.Entity, dbTransaction, commandType: ct).ConfigureAwait(false);
                    if (dataChange.DataChangeKind == DataChangeKind.Update) sb.Add(tableInfo.TableName, updatedCount: rows);
                    else if (dataChange.DataChangeKind == DataChangeKind.Delete) sb.Add(tableInfo.TableName, deletedCount: rows);
                }
            }

            dbTransaction.Commit();
        }
        catch (Exception ex) {
            dbTransaction.Rollback();
            Log.Error(ex, $"Method: {nameof(SaveAsync)} Sql: {sql}");
            sb.Add(tableInfo?.TableName ?? "No Table Set", exception: ex);
        }

        return sb.Build();
    }

    private static async Task<int> bulkInsert(SqlConnection conn, string tableName, IEnumerable items, SqlTransaction? transaction) {
        using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, transaction);

        bulkCopy.DestinationTableName = tableName;
        bulkCopy.EnableStreaming = true;
        bulkCopy.BatchSize = 1000;

        using var dataTable = convertItemsToDataTable();
        if (dataTable is null) return 0;
        //ensure columns are in the same order required by BulkLoader
        foreach (DataColumn col in dataTable.Columns) {
            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable).ConfigureAwait(false);
        return dataTable.Rows.Count;

        DataTable? convertItemsToDataTable() {
            var json = JsonConvert.SerializeObject(items, new JsonSerializerSettings {
                ContractResolver = new WritablePropertiesOnlyResolver()
            });
            return JsonConvert.DeserializeObject<DataTable>(json);
        }
    }

    private class WritablePropertiesOnlyResolver : DefaultContractResolver {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
            var properties = base.CreateProperties(type, memberSerialization);
            return properties.Where(p => {
                var propertyInfo = type.GetProperty(p.PropertyName);
                if (propertyInfo == null) return false;
                var columnInfo = ColumnInfo.FromAttribute(propertyInfo);
                return columnInfo?.CanWrite ?? p.Writable;
            }).ToList();
        }
    }
}