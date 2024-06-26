using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Shared;
using Serilog;

namespace DataAccess;

public class Reader<T> : IReader<T> where T : class {
    protected readonly IDbConnectionManager dbConnectionService;
    protected readonly SqlBuilder sqlBuilder;
    private readonly ITableInfo tableInfo;

    public Reader(IDbConnectionManager dbConnectionService, IDatabaseMapper databaseMapper) {
        this.dbConnectionService = dbConnectionService;
        tableInfo = databaseMapper.GetTableInfo<T>();
        sqlBuilder = new SqlBuilder(tableInfo);
    }

    public async Task<ReadOnlyCollection<dynamic>> GetAllDynamicAsync(IReadOnlyCollection<string> columns, Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null) {
        var sql = "";
        DynamicParameters? parms = null;

        try {
            sql = sqlBuilder.GetReadSql(filter, pageSize, pageNum, orderBy, columns);
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

    public virtual async Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null) {
        var sql = "";
        DynamicParameters? parms = null;
        try {
            sql = sqlBuilder.GetReadSql(filter, pageSize, pageNum, orderBy);
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

    private string paramsToString(DynamicParameters? parms) {
        if (parms == null) return "";
        var result = string.Join(",", parms.ParameterNames.Select(p => $"{p}={parms.Get<object>(p)}"));
        return result;
    }

    public virtual async Task<T?> GetByPkAsync(string pkValue) {
        var filter = Filter.Create(new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.Equal) {Value = pkValue});
        var result = await GetAllAsync(filter, pageSize: 1, pageNum: 1).ConfigureAwait(false);
        return result.FirstOrDefault();
    }

    public virtual async Task<dynamic?> GetByPkDynamicAsync(string pkValue, IReadOnlyCollection<string> columnNames) {
        var filter = Filter.Create(new FilterExpression<T>(tableInfo.PrimaryKeyName, Operator.Equal) {Value = pkValue});
        var result = await GetAllDynamicAsync(columnNames, filter, pageSize: 1, pageNum: 1).ConfigureAwait(false);
        return result.FirstOrDefault();
    }

    public virtual async Task<int> GetCountAsync(Filter? filter = null) {
        try {
            using var conn = dbConnectionService.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sqlBuilder.GetCountSql(filter), filter?.GetDynamicParameters());
        }
        catch (Exception e) {
            Log.Error(e, "Error in GetCountAsync");
            return 0;
        }

    }
}