using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using BaseLib;
using Dapper;
// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace DataAccess.Shared;

public class Filter {
    public List<FilterSegment> Segments { get; set; } = [];

    public Filter() { }

    public static Filter? FromJson(string? json) => string.IsNullOrWhiteSpace(json)
        ? null
        : JsonSerializer.Deserialize<Filter>(json);


    //https://long2know.com/2016/10/building-linq-expressions-part-2/
    public Func<T, bool> ToLinqExpression<T>() {
        var firstExpression = Segments.SelectMany(s => s.Expressions).First();
        if (firstExpression.FilterExpression.Operator == Operator.In) return _ => true;
        if (firstExpression.FilterExpression.ValueType == typeof(string)) {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, firstExpression.FilterExpression.PropertyName);
            var value = firstExpression.FilterExpression.ValueString?.ToLower().Trim();
            var filterValue = Expression.Constant(value);
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
                filterSegment.AddExpression(new FilterExpression(prop.Name, Operator.Equal) { Value = value });
            }
        }

        return new Filter(filterSegment);
    }
}