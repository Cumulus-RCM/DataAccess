using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DataAccess.Shared;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace DataAccess;

public sealed record TableInfo<T> : ITableInfo {
    public string TableName { get; }
    public string PrimaryKeyName { get; }
    public string SequenceName { get; } = "";
    public bool IsIdentity { get; }
    public bool IsSequencePk { get;}
    public bool IsTable { get; init; } = true;
    public IReadOnlyCollection<IColumnInfo> ColumnsMap { get; }

    public Type EntityType { get; } = typeof(T);
    
    public string? CustomSelectSqlTemplate { get; init; }
    public string? CustomDeleteSqlTemplate { get; init; }
    public string? CustomInsertSqlTemplate { get; init; }
    public string? CustomUpdateSqlTemplate { get; init; }
    public string? CustomStoredProcedureSqlTemplate { get; init; }
    
    private readonly MethodInfo? pkSetter;
    private readonly MethodInfo? pkGetter;

    public TableInfo() : this(tableName: null) { }

    public TableInfo(string? tableName = null, string? primaryKeyName = null,  string? sequence = null, bool isIdentity = false,
        IEnumerable<ColumnInfo>? mappedColumnsInfos = null) {
        TableName = tableName ?? EntityType.Name;
        PrimaryKeyName = primaryKeyName ?? "Id";
        IsIdentity = isIdentity;

        var properties = typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        if (IsTable) {
            var pkPropertyInfo = properties.SingleOrDefault(pi => pi.Name.Equals(PrimaryKeyName, StringComparison.InvariantCultureIgnoreCase)) ??
                                 throw new InvalidDataException($"No PrimaryKeyName Defined for {TableName}.");
            IsSequencePk = !isIdentity
                           && (pkPropertyInfo.PropertyType == typeof(int) || pkPropertyInfo.PropertyType == typeof(long));
            SequenceName = IsSequencePk ? sequence ?? $"{TableName}_id_seq" : "";

            pkSetter = pkPropertyInfo.GetSetMethod(true);
            pkGetter = pkPropertyInfo.GetGetMethod(true);
        }

        var mappedColumns = (mappedColumnsInfos ?? Enumerable.Empty<ColumnInfo>()).ToList();

        ColumnsMap = properties
            .GroupJoin(mappedColumns.ToList(), prop => prop.Name, mappedColumn => mappedColumn.PropertyName,
                (propInfo, columnInfo) => new { prop = propInfo, columnInfos = columnInfo }, StringComparer.InvariantCultureIgnoreCase)
            .Select(t => new { propertyInfo = t.prop, mappedColumnInfo = t.columnInfos.SingleOrDefault() })
            .Select(x => new ColumnInfo(x.mappedColumnInfo?.ColumnName ?? x.propertyInfo.Name, x.propertyInfo.Name,
                x.mappedColumnInfo?.IsSkipByDefault, x.propertyInfo.CanWrite,
                x.propertyInfo.Name.Equals(PrimaryKeyName, StringComparison.InvariantCultureIgnoreCase),
                x.propertyInfo.PropertyType)
            ).ToList();
    }

    public void SetPrimaryKeyValue(object entity, IdPk value) => pkSetter?.Invoke(entity, new object[] { value });

    public object GetPrimaryKeyValue(object entity) =>
        pkGetter?.Invoke(entity, null) ?? throw new InvalidDataException("PrimaryKeyName value is null");
}