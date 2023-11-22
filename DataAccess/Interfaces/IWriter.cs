using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Interfaces;

public interface IWriter {
    void Reset();
    void AddForUpdate<T>(T entity) where T : class;
    void AddForUpdate<T>(IEnumerable<T> entity) where T : class;
    void AddForDelete<T>(T entity) where T : class;
    void AddForDelete<T>(IEnumerable<T> entities) where T : class;
    void AddForInsert<T>(T entity) where T : class;
    Task<int> SaveAsync();
}