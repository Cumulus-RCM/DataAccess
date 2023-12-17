namespace DataAccess.Interfaces; 

public interface IWriterFactory {
    IWriter<T> GetWriter<T>() where T : class;
}