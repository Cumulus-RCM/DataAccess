using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccess.Shared.Enums;

[JsonConverter(typeof(OperatorJsonConverter))]
public sealed class Operator : Enumeration {
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
    public Operator(int value, string name) : base(value,name) { }

    //public static bool TryParse(string value, out Operator? result) {
    //    if (int.TryParse(value, out var val)) {
    //        result = TryFromValue<Operator>(val);
    //        return result is not null;
    //    }

    //    result = null;
    //    return false;
    //}
}

//public class OperatorJsonConverter : Newtonsoft.Json.JsonConverter<Operator> {
//    public override void WriteJson(JsonWriter writer, Operator? value, JsonSerializer serializer) {
//        if (value is null) writer.WriteNull();
//        else writer.WriteValue(value.DisplayName);
//    }

//    public override Operator? ReadJson(JsonReader reader, Type objectType, Operator? existingValue, bool hasExistingValue, JsonSerializer serializer) {
//        var x = reader.ReadAsInt32();
//        return Enumeration.TryFromValue<Operator>(x.Value);
//        //return reader.TokenType switch
//        //{
//        //    JsonToken.Integer => Enumeration.FromValue<Operator>(reader.Value),
//        //    JsonToken.String => GetEnumerationFromJson(reader.Value.ToString(), objectType),
//        //    JsonToken.Null => null,
//        //    _ => throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing an enumeration")

//    }
//}

public class OperatorJsonConverter : JsonConverter<Operator> {
    public override Operator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => 
        Enumeration.TryFromValue<Operator>(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, Operator oper, JsonSerializerOptions options) =>
            writer.WriteNumberValue(oper.Value);
}
