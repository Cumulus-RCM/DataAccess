using System.Collections.Generic;
using System.Text.Json;

namespace DataAccess.Shared;

public record FilterSegment {
    public List<ConnectedExpression> Expressions { get; set; } = [];

    public FilterSegment() { }

    public FilterSegment(FilterExpression filterExpression, AndOr? andOr = null) {
        AddExpression(filterExpression, andOr);
    }

    public void AddExpression(FilterExpression filterExpression, AndOr? andOr = null) {
        Expressions.Add(new ConnectedExpression(filterExpression, andOr ?? AndOr.And));
    }

    public static bool TryParse(string json, out FilterSegment? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<FilterSegment>(json);
        return filterSegment is not null;
    }
}


public record FilterSegment<T> : FilterSegment {
    public FilterSegment() { }

    public FilterSegment(FilterExpression<T> filterExpression, AndOr? andOr = null) => AddExpression(filterExpression, andOr);

    public FilterSegment(IEnumerable<ConnectedExpression<T>> filterExpressions) => Expressions.AddRange(filterExpressions);

    public void AddExpression(FilterExpression<T> filterExpression, AndOr? andOr = null) => Expressions.Add(new ConnectedExpression<T>(filterExpression, andOr ?? AndOr.And));

    public static bool TryParse(string json, out FilterSegment<T>? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<FilterSegment<T>>(json);
        return filterSegment is not null;
    }
}