using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface IReader<T> {
    Task<T?> GetByPkAsync(string pk);
    Task<int> GetCountAsync(Filter? filter = null);
    Task<IReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null);
    Task<IReadOnlyCollection<dynamic>> GetAllDynamicAsync(IReadOnlyCollection<string> columns, Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null);
    Task<dynamic?> GetByPkDynamicAsync(string pkValue, IReadOnlyCollection<string> columnNames);
}