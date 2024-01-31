using System;
using System.Collections.Generic;

namespace DataAccess.Shared;

public class DataChange<T> : IDataChange {
    public object Entity { get; }
    public DataChangeKind DataChangeKind { get; init; }
    public bool IsCollection { get; init; }
    public Type EntityType { get; } = typeof(T);
    public ITableInfo TableInfo { get; }

    internal DataChange(DataChangeKind dataChangeKind, T entity, ITableInfo tableInfo) {
        Entity = entity ?? throw new ArgumentException("Entity can not be null when creating DataChange.");
        DataChangeKind = dataChangeKind;
        IsCollection = false;
        TableInfo = tableInfo;
    }

    internal DataChange(DataChangeKind dataChangeKind, IEnumerable<T> entities, ITableInfo tableInfo) {
        Entity = entities;
        DataChangeKind = dataChangeKind;
        IsCollection = true;
        TableInfo = tableInfo;
    }
}

public class DataChangeFactory(IDatabaseMapper databaseMapper) {
    public DataChange<T> Create<T>(DataChangeKind dataChangeKind, T entity) => new(dataChangeKind, entity, databaseMapper.GetTableInfo<T>());
    public DataChange<T> Create<T>(DataChangeKind dataChangeKind,IEnumerable<T> entities) => new(dataChangeKind, entities, databaseMapper.GetTableInfo<T>());
}