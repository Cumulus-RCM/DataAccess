using System;
using System.ComponentModel;
using System.Data;

namespace DataAccess.Shared;

public class ParameterValue(string name, string valueString, string typeName) {
    public string Name { get; init; } = name;
    public string ValueString { get; set; } = valueString;
    public string TypeName { get; init; } = typeName;

    public object GetValue() {
        var type = Type.GetType(TypeName) ?? throw new InvalidExpressionException($"Type {TypeName} not found.");
        var converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(typeof(string))) {
            var result = converter.ConvertFrom(ValueString);
            if (result is not null) return result;
        }
        throw new InvalidExpressionException($"{ValueString} could not be converted to {type} from String.");
    }

    //NOTE: DO NOT remove - needed for serialization
    public ParameterValue() : this("", "", "object") { }

    public static ParameterValue Create<T>(string name, T value) => new(name, value.ToString(), typeof(T).FullName);
}