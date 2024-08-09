using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Services;
using DataAccess.Shared;
using Serilog;

namespace DataAccess;

public class Reader {
    private readonly IDbConnectionManager dbConnectionService;
    private readonly string baseSql;

    public Reader(IDbConnectionManager dbConnectionService, string baseSql) {
        this.dbConnectionService = dbConnectionService;
        this.baseSql = baseSql;
    }
    
    public async Task<IReadOnlyCollection<dynamic>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null) {        
        orderBy ??= new OrderBy("1");
        var sql = buildSql();
        var parms = filter?.GetDynamicParameters();
        try {
            using var conn = dbConnectionService.CreateConnection();
            var result = await conn.QueryAsync(sql, parms).ConfigureAwait(false);
            return result.ToArray().AsReadOnly();
        }
        catch (Exception e) {
            Log.Error(e, "Error in GetAllAsync: sql:{0}", [sql, paramsToString(parms)]);
            return Array.Empty<dynamic>().AsReadOnly();
        }

        string buildSql() {
            if (pageNum <= 0) pageNum = 1;

            var whereClause = SqlBuilder.FilterToSqlClause(filter);
            var orderByClause = SqlBuilder.OrderByToSqlClause(orderBy);
            var offsetFetchClause = pageSize > 0
                ? $"OFFSET {pageSize * (pageNum - 1)} ROWS FETCH NEXT {pageSize} ROW ONLY"
                : "";
            var result = $"{baseSql} {whereClause} {orderByClause} {offsetFetchClause}";
            return result.Trim();
        }
    }

    protected async Task<int> getCountAsync(string countSql, Filter? filter = null) {
        try {
            using var conn = dbConnectionService.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(countSql, filter?.GetDynamicParameters()).ConfigureAwait(false);
        }
        catch (Exception e) {
            Log.Error(e, "Error in GetCountAsync");
            return 0;
        }
    }

    public virtual Task<int> GetCountAsync(Filter? filter = null) => getCountAsync(SqlBuilder.GetCountSql(baseSql, filter), filter);

    protected string paramsToString(DynamicParameters? parms) {
        if (parms == null) return "";
        var result = string.Join(",", parms.ParameterNames.Select(p => $"{p}={parms.Get<object>(p)}"));
        return result;
    }
}

public class Reader<T> : Reader, IReader<T>  where T : class {
    protected readonly IDbConnectionManager dbConnectionService;
    protected readonly TableSqlBuilder TableSqlBuilder;
    private readonly ITableInfo tableInfo;

    public Reader(IDbConnectionManager dbConnectionService, IDatabaseMapper databaseMapper) : base(dbConnectionService, "") {
        this.dbConnectionService = dbConnectionService;
        tableInfo = databaseMapper.GetTableInfo<T>();
        TableSqlBuilder = new TableSqlBuilder(tableInfo);
    }

    public async Task<IReadOnlyCollection<dynamic>> GetAllDynamicAsync(IReadOnlyCollection<string> columns, Filter? filter = null,
        int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null) {
        var sql = "";
        DynamicParameters? parms = null;

        try {
            sql = TableSqlBuilder.GetReadSql(filter, pageSize, pageNum, orderBy, columns);
            using var conn = dbConnectionService.CreateConnection();
            parms = filter?.GetDynamicParameters();
            var result = await conn.QueryAsync(sql, parms).ConfigureAwait(false);
            return result.ToList().AsReadOnly();
        }
        catch (Exception e) {
            Log.Error(e, "Error in GetAllAsync: sql:{0}", [sql, paramsToString(parms)]);
            return Array.Empty<dynamic>().AsReadOnly();
        }
    }

    public override Task<int> GetCountAsync(Filter? filter = null) => getCountAsync(TableSqlBuilder.GetCountSql(filter), filter);

    public new virtual async Task<IReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null) {
        var sql = "";
        DynamicParameters? parms = null;
        try {
            sql = TableSqlBuilder.GetReadSql(filter, pageSize, pageNum, orderBy);
            using var conn = dbConnectionService.CreateConnection();
            parms = filter?.GetDynamicParameters();
            var result = await conn.QueryAsync<T>(sql, parms).ConfigureAwait(false);
            return result.ToList().AsReadOnly();
        }
        catch (Exception e) {
            Log.Error(e, "Error in GetAllAsync: sql:{0}", [sql, paramsToString(parms)]);
            return Array.Empty<T>().AsReadOnly();
        }
    }

    public virtual async Task<T?> GetByPkAsync(string pkValue) {
        var filter = Filter.Create(new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.Equal) { Value = pkValue });
        var result = await GetAllAsync(filter, pageSize: 1, pageNum: 1).ConfigureAwait(false);
        return result.FirstOrDefault();
    }

    public virtual Task<IReadOnlyCollection<T>> GetAllByPkAsync(IEnumerable<string> pkValues) {
        var filter = Filter.Create(new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.In) { Value = pkValues });
        return GetAllAsync(filter);
    }

    public virtual async Task<dynamic?> GetByPkDynamicAsync(string pkValue, IReadOnlyCollection<string> columnNames) {
        var filter = Filter.Create(new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.Equal) { Value = pkValue });
        var result = await GetAllDynamicAsync(columnNames, filter, pageSize: 1, pageNum: 1).ConfigureAwait(false);
        return result.FirstOrDefault();
    }
}