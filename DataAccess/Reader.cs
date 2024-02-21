using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Reader<T> : IReader<T> where T : class {
    protected bool IsUsingCache { get; set; } = false;

    protected readonly IDbConnectionManager dbConnectionService;
    private readonly ILogger logger;
    protected readonly SqlBuilder sqlBuilder;
    private readonly ITableInfo tableInfo;

    public Reader(IDbConnectionManager dbConnectionService, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.dbConnectionService = dbConnectionService;
        this.logger = loggerFactory.CreateLogger(typeof(T));
        tableInfo = databaseMapper.GetTableInfo<T>();
        sqlBuilder = new SqlBuilder(tableInfo);
    }

    public void UseCache() {
        IsUsingCache = true;
        //TODO: Implement caching
    }

    public virtual async Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null) {
        try {
            var sql = sqlBuilder.GetReadSql(filter, pageSize, pageNum, orderBy);
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

    public virtual async Task<T?> GetByPkAsync(string pkValue) {
        var filter = Filter.Create( new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.Equal) {ValueString = pkValue});
        var result = await GetAllAsync(filter,pageSize: 1, pageNum: 1).ConfigureAwait(false);
        return result.FirstOrDefault();
    }

    public virtual async Task<int> GetCountAsync(Filter? filter = null) {
        using var conn = dbConnectionService.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sqlBuilder.GetCountSql(filter), filter?.GetDynamicParameters());
    }
}