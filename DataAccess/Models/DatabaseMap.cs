using System;
using DataAccess.Shared;

namespace DataAccess;

public abstract class DatabaseMap(ITableInfo[]? dbMap = null) : IDatabaseMap {
    public ITableInfo[] Map { get; protected init; } = dbMap ?? Array.Empty<ITableInfo>();
}