using System.Collections.ObjectModel;
using Dapper;
using DataAccess.Shared;

namespace DataAccess;

public interface IReader<T> {
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByPkAsync(object pk);
    Task<int> GetCountAsync(Filter? filter = null);
    Task<ReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int offset = 0, int limit = 0, string orderBy = "");
}