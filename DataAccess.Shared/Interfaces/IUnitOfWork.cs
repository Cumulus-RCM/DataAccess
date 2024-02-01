using System.Collections.Generic;
using System.Threading.Tasks;
using static DataAccess.Shared.DataChangeKind;

namespace DataAccess.Shared;

public interface IUnitOfWork {
    int QueuedItemsCount { get; }
    void Reset();
    void Add<T>(DataChangeKind dataChangeKind, T entity) where T : class;
    void Add<T>(DataChangeKind dataChangeKind, IEnumerable<T> entity)  where T : class;
    Task<SaveResult> SaveAsync();

    void AddForUpdate<T>(T entity) where T : class => Add<T>(Update, entity);
    void AddForUpdate<T>(IEnumerable<T> entities)  where T : class => Add<T>(Update, entities);
    void AddForDelete<T>(T entity)  where T : class => Add<T>(Delete, entity);
    void AddForDelete<T>(IEnumerable<T> entities) where T : class => Add<T>(Delete, entities);
    void AddForInsert<T>(T entity) where T : class => Add<T>(Insert, entity);
    void AddForInsert<T>(IEnumerable<T> entities) where T : class => Add<T>(Insert, entities);
}