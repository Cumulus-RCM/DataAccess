using System.Collections.ObjectModel;
using Dapper;
using DataAccess.Interfaces;
using DataAccess.Services;
using DataAccess.Shared.DatabaseMapper;
using DataAccess.Shared.FilterService;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Reader<T> : IReader<T> where T : class {
    protected readonly DbConnectionManager dbConnectionService;
    private readonly ILogger logger;
    private readonly SqlBuilder sqlBuilder;

    public Reader(DbConnectionManager dbConnectionService, DatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.dbConnectionService = dbConnectionService;
        this.logger = loggerFactory.CreateLogger(typeof(T));
        var tableInfo = databaseMapper.GetTableInfo<T>();
        sqlBuilder = new SqlBuilder(tableInfo);
    }

    public virtual async Task<ReadOnlyCollection<T>> GetAllAsync(Filter<T>? filter = null, object? filterValues = null, int pageSize = 0, int pageNum = 0, string orderBy = "") {
        try {
            var sql = sqlBuilder.GetSelectSql(filter?.ToString() ?? "", pageSize, pageNum);
            using var conn = dbConnectionService.CreateConnection();
            var result = await conn.QueryAsync<T>(sql, filterValues).ConfigureAwait(false);
            return result.ToList().AsReadOnly();
        }
        catch (Exception e)
        {
            logger.LogError(e, nameof(GetAllAsync));
            return Array.Empty<T>().AsReadOnly();
        }
    }

    public virtual async Task<T?> GetByIdAsync(int id) {
        var sql = sqlBuilder.GetSelectSql("Id=@id");
        using var conn = dbConnectionService.CreateConnection();
        var result = await conn.QueryFirstAsync<T>(sql, new {Id = id}).ConfigureAwait(false);
        return result;
    }

    public virtual  async Task<int> GetCountAsync(Filter<T>? filter = null, object? args = null) {
        using var conn = dbConnectionService.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sqlBuilder.GetCountSql(filter?.ToString() ?? ""), args);
    }
}