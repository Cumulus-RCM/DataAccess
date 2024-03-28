using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface IReader<T> {
    Task<T?> GetByPkAsync(string pk);
    Task<int> GetCountAsync(Filter? filter = null);
    Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null);
    Task<ReadOnlyCollection<dynamic>> GetAllDynamicAsync(IReadOnlyCollection<string> columns, Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null);
}