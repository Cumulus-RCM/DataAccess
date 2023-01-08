namespace DataAccess.Extensions;

public static class EnumerableExt {
    public static IEnumerable<T> ItemAsEnumerable<T>(this T? item) {
        yield return item!;
    }
}
