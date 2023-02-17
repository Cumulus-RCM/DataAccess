using System.Collections.ObjectModel;
using Dapper;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Reader<T> : IReader<T> where T : class {
    protected readonly DbConnectionManager dbConnectionService;
    private readonly ILogger logger;
    private readonly SqlBuilder sqlBuilder;
    private readonly TableInfo<T> tableInfo;

    public Reader(DbConnectionManager dbConnectionService, DatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.dbConnectionService = dbConnectionService;
        this.logger = loggerFactory.CreateLogger(typeof(T));
        tableInfo = databaseMapper.GetTableInfo<T>();
        sqlBuilder = new SqlBuilder(tableInfo);
    }

    public virtual async Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, string orderBy = "") {
        try {
            var sql = sqlBuilder.GetSelectSql(filter, pageSize, pageNum);
            using var conn = dbConnectionService.CreateConnection();
            var parms = filter?.GetDynamicParameters();
            var result = await conn.QueryAsync<T>(sql,parms ).ConfigureAwait(false);
            return result.ToList().AsReadOnly();
        }
        catch (Exception e) {
            logger.LogError(e, nameof(GetAllAsync));
            return Array.Empty<T>().AsReadOnly();
        }
    }

    public virtual async Task<T?> GetByIdAsync(int id) {
        var filter = new Filter(new FilterExpression("Id", Operator.Equal) {ValueType = nameof(Int32)});
        var sql = sqlBuilder.GetSelectSql(filter);
        using var conn = dbConnectionService.CreateConnection();
        var result = await conn.QueryFirstAsync<T>(sql, new {Id = id}).ConfigureAwait(false);
        return result;
    }

    public virtual async Task<T?> GetByPkAsync(object pk) {
        var filter = new Filter(new FilterExpression(tableInfo.PrimaryKeyName, Operator.Equal));
        var sql = sqlBuilder.GetSelectSql(filter);
        using var conn = dbConnectionService.CreateConnection();
        var args = new Dictionary<string, object> {
            {tableInfo.PrimaryKeyName, pk}
        };
        var result = await conn.QueryFirstAsync<T>(sql, args).ConfigureAwait(false);
        return result;
    }

    public virtual async Task<int> GetCountAsync(Filter? filter = null) {
        using var conn = dbConnectionService.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sqlBuilder.GetCountSql(filter), filter?.GetDynamicParameters());
    }
}