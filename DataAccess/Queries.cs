using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using DataAccess.Shared;
using Serilog;

namespace DataAccess;

public class Queries<T>(IReader<T> reader) : IQueries<T> where T : class {
    public Task<Response<T>> GetAllAsync(string? filterJson = null, int pageSize = 0, int pageNumber = 1, string? orderByJson = null,IReadOnlyCollection<string>? columnNames = null) {
        var filter = Filter.FromJson(filterJson);
        var orderBy = OrderBy.FromJson(orderByJson);
        return getAllAsync(filter, pageSize, pageNumber, orderBy, columnNames);
    }

    public Task<int> GetCountAsync(string? filterJson = null) => reader.GetCountAsync(Filter.FromJson(filterJson));

    private async Task<Response<T>> getAllAsync(Filter? filter = null, int pageSize = 0, int pageNumber = 1, OrderBy? orderBy = null,IReadOnlyCollection<string>? columnNames = null) {
        try {
            var cnt = 0;
            if (pageNumber == 0) {
                cnt = await reader.GetCountAsync(filter).ConfigureAwait(false);
                if (cnt == 0) return Response<T>.Empty();
            }

            var items = await reader.GetAllAsync(filter, pageSize, pageNumber, orderBy, columnNames).ConfigureAwait(false);
            return new Response<T>(items, cnt);
        }
        catch (Exception ex) {
            Log.Error(ex, nameof(GetAllAsync));
            return Response<T>.Fail(ex.Message);
        }
    }

    public Task<Response<T>> GetByModelAsync(T item) => getAllAsync(Filter.FromEntity(item));

    public async Task<Response<T>> GetByPkAsync(string pk,IReadOnlyCollection<string>? columnNames = null) {
        var result = await reader.GetByPkAsync(pk, columnNames).ConfigureAwait(false);
        return result == null
            ? Response<T>.Fail($"No Entity with Primary Key Value:{pk}")
            : new Response<T>(result);
    }
}