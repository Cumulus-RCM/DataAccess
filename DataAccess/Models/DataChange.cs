using System;
using System.Collections;
using DataAccess.Interfaces;

namespace DataAccess;

public class DataChange<T> : IDataChange {
    public object Entity { get; init; }
    public DataChangeKind DataChangeKind { get; init; }
    public bool IsCollection { get; init; }
    public Type EntityType { get; } = typeof(T);

    public DataChange(DataChangeKind dataChangeKind, object entity) {
        DataChangeKind = dataChangeKind;
        Entity = entity;
        IsCollection = entity is IEnumerable;
    }
 }