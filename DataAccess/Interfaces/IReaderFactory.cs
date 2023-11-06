namespace DataAccess.Interfaces; 

public interface IReaderFactory {
    IReader<T> GetReader<T>() where T : class;
}