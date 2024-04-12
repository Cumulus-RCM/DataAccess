using System.Collections.Generic;
using System.Threading.Tasks;
using static DataAccess.Shared.DataChangeKind;

namespace DataAccess.Shared;

public interface IUnitOfWork {
    int QueuedItemsCount { get; }

    IEnumerable<T> GetQueuedInsertItems<T>() where T : class;
    void Reset();
    void Add<T>(DataChangeKind dataChangeKind, T entity) where T : class;
    void AddCollection<T>(DataChangeKind dataChangeKind, ICollection<T> entity)  where T : class;
    Task<SaveResponse> SaveAsync();

    void AddForUpdate<T>(T entity) where T : class => Add<T>(Update, entity);
    void AddForUpdate<T>(ICollection<T> entities)  where T : class => AddCollection<T>(Update, entities);
    void AddForDelete<T>(T entity)  where T : class => Add<T>(Delete, entity);
    void AddForDelete<T>(ICollection<T> entities) where T : class => AddCollection<T>(Delete, entities);
    void AddForInsert<T>(T entity) where T : class => Add<T>(Insert, entity);
    void AddCollectionForInsert<T>(ICollection<T> entities) where T : class => AddCollection<T>(Insert, entities);
}