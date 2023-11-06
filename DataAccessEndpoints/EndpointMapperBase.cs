using BaseLib;
using DataAccess.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EndpointMapper; 

public abstract class EndpointMapperBase {
    protected static (IEndpointRouteBuilder group, ICrud<T> crud) MapCrudEndpoint<T>(IEndpointRouteBuilder routeBuilder, IDataService dataService, ICrud<T>? crud = null) where T : class {
        var typeName = typeof(T).Name;
        crud ??= dataService.GetCrud<T>();
        var group = routeBuilder.MapGroup($"/{typeName}").WithTags(typeName);
        group.MapGet("/",
                async Task<Response<T>> (string? filterJson, int? pageSize, int? pageNumber) =>
                    await crud.GetAllAsync(filterJson, pageSize ?? 0, pageNumber ?? 0).ConfigureAwait(false))
            .WithName($"Get{typeName}")
            .WithOpenApi();

        group.MapGet("/{PrimaryKeyValue}", async Task<Response<T>> (string pkValue) => await crud.GetByPkAsync(pkValue).ConfigureAwait(false))
            .WithName($"Get{typeName}ByPk")
            .WithOpenApi();

        group.MapPut("/", async Task<Response> (T item) => await crud.UpdateItemAsync(item).ConfigureAwait(false))
            .WithName($"Update{typeName}")
            .WithOpenApi();

        group.MapPost("/", async Task<Response<T>> (T item) => await crud.CreateItemAsync(item).ConfigureAwait(false))
            .WithName($"Create{typeName}")
            .WithOpenApi();

        group.MapDelete("/", async Task<Response> ([FromBody] T item) => await crud.DeleteItemAsync(item).ConfigureAwait(false))
            .WithName($"Delete{typeName}")
            .WithOpenApi();

        if (typeof(T).IsAssignableTo(typeof(ISpecializedCrud))) {
            foreach (var (endPoint, handler, name) in ((ISpecializedCrud) crud).GetEndPoints()) {
                group.MapGet(endPoint, handler)
                    .WithName(name)
                    .WithOpenApi();
            }
        }

        return (group, crud);
    }
}