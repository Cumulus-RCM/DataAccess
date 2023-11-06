using DataAccess.Interfaces;

namespace DataAccess;

public class DataChange<T> : IDataChange {
    public object Entity { get; init; }
    public DataChangeKind DataChangeKind { get; init; }
    public bool IsCollection { get; init; }
    public Type EntityType { get; } = typeof(T);

    public DataChange(DataChangeKind dataChangeKind, object entity, bool isCollection) {
        DataChangeKind = dataChangeKind;
        Entity = entity;
        IsCollection = isCollection;
    }
 }