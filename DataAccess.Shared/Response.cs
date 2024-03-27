using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib;

namespace DataAccess.Shared;

public record Response(Exception? Exception = null) {
    public bool IsSuccess => Exception is null;
}

public record Response<T> : Response {
    public IReadOnlyCollection<T> Items { get; init; }
    public int TotalCount { get; init; }

    public Response() : this(new List<T>().AsReadOnly()) {}
    public Response(T value) : this(value.ItemAsReadOnlyCollection(), value.IsNullOrZero() ? 0 : 1, value.IsNullOrZero() ? "value can not be null." : "") { }
    public Response(IReadOnlyCollection<T> items, int totalCount = 0, string? errorMessage = null, Exception? exception = null) {
        this.Items = items;
        this.TotalCount = totalCount;
        Exception = exception;
        if (!string.IsNullOrWhiteSpace(errorMessage) && exception is null) Exception = new InvalidOperationException(errorMessage);
    }

    private Response(Exception? exception = null) : base(exception) {
        Items = new List<T>().AsReadOnly();
    }

    public static Response<T> Empty(string errorMessage = "") => new(new List<T>().AsReadOnly(), errorMessage: errorMessage);

    public static Response<T> Fail(string errorMessage) => new Response<T>(new InvalidOperationException(errorMessage));
    public static Response<T> Fail(Exception ex) => new(ex);

    public T GetItem() => Items.Single();
}

public static class ResponseExtensions {
    public static Response<T> AsResponse<T>(this T value) => new(value);
}