using System.Collections.ObjectModel;
using DataAccess.Shared;

namespace DataAccess; 

public interface IReader<T> {
    Task<T?> GetByIdAsync(int id);
    //Task<T?> TryGetByIdAsync(int id);
    //Task<T> GetOneAsync(string filter, object? values);
    //Task<T?> TryGetOneAsync(string filter, object? values);
    Task<int> GetCountAsync(Filter<T>? filter = null, object? args = null);
    //Task<int> GetFilteredCountAsync(string filter, object? values);
    Task<ReadOnlyCollection<T>> GetAllAsync(Filter<T>? filter = null, object? filterValues = null, int offset = 0, int limit = 0, string orderBy = ""); }