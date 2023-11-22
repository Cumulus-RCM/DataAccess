using System.Text.Json;

namespace DataAccess.Shared;

public class ParameterValue(string name, string value, string typeName) {
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
    public string TypeName { get; set; } = typeName;

    public ParameterValue() : this("", "", "object") { }

    public static bool TryParse(string json, out ParameterValue? parameterValue) {
        parameterValue = JsonSerializer.Deserialize<ParameterValue>(json);
        return parameterValue is not null;
    }
}