namespace DataAccess.Shared.DatabaseMapper;

public interface IDatabaseMap
{
    ITableInfo[] Map { get; }
}