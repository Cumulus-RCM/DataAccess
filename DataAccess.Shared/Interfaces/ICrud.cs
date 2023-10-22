using Refit;

namespace DataAccess.Shared;

public interface ICrud<T> {
    [Get("/")]
    Task<Response<T>> GetAllAsync(string? filterJson = null, int pageSize = 0, int pageNumber = 1, string? orderByJson = null);

    Task<Response<T>> GetAllAsync(Filter filter, int pageSize = 0, int pageNumber = 1, string? orderByJson = null) =>
        GetAllAsync(filter.AsJson(), pageSize, pageNumber, orderByJson);

    [Get("/{pkValue}")]
    Task<Response<T>> GetByPkAsync(string pkValue);

    //[Get("/")]
    //Task<Response<T>> GetByModelAsync([Body] T item);

    [Put("")]
    Task<Response> UpdateItemAsync([Body] T item);

    [Post("")]
    Task<Response<T>> CreateItemAsync([Body] T item);

    [Delete("")]
    Task<Response> DeleteItemAsync([Body] T item);
}