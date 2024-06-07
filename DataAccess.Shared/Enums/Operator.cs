using System.Text.Json.Serialization;
using BaseLib;

namespace DataAccess.Shared;

[JsonConverter(typeof(EnumerationJsonConverter<Operator>))]
public sealed record Operator : Enumeration {
    public static readonly Operator Equal = new(1, "=");
    public static readonly Operator NotEqual = new(2, "<>");
    public static readonly Operator LessThan = new(3, "<");
    public static readonly Operator GreatThan = new(4, ">");
    public static readonly Operator LessThanOrEqual = new(5, "<=");
    public static readonly Operator GreaterThanOrEqual = new(6, ">=");
    public static readonly Operator StartsWith = new(7, " like ") { PreTemplate = "", PostTemplate = "%"}; //spaces to allow 2 Operators to have the same value: like
    public static readonly Operator Contains = new(8, "like") {PreTemplate = "%", PostTemplate = "%"};
    public static readonly Operator In = new(9, "IN");
    public static readonly Operator IsNull = new(10, " IS NULL ") {UsesValue = false};
    public static readonly Operator IsNotNull = new(11, " IS NOT NULL ") {UsesValue = false};

    [JsonIgnore]
    public string PreTemplate { get; init; }= "";
    [JsonIgnore]
    public string PostTemplate { get; init; }= "";

    public bool UsesValue { get; init; } = true;

    public Operator() { }
    public Operator(int value, string name) : base(typeof(Operator),value,name) { }
}