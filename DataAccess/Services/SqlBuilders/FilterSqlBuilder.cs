using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using BaseLib;
using Dapper;
using DataAccess.Shared;

namespace DataAccess.Services.SqlBuilders;

public static class FilterSqlBuilder {
    public static (string whereClause, DynamicParameters dynamicParameters) ToSqlClause(this Filter filter, IReadOnlyCollection<IColumnInfo>? columnsMap) {
        var dynamicParameters = getDynamicParameters();
        if (filter.Segments.Count == 0) return ("", dynamicParameters);
        var sql = segmentToSql(filter.Segments.First(), 0);
        if (sql == "") return ("", dynamicParameters);
        var where = $"WHERE {sql} ";
        if (filter.Segments.Count == 1) return (where, dynamicParameters);
        var sb = new StringBuilder(where);
        var segIndex = 1;
        foreach (var segment in filter.Segments.Skip(1)) {
            var sqlSegment = segmentToSql(segment, segIndex++);
            if (sqlSegment != "") {
                sb.Append($" {segment.AndOr.DisplayName} ");
                sb.Append(sqlSegment);
            }
        }

        return ($"{sb}", dynamicParameters);

        string segmentToSql(FilterSegment filterSegment, int segmentIndex) {
            if (filterSegment.FilterExpressions.Count == 0) return "";
            string result;
            var expressions = filterSegment.FilterExpressions.Values;
            var firstExpression = expressionToSql(expressions.First().FilterExpression, segmentIndex, 0);
            if (expressions.Count == 1)
                result = firstExpression;
            else {
                var segmentStringBuilder = new StringBuilder(firstExpression);
                var expressionIndex = 0;
                foreach (var expression in expressions.Skip(1)) {
                    var expressionSql = expressionToSql(expression.FilterExpression, segmentIndex, expressionIndex++);
                    if (expressionSql != "") {
                        segmentStringBuilder.Append($" {expression.AndOr.DisplayName} ");
                        segmentStringBuilder.Append(expressionSql);
                    }
                }

                result = segmentStringBuilder.ToString();
            }

            return $"({result})";
        }

        string expressionToSql(FilterExpression fe, int segmentIndex, int expressionIndex) {
            if (fe.PropertyName == "") return "";
            var columnName = $"{fe.Alias}{getMappedPropertyName(fe.PropertyName)}";
            var (pre, post) = stringifyTemplates();
            var value = fe.Operator.UsesValue ? $" {pre}@{fe.Name}{segmentIndex}{expressionIndex}{post}" : "";
            return $" {columnName} {fe.Operator.SqlOperator}{value} ";

            (string pre, string post) stringifyTemplates() {
                var before = string.IsNullOrWhiteSpace(fe.Operator.PreTemplate) ? "" : $"'{fe.Operator.PreTemplate}' + ";
                var after = string.IsNullOrWhiteSpace(fe.Operator.PostTemplate) ? "" : $" + '{fe.Operator.PostTemplate}'";
                return (before, after);
            }

            string getMappedPropertyName(string propertyName) =>
                columnsMap is null
                    ? propertyName
                    : columnsMap.SingleOrDefault(x => x.PropertyName == propertyName)?.ColumnName ?? propertyName;
        }

        DynamicParameters getDynamicParameters() {
            var parameters = new DynamicParameters();
            for (var segmentIndex = 0; segmentIndex < filter.Segments.Count; segmentIndex++) {
                var segment = filter.Segments[segmentIndex];
                var expressionIndex = 0;
                foreach (var expr in segment.FilterExpressions.Where(f => f.Value.FilterExpression.Operator.UsesValue)) {
                    var dbType = TypeHelper.GetDbType(expr.Value.FilterExpression.ValueTypeName);
                    parameters.Add($"{expr.Key}{segmentIndex}{expressionIndex++}", expr.Value.FilterExpression.Value, dbType);
                }
            }

            return parameters;


        }
    }
}