namespace DataAccess; 

public interface IReaderFactory {
    IReader<T> GetReader<T>() where T : class;
}