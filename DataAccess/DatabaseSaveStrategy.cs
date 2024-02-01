using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public abstract class DatabaseSaveStrategy(IDbConnectionManager dbConnectionManager, ILoggerFactory loggerFactory) : ISaveStrategy {
    private readonly ILogger logger = loggerFactory.CreateLogger<DatabaseSaveStrategy>();
    public abstract Task<SaveResult> SaveAsync(IEnumerable<IDataChange> dataChanges);

    public async Task<IdPk> GetSequenceValuesAsync(string sequenceName, int cnt) {
        var conn = dbConnectionManager.CreateConnection();
        return await getSequenceValuesAsync(conn, sequenceName, cnt).ConfigureAwait(false);
    }

    protected async Task<int> getSequenceValuesAsync(IDbConnection conn, string sequenceName, int cnt) {
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
}