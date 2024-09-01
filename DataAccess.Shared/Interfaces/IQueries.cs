using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Refit;

namespace DataAccess.Shared;

public interface IQueries<T> {
    //NOTE: This is a generic interface for all queries. It is used to get data from the database.
    //NOTE: All parameters to interface methods must be serializable to JSON to allow them to be processed by the API.

    [Get("/")]
    Task<Response<T>> GetAllAsync(string? filterJson = null, int pageSize = 0, int pageNumber = 1, string? orderByJson = null,
        IReadOnlyCollection<string>? columnNames = null);

    IObservable<Response<T>> GetAll(Filter? filterJson = null, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null) =>
        Observable.FromAsync(() => GetAllAsync(filterJson, pageSize, pageNumber, orderBy));

    [Get("/Count")]
    Task<int> GetCountAsync(string? filterJson = null);

    Task<int> GetCountAsync(Filter? filter = null) => GetCountAsync(filter?.AsJson());

    Task<Response<T>> GetAllAsync(Filter? filter, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null) =>
        GetAllAsync(filter?.AsJson(), pageSize, pageNumber, orderBy?.AsJson());

    [Get("/{pkValue}")]
    Task<Response<T>> GetByPkAsync(string pkValue, IReadOnlyCollection<string>? columnNames = null);

    Task<Response<T>> GetByPkAsync<TKey>(TKey pk, IReadOnlyCollection<string>? columnNames = null) where TKey : notnull =>
        GetByPkAsync(pk.ToString() ?? throw new InvalidDataException("Key must be have valid ToString() method."), columnNames);
}