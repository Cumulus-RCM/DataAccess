using DataAccess.Interfaces;

namespace DataAccess;
public class DatabaseWriterFactory(ISaveStrategy strategy) : IWriterFactory {
    public IWriter<T> GetWriter<T>() where T : class => new SimpleDatabaseWriter<T>(strategy);
}