using System;
using System.Collections.Generic;

namespace DataAccess.Shared;

public interface ITableInfo {
    Type EntityType { get; }
    string TableName { get; }
    string PrimaryKeyName { get; }
    Type PrimaryKeyType { get; }
    string SequenceName { get; }
    bool IsIdentity { get; }
    bool IsSequencePk { get;}
    int Priority { get; }
    IReadOnlyCollection<IColumnInfo> ColumnsMap { get; }
    void SetPrimaryKeyValue(object entity, IdPk value);
    object GetPrimaryKeyValue(object entity);
    string? CustomSelectSqlTemplate { get; }
    string? CustomDeleteSqlTemplate { get; }
    string? CustomInsertSqlTemplate { get; }
    string? CustomUpdateSqlTemplate { get; }
    string? CustomStoredProcedureSqlTemplate { get; }
}