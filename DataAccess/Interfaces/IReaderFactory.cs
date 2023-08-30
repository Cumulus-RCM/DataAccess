namespace DataAccess; 

public interface IReaderFactory {
    Reader<T> GetReader<T>() where T : class;
}