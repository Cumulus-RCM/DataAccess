using System.Text.Json.Serialization;

namespace DataAccess.Shared;

[JsonConverter(typeof(EnumerationJsonConverter<AndOr>))]
public sealed record AndOr : Enumeration {
    public static readonly AndOr And = new AndOr(1, "AND");
    public static readonly AndOr Or = new AndOr(2, "OR");

    public AndOr() { }
    public AndOr(int value, string name) : base(typeof(AndOr), value,name) { }
}
