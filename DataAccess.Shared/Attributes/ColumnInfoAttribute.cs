using System;

namespace DataAccess.Shared;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnInfoAttribute : Attribute
{
    public string? ColumnName { get; }
    public bool IsSkipByDefault { get; }
    public bool CanWrite { get; }
    public bool IsPrimaryKey { get; }
    public Type Type { get; }

    public ColumnInfoAttribute(string? columnName = null, bool isSkipByDefault = false, bool canWrite = true, bool isPrimaryKey = false, Type? type = null) {
        ColumnName = columnName;
        IsSkipByDefault = isSkipByDefault;
        CanWrite = canWrite;
        IsPrimaryKey = isPrimaryKey;
        Type = type ?? typeof(string);
    }
}