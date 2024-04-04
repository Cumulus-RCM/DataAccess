using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using DataAccess.Shared;
using Serilog;

namespace DataAccess;

public class SqlBuilder(ITableInfo tableInfo) {
    private const string PREFIX_PARAMETER_NAME = "@";

    public static string GetWriteSql(IDataChange dataChange) {
        var sqlBuilder = new SqlBuilder(dataChange.TableInfo);
        return dataChange.DataChangeKind.Value switch {
            DataChangeKind.UPDATE => sqlBuilder.getUpdateSql(),
            DataChangeKind.INSERT => sqlBuilder.getInsertSql(dataChange.SqlShouldGenPk, dataChange.SqlShouldReturnPk),
            DataChangeKind.DELETE => sqlBuilder.getDeleteSql(),
            DataChangeKind.STORED_PROCEDURE => sqlBuilder.getStoredProcedureSql(),
            _ => throw new InvalidEnumArgumentException(nameof(IDataChange.DataChangeKind))
        };
    }

    public string GetReadSql(Filter? filter = null, int pageSize = 0, int pageNum = 1, OrderBy? orderBy = null, IReadOnlyCollection<string>? columnNames = null) {
        if (pageNum <= 0) pageNum = 1;
        if (columnNames is not null) {
            var errColumnNames = columnNames.Where(colName => tableInfo.ColumnsMap.All(mappedCol => !colName.Equals(mappedCol.PropertyName, StringComparison.OrdinalIgnoreCase)));
            foreach (var columnName in errColumnNames) Log.Warning("Column:{columnName} specified in dynamic columnNames not found in Table:{TableName}", columnName, tableInfo.TableName);
        }

        var whereClause = GetWhereClause(filter);
        var orderByClause = generateOrderByClause(orderBy ?? new OrderBy(tableInfo.PrimaryKeyName));
        var offsetFetchClause = pageSize > 0
            ? $"OFFSET {pageSize * (pageNum - 1)} ROWS FETCH NEXT {pageSize} ROW ONLY"
            : "";
        string selectClause;
        if (string.IsNullOrWhiteSpace(tableInfo.CustomSelectSqlTemplate)) {
            var cols = columnNames is not null
                ? tableInfo.ColumnsMap.Where(c => columnNames.Contains(c.ColumnName))
                : tableInfo.ColumnsMap.Where(c => !c.IsSkipByDefault);
            var columns = string.Join(",", cols.Select(c => $"{c.ColumnName} {c.Alias}"));
            selectClause = $"SELECT {columns} FROM {tableInfo.TableName}";
        }
        else selectClause = tableInfo.CustomSelectSqlTemplate;

        var result = $"{selectClause} {whereClause} {orderByClause} {offsetFetchClause}";
        return result.Trim();
    }

    public string GetCountSql(Filter? filter = null) {
        var whereClause = GetWhereClause(filter);
        return $"SELECT COUNT(*) FROM {tableInfo.TableName} {whereClause}";
    }

    public string GetWhereClause(Filter? filter) {
        if (filter == null || filter.Segments.Count == 0) return "";
        var where = $"WHERE {segmentToSql(filter.Segments.First(),0)} ";
        if (filter.Segments.Count == 1) return where;
        var sb = new StringBuilder(where);
        var segmentIndex = 1;
        foreach (var segment in filter.Segments.Skip(1)) {
            sb.Append($" {segment.AndOr.DisplayName} ");
            sb.Append(segmentToSql(segment, segmentIndex++));
        }
        return $"{sb}";

        string segmentToSql(FilterSegment filterSegment, int segmentNumber) {
            string result;
            var firstExpression = expressionToSql(filterSegment.Expressions.First().FilterExpression, segmentNumber);
            if (filterSegment.Expressions.Count == 1) 
                result = firstExpression;
            else {
                var sb = new StringBuilder(firstExpression);
                foreach (var expression in filterSegment.Expressions.Skip(1)) {
                    sb.Append($" {expression.AndOr.DisplayName} ");
                    sb.Append(expressionToSql(expression.FilterExpression, segmentNumber));
                }

                result = sb.ToString();
            }
            return $"({result})";
        }
        string expressionToSql(FilterExpression fe, int segmentNumber) {
            var columnName = $"{getMappedPropertyName(fe.PropertyName)}";
            var (pre, post) = stringifyTemplates();
            var value = fe.Operator.UsesValue ? $" {pre}@{fe.PropertyName}{segmentNumber}{post}" : "";
            return $" {columnName} {fe.Operator.DisplayName}{value} ";

            (string pre, string post) stringifyTemplates() {
                //if (!isString(fe.PropertyName)) return ("", "");
                var before = string.IsNullOrWhiteSpace(fe.Operator.PreTemplate) ? "" : $"{fe.Operator.PreTemplate}";
                var after = string.IsNullOrWhiteSpace(fe.Operator.PostTemplate) ? "" : $"{fe.Operator.PostTemplate}";
                return (before, after);
            }

            string getMappedPropertyName(string propertyName) =>
                tableInfo.ColumnsMap.SingleOrDefault(x => x.PropertyName == propertyName)?.ColumnName ?? propertyName;
        }
    }

