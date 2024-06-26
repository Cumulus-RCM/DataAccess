using System;
using System.Linq.Expressions;
using System.Text.Json;

namespace DataAccess.Shared;

public record FilterExpression(string PropertyName, Operator Operator) {
    private object? val;
    public object? Value { 
        get => val;
        set {
            val = value;
            setValueTypeName(value);
        }
    }

    private void setValueTypeName(object? value) {
        if (value is null) return;
        var t = value.GetType();
        if (t != typeof(JsonElement)) ValueTypeName = t.AssemblyQualifiedName!;
    }

    public string ValueTypeName { get; set; } = "";

    public string Name { get; init; } = PropertyName;

    protected FilterExpression() : this("", Operator.Contains) { }

    public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is not null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator op, object? value = null) : base(propertyName, op) {
        //ensure property exists on T
        _ = typeof(T).GetProperty(propertyName) ??
                           throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
        if (value is not null) Value = value;
    }

    public FilterExpression() { }

    public static bool TryParse(string value, out FilterExpression<T>? result) {
        result = JsonSerializer.Deserialize<FilterExpression<T>>(value);
        return result is not null;
    }

    public FilterExpression(Expression<Func<T, object>> propertyNameExpression, Operator op, object? value = null) :
        this(MemberHelpers.GetMemberName(propertyNameExpression), op, value) {
        if (value is not null) Value = value;
    }
}