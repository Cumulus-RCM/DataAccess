using System;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public string Name { get; set; } = PropertyName;

    [JsonInclude]
    private string? alias;
    [JsonIgnore]
    public string Alias {
        get => alias is null ? "" : $"{alias}.";
        init => alias = value;
    }

    protected FilterExpression() : this("", Operator.Contains) { }

    public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is not null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator op, object? value = null, string? name = null) : base(propertyName, op) {
        //ensure property exists on T
        _ = typeof(T).GetProperty(propertyName) ??
            throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
        if (value is not null) Value = value;
        if (name is not null) Name = name;
    }

    public FilterExpression() { }

    public static bool TryParse(string value, out FilterExpression<T>? result) {
        result = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try {
            result = JsonSerializer.Deserialize<FilterExpression<T>>(value);
            if (result?.Value is JsonElement jsonElement) {
                var type = Type.GetType(result.ValueTypeName);
                if (type is not null) result.Value = JsonSerializer.Deserialize(jsonElement.GetRawText(), type);
            }

            return result is not null;
        }
        catch (Exception) {
            return false;
        }
    }

    public FilterExpression(Expression<Func<T, object>> propertyNameExpression, Operator op, object? value = null, string? name = null) :
        this(MemberHelpers.GetMemberName(propertyNameExpression), op, value) {
        if (value is not null) Value = value;
        if (name is not null) Name = name;
    }

    public FilterExpression(Expression<Func<T, bool>> linqExpression) {
        if (!(linqExpression.Body is BinaryExpression {Left: MemberExpression memberExpression} binaryExpression))
            throw new ArgumentException("The expression body must be a BinaryExpression with a MemberExpression operand.", nameof(linqExpression));

        PropertyName = memberExpression.Member.Name;
        Operator = binaryExpression.NodeType switch {
            ExpressionType.Equal => Operator.Equal,
            ExpressionType.NotEqual => Operator.NotEqual,
            ExpressionType.GreaterThan => Operator.GreaterThan,
            ExpressionType.GreaterThanOrEqual => Operator.GreaterThanOrEqual,
            ExpressionType.LessThan => Operator.LessThan,
            ExpressionType.LessThanOrEqual => Operator.LessThanOrEqual,
            _ => throw new ArgumentException("Unsupported operator", nameof(linqExpression))
        };

        Value = binaryExpression.Right switch {
            ConstantExpression constantExpression => constantExpression.Value,
            MemberExpression valueExpression => Expression.Lambda(valueExpression).Compile().DynamicInvoke(),
            _ => throw new ArgumentException("Invalid expression format", nameof(linqExpression))
        };
    }
}