using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface IReader<T> {
    void UseCache();
    Task<T?> GetByPkAsync(string pk);
    Task<int> GetCountAsync(Filter? filter = null);
    Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null);
}