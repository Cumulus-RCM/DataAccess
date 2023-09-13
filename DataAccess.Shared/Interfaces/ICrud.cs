using Refit;

namespace DataAccess.Shared;

public interface ICrud<T> {
    [Get("/")]
    Task<Response<T>> GetAllAsync(string? filterJson = null, int pageSize = 0, int pageNumber = 1, IEnumerable<OrderByExpression>? orderBy = null);

    [Get("/{pkValue}")]
    Task<Response<T>> GetByPkAsync(object pkValue);

    [Get("/")]
    Task<Response<T>> GetByModelAsync([Body] T item);

    [Put("")]
    Task<Response> UpdateItemAsync([Body] T item);

    [Post("")]
    Task<Response<T>> CreateItemAsync([Body] T item);

    [Delete("")]
    Task<Response> DeleteItemAsync(T item);
}