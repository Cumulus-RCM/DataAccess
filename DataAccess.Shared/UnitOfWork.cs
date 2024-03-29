﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public class UnitOfWork : IUnitOfWork {
    public int QueuedItemsCount => queuedItems.Sum(x=>x.Count);

    private readonly HashSet<IDataChange> queuedItems = new(new DataChangeComparer());
    private readonly ISaveStrategy strategy;
    private readonly DataChangeFactory dataChangeFactory;

    public UnitOfWork(ISaveStrategy strategy, IDatabaseMapper databaseMapper) {
        this.strategy = strategy;
        dataChangeFactory = new DataChangeFactory(databaseMapper);
    }

    public void Add<T>(DataChangeKind dataChangeKind, T entity) where T : class => 
        queuedItems.Add(dataChangeFactory.Create(dataChangeKind, entity));

    public void Add<T>(DataChangeKind dataChangeKind, IEnumerable<T> entities) where T : class =>
        queuedItems.Add(dataChangeFactory.Create(dataChangeKind, entities));

    public async Task<SaveResponse> SaveAsync() {
        var saveResult = await strategy.SaveAsync(queuedItems);
        queuedItems.Clear();
        return saveResult;
    }

    public void Reset() => queuedItems.Clear();
}

public class DataChangeComparer : IEqualityComparer<IDataChange> {
    public bool Equals(IDataChange? x, IDataChange? y) {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        if (x.IsCollection || y.IsCollection) return false;  //No dup check for collections
        if (x.TableInfo.TableName != y.TableInfo.TableName) return false;
        var xPk = x.TableInfo.GetPrimaryKeyValue(x);
        var yPk = y.TableInfo.GetPrimaryKeyValue(y);
        return xPk.Equals(yPk);
    }

    public int GetHashCode(IDataChange? dataChange) {
        if (dataChange is null) return 0;
        if (dataChange.IsCollection) return (dataChange.TableInfo.TableName, dataChange.Entity).GetHashCode();
        var pk = dataChange.TableInfo.GetPrimaryKeyValue(dataChange.Entity);
        return (dataChange.TableInfo.TableName,pk).GetHashCode();
    }
}
