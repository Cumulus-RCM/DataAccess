using System.Threading.Tasks;
using BaseLib;
using DataAccess.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EndpointMapper; 

public abstract class EndpointMapperBase {
    protected static IEndpointRouteBuilder MapCrudEndpoint<T>(IEndpointRouteBuilder routeBuilder, IDataService dataService, IQueries<T>? queries = null) where T : class {
        var typeName = typeof(T).Name;
        queries ??= dataService.GetQueries<T>();
        var group = routeBuilder.MapGroup($"/{typeName}").WithTags(typeName);
        group.MapGet("/",
                async Task<Response<T>> (string? filterJson, int? pageSize, int? pageNumber) =>
                    await queries.GetAllAsync(filterJson, pageSize ?? 0, pageNumber ?? 0).ConfigureAwait(false))
            .WithName($"Get{typeName}")
            .WithOpenApi();

        group.MapGet("/{PrimaryKeyValue}", async Task<Response<T>> (string pkValue) => await queries.GetByPkAsync(pkValue).ConfigureAwait(false))
            .WithName($"Get{typeName}ByPk")
            .WithOpenApi();
        
        if (typeof(T).IsAssignableTo(typeof(ICustomQueries))) {
            foreach (var (endPoint, handler, name) in ((ICustomQueries) queries).GetEndPoints()) {
                group.MapGet(endPoint, handler)
                    .WithName(name)
                    .WithOpenApi();
            }
        }

        var commands = dataService.GetCommands<T>();
        group.MapPut("/", async Task<Response> (T item) => await commands.UpdateItemAsync(item).ConfigureAwait(false))
            .WithName($"Update{typeName}")
            .WithOpenApi();

        group.MapPost("/", async Task<Response<T>> (T item) => await commands.CreateItemAsync(item).ConfigureAwait(false))
            .WithName($"Create{typeName}")
            .WithOpenApi();

        group.MapDelete("/", async Task<Response> ([FromBody] T item) => await commands.DeleteItemAsync(item).ConfigureAwait(false))
            .WithName($"Delete{typeName}")
            .WithOpenApi();

        return group;
    }
}