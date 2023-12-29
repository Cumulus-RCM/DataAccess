using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Interfaces;
using DataAccess.Shared;

namespace DataAccess;

public class UnitOfWork(ISaveStrategy strategy, IDatabaseMapper databaseMapper) : IUnitOfWork {
    public int QueuedItemsCount => queuedItems.Count;

    private readonly HashSet<IDataChange> queuedItems = new(new DataChangeComparer(databaseMapper));

    public Task<int> SaveAsync() {
        var count = strategy.SaveAsync(queuedItems);
        queuedItems.Clear();
        return count;
    }

    public void Reset() => queuedItems.Clear();

    public void AddForUpdate<T>(T entity) where T : class => queuedItems.Add(new DataChange<T>(DataChangeKind.Update, entity));

    public void AddForUpdate<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Update, entities));

    public void AddForDelete<T>(T entity) where T : class => queuedItems.Add(new DataChange<T>(DataChangeKind.Delete, entity));

    public void AddForDelete<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Delete, entities));

    public void AddForInsert<T>(T entity) where T : class =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Insert, entity));

    public void AddForInsert<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Insert, entities));

}

public class DataChangeComparer(IDatabaseMapper mapper) : IEqualityComparer<IDataChange> {
    public bool Equals(IDataChange? x, IDataChange? y) {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        if (x.IsCollection || y.IsCollection) return false;  //No dup check for collections
        var xTableInfo = mapper.GetTableInfo(x.EntityType);
        var yTableInfo = mapper.GetTableInfo(y.EntityType);
        if (xTableInfo.TableName != yTableInfo.TableName) return false;
        var xPk = xTableInfo.GetPrimaryKeyValue(x);
        var yPk = yTableInfo.GetPrimaryKeyValue(y);
        return xPk.Equals(yPk);
    }

    public int GetHashCode(IDataChange? dataChange) {
        if (dataChange is null) return 0;
        var tableInfo = mapper.GetTableInfo(dataChange.EntityType);
        if (dataChange.IsCollection) return (tableInfo.TableName, dataChange.Entity).GetHashCode();
        var pk = tableInfo.GetPrimaryKeyValue(dataChange.Entity);
        return (tableInfo.TableName,pk).GetHashCode();
    }
}
