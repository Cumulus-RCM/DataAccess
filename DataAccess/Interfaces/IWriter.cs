using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Interfaces;

public interface IWriter<in T> where T : class {
    void Reset();
    void AddForUpdate(T entity);
    void AddForUpdate(IEnumerable<T> entity);
    void AddForDelete(T entity);
    void AddForDelete(IEnumerable<T> entities);
    void AddForInsert(T entity);
    void AddForInsert(IEnumerable<T> entities);
    Task<int> SaveAsync();
}