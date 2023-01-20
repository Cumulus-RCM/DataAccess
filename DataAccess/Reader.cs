using System.Collections.ObjectModel;
using Dapper;
using DataAccess.Models;
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

    private Filter<T>? mapFilter(Filter<T>? inFilter) {
        if (inFilter is null) return null;
        var newFilter = new Filter<T>();
        foreach (var expr in inFilter.Expressions) {
            var mappedName = tableInfo.ColumnsMap.SingleOrDefault(x => x.PropertyName == expr.PropertyName)?.ColumnName;
            newFilter.Add(new FilterExpression<T>(mappedName ?? expr.PropertyName, expr.Operator));
        }
        return newFilter;
    }

    public virtual async Task<ReadOnlyCollection<T>> GetAllAsync(Filter<T>? filter = null, object? filterValues = null, int pageSize = 0, int pageNum = 0, string orderBy = "") {
        try {
            var mappedFilter = mapFilter(filter)?.ToString() ?? "";
            var sql = sqlBuilder.GetSelectSql(mappedFilter, pageSize, pageNum);
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

    //public virtual async Task<T?> TryGetByIdAsync(int id) {
    //    var rows = await GetAllAsync("Id=@id", new { id }).ConfigureAwait(false);
    //    return rows.Count != 1 ? null : rows.Single();
    //}

    //public virtual async Task<T> GetOneAsync(string filter, object? values) {
    //    var result = await GetAllAsync(filter, values).ConfigureAwait(false);
    //    if (result.Count != 1) throw new InvalidDataException($"{result.Count} rows returned for {ObjectDumper.Dump(filter)}");
    //    return result.Single();
    //}

    //public virtual async Task<T?> TryGetOneAsync(string filter, object? values) {
    //    var rows = await GetAllAsync(filter, values).ConfigureAwait(false);
    //    return rows.Count != 1 ? null : rows.Single();
    //}

    //public virtual async Task<int> GetCountAsync() {
    //    using var conn = dbConnectionService.CreateConnection();
    //    return await conn.ExecuteScalarAsync<int>(sqlBuilder.GetCountSql());
    //}

    public virtual  async Task<int> GetCountAsync(Filter<T>? filter = null, object? args = null) {
        var mappedFilter = mapFilter(filter)?.ToString() ?? "";
        using var conn = dbConnectionService.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sqlBuilder.GetCountSql(mappedFilter), args );
    }
}