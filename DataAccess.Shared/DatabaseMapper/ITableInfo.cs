using DataAccess.Shared.DatabaseMapper.Models;

namespace DataAccess.Shared.DatabaseMapper;

public interface ITableInfo
{
    Type EntityType { get; }
    string TableName { get; }
    string PrimaryKeyName { get; }
    string SequenceName { get; }
    bool IsIdentity { get; }
    IReadOnlyCollection<ColumnInfo> ColumnsMap { get; }
    void SetPrimaryKeyValue(object entity, int value);
    object GetPrimaryKeyValue(object entity);
}