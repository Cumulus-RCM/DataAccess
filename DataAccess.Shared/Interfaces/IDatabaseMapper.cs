using System;

namespace DataAccess.Shared;

public interface IDatabaseMapper {
    TableInfo<T> GetTableInfo<T>();
    ITableInfo GetTableInfo(Type entityType);
}