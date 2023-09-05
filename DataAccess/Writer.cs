﻿namespace DataAccess;

public abstract class Writer : IWriter {
    private readonly SaveStrategy saveStrategy;
    private readonly List<IDataChange> queuedItems = new();

    protected Writer(SaveStrategy saveStrategy) {
        this.saveStrategy = saveStrategy;
    }

    public Task<int> SaveAsync() => saveStrategy.SaveAsync(queuedItems);

    public void Reset() => queuedItems.Clear();

    public void AddForUpdate<T>(T entity) where T : class => queuedItems.Add(new DataChange<T>(DataChangeKind.Update, entity, false));

    public void AddForUpdate<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Update, entities, true));

    public void AddForDelete<T>(T entity) where T : class => queuedItems.Add(new DataChange<T>(DataChangeKind.Delete, entity, false));

    public void AddForDelete<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Delete, entities, true));

    public void AddForInsert<T>(T entity) where T : class => queuedItems.Add(new DataChange<T>(DataChangeKind.Insert, entity, false));

    public void AddForInsert<T>(IEnumerable<T> entities) where T : class =>
        queuedItems.Add(new DataChange<T>(DataChangeKind.Insert, entities, true));
}