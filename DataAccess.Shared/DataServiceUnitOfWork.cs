using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public record DataServiceUnitOfWork : IUnitOfWork {
    private readonly IUnitOfWork unitOfWork;

    public DataServiceUnitOfWork(ISaveStrategy SaveStrategy, IDatabaseMapper DatabaseMapper) {
        this.SaveStrategy = SaveStrategy;
        this.DatabaseMapper = DatabaseMapper;
        unitOfWork = new UnitOfWork(SaveStrategy, DatabaseMapper);
    }

    public int QueuedItemsCount => unitOfWork.QueuedItemsCount;
    public ISaveStrategy SaveStrategy { get; }
    public IDatabaseMapper DatabaseMapper { get; init; }

    public IEnumerable<T> GetQueuedInsertItems<T>() where T : class => unitOfWork.GetQueuedInsertItems<T>();

    public void Reset() => unitOfWork.Reset();

    public void Add<T> (DataChangeKind dataChangeKind, T entity) where T : class => unitOfWork.Add<T>(dataChangeKind,entity);
    public void AddCollection<T>(DataChangeKind dataChangeKind, ICollection<T> entities) where T : class => 
        unitOfWork.AddCollection<T>(dataChangeKind,entities);

    public async Task<SaveResponse> SaveAsync() => await unitOfWork.SaveAsync();
}