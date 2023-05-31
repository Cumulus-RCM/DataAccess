using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccess.Shared;

[JsonConverter(typeof(EnumerationJsonConverter<Operator>))]
public sealed record Operator : Enumeration {
    public static readonly Operator Equal = new(1, "=");
    public static readonly Operator NotEqual = new(2, "<>");
    public static readonly Operator LessThan = new(3, "<");
    public static readonly Operator GreatThan = new(4, ">");
    public static readonly Operator LessThanOrEqual = new(5, "<=");
    public static readonly Operator GreaterThanOrEqual = new(6, ">=");
    public static readonly Operator StartsWith = new(7, "like") {PostTemplate = "%"};
    public static readonly Operator Contains = new(8, "like") {PreTemplate = "%", PostTemplate = "%"};

    [JsonIgnore]
    public string PreTemplate { get; init; }= "";
    [JsonIgnore]
    public string PostTemplate { get; init; }= "";

    public Operator() { }
    public Operator(int value, string name) : base(typeof(Operator),value,name) { }
}