using System;
using System.Linq.Expressions;
using System.Text.Json;

namespace DataAccess.Shared;

public record FilterExpression(string PropertyName, Operator Operator) {
    public string PropertyName { get; set; } = PropertyName;
    public Operator Operator { get; set; } = Operator;

    private object? _value;
    public object? Value { 
        get => _value;
        set {
            _value = value;
            if (value is not null) ValueTypeName = value.GetType().AssemblyQualifiedName!;
        }
    }

    public string ValueTypeName { get; set; } = "";

    protected FilterExpression() : this("", Operator.Contains) { }

    public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is not null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator oper, object? value = null) : base(propertyName, oper) {
        //ensure property exists on T
        _ = typeof(T).GetProperty(propertyName) ??
                           throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
        PropertyName = propertyName;
        Operator = oper;
        if (value is not null) Value = value;
    }

    public FilterExpression() { }

    public static bool TryParse(string value, out FilterExpression<T>? result) {
        result = JsonSerializer.Deserialize<FilterExpression<T>>(value);
        return result is not null;
    }

    public FilterExpression(Expression<Func<T, object>> propertyNameExpression, Operator oper, object? value = null) :
        this(MemberHelpers.GetMemberName(propertyNameExpression), oper, value) {
        if (value is not null) Value = value;
    }
}