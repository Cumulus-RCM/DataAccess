using System;
using System.Linq;
using DataAccess.Shared;

namespace DataAccess.Services.SqlBuilders;

public class SqlBuilder {
    protected const string PREFIX_PARAMETER_NAME = "@";

    public static string OrderByToSqlClause(OrderBy orderBy, ITableInfo? tableInfo = null) {
        var cols = string.Join(",",
            orderBy.OrderByExpressions.Select(expr => $"{getMappedColumnName(expr)} {expr.OrderDirection.DisplayName}"));
        return readifyOrderByClause(cols);

        string getMappedColumnName(OrderByExpression orderByExpression) => tableInfo is null
            ? orderByExpression.PropertyName
            : tableInfo.ColumnsMap.Single(x => x.PropertyName == orderByExpression.PropertyName).ColumnName;

        string readifyOrderByClause(string? rawOrderByClause) => readifyClause(rawOrderByClause, "ORDER BY");
    }

    public static string GetCountSql(string baseSql, Filter? filter) {
        var f = filter?.ToSqlClause(null);
        return $"SELECT COUNT(*) FROM {baseSql} {f?.whereClause}";
    }

    private static string readifyClause(string? rawClause, string op) {
        if (string.IsNullOrWhiteSpace(rawClause)) return "";
        var result = rawClause.Trim();
        return result.StartsWith(op, StringComparison.OrdinalIgnoreCase)
            ? $" {result}"
            : $" {op} {result}";
    }
}