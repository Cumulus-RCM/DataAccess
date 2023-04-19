using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccess.Shared;

////
////https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/
///
///
/// 
[DefaultProperty(nameof(Value))]
public abstract record Enumeration {
    [JsonIgnore] public string Name { get; protected init; } = "";
    [JsonIgnore] public string DisplayName { get; protected init; } = "";
    public int Value { get; protected init; }

    public override string ToString() => DisplayName;

    private static Dictionary<Type, List<Enumeration>> enums = new();

    protected Enumeration() { }
    protected Enumeration(Type type, int value, string displayName) {
        Value = value;
        DisplayName = displayName;
        addToEnumDictionary(type, this);
    }

    private static void addToEnumDictionary(Type type, Enumeration enumeration) {
        if (!enums.ContainsKey(type)) enums.Add(type, new List<Enumeration>());
        enums[type].Add(enumeration);
    }

    public static List<T> GetAll<T>() where T : Enumeration => enums[typeof(T)].Cast<T>().ToList();

     public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue) {
        var absoluteDifference = Math.Abs(firstValue.Value - secondValue.Value);
        return absoluteDifference;
    }

    public static T FromValue<T>(int value) where T : Enumeration, new() {
        var matchingItem = parse<T, int>(value, "value", item => item.Value == value);
        return matchingItem;
    }

    public static T? TryFromValue<T>(int value) where T : Enumeration, new() => GetAll<T>().FirstOrDefault(item => item.Value == value);

    public static T FromDisplayName<T>(string displayName) where T : Enumeration, new() {
        var matchingItem = parse<T, string>(displayName, "display name", item => item.DisplayName == displayName);
        return matchingItem;
    }

    public static bool ContainsValue<T>(int value) where T : Enumeration, new() =>
        GetAll<T>().FirstOrDefault(x => x.Value == value) is not null;

    private static T parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration, new() {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);
        if (matchingItem is null) {
            var message = $"'{value}' is not a valid {description} in {typeof(T)}";
            throw new ApplicationException(message);
        }
        return matchingItem;
    }
}

public class EnumerationJsonConverter<T> : JsonConverter<T> where T : Enumeration {
    private static MethodInfo methodInfo;

    static EnumerationJsonConverter()     {
        methodInfo = typeof(Enumeration).GetMethod(nameof(Enumeration.TryFromValue), BindingFlags.Static | BindingFlags.Public)!;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var genericMethod = methodInfo.MakeGenericMethod(typeToConvert);
        var args = new object?[] {reader.GetInt32()};
        var result = genericMethod.Invoke(null, args);
        return (T?)result;
    }

    public override void Write(Utf8JsonWriter writer, T enumeration, JsonSerializerOptions options) =>
        writer.WriteNumberValue(enumeration.Value);

}
