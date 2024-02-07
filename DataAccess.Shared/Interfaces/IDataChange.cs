using System;
using System.Collections;
using System.Linq;

namespace DataAccess.Shared;

public interface IDataChange {
    object Entity { get; }
    DataChangeKind DataChangeKind { get; }
    bool IsCollection { get; }
    Type EntityType { get; }
    ITableInfo TableInfo { get; }
    bool SqlShouldGenPk => DataChangeKind == DataChangeKind.Insert && !IsCollection && !TableInfo.IsIdentity && TableInfo.GetPrimaryKeyValue(Entity) is 0L;
    bool SqlShouldReturnPk => (IsCollection && TableInfo.IsIdentity) || !IsCollection;
    int Count => IsCollection ? ((IEnumerable)Entity).Cast<object>().Count() : 1;
}