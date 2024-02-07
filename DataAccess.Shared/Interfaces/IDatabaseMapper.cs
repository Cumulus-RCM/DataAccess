using System;

namespace DataAccess.Shared;

public interface IDatabaseMapper {
    ITableInfo GetTableInfo<T>();
    ITableInfo GetTableInfo(Type entityType);
}