namespace DataAccess.Interfaces;

public interface IDatabaseMapper {
    TableInfo<T> GetTableInfo<T>();
    ITableInfo GetTableInfo(Type entityType);
}