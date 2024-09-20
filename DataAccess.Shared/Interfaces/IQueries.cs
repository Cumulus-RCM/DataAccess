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
        IReadOnlyCollection<string>? columnNames = null, string? parameterValues = null);

    IObservable<Response<T>> GetAll(Filter? filter = null, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null,
        IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameterValues = null) =>
        Observable.FromAsync(() => GetAllAsync(filter, pageSize, pageNumber, orderBy, columnNames, parameterValues));

    [Get("/Count")]
    Task<int> GetCountAsync(string? filterJson = null, string? parameterValues = null);

    Task<int> GetCountAsync(Filter? filter = null, ParameterValues? parameterValues = null) => GetCountAsync(filter?.AsJson(), parameterValues?.AsJson());

    Task<Response<T>> GetAllAsync(Filter? filter, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null,
        IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameterValues = null) =>
        GetAllAsync(filterJson: filter?.AsJson(), pageSize, pageNumber, orderBy?.AsJson(), columnNames, parameterValues?.AsJson());

    [Get("/{pkValue}")]
    Task<Response<T>> GetByPkAsync(string pkValue, IReadOnlyCollection<string>? columnNames = null, string? parameterValues = null);

    Task<Response<T>> GetByPkAsync<TKey>(TKey pk, IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameterValues = null) where TKey : notnull {
        var pkString = pk.ToString() ?? throw new InvalidDataException("Key must be have valid ToString() method.");
        return GetByPkAsync(pkString, columnNames, parameterValues?.AsJson());
    }
}