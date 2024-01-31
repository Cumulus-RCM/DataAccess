﻿using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BaseLib;
using Refit;

namespace DataAccess.Shared;

public interface IQueries<T> {
    [Get("/")]
    Task<Response<T>> GetAllAsync(string? filterJson = null, int pageSize = 0, int pageNumber = 1, string? orderByJson = null);

    IObservable<Response<T>> GetAll(Filter? filterJson = null, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null) =>
        Observable.FromAsync(() => GetAllAsync(filterJson, pageSize, pageNumber, orderBy));

    Task<Response<T>> GetAllAsync(Filter? filter, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null) =>
        GetAllAsync(filter?.AsJson(), pageSize, pageNumber, orderBy?.AsJson());

    [Get("/{pkValue}")]
    Task<Response<T>> GetByPkAsync(string pkValue);

    Task<Response<T>> GetByPkAsync<TKey>(TKey id) where TKey : notnull => GetByPkAsync(id.ToString() ?? throw new InvalidDataException("Key must be have valid ToString() method."));
}
