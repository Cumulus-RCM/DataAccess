using System;
using System.Reflection;
using DataAccess.Shared;

namespace DataAccess;

public class ColumnInfo : IColumnInfo {
    public string ColumnName { get; init; }
    public string PropertyName { get; init; }
    public bool IsSkipByDefault { get; init; }
    public bool CanWrite { get; init; }
    public bool IsPrimaryKey { get; init; }
    public Type Type { get; init; }
    public string Alias => PropertyName.Equals(ColumnName, StringComparison.InvariantCultureIgnoreCase) ? string.Empty : $" {PropertyName}";

    public ColumnInfo(string columnName, string? propertyName = null, bool? isIsSkipByDefault = false, bool? canWrite = true, bool? isPrimaryKey = false, Type? type = null) {
        ColumnName = columnName;
        PropertyName = propertyName ?? ColumnName;
        IsSkipByDefault = isIsSkipByDefault ?? false;
        CanWrite = canWrite ?? true;
        IsPrimaryKey = isPrimaryKey ?? false;
        Type = type ?? typeof(string);
    }

    public static ColumnInfo? FromAttribute(PropertyInfo property) {
        var attribute = property.GetCustomAttribute<ColumnInfoAttribute>();
        if (attribute is null) return null;

        return new ColumnInfo(
            columnName: attribute.ColumnName ?? property.Name,
            propertyName: property.Name,
            isIsSkipByDefault: attribute.IsSkipByDefault,
            canWrite: attribute.CanWrite,
            isPrimaryKey: attribute.IsPrimaryKey,
            type: attribute.Type
        );
    }

    public static ColumnInfo Combine(ColumnInfo? first, ColumnInfo? second) {
        if (first is null && second is null) throw new InvalidOperationException("Both arguments are null.");
        if (first is null) return second!;
        if (second is null) return first;
        return new ColumnInfo(
            columnName: first.ColumnName,
            propertyName: first.PropertyName,
            isIsSkipByDefault: first.IsSkipByDefault || second.IsSkipByDefault,
            canWrite: first.CanWrite && second.CanWrite,
            isPrimaryKey: first.IsPrimaryKey || second.IsPrimaryKey,
            type: first.Type
        );
    }
}