    private string generateOrderByClause(OrderBy orderBy) {
        var cols = string.Join(",", orderBy.OrderByExpressions.Select(expr => $"{getMappedColumnName(expr)} {expr.OrderDirection.DisplayName}"));
        return readifyOrderByClause(cols);

        string getMappedColumnName(OrderByExpression orderByExpression) => tableInfo.ColumnsMap.Single(x => x.PropertyName == orderByExpression.PropertyName).ColumnName;
    }

    private static string readifyOrderByClause(string? rawOrderByClause) => readifyClause(rawOrderByClause, "ORDER BY");

    private static string readifyClause(string? rawClause, string op) {
        if (string.IsNullOrWhiteSpace(rawClause)) return "";
        var result = rawClause.Trim();
        return result.StartsWith(op, StringComparison.OrdinalIgnoreCase)
            ? $" {result}"
            : $" {op} {result}";
    }

    private string getNextSequenceStatement() => !tableInfo.IsIdentity
        ? $"NEXT VALUE FOR {tableInfo.SequenceName}"
        : throw new InvalidOperationException($"No SequenceName for Table:{tableInfo.TableName}, it uses Identity.");

    private string getInsertSql(bool shouldGenerateNextSeqValue, bool shouldReturnNewId) {
        if (tableInfo.IsIdentity && shouldGenerateNextSeqValue) throw new InvalidOperationException("Can not set PK on Identity.");
        var returnNewId = "";
        var getNewIdFromSequence = "";
        var pkValue = "";
        var pkName = "";
        if (tableInfo.IsSequencePk) {
            pkName = $"{tableInfo.PrimaryKeyName},";
            pkValue = $"{PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName},";
            if (shouldGenerateNextSeqValue) {
                pkValue = $"{PREFIX_PARAMETER_NAME}newID, ";
                getNewIdFromSequence = $"DECLARE @newID INT;SELECT @newID = {getNextSequenceStatement()}";
            }
        }

        if (shouldReturnNewId)
            returnNewId = tableInfo.IsIdentity ? "SELECT SCOPE_IDENTITY() as newID" : "SELECT @newId as newID";

        var columnNames = string.Join(", ", tableInfo.ColumnsMap.Where(p => p is {CanWrite: true, IsPrimaryKey: false}).Select(x => x.ColumnName));
        var parameterNames = string.Join(", ", tableInfo.ColumnsMap.Where(p => p is {CanWrite: true, IsPrimaryKey: false}).Select(x => $"@{x.PropertyName}"));
        return @$"
{getNewIdFromSequence};
INSERT INTO {tableInfo.TableName} ({pkName}{columnNames}) VALUES ({pkValue}{parameterNames});
{returnNewId};";
    }

    private string getUpdateSql(IEnumerable<string>? changedPropertyNames = null) {
        var changedPropertiesList = changedPropertyNames is null ? [] : changedPropertyNames.ToList();
        var setStatements = tableInfo.ColumnsMap
            .Where(property => changedPropertyNames is null ||
                               changedPropertiesList.Contains(property.PropertyName, StringComparer.OrdinalIgnoreCase))
            .Where(property => property is {IsPrimaryKey: false, IsSkipByDefault: false})
            .Select(col => $"[{col.ColumnName}] = {PREFIX_PARAMETER_NAME}{col.PropertyName}");
        return string.Format($"UPDATE {tableInfo.TableName} SET {string.Join(",", setStatements)} WHERE {tableInfo.PrimaryKeyName}={PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName}");
    }

    private string getDeleteSql() => tableInfo.CustomDeleteSqlTemplate ?? $"DELETE FROM {tableInfo.TableName} WHERE {tableInfo.PrimaryKeyName} IN ({PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName})";

    private string getStoredProcedureSql() =>
        tableInfo.CustomStoredProcedureSqlTemplate ??
        throw new InvalidDataException($"No stored procedure template defined for Table:{tableInfo.TableName}");

    bool isString(string propertyName) => tableInfo.EntityType.GetProperty(propertyName)?.PropertyType == typeof(string);
}