using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using BaseLib;
using Dapper;

namespace DataAccess.Shared;

public record FilterExpression(string PropertyName, Operator Operator) {
    public string PropertyName { get; set; } = PropertyName;
    public Operator Operator { get; set; } = Operator;

    [JsonIgnore]
    public object Value {
        set => ValueString = value.ToString();
    }
    public string? ValueString { get; set; }

    [JsonIgnore]
    public Type ValueType => Type.GetType(ValueTypeName ?? typeof(object).FullName!)!;

    public string? ValueTypeName { get; set; }
    //NOTE: public setter is to get around the fact that the JsonSerializer does not deserialize non-public setters

    protected FilterExpression() : this("", Operator.Contains) { }

   public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is not null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator oper) : base(propertyName, oper) {
        var propertyInfo = typeof(T).GetProperty(propertyName);
        if (propertyInfo is null) throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
        ValueTypeName = propertyInfo.PropertyType.FullName;
    }

    public FilterExpression() {}

    public static bool TryParse(string value, out FilterExpression<T>? result) {
        result = JsonSerializer.Deserialize<FilterExpression<T>>(value);
        return result is null;
    }

    public FilterExpression(Expression<Func<T, object>> propertyNameExpression, Operator oper) : 
        this(MemberHelpers.GetMemberName(propertyNameExpression), oper) { }
 }

public record ConnectedExpression {
    public FilterExpression FilterExpression { get; set; }
    public AndOr AndOr { get; init; } = AndOr.And;

    public ConnectedExpression(FilterExpression filterExpression, AndOr andOr ) {
        this.FilterExpression = filterExpression;
        this.AndOr = andOr;
    }

    public static bool TryParse(string json, out ConnectedExpression? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<ConnectedExpression>(json);
        return filterSegment is not null;
    }
}

public class FilterSegment {
    public List<ConnectedExpression> Expressions { get; set; } = new();

    public FilterSegment() { }

    public FilterSegment(FilterExpression filterExpression,AndOr? andOr = null) {
       AddExpression(filterExpression, andOr);
    }

    public void AddExpression(FilterExpression filterExpression, AndOr? andOr = null) {
        Expressions.Add( new ConnectedExpression(filterExpression, andOr ?? AndOr.And));
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

    public ParameterValue() {
        Name = "";
        Value = "";
        TypeName = "object";
    }

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

public class Filter {
    public List<FilterSegment> Segments { get; set; } = new();
   
    public Filter() { }

    public static Filter? FromJson(string? json) => string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<Filter>(json);


    //https://long2know.com/2016/10/building-linq-expressions-part-2/
    public Func<T, bool> ToLinqExpression<T>() {
        var firstExpression = Segments.SelectMany(s => s.Expressions).First();
        if (firstExpression.FilterExpression.ValueType == typeof(string)) {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, firstExpression.FilterExpression.PropertyName);
            var filterValue = Expression.Constant(firstExpression.FilterExpression.ValueString);
            var miTrim = typeof(string).GetMethod("Trim", Type.EmptyTypes);
            var miLower = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            var miStartsWith = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            var trimmed = Expression.Call(property, miTrim!);
            var lowered = Expression.Call(trimmed, miLower!);
            var body = Expression.Call(lowered, miStartsWith!, filterValue);
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            return lambda.Compile();
        }
        if (firstExpression.FilterExpression.ValueType.IsNumeric()) {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, firstExpression.FilterExpression.PropertyName);
            var converter = TypeDescriptor.GetConverter(firstExpression.FilterExpression.ValueType);
            var numeric = converter.ConvertFrom(firstExpression.FilterExpression.ValueString!);
            var filterValue = Expression.Constant(numeric);
            var body =  Expression.Equal(property, filterValue);
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            return lambda.Compile();
        }
        return _ => true;
    }

    public string AsJson() {
        try {
            return JsonSerializer.Serialize(this);
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }


    public static Filter CreateFilter<T>(FilterExpression<T> filterExpression) where T : class {
        var filter = new Filter();
        filter.Segments.Add(new FilterSegment(filterExpression));
        return filter;
    }

    private Filter(FilterSegment filterSegment) => Segments.Add(filterSegment);

    public string PrimaryExpressionPropertyName() => Segments.First().Expressions.First().FilterExpression.PropertyName;

    public string? PrimaryExpressionStringValue() => Segments.First().Expressions.First().FilterExpression.ValueString;

    public void SetParameterValue(string propertyName, string value) {
        var exp = Segments.SelectMany(s => s.Expressions).FirstOrDefault(e => e.FilterExpression.PropertyName == propertyName);
        if (exp is not null) exp.FilterExpression.ValueString = value;
    }

    public DynamicParameters GetDynamicParameters() {
        var expressions = Segments.SelectMany(s => s.Expressions).Where(e => e.FilterExpression.ValueString is not null);
        var d = expressions
            .Select(e => new KeyValuePair<string, object>(e.FilterExpression.PropertyName, e.FilterExpression.ValueString!))
            .ToDictionary(x => x.Key, x => x.Value);

        return new DynamicParameters(d);
    }

    public static bool TryParse(string json, out Filter? filter) {
        filter = JsonSerializer.Deserialize<Filter>(json);
        return filter is not null;
    }

    public Filter Merge(Filter filter) {
        foreach (var filterSegment in filter.Segments) {
            Segments.Add(filterSegment);
        }
        return this;
    }

    public static Filter FromEntity<T>(T item) where T : class {
        var type = typeof(T);
        var props = type.GetProperties();
        var filterSegment = new FilterSegment();
        foreach (var prop in props) {
            var value = prop.GetValue(item);
            if (value != null) {
                filterSegment.AddExpression(new FilterExpression(prop.Name,Operator.Equal) {Value=value});
            }
        }
        return new Filter(filterSegment);
    }
}
