using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace DataAccess.Shared;

public class ParameterValues  {
    [JsonInclude]
    private List<ParameterValue> values = [];
    [JsonIgnore]
    public IReadOnlyCollection<ParameterValue> Values => values;
    
    [JsonIgnore]
    public int Count => values.Count;

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

    public DynamicParameters AddToDynamicParameters(DynamicParameters? dynamicParameters) {
        if (dynamicParameters is null) return this.ToDynamicParameters();
        foreach (var pv in values) {
            if (!dynamicParameters.ParameterNames.Contains(pv.Name))
              dynamicParameters.Add(name:pv.Name, value:pv.Value, dbType:pv.DbType);
        }
        return dynamicParameters;
    }

    
    public DynamicParameters ToDynamicParameters() {
        var d = values.Select(v => new KeyValuePair<string, object>(v.Name, convert(v))).ToDictionary(x => x.Key, x => x.Value);
        return new DynamicParameters(d);

        static object convert(ParameterValue parameterValue) =>
            parameterValue.TypeName switch {
                "string" => parameterValue.Value,
                "Int32" => int.Parse(parameterValue.Value),
                "Int64" => long.Parse(parameterValue.Value),
                "decimal" => decimal.Parse(parameterValue.Value),
                _ => throw new NotImplementedException()
            };
    }

    public static ParameterValues? FromJson(string? json) =>
        json is null
            ? null
            : JsonSerializer.Deserialize<ParameterValues>(json);

    public static string? AsJson(ParameterValues? parameterValues) {
        return parameterValues is null
            ? null
            : JsonSerializer.Serialize(parameterValues);
    }
}

public static class ParameterValuesExtensions {
    public static ParameterValues ToParameterValues(this IEnumerable<ParameterValue> parameterValues) => new ParameterValues(parameterValues);
    public static ParameterValues ToParameterValues(this ParameterValue parameterValue) => new ParameterValues(parameterValue);
    public static string AsJson(this ParameterValues parameterValues) => ParameterValues.AsJson(parameterValues)!;
    public static DynamicParameters AddToDynamicParameters(this ParameterValues parameterValues, DynamicParameters? otherDynamicParameters) => 
        parameterValues.AddToDynamicParameters(otherDynamicParameters);
}