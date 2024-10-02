using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BaseLib;
using Dapper;
using DataAccess.Shared;
using Serilog;

namespace DataAccess.Services.SqlBuilders;

public class TableSqlBuilder(ITableInfo tableInfo) : SqlBuilder {
    public string GetWriteSql(IDataChange dataChange) {
        if (tableInfo.IsReadOnly) throw new InvalidOperationException($"Table:{tableInfo.TableName} is read-only.");
        return dataChange.DataChangeKind.Value switch {
            DataChangeKind.UPDATE => getUpdateSql(),
            DataChangeKind.INSERT => getInsertSql(dataChange.SqlShouldGenPk, dataChange.SqlShouldReturnPk),
            DataChangeKind.DELETE => getDeleteSql(),
            DataChangeKind.STORED_PROCEDURE => getStoredProcedureSql(),
            _ => throw new InvalidEnumArgumentException(nameof(IDataChange.DataChangeKind))
        };
    }

    public (string sql, DynamicParameters? dynamicParameters) GetReadSql(Filter? filter = null, int pageSize = 0, int pageNum = 1,
        OrderBy? orderBy = null, IReadOnlyCollection<string>? columnNames = null) {
        if (pageNum <= 0) pageNum = 1;
        if (columnNames is not null) {
            var errColumnNames = columnNames.Where(colName =>
                tableInfo.ColumnsMap.All(mappedCol => !colName.Equals(mappedCol.PropertyName, StringComparison.OrdinalIgnoreCase)));
            foreach (var columnName in errColumnNames)
                Log.Warning("Column:{columnName} specified in dynamic columnNames not found in Table:{TableName}", columnName, tableInfo.TableName);
        }

        var f = getFilterClause(filter);
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
        if (tableInfo.CustomCountSqlTemplate.IsNotNullOrEmpty()) return tableInfo.CustomCountSqlTemplate;
        var f = getFilterClause(filter);
        return $"SELECT COUNT(*) FROM {tableInfo.TableName} {f?.whereClause ?? ""}";
    }

    private (string whereClause, DynamicParameters dynamicParameters)? getFilterClause(Filter? filter) {
        const string SOFT_DELETE_SEGMENT_NAME = "SoftDelete";
        if (tableInfo.IsSoftDelete) {
            if (filter is null) filter = Filter.Create(new FilterSegment(new FilterExpression("IsDeleted", Operator.Equal) {Value = false}) {Name = SOFT_DELETE_SEGMENT_NAME});
            else if (filter.Segments.All(s => s.Name != SOFT_DELETE_SEGMENT_NAME))
                filter.AddSegment(new FilterSegment(new FilterExpression("IsDeleted", Operator.Equal) {Value = false}) {Name = SOFT_DELETE_SEGMENT_NAME});
        }
        return filter?.ToSqlClause(tableInfo.ColumnsMap) ?? ("", new DynamicParameters());
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

        var columnNames = string.Join(", ",
            tableInfo.ColumnsMap.Where(p => p is {CanWrite: true, IsPrimaryKey: false}).Select(x => x.ColumnName));
        var parameterNames = string.Join(", ",
            tableInfo.ColumnsMap.Where(p => p is {CanWrite: true, IsPrimaryKey: false}).Select(x => $"@{x.PropertyName}"));
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
        return string.Format(
            $"UPDATE {tableInfo.TableName} SET {string.Join(",", setStatements)} WHERE {tableInfo.PrimaryKeyName}={PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName}");
    }

    private string getDeleteSql() => tableInfo.CustomDeleteSqlTemplate ??
                                     (tableInfo.IsSoftDelete
                                         ? $"UPDATE {tableInfo.TableName} SET IsDeleted = 1 WHERE {tableInfo.PrimaryKeyName} IN ({PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName}) "
                                         : $"DELETE FROM {tableInfo.TableName} WHERE {tableInfo.PrimaryKeyName} IN ({PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName})"
                                     );

    private string getStoredProcedureSql() =>
        tableInfo.CustomStoredProcedureSqlTemplate ??
        throw new InvalidDataException($"No stored procedure template defined for Table:{tableInfo.TableName}");

    public bool IsString(string propertyName) => tableInfo.EntityType.GetProperty(propertyName)?.PropertyType == typeof(string);
}