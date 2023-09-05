// ReSharper disable once CheckNamespace

using DataAccess.Models;

namespace DataAccess.Interfaces;

public interface ITableInfo {
    Type EntityType { get; }
    string TableName { get; }
    string PrimaryKeyName { get; }
    string SequenceName { get; }
    bool IsIdentity { get; }
    bool IsCompoundPk { get; }
    IReadOnlyCollection<ColumnInfo> ColumnsMap { get; }
    void SetPrimaryKeyValue(object entity, int value);
    object GetPrimaryKeyValue(object entity);
    string? CustomDeleteSqlTemplate { get; }
}