using DataAccess.Interfaces;
using DataAccess.Shared;

namespace DataAccess;

public class SqlBuilder {
    private const string PREFIX_PARAMETER_NAME = "@";
    private readonly ITableInfo tableInfo;

    public SqlBuilder(ITableInfo tableInfo) {
        this.tableInfo = tableInfo;
    }

    public string GetSelectSql(Filter? filter = null, int pageSize = 0, int pageNum = 1, OrderBy? orderBy = null ) {
        if (pageNum <= 0) pageNum = 1;
        var whereClause = generateWhereClause(filter);
        var orderByClause = generateOrderByClause(orderBy ?? new OrderBy(tableInfo.PrimaryKeyName));
        var offsetFetchClause = pageSize > 0
            ? $"OFFSET {pageSize * (pageNum - 1)} ROWS FETCH NEXT {pageSize} ROW ONLY"
            : "";
        var columns = string.Join(",", tableInfo.ColumnsMap.Where(c => !c.IsSkipByDefault).Select(c => $"{c.ColumnName} {c.Alias}"));
        var result = $"SELECT {columns} FROM {tableInfo.TableName} {whereClause} {orderByClause} {offsetFetchClause}";
        return result.Trim();
    }


    private string generateOrderByClause(OrderBy orderBy) {
        var cols = string.Join(",", orderBy.OrderByExpressions.Select(expr => $"{getMappedColumnName(expr)} {expr.OrderDirection.DisplayName}" ));
        return readifyOrderByClause(cols);

        string getMappedColumnName(OrderByExpression orderByExpression) => tableInfo.ColumnsMap.Single(x=>x.PropertyName == orderByExpression.PropertyName ).ColumnName;
    }

    private static string readifyOrderByClause(string? rawOrderByClause) => readifyClause(rawOrderByClause, "ORDER BY");

    private static string readifyClause(string? rawClause, string op) {
        if (string.IsNullOrWhiteSpace(rawClause)) return "";
        var result = rawClause.Trim();
        return result.StartsWith(op, StringComparison.OrdinalIgnoreCase)
            ? $" {result}"
            : $" {op} {result}";
    }

    public string GetCountSql(Filter? filter = null) {
        var whereClause = generateWhereClause(filter);
        return $"SELECT COUNT(*) FROM {tableInfo.TableName} {whereClause}";
    }

    public string GetNextSequenceStatement() => !tableInfo.IsIdentity
        ? $"NEXT VALUE FOR {tableInfo.SequenceName}"
        : throw new InvalidOperationException($"No SequenceName for Table:{tableInfo.TableName}, it uses Identity.");

    public string GetInsertSql(bool shouldSetPk, bool shouldReturnNewId) {
        if (tableInfo.IsIdentity && shouldSetPk) throw new InvalidOperationException("Can not set PK on Identity.");
        var returnNewId = "";
        var getNewIdFromSequence = "";
        var pkValue = "";
        var pkName = "";
        if (shouldSetPk && !tableInfo.IsIdentity) {
            pkValue = "@newID, ";
            pkName = $"{tableInfo.PrimaryKeyName},";
            getNewIdFromSequence = $"DECLARE @newID INT;SELECT @newID = {GetNextSequenceStatement()}";
        }

        if (shouldReturnNewId)
            returnNewId = !tableInfo.IsIdentity ? "SELECT @newId as newID" : "SELECT SCOPE_IDENTITY() as newID";

        var columnNames = string.Join(", ", tableInfo.ColumnsMap.Where(p => p is {CanWrite: true, IsPrimaryKey: false}).Select(x => x.ColumnName));
        var parameterNames = string.Join(", ", tableInfo.ColumnsMap.Where(p => p is {CanWrite: true, IsPrimaryKey: false}).Select(x => $"@{x.PropertyName}"));
        return @$"
{getNewIdFromSequence};
INSERT INTO {tableInfo.TableName} ({pkName}{columnNames}) VALUES ({pkValue}{parameterNames});
{returnNewId};";
    }

    public string GetUpdateSql(IEnumerable<string>? changedPropertyNames = null) {
        var changedPropertiesList = changedPropertyNames is null ? new List<string>() : changedPropertyNames.ToList();
        var setStatements = tableInfo.ColumnsMap
            .Where(property => changedPropertyNames is null ||
                               changedPropertiesList.Contains(property.PropertyName, StringComparer.OrdinalIgnoreCase))
            .Where(property => property is {IsPrimaryKey: false, IsSkipByDefault: false})
            .Select(col => $"[{col.ColumnName}] = {PREFIX_PARAMETER_NAME}{col.PropertyName}");
        return string.Format($"UPDATE {tableInfo.TableName} SET {string.Join(",", setStatements)} WHERE {tableInfo.PrimaryKeyName}={PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName}");
    }

    public string GetDeleteSql() => tableInfo.CustomDeleteSqlTemplate ?? $"DELETE FROM {tableInfo.TableName} WHERE {tableInfo.PrimaryKeyName} IN ({PREFIX_PARAMETER_NAME}{tableInfo.PrimaryKeyName})";

    private string generateWhereClause(Filter? filter) {
        if (filter == null) return "";
        //var x = filter.Segments.SelectMany(segment => segment.Expressions.Select(exp => toSql(exp.FilterExpression)));
        var where =  "WHERE " + string.Join(AndOr.And.DisplayName, 
            filter.Segments.SelectMany(segment => segment.Expressions.Select(exp => toSql(exp.FilterExpression))));
        return where;
    }

    bool isString(string propertyName) => tableInfo.EntityType.GetProperty(propertyName)?.PropertyType == typeof(string);

    string toSql(FilterExpression fe) {
        var columnName = getMappedPropertyName(fe.PropertyName);
        var (pre, post) = stringifyTemplates();
        return $" {columnName} {fe.Operator.DisplayName} {pre}@{fe.PropertyName}{post} ";

        (string pre, string post) stringifyTemplates() {
            if (!isString(fe.PropertyName)) return ("", "");
            var before = string.IsNullOrWhiteSpace(fe.Operator.PreTemplate) ? "" : $"'{fe.Operator.PreTemplate}' + ";
            var after = string.IsNullOrWhiteSpace(fe.Operator.PostTemplate) ? "" : $" + '{fe.Operator.PostTemplate}'";
            return (before, after);
        }

        string getMappedPropertyName(string propertyName) =>
            tableInfo.ColumnsMap.SingleOrDefault(x => x.PropertyName == propertyName)?.ColumnName ?? propertyName;
    }
}
