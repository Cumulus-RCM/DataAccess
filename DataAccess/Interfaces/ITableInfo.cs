using System;
using System.Collections.Generic;

namespace DataAccess.Interfaces;

public interface ITableInfo {
    Type EntityType { get; }
    string TableName { get; }
    string PrimaryKeyName { get; }
    string SequenceName { get; }
    bool IsIdentity { get; }
    IReadOnlyCollection<ColumnInfo> ColumnsMap { get; }
    void SetPrimaryKeyValue(object entity, int value);
    object GetPrimaryKeyValue(object entity);
    string? CustomSelectSqlTemplate { get; }
    string? CustomDeleteSqlTemplate { get; }
    string? CustomInsertSqlTemplate { get; }
    string? CustomUpdateSqlTemplate { get; }

}