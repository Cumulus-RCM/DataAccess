namespace DataAccess.Interfaces;

public interface IDataChange {
    object Entity { get; init; }
    DataChangeKind DataChangeKind { get; init; }
    bool IsCollection { get; init; }
    Type EntityType { get; }
}