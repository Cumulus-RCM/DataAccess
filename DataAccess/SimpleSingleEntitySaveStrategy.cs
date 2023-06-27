using System.Collections;
using System.Data;
using System.Data.SqlClient;
using BaseLib;
using Dapper;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataAccess;

public class SimpleSingleEntitySaveStrategy : SaveStrategy {
    private readonly ILogger<SimpleSingleEntitySaveStrategy> logger;
    public SimpleSingleEntitySaveStrategy(IDbConnectionManager dbConnection, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) : base(dbConnection, databaseMapper) {
        this.logger = loggerFactory.CreateLogger<SimpleSingleEntitySaveStrategy>();
    }

    public override async Task<int> SaveAsync(IEnumerable<IDataChange> dataChanges) {
        var conn = dbConnection.CreateConnection();
        var dbTransaction = conn.BeginTransaction();
        try {
            var totalRowsEffected = 0;
            foreach (var dataChange in dataChanges) {
                var tableInfo = databaseMapper.GetTableInfo(dataChange.EntityType);
                var sql = getSql(dataChange, tableInfo);
                int rowsEffected = 0;
                if (dataChange.DataChangeKind == DataChangeKind.Insert) {
                    if (dataChange.IsCollection) {
                        var collection = (ICollection) dataChange.Entity;
                        if (tableInfo.IsIdentity) {
                            foreach (var item in collection) {
                                var id = await conn.ExecuteAsync($"{sql}", item).ConfigureAwait(false);
                                tableInfo.SetPrimaryKeyValue(item, id);
                            }
                            rowsEffected = collection.Count;
                        }
                        else {
                            if (conn is SqlConnection sqlConn && dbTransaction is SqlTransaction sqlTransaction) {
                                var firstId = await getSequenceValuesAsync(conn, tableInfo.SequenceName, collection.Count).ConfigureAwait(false);
                                foreach (var item in collection) {
                                    tableInfo.SetPrimaryKeyValue(item, firstId++);
                                }
                                rowsEffected = await bulkInsert(sqlConn, tableName: tableInfo.TableName, collection, sqlTransaction).ConfigureAwait(false);
                            }
                        }
                    }
                    else {
                        var id = await conn.ExecuteScalarAsync<int>($"{sql}", dataChange.Entity, dbTransaction).ConfigureAwait(false);
                        tableInfo.SetPrimaryKeyValue(dataChange.Entity, id);
                        rowsEffected = 1;
                    }
                }
                else {
                    rowsEffected = await conn.ExecuteAsync(sql, dataChange.Entity, dbTransaction).ConfigureAwait(false);
                }
                totalRowsEffected += rowsEffected;
            }
            dbTransaction.Commit();
            return totalRowsEffected;
        }
        catch (Exception ex) {
            dbTransaction.Rollback();
            logger.LogError(ex, nameof(SaveAsync));
            return 0;
        }
    }

    private string getSql(IDataChange dataChange, ITableInfo tableInfo) {
        var sqlBuilder = new SqlBuilder(tableInfo);
        if (dataChange.DataChangeKind == DataChangeKind.Update) return sqlBuilder.GetUpdateSql();
        if (dataChange.DataChangeKind == DataChangeKind.Insert) return sqlBuilder.GetInsertSql(!dataChange.IsCollection, !dataChange.IsCollection);
        if (dataChange.DataChangeKind == DataChangeKind.Delete) return sqlBuilder.GetDeleteSql();
        throw new ArgumentOutOfRangeException();
    }

    private async Task<int> getSequenceValuesAsync(IDbConnection conn, string sequenceName, int cnt) {
        try {
            object objResult = new();
            var parameters = new DynamicParameters();
            parameters.Add("@sequence_name", dbType: DbType.String, value: sequenceName,
                direction: ParameterDirection.Input);
            parameters.Add("@range_size", dbType: DbType.Int32, value: cnt, direction: ParameterDirection.Input);
            parameters.Add("@range_first_value", dbType: DbType.Object, value: objResult,
                direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sys.sp_sequence_get_range", parameters, commandType: CommandType.StoredProcedure)
                .ConfigureAwait(false);
            return objResult as int? ?? throw new Exception("No SequenceName value returned.");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to get new SequenceName value");
            throw;
        }
    }

    private static async Task<int> bulkInsert(SqlConnection conn, string tableName, ICollection items,
        SqlTransaction? transaction) {
        using var bulkCopy = new SqlBulkCopy(conn,
            SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, transaction);
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