namespace DataAccess.Shared.DatabaseMapper.Models;

public class DatabaseMap : IDatabaseMap
{
    public ITableInfo[] Map { get; protected set; }

    public DatabaseMap(ITableInfo[]? dbMap = null)
    {
        Map = dbMap ?? Array.Empty<ITableInfo>();
    }
}