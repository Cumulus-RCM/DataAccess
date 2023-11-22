using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Interfaces;

namespace DataAccess;

public class Writer(ISaveStrategy strategy) : IWriter {
    private readonly List<IDataChange> queuedItems = [];

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

    public void AddForInsert<T>(T entity) where T : class => queuedItems.Add(new DataChange<T>(DataChangeKind.Insert, entity));
}