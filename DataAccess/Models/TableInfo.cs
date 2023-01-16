using System.Reflection;

namespace DataAccess.Models;

public sealed class TableInfo<T> : ITableInfo {
    public string TableName { get; }
    public string PrimaryKeyName { get; }
    public string SequenceName { get; }
    public bool IsIdentity { get; }
    public IReadOnlyCollection<ColumnInfo> ColumnsMap { get; }
    public Type EntityType { get; } = typeof(T);

    private readonly MethodInfo? pkSetter;
    private readonly MethodInfo? pkGetter;

    public TableInfo() : this(tableName:null) { }

    public TableInfo(string? tableName = null, string? primaryKeyName = null, string? sequence = null, bool isIdentity = false, IEnumerable<ColumnInfo>? mappedColumnsInfos = null) {
        TableName = tableName ?? EntityType.Name;
        SequenceName = sequence ?? $"{TableName}_id_seq";
        PrimaryKeyName = primaryKeyName ?? "Id";
        IsIdentity = isIdentity;

        var properties = typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        var pkPropertyInfo = properties.SingleOrDefault(pi => pi.Name.Equals(PrimaryKeyName, StringComparison.InvariantCultureIgnoreCase)) ?? throw new InvalidDataException($"No PrimaryKeyName Defined for {TableName}.");
        pkSetter = pkPropertyInfo.GetSetMethod(true);
        pkGetter = pkPropertyInfo.GetGetMethod(true);

        var mappedColumns = (mappedColumnsInfos ?? Enumerable.Empty<ColumnInfo>()).ToList();


        ColumnsMap = properties
            .GroupJoin(mappedColumns.ToList(), prop => prop.Name, mappedColumn => mappedColumn.PropertyName,
                (propInfo, columnInfo) => new { prop = propInfo, columnInfos = columnInfo}, StringComparer.InvariantCultureIgnoreCase)
            .Select(t => new {propertyInfo = t.prop, mappedColumnInfo = t.columnInfos.SingleOrDefault()})
            .Select(x => new ColumnInfo( x.mappedColumnInfo?.ColumnName ?? x.propertyInfo.Name, x.propertyInfo.Name,
                x.mappedColumnInfo?.IsSkipByDefault, x.propertyInfo.CanWrite,
                x.propertyInfo.Name.Equals(PrimaryKeyName, StringComparison.InvariantCultureIgnoreCase)))
            .ToList();
    }

    public void SetPrimaryKeyValue(object entity,int value) => pkSetter?.Invoke(entity, new object[] { value });
    public object GetPrimaryKeyValue(object entity) => pkGetter?.Invoke(entity, null) ?? throw new InvalidDataException("PrimaryKeyName value is null");

}