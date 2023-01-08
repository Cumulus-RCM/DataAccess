using System.Collections.ObjectModel;

namespace DataAccess; 

public interface IReader<T> {
    Task<T?> GetByIdAsync(int id);
    //Task<T?> TryGetByIdAsync(int id);
    //Task<T> GetOneAsync(string filter, object? values);
    //Task<T?> TryGetOneAsync(string filter, object? values);
    Task<int> GetCountAsync(string filter = "", object? values = null);
    //Task<int> GetFilteredCountAsync(string filter, object? values);
    Task<ReadOnlyCollection<T>> GetAllAsync(string where = "", object? args = null, int offset = 0, int limit = 0, string orderBy = ""); }