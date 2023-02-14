// ReSharper disable once CheckNamespace
namespace DataAccess;

public class DatabaseMap : IDatabaseMap {
    public ITableInfo[] Map { get; protected set; }

    public DatabaseMap(ITableInfo[]? dbMap = null) {
        Map = dbMap ?? Array.Empty<ITableInfo>();
    }
}