using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Dapper;
using DataAccess.Shared;
using Serilog;

namespace DataAccess.Services;

public class SqlBuilder {
    public static string OrderByToSqlClause(OrderBy orderBy, ITableInfo? tableInfo = null) {
        var cols = string.Join(",", orderBy.OrderByExpressions.Select(expr => $"{getMappedColumnName(expr)} {expr.OrderDirection.DisplayName}"));
        return readifyOrderByClause(cols);

        string getMappedColumnName(OrderByExpression orderByExpression) => tableInfo is null 
          ? orderByExpression.PropertyName
          : tableInfo.ColumnsMap.Single(x => x.PropertyName == orderByExpression.PropertyName).ColumnName;
            
        string readifyOrderByClause(string? rawOrderByClause) => readifyClause(rawOrderByClause, "ORDER BY");
    }

    public static string GetCountSql(string baseSql,Filter? filter) {
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

public class TableSqlBuilder(ITableInfo tableInfo) : SqlBuilder {
    private const string PREFIX_PARAMETER_NAME = "@";
    
    public static string GetWriteSql(IDataChange dataChange) {
        var sqlBuilder = new TableSqlBuilder(dataChange.TableInfo);
        return dataChange.DataChangeKind.Value switch {
            DataChangeKind.UPDATE => sqlBuilder.getUpdateSql(),
            DataChangeKind.INSERT => sqlBuilder.getInsertSql(dataChange.SqlShouldGenPk, dataChange.SqlShouldReturnPk),
            DataChangeKind.DELETE => sqlBuilder.getDeleteSql(),
            DataChangeKind.STORED_PROCEDURE => sqlBuilder.getStoredProcedureSql(),
            _ => throw new InvalidEnumArgumentException(nameof(IDataChange.DataChangeKind))
        };
    }

    public (string sql, DynamicParameters? dynamicParameters) GetReadSql(Filter? filter = null, int pageSize = 0, int pageNum = 1, OrderBy? orderBy = null, IReadOnlyCollection<string>? columnNames = null) {
        if (pageNum <= 0) pageNum = 1;
        if (columnNames is not null) {
            var errColumnNames = columnNames.Where(colName => tableInfo.ColumnsMap.All(mappedCol => !colName.Equals(mappedCol.PropertyName, StringComparison.OrdinalIgnoreCase)));
            foreach (var columnName in errColumnNames) Log.Warning("Column:{columnName} specified in dynamic columnNames not found in Table:{TableName}", columnName, tableInfo.TableName);
        }

        var f = filter?.ToSqlClause(tableInfo.ColumnsMap);
        var orderByClause = OrderByToSqlClause(orderBy ?? new OrderBy(tableInfo.PrimaryKeyName), tableInfo);
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

        var result = $"{selectClause} {f?.whereClause} {orderByClause} {offsetFetchClause}";
        return (result.Trim(), f?.dynamicParameters);
    }

    public string GetCountSql(Filter? filter = null) {
        var f = filter?.ToSqlClause(tableInfo.ColumnsMap);
        return $"SELECT COUNT(*) FROM {tableInfo.TableName} {f?.whereClause ?? ""}";
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

    public bool IsString(string propertyName) => tableInfo.EntityType.GetProperty(propertyName)?.PropertyType == typeof(string);
}