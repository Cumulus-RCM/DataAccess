using System.Linq.Expressions;
using System;
using System.Text.Json;

namespace DataAccess.Shared;

public record ConnectedExpression {
    public FilterExpression FilterExpression { get; set; } = null!;
    public AndOr AndOr { get; init; } = AndOr.And;

    public ConnectedExpression() { }

    public ConnectedExpression(FilterExpression filterExpression, AndOr? andOr = null) {
        this.FilterExpression = filterExpression;
        this.AndOr = andOr ?? AndOr.And;
    }

    public ConnectedExpression(string propertyName, Operator op, AndOr? andOr = null) 
        : this(new FilterExpression(propertyName, op), andOr) { }

    public static bool TryParse(string json, out ConnectedExpression? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<ConnectedExpression>(json);
        return filterSegment is not null;
    }
}

public record ConnectedExpression<T> : ConnectedExpression {
    public ConnectedExpression(FilterExpression<T> filterExpression, AndOr andOr, object? value = null) : base(filterExpression, andOr) {
        if (value is not null) FilterExpression.Value = value;
    }

    public ConnectedExpression(string propertyName, Operator op, AndOr andOr, object? value = null)
        : base(new FilterExpression<T>(propertyName, op, value),  andOr) { }

    public ConnectedExpression(Expression<Func<T, object>> propertyNameExpression, Operator op, AndOr andOr, object? value = null)
    : base(new FilterExpression<T>(propertyNameExpression, op,value), andOr) { }
}