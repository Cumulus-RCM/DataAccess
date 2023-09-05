namespace DataAccess.Interfaces;

public interface IDatabaseMap
{
    ITableInfo[] Map { get; }
}