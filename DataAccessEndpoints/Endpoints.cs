using DataAccess.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataAccessEndpoints; 

public abstract class Endpoints {
    protected static (IEndpointRouteBuilder group, ICrud<T> crud) MapCrudEndpoints<T>(IEndpointRouteBuilder routeBuilder, IDataService dataService) where T : class {
        var typeName = typeof(T).Name;
        var crud = dataService.GetCrud<T>();
        var group = routeBuilder.MapGroup($"/{typeName}").WithTags(typeName);
        group.MapGet("/",
                async Task<Response<T>> (string? filterJson, int? pageSize, int? pageNumber) =>
                    await crud.GetAllAsync(filterJson, pageSize ?? 0, pageNumber ?? 1).ConfigureAwait(false))
            .WithName($"Get{typeName}")
            .WithOpenApi();

        group.MapGet("/{pk}", async Task<Response<T>>(object pk) => await crud.GetByPkAsync(pk).ConfigureAwait(false))
            .WithName($"Get{typeName}ByPk")
            .WithOpenApi();

        group.MapPut("/", async Task<Response>(T item) => await crud.UpdateItemAsync(item).ConfigureAwait(false))
            .WithName($"Update{typeName}")
            .WithOpenApi();

        group.MapPost("/", async Task<Response<T>>(T item) => await crud.CreateItemAsync(item).ConfigureAwait(false))
            .WithName($"Create{typeName}")
            .WithOpenApi();

        group.MapDelete("/", async Task<Response>(T item) => await crud.DeleteItemAsync(item).ConfigureAwait(false))
            .WithName($"Delete{typeName}")
            .WithOpenApi();

        return (group, crud);
    }
}