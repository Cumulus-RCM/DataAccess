using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using DataAccess.Shared;

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

    public string GetReadSql(Filter? filter = null, int pageSize = 0, int pageNum = 1, OrderBy? orderBy = null) {
        if (pageNum <= 0) pageNum = 1;
        var whereClause = GetWhereClause(filter);
        var orderByClause = generateOrderByClause(orderBy ?? new OrderBy(tableInfo.PrimaryKeyName));
        var offsetFetchClause = pageSize > 0
            ? $"OFFSET {pageSize * (pageNum - 1)} ROWS FETCH NEXT {pageSize} ROW ONLY"
            : "";
        string selectClause;
        if (string.IsNullOrWhiteSpace(tableInfo.CustomSelectSqlTemplate)) {
            var columns = string.Join(",", tableInfo.ColumnsMap.Where(c => !c.IsSkipByDefault).Select(c => $"{c.ColumnName} {c.Alias}"));
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
        if (filter == null) return "";
        var where = "WHERE " + string.Join(AndOr.And.DisplayName,
            filter.Segments.SelectMany(segment => segment.Expressions.Where(exp => exp.FilterExpression.Value is not null).Select(exp => toSql(exp.FilterExpression))));
        return where;
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

    private string toSql(FilterExpression fe) {
        var columnName = getMappedPropertyName(fe.PropertyName);
        var (pre, post) = stringifyTemplates();
        return $" {columnName} {fe.Operator.DisplayName} {pre}@{fe.PropertyName}{post} ";

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