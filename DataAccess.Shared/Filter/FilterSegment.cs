using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DataAccess.Shared;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public record FilterSegment {
    //NOTE: setters are required for deserialization
    public List<ConnectedExpression> Expressions { get; set; } = [];
    public AndOr AndOr { get; set; } = AndOr.And; 

    public FilterSegment() { }

    public FilterSegment(FilterExpression filterExpression) {
        Expressions.Add(new ConnectedExpression(filterExpression, AndOr.And));
    }

    public static bool TryParse(string json, out FilterSegment? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<FilterSegment>(json);
        return filterSegment is not null;
    }
}


public record FilterSegment<T> : FilterSegment {
    public FilterSegment(AndOr? andOr = null) {
        AndOr = andOr ?? AndOr.And;
    }

    public FilterSegment(FilterExpression<T> filterExpression, AndOr? andOr = null) {
        AddExpression(new ConnectedExpression<T>(filterExpression, AndOr.And));
        AndOr = andOr ?? AndOr.And;
    }

    public FilterSegment(IEnumerable<ConnectedExpression<T>> filterExpressions, AndOr? andOr = null) {
        Expressions.AddRange(filterExpressions);
        AndOr = andOr ?? AndOr.And;
    }

    public void AddExpression(ConnectedExpression<T> filterExpression) {
        Expressions.Add(filterExpression);
    }

    public static bool TryParse(string json, out FilterSegment<T>? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<FilterSegment<T>>(json);
        return filterSegment is not null;
    }
}