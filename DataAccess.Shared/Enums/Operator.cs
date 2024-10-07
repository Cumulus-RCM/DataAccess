using System.Text.Json.Serialization;
using BaseLib;

namespace DataAccess.Shared;

[JsonConverter(typeof(EnumerationJsonConverter<Operator>))]
public sealed record Operator : Enumeration {
    public static readonly Operator Equal = new(1, "=", "Equal");
    public static readonly Operator NotEqual = new(2, "<>", "Not Equal"  );
    public static readonly Operator LessThan = new(3, "<", "Less Than");
    public static readonly Operator GreaterThan = new(4, ">", "Greater Than");
    public static readonly Operator LessThanOrEqual = new(5, "<=", "Less Than Or Equal");
    public static readonly Operator GreaterThanOrEqual = new(6, ">=", "Greater Than Or Equal");
    public static readonly Operator StartsWith = new(7, "like", "Starts With") { PreTemplate = "", PostTemplate = "%"}; //spaces to allow 2 Operators to have the same value: like
    public static readonly Operator EndsWith = new(8, "like", "Ends With") { PreTemplate = "%", PostTemplate = ""}; //spaces to allow 2 Operators to have the same value: like
    public static readonly Operator Contains = new(9, "like", "Contains") {PreTemplate = "%", PostTemplate = "%"};
    public static readonly Operator In = new(10, "IN", "In");
    public static readonly Operator IsNull = new(11, " IS NULL ", "is Null") {UsesValue = false};
    public static readonly Operator IsNotNull = new(12, " IS NOT NULL ", "is NOT Null") {UsesValue = false};
    public static readonly Operator IsTrue = new(13, " = 1 ", "is true") { UsesValue = false };
    public static readonly Operator IsFalse = new(14, " = 0 ", "is false") { UsesValue = false };

    [JsonIgnore]
    public string PreTemplate { get; private init; }= "";
    [JsonIgnore]
    public string PostTemplate { get; private init; } = "";

    [JsonIgnore] public string SqlOperator { get; private init; } = "";

    public bool UsesValue { get; private init; } = true;

    public Operator() { }

    public Operator(int value, string sqlOperator, string displayName) : base(typeof(Operator), value, displayName) {
        SqlOperator = sqlOperator;
    }
}