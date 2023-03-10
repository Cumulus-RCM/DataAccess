namespace DataAccess.Shared;

public interface IDataService {
    ICrud<T> GetCrud<T>() where T : class;
}
