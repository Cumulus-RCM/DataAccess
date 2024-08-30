namespace DataAccess.Shared;

public interface IDataService : IUnitOfWork {
    IQueries<T> GetQueries<T>() where T : class, new();
}
