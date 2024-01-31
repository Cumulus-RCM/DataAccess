using System;
using System.Collections;

namespace DataAccess.Shared;

public class DataChange<T>(DataChangeKind dataChangeKind, object entity) : IDataChange {
    public object Entity { get; init; } = entity;
    public DataChangeKind DataChangeKind { get; init; } = dataChangeKind;
    public bool IsCollection { get; init; } = entity is IEnumerable;
    public Type EntityType { get; } = typeof(T);
}