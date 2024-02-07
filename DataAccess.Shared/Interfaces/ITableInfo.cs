﻿using System;
using System.Collections.Generic;

namespace DataAccess.Shared;

public interface ITableInfo : ISqlBuilder {
    Type EntityType { get; }
    string TableName { get; }
    string PrimaryKeyName { get; }
    string SequenceName { get; }
    bool IsIdentity { get; }
    bool IsSequencePk { get;}
    bool IsTable { get; }
    IReadOnlyCollection<IColumnInfo> ColumnsMap { get; }
    void SetPrimaryKeyValue(object entity, int value);
    object GetPrimaryKeyValue(object entity);
    string? CustomSelectSqlTemplate { get; }
    string? CustomDeleteSqlTemplate { get; }
    string? CustomInsertSqlTemplate { get; }
    string? CustomUpdateSqlTemplate { get; }
    string? CustomStoredProcedureSqlTemplate { get; }
}