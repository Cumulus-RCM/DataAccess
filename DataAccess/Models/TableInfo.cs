using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using DataAccess.Shared;

namespace DataAccess;

public sealed record TableInfo<T> : ITableInfo {
    public string TableName { get; private set; }
    public string PrimaryKeyName { get; private init; } = "Id";
    public Type PrimaryKeyType { get; }
    public string SequenceName { get; private init; } = "";
    public bool IsIdentity { get; private init; }
    public bool IsSequencePk { get; }
    public int Priority { get; init; } = 1000;
    public bool IsSoftDelete { get; }
    public bool IsReadOnly { get; }

    public IReadOnlyCollection<IColumnInfo> ColumnsMap { get; }

    public Type EntityType { get; } = typeof(T);

    public string? CustomSelectSqlTemplate { get; init; }
    public string? CustomDeleteSqlTemplate { get; init; }
    public string? CustomInsertSqlTemplate { get; init; }
    public string? CustomUpdateSqlTemplate { get; init; }
    public string? CustomStoredProcedureSqlTemplate { get; init; }
    public ParameterValues? CustomSelectParameters { get; init; } 

    private readonly MethodInfo? pkSetter;
    private readonly MethodInfo? pkGetter;

    public TableInfo() : this(tableName: null) { }

    public TableInfo(string? tableName = null, string? sequence = null, bool isIdentity = false, IEnumerable<ColumnInfo>? mappedColumnsInfos = null) {
        var tableInfoAttribute = typeof(T).GetCustomAttribute<TableInfoAttribute>();
        TableName = tableName ?? tableInfoAttribute?.TableName ?? EntityType.Name;
        IsIdentity = tableInfoAttribute?.IsIdentity ?? isIdentity;

        var properties = typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public )
            .Where(p => p.CanWrite).ToArray();
        IsSoftDelete = properties.Any(p => p.Name == "IsDeleted" && p.PropertyType == typeof(bool));
        var mappedColumns = getColumnInfos(properties, mappedColumnsInfos).ToList();
        if (mappedColumns.Any(c => c.IsPrimaryKey)) PrimaryKeyName = mappedColumns.Single(c => c.IsPrimaryKey).ColumnName;

        if (tableInfoAttribute?.PrimaryKeyName is not null) PrimaryKeyName = tableInfoAttribute.PrimaryKeyName;
        var pkPropertyInfo = properties.SingleOrDefault(pi => pi.Name.Equals(PrimaryKeyName, StringComparison.InvariantCultureIgnoreCase));
        IsReadOnly = pkPropertyInfo is null;
        if (pkPropertyInfo is not null) {
            if (tableInfoAttribute?.IsIdentity is not null) IsIdentity = tableInfoAttribute.IsIdentity;
            IsSequencePk = !isIdentity && (pkPropertyInfo.PropertyType == typeof(int) || pkPropertyInfo.PropertyType == typeof(IdPk));
            if (tableInfoAttribute?.SequenceName is not null) sequence = tableInfoAttribute.SequenceName;
            SequenceName = IsSequencePk ? sequence ?? $"{TableName}_id_seq" : "";

            PrimaryKeyType = pkPropertyInfo.PropertyType;
            pkSetter = pkPropertyInfo.GetSetMethod(true);
            pkGetter = pkPropertyInfo.GetGetMethod(true);
        }

        ColumnsMap = properties
            .GroupJoin(mappedColumns.ToList(), prop => prop.Name, mappedColumn => mappedColumn.PropertyName,
                (propInfo, columnInfo) => new { prop = propInfo, columnInfos = columnInfo }, StringComparer.InvariantCultureIgnoreCase)
            .Select(t => new { propertyInfo = t.prop, mappedColumnInfo = t.columnInfos.SingleOrDefault() })
            .Select(x => new ColumnInfo(x.mappedColumnInfo?.ColumnName ?? x.propertyInfo.Name, x.propertyInfo.Name,
                x.mappedColumnInfo?.IsSkipByDefault, x.mappedColumnInfo?.CanWrite ?? x.propertyInfo.CanWrite,
                x.propertyInfo.Name.Equals(PrimaryKeyName, StringComparison.InvariantCultureIgnoreCase),
                x.propertyInfo.PropertyType)
            ).ToList();
    }

    public void SetPrimaryKeyValue(object entity, IdPk value) => pkSetter?.Invoke(entity, new object[] { value });

    public object GetPrimaryKeyValue(object entity) =>
        pkGetter?.Invoke((T)entity, null) ?? throw new InvalidDataException("PrimaryKeyName value is null");

    private IEnumerable<ColumnInfo> getColumnInfos(PropertyInfo[] properties, IEnumerable<ColumnInfo>? mappedColumnsInfos) {
        var attributedProperties = properties
            .Select(p => ColumnInfo.FromAttribute(p))
            .Where(x => x is not null)
            .Cast<ColumnInfo>()
            .ToArray();

        return attributedProperties
            .Concat(mappedColumnsInfos ?? [])
            .GroupBy(c => c.PropertyName, StringComparer.InvariantCultureIgnoreCase)
            .Select(g => g.Aggregate((first, second) => ColumnInfo.Combine(first, second)));
    }
}