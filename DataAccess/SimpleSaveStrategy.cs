using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace DataAccess;

public class SimpleSaveStrategy(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory)
    : SaveStrategyBase(connectionManager, databaseMapper, loggerFactory) {
    private readonly ILogger logger = loggerFactory.CreateLogger<SimpleSaveStrategy>();

    public override async Task<SaveResult> SaveAsync(IEnumerable<IDataChange> dataChanges) {
        var conn = connectionManager.CreateConnection();
        var dbTransaction = conn.BeginTransaction();
        try {
            var updatedRowCount = 0;
            var insertedIds = new List<long>();
            var deletedRowCount = 0;
            foreach (var dataChange in dataChanges.ToList()) {
                var tableInfo = databaseMapper.GetTableInfo(dataChange.EntityType);
                var sql = SqlBuilder.GetWriteSql(tableInfo, dataChange.DataChangeKind, !dataChange.IsCollection, !dataChange.IsCollection);
                if (dataChange.DataChangeKind == DataChangeKind.Insert) {
                    if (dataChange.IsCollection) {
                        var collection = (ICollection<IDataChange>) dataChange.Entity;
                        if (tableInfo.IsIdentity) {
                            foreach (var item in collection) {
                                var id = await conn.ExecuteAsync($"{sql}", item).ConfigureAwait(false);
                                tableInfo.SetPrimaryKeyValue(item, id);
                                insertedIds.Add(id);
                            }
                        }
                        else {
                            if (conn is SqlConnection sqlConn && dbTransaction is SqlTransaction sqlTransaction) {
                                var idsNeeded = collection.Count(x => x.TableInfo.IsSequencePk && (IdPk)x.TableInfo.GetPrimaryKeyValue(x.Entity) == 0);
                                var id = await getSequenceValuesAsync(conn, tableInfo.SequenceName, idsNeeded).ConfigureAwait(false);
                                foreach (var item in collection) {
                                    if ((IdPk)item.TableInfo.GetPrimaryKeyValue(item) == 0) tableInfo.SetPrimaryKeyValue(item, id);
                                    insertedIds.Add(id);
                                    id++;
                                }
                                await bulkInsert(sqlConn, tableName: tableInfo.TableName, collection.Select(x=>x.Entity), sqlTransaction).ConfigureAwait(false);
                            }
                        }
                    }
                    else {
                        var id = await conn.ExecuteScalarAsync<int>($"{sql}", dataChange.Entity, dbTransaction).ConfigureAwait(false);
                        tableInfo.SetPrimaryKeyValue(dataChange.Entity, id);
                        insertedIds.Add(id);
                    }
                }
                else {
                    var ct = dataChange.DataChangeKind == DataChangeKind.StoredProcedure
                        ? CommandType.StoredProcedure
                        : CommandType.Text;
                    var rows = await conn.ExecuteAsync(sql, dataChange.Entity, dbTransaction, commandType:ct).ConfigureAwait(false);
                    if (dataChange.DataChangeKind == DataChangeKind.Update) updatedRowCount += rows;
                    else if (dataChange.DataChangeKind == DataChangeKind.Delete) deletedRowCount += rows;
                }
            }
            dbTransaction.Commit();
            return new SaveResult(updatedRowCount, deletedRowCount, insertedIds.ToArray() );
        }
        catch (Exception ex) {
            dbTransaction.Rollback();
            logger.LogError(ex, nameof(SaveAsync));
            return new SaveResult(0,0,[]);
        }
    }

    private static async Task<int> bulkInsert(SqlConnection conn, string tableName, IEnumerable items, SqlTransaction? transaction) {
        using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, transaction);
        bulkCopy.BulkCopyTimeout = 0;
        bulkCopy.BatchSize = 500;
        bulkCopy.DestinationTableName = tableName;
        bulkCopy.EnableStreaming = true;

        using var dataTable = convertItemsToDataTable();
        if (dataTable is null) return 0;
        //ensure columns are in the same order required by BulkLoader
        foreach (DataColumn col in dataTable.Columns) {
            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable).ConfigureAwait(false);
        return dataTable.Rows.Count;

        DataTable? convertItemsToDataTable() {
            var json = JsonConvert.SerializeObject(items, new JsonSerializerSettings {ContractResolver = new WritablePropertiesOnlyResolver()});
            return JsonConvert.DeserializeObject<DataTable>(json);
        }
    }

    private class WritablePropertiesOnlyResolver : DefaultContractResolver {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) =>
            base.CreateProperties(type, memberSerialization).Where(p => p.Writable).ToList();
    }
}