using System;

namespace DataAccess;

public interface IColumnInfo {
    string ColumnName { get; init; }
    string PropertyName { get; init; }
    bool IsSkipByDefault { get; init; }
    bool CanWrite { get; init; }
    bool IsPrimaryKey { get; init; }
    Type Type { get; init; }
    string Alias { get; }
}