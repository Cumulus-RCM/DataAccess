using System.Text.Json.Serialization;
using BaseLib;

namespace DataAccess.Shared;

[JsonConverter(typeof(EnumerationJsonConverter))]
public sealed class AndOr : Enumeration {
    public static readonly AndOr And = new AndOr(1, "AND");
    public static readonly AndOr Or = new AndOr(2, "OR");

    public AndOr() { }
    public AndOr(int value, string name) : base(value,name) { }
}
