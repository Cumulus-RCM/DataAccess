using System.Collections.ObjectModel;
using DataAccess.Shared;

namespace DataAccess;

public interface IReader<T> {
    Task<T?> GetByPkAsync(object pk);
    Task<int> GetCountAsync(Filter? filter = null);
    Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int offset = 0, int limit = 0, IEnumerable<OrderByExpression>? orderBy = null);
}