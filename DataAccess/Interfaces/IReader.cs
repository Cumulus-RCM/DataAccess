using System.Collections.ObjectModel;
using DataAccess.Shared;

namespace DataAccess;

public interface IReader<T> {
    Task<T?> GetByPkAsync(string pk);
    Task<int> GetCountAsync(Filter? filter = null);
    Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null);
}