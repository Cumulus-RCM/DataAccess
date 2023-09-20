using DataAccess.Interfaces;

namespace DataAccess;

public abstract class DatabaseMap : IDatabaseMap {
    public ITableInfo[] Map { get; protected init; }

    protected DatabaseMap(ITableInfo[]? dbMap = null) {
        Map = dbMap ?? Array.Empty<ITableInfo>();
    }
}