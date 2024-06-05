using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public class UnitOfWork : IUnitOfWork {
    public int QueuedItemsCount => queuedItems.Sum(x=>x.Count);

    private readonly HashSet<IDataChange> queuedItems = new(new DataChangeComparer());
    private readonly ISaveStrategy saveStrategy;
    private readonly DataChangeFactory dataChangeFactory;

    public UnitOfWork(ISaveStrategy saveStrategy, IDatabaseMapper databaseMapper) {
        this.saveStrategy = saveStrategy;
        dataChangeFactory = new DataChangeFactory(databaseMapper);
    }

    public void Add<T>(DataChangeKind dataChangeKind, T entity) where T : class => 
        queuedItems.Add(dataChangeFactory.Create(dataChangeKind, entity));

    public void AddCollection<T>(DataChangeKind dataChangeKind, ICollection<T> entities) where T : class =>
        queuedItems.Add(dataChangeFactory.Create(dataChangeKind, entities, true));

    public async Task<SaveResponse> SaveAsync() {
        var saveResult = await saveStrategy.SaveAsync(queuedItems);
        queuedItems.Clear();
        return saveResult;
    }

    public IEnumerable<T> GetQueuedInsertItems<T>() where T : class => 
        queuedItems.Where(x=> x.DataChangeKind == DataChangeKind.Insert && x.EntityType == typeof(T)).Select(x=>(T)x.Entity);

    public void Reset() => queuedItems.Clear();
}

public class DataChangeComparer : IEqualityComparer<IDataChange> {
    public bool Equals(IDataChange? x, IDataChange? y) {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;
        if (x.IsCollection || y.IsCollection) return false;  //No dup check for collections
        if (x.TableInfo.TableName != y.TableInfo.TableName) return false;
        var xPk = x.TableInfo.GetPrimaryKeyValue(x.Entity);
        var yPk = y.TableInfo.GetPrimaryKeyValue(y.Entity);

        if (xPk is IdPk && yPk is IdPk 
                        && x.DataChangeKind == DataChangeKind.Insert
                        && y.DataChangeKind == DataChangeKind.Insert) 
            return x.Entity.GetHashCode().Equals(y.Entity.GetHashCode());
        return xPk.Equals(yPk);
    }

    public int GetHashCode(IDataChange? dataChange) {
        if (dataChange is null) return 0;
        if (dataChange.IsCollection) return (dataChange.TableInfo.TableName, dataChange.Entity).GetHashCode();
        var pk = dataChange.TableInfo.GetPrimaryKeyValue(dataChange.Entity);
        return (dataChange.TableInfo.TableName,pk).GetHashCode();
    }
}
