namespace DataAccess.Shared;

public interface IDatabaseMap {
    ITableInfo[] Map { get; }
}