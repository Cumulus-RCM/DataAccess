﻿using DataAccess.Extensions;
using DataAccess.Models;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Crud<T> where T : class {
    protected readonly DbConnectionManager connectionManager;
    protected readonly DatabaseMapper databaseMapper;
    protected readonly ILoggerFactory loggerFactory;
    private readonly ILogger<Crud<T>> logger;
    private readonly Reader<T> reader;
    private readonly SimpleWriter writer;

    public Crud(DbConnectionManager connectionManager, DatabaseMapper databaseMapper, ILoggerFactory loggerFactory)  {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<Crud<T>>();
        this.reader = new Reader<T>(connectionManager, databaseMapper, loggerFactory);
        this.writer = new SimpleWriter(connectionManager, databaseMapper, loggerFactory);
    }

    public async Task<Response<T>> GetAllAsync(string? filter, int? pageSize, int? pageNumber)  {
        try {
            var cnt = 0;
            if (pageNumber == 0) {
                cnt = await reader.GetCountAsync(filter ?? "").ConfigureAwait(false);
                if (cnt == 0) return new Response<T>(true, Enumerable.Empty<T>());
            }
            var items = await reader.GetAllAsync(where: filter ?? "", pageSize: pageSize ?? 0, pageNum: pageNumber ?? 1).ConfigureAwait(false);
            return new Response<T>(true, items, cnt);

        }
        catch (Exception ex) {
            logger.LogError(ex, nameof(GetAllAsync));
            return new Response<T>(false, Enumerable.Empty<T>(), 0, ex.Message);
        }
    }

    public async Task<Response<T>> GetByIdAsync(int id) {
        var item = await reader.GetByIdAsync(id);
        return item is not null ? new Response<T>(true, item.ItemAsEnumerable(), 1) : Response<T>.Empty(false, $"No row for {typeof(T).Name} found with Id={id}");
    }

    public async Task<bool> UpdateItemAsync(T item) {
        writer.AddForUpdate(item);
        return await writer.SaveAsync().ConfigureAwait(false) == 1;
    }

    public async Task<bool> CreateItemAsync(T item)  {
        writer.AddForInsert(item);
        return await writer.SaveAsync().ConfigureAwait(false) == 1;
    }

    public async Task<bool> DeleteItemAsync(int id) {
        writer.AddForDelete<T>(id);
        return await writer.SaveAsync().ConfigureAwait(false) == 1;
    }
}