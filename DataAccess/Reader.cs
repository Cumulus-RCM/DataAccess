using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Services.SqlBuilders;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Reader {
    private readonly IDbConnectionManager dbConnectionService;
    private readonly string baseSql;
    private readonly ILogger<Reader> logger;

    public Reader(IDbConnectionManager dbConnectionService, string baseSql, ILoggerFactory loggerFactory) {
        this.dbConnectionService = dbConnectionService;
        this.baseSql = baseSql;
        logger = loggerFactory.CreateLogger<Reader>();
    }
    
    public async Task<IReadOnlyCollection<dynamic>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 1, OrderBy? orderBy = null) {        
        orderBy ??= new OrderBy("1");
        var f = filter?.ToSqlClause(null);
        var sql = buildSql();
        try {
            using var conn = dbConnectionService.CreateConnection();
            var result = await conn.QueryAsync(sql, f?.dynamicParameters).ConfigureAwait(false);
            return result.ToArray().AsReadOnly();
        }
        catch (Exception e) {
            logger.LogError(e, "Error in GetAllAsync: sql:{0}", [sql, paramsToString(f?.dynamicParameters)]);
            return Array.Empty<dynamic>().AsReadOnly();
        }

        string buildSql() {
            if (pageNum <= 0) pageNum = 1;

            var orderByClause = SqlBuilder.OrderByToSqlClause(orderBy);
            var offsetFetchClause = pageSize > 0
                ? $"OFFSET {pageSize * (pageNum - 1)} ROWS FETCH NEXT {pageSize} ROW ONLY"
                : "";
            var result = $"{baseSql} {f?.whereClause ?? ""} {orderByClause} {offsetFetchClause}";
            return result.Trim();
        }
    }

    public async Task<int> GetCountAsync(Filter? filter = null) {
        string countSql = "";
        try {
            countSql = SqlBuilder.GetCountSql(baseSql, filter);
            var f = filter?.ToSqlClause(null);
            var dynamicParameters = f?.dynamicParameters;
            using var conn = dbConnectionService.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(countSql, dynamicParameters);
        }
        catch (Exception e) {
            logger.LogError(e, "Error in GetCountAsync. SQL:{countSql}", countSql);
            return 0;
        }
    }

    protected string paramsToString(DynamicParameters? parms) {
        if (parms == null) return "";
        var result = string.Join(",", parms.ParameterNames.Select(p => $"{p}={parms.Get<object>(p)}"));
        return result;
    }
}

public class Reader<T> : Reader, IReader<T>  where T : class {
    private ILogger<Reader> logger {  get; }
    protected readonly IDbConnectionManager dbConnectionService;
    protected readonly TableSqlBuilder TableSqlBuilder;
    private readonly ITableInfo tableInfo;

    public Reader(IDbConnectionManager dbConnectionService, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) : base(dbConnectionService, "", loggerFactory) {
        this.dbConnectionService = dbConnectionService;
        tableInfo = databaseMapper.GetTableInfo<T>();
        TableSqlBuilder = new TableSqlBuilder(tableInfo);
        logger = loggerFactory.CreateLogger<Reader>();
    }

    public virtual async Task<IReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null,
        IReadOnlyCollection<string>? columnsNames = null, ParameterValues? parameters = null) {
        var (sql, dynamicParameters) = TableSqlBuilder.GetReadSql(filter, pageSize, pageNum, orderBy, columnsNames);
        setParameterValues(dynamicParameters, parameters);
        try {
            using var conn = dbConnectionService.CreateConnection();
            var result = await conn.QueryAsync<T>(sql, dynamicParameters).ConfigureAwait(false);
            return result.ToList().AsReadOnly();
        }
        catch (Exception e) {
            logger.LogError(e, "Error in GetAllAsync:{0}", [paramsToString(dynamicParameters)]);
            return Array.Empty<T>().AsReadOnly();
        }
    }

    private void setParameterValues(DynamicParameters? dynamicParameters, ParameterValues? parameters) {
        if (parameters is null) return;
        foreach (var pv in parameters.Values) {
            var dbType = TypeHelper.GetDbType(pv.TypeName);
            logger.LogInformation("Parameter: Name:{0} ValueString:{1} TypeName:{2}, Type:{3}, DbType:{4}", pv.Name, pv.ValueString, pv.TypeName, Type.GetType(pv.TypeName), dbType);
            dynamicParameters ??= new DynamicParameters();
            dynamicParameters.Add(pv.Name, pv.GetValue(), dbType);
        }
    }

    public async Task<int> GetCountAsync(Filter? filter = null, ParameterValues? parameterValues = null) {
        var sql = TableSqlBuilder.GetCountSql(filter);
        var f = filter?.ToSqlClause(null);
        setParameterValues(f?.dynamicParameters, parameterValues);
        try {
            using var conn = dbConnectionService.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, f?.dynamicParameters);
        }
        catch (Exception e) {
            logger.LogError(e, "Error in GetCountAsync: sql:{0}", [sql, paramsToString(f?.dynamicParameters)]);
            return 0;
        }
    }

    public virtual async Task<T?> GetByPkAsync(string pkValue, IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameterValues = null) {
        var filter = Filter.Create(new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.Equal) { Value = pkValue });
        var result = await GetAllAsync(filter, pageSize: 1, pageNum: 1, columnsNames:columnNames, parameters:parameterValues).ConfigureAwait(false);
        return result.FirstOrDefault();
    }

    public virtual Task<IReadOnlyCollection<T>> GetAllByPkAsync(IEnumerable<string> pkValues, ParameterValues? parameterValues = null) {
        var filter = Filter.Create(new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.In) { Value = pkValues });
        return GetAllAsync(filter, parameters: parameterValues);
    }
}