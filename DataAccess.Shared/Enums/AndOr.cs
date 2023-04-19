using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccess.Shared;

[JsonConverter(typeof(AndOrJsonConverter))]
public sealed record AndOr : Enumeration {
    public static readonly AndOr And = new AndOr(1, "AND");
    public static readonly AndOr Or = new AndOr(2, "OR");

    public AndOr() { }
    public AndOr(int value, string name) : base(typeof(AndOr), value,name) { }
}

public class AndOrJsonConverter : JsonConverter<AndOr> {
    public override AndOr? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
        Enumeration.TryFromValue<AndOr>(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, AndOr andOr, JsonSerializerOptions options) =>
        writer.WriteNumberValue(andOr.Value);
}
