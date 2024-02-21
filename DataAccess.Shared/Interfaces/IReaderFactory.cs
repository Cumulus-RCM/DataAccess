namespace DataAccess.Shared; 

public interface IReaderFactory {
    IReader<T> GetReader<T>(bool useCache) where T : class;
}