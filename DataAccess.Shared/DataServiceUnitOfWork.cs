using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public abstract record DataServiceUnitOfWork : IUnitOfWork {
    private readonly IUnitOfWork unitOfWork;

    protected DataServiceUnitOfWork(ISaveStrategy SaveStrategy, IDatabaseMapper DatabaseMapper) {
        this.SaveStrategy = SaveStrategy;
        this.DatabaseMapper = DatabaseMapper;
        unitOfWork = new UnitOfWork(SaveStrategy, DatabaseMapper);
    }

    public int QueuedItemsCount => unitOfWork.QueuedItemsCount;
    public ISaveStrategy SaveStrategy { get; }
    public IDatabaseMapper DatabaseMapper { get; init; }

    public void Reset() => unitOfWork.Reset();

    public void Add<T> (DataChangeKind dataChangeKind, T entity) where T : class => unitOfWork.Add<T>(dataChangeKind,entity);
    public void Add<T>(DataChangeKind dataChangeKind, IEnumerable<T> entities) where T : class => 
        unitOfWork.Add<T>(dataChangeKind,entities);

    public async Task<SaveResponse> SaveAsync() => await unitOfWork.SaveAsync();
}