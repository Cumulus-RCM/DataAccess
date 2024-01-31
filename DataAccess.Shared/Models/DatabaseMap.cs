using System;

namespace DataAccess.Shared;

public abstract class DatabaseMap(ITableInfo[]? dbMap = null) : IDatabaseMap {
    public ITableInfo[] Map { get; protected init; } = dbMap ?? Array.Empty<ITableInfo>();
}