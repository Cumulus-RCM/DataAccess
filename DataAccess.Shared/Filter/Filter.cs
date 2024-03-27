using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using BaseLib;
using Dapper;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace DataAccess.Shared;

public class Filter {
    public List<FilterSegment> Segments { get; set; } = [];

    public Filter() { }

    public static Filter? FromJson(string? json) {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var filter = JsonSerializer.Deserialize<Filter>(json);
        var expressions = filter!.Segments.SelectMany(s => s.Expressions).ToList();
        foreach (var expression in expressions) {
            if (expression.FilterExpression.Value is JsonElement jsonElement) {
                var type = Type.GetType(expression.FilterExpression.ValueTypeName);
                if (type is not null) expression.FilterExpression.Value = JsonSerializer.Deserialize(jsonElement.GetRawText(), type);
            }
        }
        return filter;
    }


    //https://long2know.com/2016/10/building-linq-expressions-part-2/
    public Func<T, bool> ToLinqExpression<T>() {
        var firstExpression = Segments.SelectMany(s => s.Expressions).First();
        if (firstExpression.FilterExpression.Value is string filterStringValue) {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, firstExpression.FilterExpression.PropertyName);
            var value = filterStringValue.ToLower().Trim();
            var filterValue = Expression.Constant(value);
            var miTrim = typeof(string).GetMethod("Trim", Type.EmptyTypes);
            var miLower = typeof(string).GetMethod("ToLower", Type.EmptyTypes);

            var trimmed = Expression.Call(property, miTrim!);
            var lowered = Expression.Call(trimmed, miLower!);
            MethodInfo methodInfo;
            MethodCallExpression body;
            if (firstExpression.FilterExpression.Operator == Operator.In) {
                methodInfo = typeof(string).GetMethod("Contains", new[] {typeof(string)})!;
                body = Expression.Call(filterValue,methodInfo!, lowered);
            }
            else {
                methodInfo = typeof(string).GetMethod("StartsWith", new[] {typeof(string)})!;
                body = Expression.Call(lowered, methodInfo!, filterValue);
            }
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            return lambda.Compile();
        }

        var t = firstExpression.FilterExpression.Value?.GetType();
        if (t.IsNumeric()) {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, firstExpression.FilterExpression.PropertyName);
            //var converter = TypeDescriptor.GetConverter(t);
            //var numeric = converter.ConvertFrom(firstExpression.FilterExpression.ValueString!);
            var filterValue = Expression.Constant(firstExpression.FilterExpression.Value);
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

    private Filter(FilterSegment filterSegment) => Segments.Add(filterSegment);

    public string PrimaryExpressionPropertyName() => Segments.First().Expressions.First().FilterExpression.PropertyName;

 //   public string? PrimaryExpressionStringValue() => Segments.First().Expressions.First().FilterExpression.ValueString;

    //public void SetParameterValue(string propertyName, string value) {
    //    var exp = Segments.SelectMany(s => s.Expressions).FirstOrDefault(e => e.FilterExpression.PropertyName == propertyName);
    //    if (exp is not null) exp.FilterExpression.ValueString = value;
    //}

    public DynamicParameters GetDynamicParameters() {
        var expressions = Segments.SelectMany(s => s.Expressions);
        var parameters = new DynamicParameters();
        foreach (var expression in expressions) {
            parameters.Add(expression.FilterExpression.PropertyName, expression.FilterExpression.Value);
        }
        return parameters;
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
                filterSegment.AddExpression(new FilterExpression(prop.Name, Operator.Equal) {Value = value});
            }
        }

        return new Filter(filterSegment);
    }
}