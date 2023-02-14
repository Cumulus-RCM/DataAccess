using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using DataAccess.Shared.Enums;
using DataAccess.Shared.Helpers;

namespace DataAccess.Shared;

public record FilterExpression(string PropertyName, Operator Operator) {
    public string PropertyName { get; set; } = PropertyName;
    public Operator Operator { get; set; } = Operator;
    [JsonIgnore]
    public object Value {
        set => ValueString = value.ToString();
    }
    public string? ValueString { get; set; }
    public string ValueType { get; set; }
    protected FilterExpression() : this("", Operator.Contains) { }

   public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator oper) : base(propertyName, oper) {
        var propertyInfo = typeof(T).GetProperty(propertyName);
        if (propertyInfo is null) throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
        ValueType = propertyInfo.PropertyType.Name;
    }

    public FilterExpression() {}

    public static bool TryParse(string value, out FilterExpression<T>? result) {
        result = JsonSerializer.Deserialize<FilterExpression<T>>(value);
        return result is null;
    }

    public FilterExpression(Expression<Func<T, object>> propertyNameExpression, Operator oper) : this(MemberHelpers.GetMemberName(propertyNameExpression), oper) {
    }
 }

public record ConnectedExpression {
    public FilterExpression FilterExpression { get; set; }
    public AndOr AndOr { get; init; } = AndOr.And;

    public ConnectedExpression(FilterExpression filterExpression, AndOr? andOr = null) {
        this.FilterExpression = filterExpression;
        this.AndOr = andOr ?? AndOr.And;
    }

    public static bool TryParse(string json, out ConnectedExpression? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<ConnectedExpression>(json);
        return filterSegment is not null;
    }
}

public class FilterSegment {
    public List<ConnectedExpression> Expressions { get; set; } = new();

    public FilterSegment() { }

    public FilterSegment(FilterExpression filterExpression) {
        Expressions.Add( new ConnectedExpression(filterExpression));
    }

    public static bool TryParse(string json, out FilterSegment? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<FilterSegment>(json);
        return filterSegment is not null;
    }
}

public class ParameterValue {
    public string Name { get; set; }
    public string Value { get; set; }
    public string TypeName { get; set; }

    public ParameterValue() { }

    public ParameterValue(string name, string value, string typeName) {
        Name = name;
        Value = value;
        TypeName = typeName;
    }

    public static bool TryParse(string json, out ParameterValue? parameterValue) {
        parameterValue = JsonSerializer.Deserialize<ParameterValue>(json);
        return parameterValue is not null;
    }
}

public class ParameterValues  {
    private readonly List<ParameterValue> values = new();
    public IEnumerable<ParameterValue> Values => values;

    public ParameterValues() { }

    public ParameterValues(IEnumerable<ParameterValue> parameterValues) {
        values.AddRange(parameterValues);
    }

    public ParameterValues(ParameterValue parameterValue) {
        values.Add(parameterValue);
    }

    public void Add(ParameterValue parameterValue) {values.Add(parameterValue);}

    public void AddRange(IEnumerable<ParameterValue> parameterValues) {
        values.AddRange(parameterValues);
    }

    public DynamicParameters ToDynamicParameters() {
        var d = values.Select(v => new KeyValuePair<string, object>(v.Name, convert(v))).ToDictionary(x=>x.Key, x=>x.Value);
        return new DynamicParameters(d);

        object convert(ParameterValue parameterValue) =>
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

public class Filter {
    public List<FilterSegment> Segments { get; set; } = new();
   
    public Filter() { }

    public static Filter? FromJson(string? json) => string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<Filter>(json);

    public string AsJson() => JsonSerializer.Serialize(this);

    public Filter(FilterExpression filterExpression) {
        Segments.Add(new FilterSegment(filterExpression));
    }

    public string PrimaryExpressionPropertyName() => Segments.First().Expressions.First().FilterExpression.PropertyName;

    public void SetParameterValue(string propertyName, string value) {
        var exp = Segments.SelectMany(s => s.Expressions).FirstOrDefault(e => e.FilterExpression.PropertyName == propertyName);
        if (exp is not null) exp.FilterExpression.ValueString = value;
    }

    public DynamicParameters GetDynamicParameters() {
        var expressions = Segments.SelectMany(s => s.Expressions).Where(e => e.FilterExpression.ValueString is not null);
        var d = expressions
            .Select(e => new KeyValuePair<string, object>(e.FilterExpression.PropertyName, e.FilterExpression.ValueString!))
            .ToDictionary(x => x.Key, x => x.Value);

        //var d = values.Select(v => new KeyValuePair<string, object>(v.Name, convert(v))).ToDictionary(x=>x.Key, x=>x.ValueString);
        return new DynamicParameters(d);

        object convert(ParameterValue parameterValue) =>
            parameterValue.TypeName switch {
                "string" => parameterValue.Value,
                "Int32" => int.Parse(parameterValue.Value),
                "decimal" => decimal.Parse(parameterValue.Value),
                _ => throw new NotImplementedException()
            };
    }

    public static bool TryParse(string json, out Filter? filter) {
        filter = JsonSerializer.Deserialize<Filter>(json);
        return filter is not null;
    }
}
