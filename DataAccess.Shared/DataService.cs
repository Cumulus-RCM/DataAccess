using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public class DataService(ISaveStrategy saveStrategy, IDatabaseMapper databaseMapper) : IUnitOfWork {
    private readonly IUnitOfWork unitOfWork = new UnitOfWork(saveStrategy, databaseMapper);

    public int QueuedItemsCount => unitOfWork.QueuedItemsCount;

    public void Reset() => unitOfWork.Reset();

    public void Add<T> (DataChangeKind dataChangeKind, T entity) where T : class => unitOfWork.Add<T>(dataChangeKind,entity);
    public void Add<T>(DataChangeKind dataChangeKind, IEnumerable<T> entities) where T : class => 
        unitOfWork.Add<T>(dataChangeKind,entities);

    public async Task<SaveResult> SaveAsync() => await unitOfWork.SaveAsync();
}