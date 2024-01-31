using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public class DataServiceBase(ISaveStrategy saveStrategy, IDatabaseMapper databaseMapper) : IUnitOfWork {
    private readonly IUnitOfWork unitOfWork = new UnitOfWork(saveStrategy, databaseMapper);

    public int QueuedItemsCount => unitOfWork.QueuedItemsCount;

    public void Reset() => unitOfWork.Reset();

    public void AddForUpdate<T>(T entity) where T : class => unitOfWork.AddForUpdate(entity);

    public void AddForUpdate<T>(IEnumerable<T> entity) where T : class => unitOfWork.AddForUpdate(entity);

    public void AddForDelete<T>(T entity) where T : class => unitOfWork.AddForDelete(entity);

    public void AddForDelete<T>(IEnumerable<T> entities) where T : class => unitOfWork.AddForDelete(entities);

    public void AddForInsert<T>(T entity) where T : class => unitOfWork.AddForInsert(entity);

    public void AddForInsert<T>(IEnumerable<T> entities) where T : class => unitOfWork.AddForInsert(entities);

    public async Task<SaveResult> SaveAsync() => await unitOfWork.SaveAsync();
}