namespace DataAccess.Interfaces;

public interface IWriter
{
    void Reset();
    void AddForUpdate<T>(T entity) where T : class;
    void AddForUpdate<T>(IEnumerable<T> entity) where T : class;
    void AddForDelete<T>(int id) where T : class;
    void AddForDelete<T>(IEnumerable<int> ids) where T : class;
    void AddForInsert<T>(T entity) where T : class;
    void AddForInsert<T>(IEnumerable<T> entities) where T : class;
    Task<int> SaveAsync();
}