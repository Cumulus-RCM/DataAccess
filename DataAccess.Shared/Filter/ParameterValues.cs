using System.Text.Json;
using Dapper;

namespace DataAccess.Shared;

public class ParameterValues {
    private readonly List<ParameterValue> values = [];
    public IEnumerable<ParameterValue> Values => values;

    public ParameterValues() { }

    public ParameterValues(IEnumerable<ParameterValue> parameterValues) {
        values.AddRange(parameterValues);
    }

    public ParameterValues(ParameterValue parameterValue) {
        values.Add(parameterValue);
    }

    public void Add(ParameterValue parameterValue) {
        values.Add(parameterValue);
    }

    public void AddRange(IEnumerable<ParameterValue> parameterValues) {
        values.AddRange(parameterValues);
    }

    public DynamicParameters ToDynamicParameters() {
        var d = values.Select(v => new KeyValuePair<string, object>(v.Name, convert(v))).ToDictionary(x => x.Key, x => x.Value);
        return new DynamicParameters(d);

        static object convert(ParameterValue parameterValue) =>
            parameterValue.TypeName switch {
                "string" => parameterValue.Value,
                "Int32" => int.Parse(parameterValue.Value),
                "decimal" => decimal.Parse(parameterValue.Value),
                _ => throw new NotImplementedException()
            };
    }

    public static bool TryParse(string json, out ParameterValues? parameterValues) {
        parameterValues = JsonSerializer.Deserialize<ParameterValues>(json);
        return parameterValues is not null;
    }
}