using DataAccess.Shared.DatabaseMapper;

namespace DataAccess;

public class SqlBuilder {
    private const string PREFIX_PARAMETER_NAME = "@";
    private readonly ITableInfo tableInfo;

    public SqlBuilder(ITableInfo tableInfo) {
        this.tableInfo = tableInfo;
    }

    public string GetSelectSql(string where = "", int pageSize = 0, int pageNum = 1, string orderBy = "") {
        var whereClause = readifyWhereClause(where);
        var orderByClause = orderBy == "" && pageSize > 0
            ? readifyOrderByClause(tableInfo.PrimaryKeyName)
            : readifyOrderByClause(orderBy);
        var offsetFetchClause = pageSize > 0
            ? $"OFFSET {pageSize * pageNum} ROWS FETCH NEXT {pageSize} ROW ONLY"
            : "";
        var columns = string.Join(",", tableInfo.ColumnsMap.Where(c => !c.IsSkipByDefault).Select(c => $"{c.ColumnName}{c.Alias}"));
        var result = $"SELECT {columns} FROM {tableInfo.TableName} {whereClause} {orderByClause} {offsetFetchClause}";
        return result.Trim();
    }

    public string GetCountSql(string where = "") {
        var whereClause = readifyWhereClause(where);
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

    public string GetDeleteSql() => $"DELETE FROM {tableInfo.TableName} WHERE {tableInfo.PrimaryKeyName} IN (@{tableInfo.PrimaryKeyName})";

    private static string readifyWhereClause(string? rawWhereClause) => readifyClause(rawWhereClause, "WHERE");
    private static string readifyOrderByClause(string? rawOrderByClause) => readifyClause(rawOrderByClause, "ORDER BY");

    private static string readifyClause(string? rawClause, string op) {
        if (string.IsNullOrWhiteSpace(rawClause)) return "";
        var result = rawClause.Trim();
        return result.StartsWith(op, StringComparison.OrdinalIgnoreCase) 
            ? $" {result}" 
            : $" {op} {result}";
    }
}