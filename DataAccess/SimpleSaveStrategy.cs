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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace DataAccess;

public class SimpleSaveStrategy(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper) : ISaveStrategy {

    public async Task<IdPk> GetSequenceValuesAsync<T>(int cnt) where T : class {
        if (cnt == 0) return 0;
        var tableInfo = databaseMapper.GetTableInfo<T>();
        IdPk result;
        using (var conn = connectionManager.CreateConnection()) {
            result = await getSequenceValuesAsync(conn, tableInfo.SequenceName, cnt).ConfigureAwait(false);
        }
        return result;
    }

    public async Task<SaveResponse> SaveAsync(IEnumerable<IDataChange> dataChanges) {
        //Implements simplistic approach to ordering the data changes to ensure referential integrity
        //  i.e. parent table is inserted before child table
        //  Each table has a priority which can be set in the TableInfo
        using var conn = connectionManager.CreateConnection();
        using var dbTransaction = conn.BeginTransaction();
        try {
            var updatedRowCount = 0;
            var insertedIds = new List<long>();
            var deletedRowCount = 0;
            var changes = dataChanges
                .OrderBy(dc => dc.TableInfo.Priority)
                .ThenBy(dc => dc.TableInfo.TableName)
                .ToList();
            foreach (var dataChange in changes) {
                var tableInfo = dataChange.TableInfo;
                var sql = SqlBuilder.GetWriteSql(dataChange);
                if (dataChange.DataChangeKind == DataChangeKind.Insert) {
                    if (dataChange.IsCollection) {
                        var collection = (ICollection<IDataChange>) dataChange.Entity;
                        if (dataChange.TableInfo.IsIdentity) {
                            foreach (var item in collection) {
                                var id = await conn.ExecuteAsync($"{sql}", item).ConfigureAwait(false);
                                dataChange.TableInfo.SetPrimaryKeyValue(item, id);
                                insertedIds.Add(id);
                            }
                        }
                        else {
                            if (conn is SqlConnection sqlConn && dbTransaction is SqlTransaction sqlTransaction) {
                                var idsNeeded = collection.Count(x => x.TableInfo.IsSequencePk && (IdPk)x.TableInfo.GetPrimaryKeyValue(x.Entity) == 0);
                                var id = await getSequenceValuesAsync(conn, dataChange.TableInfo.SequenceName, idsNeeded).ConfigureAwait(false);
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
            return new SaveResponse(updatedRowCount, deletedRowCount, insertedIds.ToArray() );
        }
        catch (Exception ex) {
            dbTransaction.Rollback();
            Log.Error(ex, nameof(SaveAsync));
            return new SaveResponse(0,0,[], ex);
        }
    }

    private async Task<long> getSequenceValuesAsync(IDbConnection conn, string sequenceName, int cnt) {
        try {
            var parameters = new DynamicParameters();
            parameters.Add("@sequence_name", dbType: DbType.String, value: sequenceName, direction: ParameterDirection.Input);
            parameters.Add("@range_size", dbType: DbType.Int32, value: cnt, direction: ParameterDirection.Input);
            parameters.Add("@range_first_value", dbType: DbType.Object, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_sequence_get_range", parameters, commandType: CommandType.StoredProcedure)
                .ConfigureAwait(false);
            return parameters.Get<long>("@range_first_value");
        }
        catch (Exception ex) {
            Log.Error(ex, "Failed to get new SequenceName value");
            throw;
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