using System.Collections.Generic;
using Dapper;
using DataAccess.Shared;

namespace DataAccess.Services.SqlBuilders;

public static class TableSqlBuilderExtensions {
    public static string GetWriteSql(this ITableInfo tableInfo, IDataChange dataChange) => new TableSqlBuilder(tableInfo).GetWriteSql(dataChange);

    public static (string sql, DynamicParameters? dynamicParameters) GetReadSql(this ITableInfo tableInfo, Filter? filter = null, 
        int pageSize = 0, int pageNum = 1,
        OrderBy? orderBy = null, IReadOnlyCollection<string>? columnNames = null) =>
        new TableSqlBuilder(tableInfo).GetReadSql(filter, pageSize, pageNum, orderBy, columnNames);

    public static string GetCountSql(this ITableInfo tableInfo, Filter? filter = null) => new TableSqlBuilder(tableInfo).GetCountSql(filter);
}