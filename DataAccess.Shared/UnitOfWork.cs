using System.Collections.Generic;
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


    public async Task<SaveResult> SaveAsync() {
        var saveResult = await strategy.SaveAsync(queuedItems).ConfigureAwait(false);
        queuedItems.Clear();
        return saveResult;
    }

    public void Reset() => queuedItems.Clear();

    public void AddForUpdate<T>(T entity) where T : class => queuedItems.Add(dataChangeFactory.Create(DataChangeKind.Update, entity));

    public void AddForUpdate<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(dataChangeFactory.Create(DataChangeKind.Update, entities));

    public void AddForDelete<T>(T entity) where T : class => queuedItems.Add(dataChangeFactory.Create(DataChangeKind.Delete, entity));

    public void AddForDelete<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(dataChangeFactory.Create(DataChangeKind.Delete, entities));

    public void AddForInsert<T>(T entity) where T : class =>
        queuedItems.Add(dataChangeFactory.Create(DataChangeKind.Insert, entity));

    public void AddForInsert<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(dataChangeFactory.Create(DataChangeKind.Insert, entities));

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
