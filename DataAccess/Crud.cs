using BaseLib;
using DataAccess.Interfaces;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class Crud<T> : ICrud<T> where T : class {
    protected readonly ILogger logger;
    protected readonly IReader<T> reader;
    protected readonly IWriter writer;

    public Crud(IReaderFactory readerFactory, IWriter writer, ILogger logger) {
        this.logger = logger;
        this.reader = readerFactory.GetReader<T>();
        this.writer = writer;
    }

    public Task<Response<T>> GetAllAsync(string? filterJson = null, int pageSize = 0, int pageNumber = 1, string? orderByJson = null) {
        var filter = Filter.FromJson(filterJson);
        var orderBy = OrderBy.FromJson(orderByJson);  
        return getAllAsync(filter, pageSize, pageNumber, orderBy);
    }

    public Task<Response<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null) => 
        getAllAsync(filter, pageSize, pageNumber, orderBy);

    private async Task<Response<T>> getAllAsync(Filter? filter = null, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null) {
        try {
            var cnt = 0;
            if (pageNumber == 0) {
                cnt = await reader.GetCountAsync(filter).ConfigureAwait(false);
                if (cnt == 0) return Response<T>.Empty();
            }

            var items = await reader.GetAllAsync(filter, pageSize, pageNumber, orderBy).ConfigureAwait(false);
            return new Response<T>(true, items, cnt);
        }
        catch (Exception ex) {
            logger.LogError(ex, nameof(GetAllAsync));
            return Response<T>.Empty(false, ex.Message);
        }
    }

    public Task<Response<T>> GetByModelAsync(T item) => GetAllAsync(Filter.FromEntity(item));

    public async Task<Response<T>> GetByPkAsync(string pkValue) {
        var result = await reader.GetByPkAsync(pkValue).ConfigureAwait(false);
        return result == null
            ? Response<T>.Fail($"No Entity with Primary Key Value:{pkValue}")
            : new Response<T>(result);
    }


    public async Task<Response> UpdateItemAsync(T item) {
        writer.AddForUpdate(item);
        var updatedRowCount = await writer.SaveAsync().ConfigureAwait(false);
        return new Response(updatedRowCount == 1);
    }

    public async Task<Response<T>> CreateItemAsync(T item) {
        writer.AddForInsert(item);
        var updatedRowCount = await writer.SaveAsync().ConfigureAwait(false);
        return new Response<T>(item, updatedRowCount == 1);
    }

    public async Task<Response> DeleteItemAsync(T item) {
        writer.AddForDelete<T>(item);
        var updatedRowCount = await writer.SaveAsync().ConfigureAwait(false);
        return new Response(updatedRowCount == 1);
    }
}