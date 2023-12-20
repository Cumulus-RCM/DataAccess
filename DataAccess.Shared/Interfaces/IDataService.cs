namespace DataAccess.Shared;

public interface IDataService {
    IQueries<T> GetQueries<T>() where T : class;
    ICommands<T> GetCommands<T>() where T : class;
}
