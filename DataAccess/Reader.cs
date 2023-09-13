using System.Collections.ObjectModel;
using BaseLib;
using Dapper;
using DataAccess.Interfaces;
using DataAccess.Models;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Reader<T> : IReader<T> where T : class {
    protected readonly IDbConnectionManager dbConnectionService;
    private readonly ILogger logger;
    private readonly SqlBuilder sqlBuilder;
    private readonly TableInfo<T> tableInfo;

    public Reader(IDbConnectionManager dbConnectionService, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.dbConnectionService = dbConnectionService;
        this.logger = loggerFactory.CreateLogger(typeof(T));
        tableInfo = databaseMapper.GetTableInfo<T>();
        sqlBuilder = new SqlBuilder(tableInfo);
    }

    public virtual async Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, IEnumerable<OrderByExpression>? orderBy = null) {
        try {
            var sql = sqlBuilder.GetSelectSql(filter, pageSize, pageNum, orderBy);
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