// ReSharper disable once CheckNamespace
namespace DataAccess;

public interface IDatabaseMap {
    ITableInfo[] Map { get; }
}