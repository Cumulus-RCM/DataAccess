using BaseLib;
using DataAccess.Interfaces;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Crud<T> : ICrud<T> where T : class {
    protected readonly IDbConnectionManager connectionManager;
    protected readonly IDatabaseMapper databaseMapper;
    protected readonly ILoggerFactory loggerFactory;
    private readonly ILogger<Crud<T>> logger;
    private readonly Reader<T> reader;
    private readonly SimpleWriter writer;

    public Crud(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<Crud<T>>();
        this.reader = new Reader<T>(connectionManager, databaseMapper, loggerFactory);
        this.writer = new SimpleWriter(connectionManager, databaseMapper, loggerFactory);
    }

    public Task<Response<T>> GetAllAsync(string? filterJson = "", int pageSize = 0, int pageNumber = 1) {
        var filter = Filter.FromJson(filterJson);
        return getAllAsync(filter, pageSize, pageNumber);
    }

    public Task<Response<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNumber = 1) {
        return getAllAsync(filter, pageSize, pageNumber);
    }
   
    private async Task<Response<T>> getAllAsync(Filter? filter = null, int pageSize = 0, int pageNumber = 1) {
        try {
            var cnt = 0;
            if (pageNumber == 0) {
                cnt = await reader.GetCountAsync(filter).ConfigureAwait(false);
                if (cnt == 0) return Response<T>.Empty();
            }

            var items = await reader.GetAllAsync(filter, pageSize, pageNumber).ConfigureAwait(false);
            return new Response<T>(true, items, cnt);
        }
        catch (Exception ex) {
            logger.LogError(ex, nameof(GetAllAsync));
            return Response<T>.Empty(false, ex.Message);
        }
    }

    public async Task<Response<T>> GetByPkAsync(object pkValue) {
        var result = await reader.GetByPkAsync(pkValue).ConfigureAwait(false);
        return new Response<T>(result);
    }

    public async Task<Response<T>> GetByIdAsync(int id) {
        var result = await reader.GetByIdAsync(id).ConfigureAwait(false);
        return new Response<T>(result);
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