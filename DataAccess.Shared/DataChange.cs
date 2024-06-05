using System;

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
}

public class DataChangeFactory(IDatabaseMapper databaseMapper) {
    public IDataChange Create<T>(DataChangeKind dataChangeKind, T entity, bool isCollection = false) => 
        new DataChange<T>(dataChangeKind, entity, databaseMapper.GetTableInfo<T>()) {IsCollection = isCollection};
}