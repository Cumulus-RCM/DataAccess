﻿using System.Text.Json;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Crud<T> : ICrud<T> where T : class {
    protected readonly DbConnectionManager connectionManager;
    protected readonly DatabaseMapper databaseMapper;
    protected readonly ILoggerFactory loggerFactory;
    private readonly ILogger<Crud<T>> logger;
    private readonly Reader<T> reader;
    private readonly SimpleWriter writer;

    public static ICrud<T> Create(DbConnectionManager connectionManager, DatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        return new Crud<T>(connectionManager, databaseMapper, loggerFactory);
    }

    public Crud(DbConnectionManager connectionManager, DatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<Crud<T>>();
        this.reader = new Reader<T>(connectionManager, databaseMapper, loggerFactory);
        this.writer = new SimpleWriter(connectionManager, databaseMapper, loggerFactory);
    }

    public Task<Response<T>> GetAllAsync(Filter<T>? filter = null, object? args = null, int pageSize = 0, int pageNumber = 1) => getAllAsync(filter, args, pageSize, pageNumber);

    private async Task<Response<T>> getAllAsync(Filter<T>? filter = null, object? parameterValues = null, int pageSize = 0, int pageNumber = 1) {
        try {
            var cnt = 0;
            if (pageNumber == 0) {
                cnt = await reader.GetCountAsync(filter, parameterValues).ConfigureAwait(false);
                if (cnt == 0) return Response<T>.Empty();
            }

            var items = await reader.GetAllAsync(filter, parameterValues, pageSize, pageNumber).ConfigureAwait(false);
            return new Response<T>(true, items, cnt);
        }
        catch (Exception ex) {
            logger.LogError(ex, nameof(GetAllAsync));
            return Response<T>.Empty(false, ex.Message);
        }
    }

    public Task<Response<T>> GetByPkAsync(object pkValue) {
        var tableInfo = databaseMapper.GetTableInfo<T>();
        var filter = new Filter<T>(tableInfo.PrimaryKeyName, Operator.Equal);
        return getAllAsync(filter, pkValue);
    }

    public Task<Response<T>> GetByIdAsync(int id) {
        var filter = new Filter<T>("Id", Operator.Equal);
        return getAllAsync(filter, new{Id = id});
    }

    public async Task<Response> UpdateItemAsync(T item) {
        writer.AddForUpdate(item);
        var updatedRowCount = await writer.SaveAsync().ConfigureAwait(false);
        return new Response(updatedRowCount == 1);
    }

    public async Task<Response<T>> CreateItemAsync(T item) {
        writer.AddForInsert(item);
        await writer.SaveAsync().ConfigureAwait(false);
        return new Response<T>(item);
    }

    public async Task<Response> DeleteItemAsync(int id) {
        writer.AddForDelete<T>(id);
        var updatedRowCount = await writer.SaveAsync().ConfigureAwait(false);
        return new Response(updatedRowCount == 1);
    }
}