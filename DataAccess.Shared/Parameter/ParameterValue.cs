using System;
using System.Data;
using System.Text.Json.Serialization;

namespace DataAccess.Shared;

public class ParameterValue(string name, string value, string typeName) {
    public string Name { get; init; } = name;
    public string Value { get; set; } = value;
    public string TypeName { get; init; } = typeName;

    [JsonIgnore]
    public DbType DbType => TypeName switch {
        "string" => DbType.String,
        "Int32" => DbType.Int32,
        "Int64" => DbType.Int64,
        "decimal" => DbType.Decimal,
        _ => throw new NotImplementedException()
    };

    public ParameterValue() : this("", "", "object") { }
    
    public static ParameterValue Create<T>(string name, T value) => new(name, value.ToString(), typeof(T).Name);
}