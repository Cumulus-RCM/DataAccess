using System;
using System.Data;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Shared;
using Serilog;

namespace DataAccess;

public class SequenceGenerator(IDatabaseMapper databaseMapper, IDbConnectionManager connectionManager) : ISequenceGenerator {
    public Task<IdPk> GetSequencesAsync<T>(int cnt,IDbConnection? dbConnection = null, IDbTransaction? dbTransaction = null) where T : class => 
        GetSequencesAsync(databaseMapper.GetTableInfo<T>(), cnt, dbConnection, dbTransaction);

    public async Task<IdPk> GetSequencesAsync(ITableInfo tableInfo, int cnt, IDbConnection? dbConnection = null, IDbTransaction? dbTransaction = null) {
        if (cnt == 0) return 0;
        if (dbConnection != null) {
            return await getSequencesAsync(dbConnection, tableInfo.SequenceName, cnt, dbTransaction).ConfigureAwait(false);
        }
        using var conn = connectionManager.CreateConnection();
        return await getSequencesAsync(conn, tableInfo.SequenceName, cnt).ConfigureAwait(false);
    }

    private static async Task<IdPk> getSequencesAsync(IDbConnection conn, string sequenceName, int cnt, IDbTransaction? dbTransaction = null) {
        if (cnt == 0) return 0;
        try {
            var parameters = new DynamicParameters();
            parameters.Add("@sequence_name", dbType: DbType.String, value: sequenceName, direction: ParameterDirection.Input);
            parameters.Add("@range_size", dbType: DbType.Int32, value: cnt, direction: ParameterDirection.Input);
            parameters.Add("@range_first_value", dbType: DbType.Object, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_sequence_get_range", parameters, commandType: CommandType.StoredProcedure, transaction:dbTransaction).ConfigureAwait(false);
            return parameters.Get<long>("@range_first_value");
        }
        catch (Exception ex) {
            Log.Error(ex, "Failed to get new SequenceName value");
            throw;
        }
    }

}