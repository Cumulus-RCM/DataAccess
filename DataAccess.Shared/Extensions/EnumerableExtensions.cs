namespace DataAccess.Shared.Extensions; 

public static class EnumerableExtensions {
    public static IEnumerable<T> ItemAsEnumerable<T>(this T? item) => item is null ? Enumerable.Empty<T>() : new [] {item};

    public static IReadOnlyCollection<T> ItemAsReadOnlyCollection<T>(this T item) {
        return new List<T>(item.ItemAsEnumerable());
    }
}