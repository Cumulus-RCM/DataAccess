using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using BaseLib;

namespace DataAccess.Shared;

public class Filter {
    public List<FilterSegment> Segments { get; init; } = [];

    public Filter() {}

    public static Filter? FromJson(string? json) {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var filter = JsonSerializer.Deserialize<Filter>(json);
        var expressions = filter!.Segments.SelectMany(s => s.FilterExpressions).ToList();
        foreach (var expr in expressions) {
            if (expr.Value.FilterExpression.Value is JsonElement jsonElement) {
                var type = Type.GetType(expr.Value.FilterExpression.ValueTypeName);
                if (type is not null) expr.Value.FilterExpression.Value = JsonSerializer.Deserialize(jsonElement.GetRawText(), type);
            }
        }

        return filter;
    }

    //https://long2know.com/2016/10/building-linq-expressions-part-2/
    public Func<T, bool> ToLinqExpression<T>() {
        //Note: this only works for the first expression
        //Note: this only works for IN and StartsWith
        return _ => true;
        var firstExpression = Segments.SelectMany(s => s.FilterExpressions).First();
        var expr = firstExpression.Value.FilterExpression;
        if (expr.Value is string filterStringValue) {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, expr.PropertyName);
            var value = filterStringValue.ToLower().Trim();
            var filterValue = Expression.Constant(value);
            var miTrim = typeof(string).GetMethod("Trim", Type.EmptyTypes);
            var miLower = typeof(string).GetMethod("ToLower", Type.EmptyTypes);

            var trimmed = Expression.Call(property, miTrim!);
            var lowered = Expression.Call(trimmed, miLower!);
            MethodInfo methodInfo;
            MethodCallExpression body;
            if (expr.Operator == Operator.In) {
                methodInfo = typeof(string).GetMethod("Contains", [typeof(string)])!;
                body = Expression.Call(filterValue, methodInfo, lowered);
            }
            else if (expr.Operator == Operator.Equal) {
                methodInfo = typeof(string).GetMethod("Equal", [typeof(string)])!;
                body = Expression.Call(lowered, methodInfo, filterValue);
            }
            else {
                methodInfo = typeof(string).GetMethod("StartsWith", [typeof(string)])!;
                body = Expression.Call(lowered, methodInfo, filterValue);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            return lambda.Compile();
        }

        var t = expr.Value?.GetType();
        if (t.IsNumeric()) {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, expr.PropertyName);
            //var converter = TypeDescriptor.GetConverter(t);
            //var numeric = converter.ConvertFrom(firstExpression.FilterExpression.ValueString!);
            var filterValue = Expression.Constant(expr.Value);
            var body = Expression.Equal(property, filterValue);
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

    public static Filter Create<T>(IEnumerable<ConnectedExpression<T>> filterExpressions) =>
        new Filter(new FilterSegment<T>(filterExpressions));

    public static Filter Create<T>(FilterExpression<T> filterExpression, AndOr? andOr = null) where T : class =>
        new Filter(new FilterSegment<T>(filterExpression, andOr));

    public static Filter Create<T>(IEnumerable<FilterSegment<T>> filterSegments) {
        var filter = new Filter();
        filter.Segments.AddRange(filterSegments);
        return filter;
    }
    
    public static Filter Create(FilterSegment filterSegment) => new Filter(filterSegment);

    private Filter(FilterSegment filterSegment) => Segments.Add(filterSegment);

    public string PrimaryExpressionPropertyName() => Segments.First().FilterExpressions.First().Value.FilterExpression.PropertyName;

    public void SetParameterValue<T>(string? expressionName, T value) {
        if (expressionName is null) return;
        foreach (var segment in Segments) {
            if (segment.FilterExpressions.TryGetValue(expressionName, out var exp)) {
                exp.FilterExpression.Value = value;
                break;
            }
        }
    }

    public static bool TryParse(string json, out Filter? filter) {
        filter = JsonSerializer.Deserialize<Filter>(json);
        return filter is not null;
    }

    public Filter Merge(Filter? filter) {
        if (filter is not null) {
            foreach (var filterSegment in filter.Segments) {
                Segments.Add(filterSegment);
            }
        }

        return this;
    }

    public void AddSegment(FilterSegment filterSegment) => Segments.Add(filterSegment);

    public static Filter FromEntity<T>(T item) where T : class {
        var type = typeof(T);
        var props = type.GetProperties();
        var filterSegment = new FilterSegment<T>();
        foreach (var prop in props) {
            var value = prop.GetValue(item);
            if (value != null) {
                var exp = new FilterExpression<T>(prop.Name, Operator.Equal, value);
                filterSegment.AddExpression(new ConnectedExpression<T>(exp, AndOr.And));
            }
        }

        return new Filter(filterSegment);
    }
 }