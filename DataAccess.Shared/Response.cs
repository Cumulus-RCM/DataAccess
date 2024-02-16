using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib;

namespace DataAccess.Shared;

public record Response(Exception? Exception = null) {
    public bool Success => Exception is not null;

    public static Response Failed(string exceptionMessage) => 
        new Response(new InvalidOperationException(exceptionMessage));
}

public record Response<T>(IReadOnlyCollection<T> Items, int TotalCount = 0, string ErrorMessage = "") : Response {
    public Response() : this(new List<T>().AsReadOnly()) {}

    public Response(T value) : this(value.ItemAsReadOnlyCollection(), value.IsNullOrZero() ? 0 : 1, value.IsNullOrZero() ? "value can not be null." : "") { }

    public static Response<T> Empty(string errorMessage = "") => new(new List<T>().AsReadOnly(), ErrorMessage: errorMessage);

    public static Response<T> Fail(string errorMessage) => new(new List<T>().AsReadOnly(), ErrorMessage: errorMessage);

    public T GetItem() => Items.Single();
}

public static class ResponseExtensions {
    public static Response<T> AsResponse<T>(this T value) => new(value);
}