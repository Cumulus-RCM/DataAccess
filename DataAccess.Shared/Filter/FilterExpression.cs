using System;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccess.Shared;

public record FilterExpression(string PropertyName, Operator Operator) {
    public string PropertyName { get; set; } = PropertyName;
    public Operator Operator { get; set; } = Operator;

    [JsonIgnore]
    public object Value {
        set => ValueString = value.ToString();
    }

    public string? ValueString { get; set; }

    [JsonIgnore] public Type ValueType => Type.GetType(ValueTypeName ?? typeof(object).FullName!)!;

    public string? ValueTypeName { get; set; }

    protected FilterExpression() : this("", Operator.Contains) { }

    public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is not null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator oper, object? value = null) : base(propertyName, oper) {
        //ensure property exists on T
        var propertyInfo = typeof(T).GetProperty(propertyName) ??
                           throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
        PropertyName = propertyName;
        Operator = oper;
        ValueTypeName = propertyInfo.PropertyType.FullName;
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