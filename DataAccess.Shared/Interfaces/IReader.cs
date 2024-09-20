using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccess.Shared;

public interface IReader<T> {
    Task<T?> GetByPkAsync(string pk, IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameterValues = null);
    Task<int> GetCountAsync(Filter? filter = null, ParameterValues? parameterValues = null);
    Task<IReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null, IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameters = null);
}