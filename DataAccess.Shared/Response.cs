namespace DataAccess.Shared;

public record Response(bool Success, string ErrorMessage = "");

public record Response<T>(bool Success, IReadOnlyCollection<T> Items, int TotalCount = 0, string ErrorMessage = "") : Response(Success, ErrorMessage) {
    public T? Item => Items.FirstOrDefault();

    // ReSharper disable once StaticMemberInGenericType
    private static readonly HashSet<Type?> numericTypes = new() {
        typeof(decimal), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort)
    };
    private static readonly bool isNumeric = numericTypes.Contains(typeof(T)) || numericTypes.Contains(Nullable.GetUnderlyingType(typeof(T)));

    public Response() : this(true, new List<T>().AsReadOnly()) {}

    public Response(T value) : this(!isNullOrZero(value), value.ItemAsReadOnlyCollection(), isNullOrZero(value) ? 0 : 1) { }

    public Response(T value, bool success) : this(success, value.ItemAsReadOnlyCollection(), isNullOrZero(value) ? 0 : 1) { }

    public static Response<T> Empty(bool success = true, string errorMessage = "") => new(success, new List<T>().AsReadOnly());

    private static bool isNullOrZero(T? value) => value is null || (isNumeric && Convert.ToDouble(value) == 0);
}