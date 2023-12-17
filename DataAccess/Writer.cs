using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Interfaces;

namespace DataAccess;

public class Writer<T>(ISaveStrategy strategy) : IWriter<T> where T : class {
    private readonly List<IDataChange> queuedItems = [];

    public Task<int> SaveAsync() {
        var count = strategy.SaveAsync(queuedItems);
        queuedItems.Clear();
        return count;
    }

    public void Reset() => queuedItems.Clear();

    public void AddForUpdate(T entity) => queuedItems.Add(new DataChange<T>(DataChangeKind.Update, entity));

    public void AddForUpdate(IEnumerable<T> entities) =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Update, entities));

    public void AddForDelete(T entity) => queuedItems.Add(new DataChange<T>(DataChangeKind.Delete, entity));

    public void AddForDelete(IEnumerable<T> entities) =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Delete, entities));

    public void AddForInsert(T entity) => 
        queuedItems.Add(new DataChange<T>(DataChangeKind.Insert, entity));
    
    public void AddForInsert(IEnumerable<T> entities) => 
        queuedItems.Add(new DataChange<T>(DataChangeKind.Insert, entities));
}