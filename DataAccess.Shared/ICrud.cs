using DataAccess.Shared.FilterService;
using Refit;

namespace DataAccess.Shared;
public interface ICrud {}

public interface ICrud<T> : ICrud {
    [Get("/")]
    Task<Response<T>> GetAllAsync(Filter<T>? filter = null, object? args = null, int pageSize = 0, int pageNumber=1);
    
    [Get("/{pkValue")]
    Task<Response<T>> GetByPkAsync(object pkValue);

    [Get("/{id}")]
    Task<Response<T>> GetByIdAsync(int id);

    [Post("")]
    Task<Response> UpdateItemAsync([Body] T item);

    [Put("")]
    Task<Response<T>> CreateItemAsync([Body] T item);

    [Delete("")]
    Task<Response> DeleteItemAsync(int id);
}