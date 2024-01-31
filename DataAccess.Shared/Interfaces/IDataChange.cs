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
    int Count => IsCollection ? ((IEnumerable)Entity).Cast<object>().Count() : 1;
}