using System.Collections.Generic;
using System.Text.Json;

namespace DataAccess.Shared;

public record FilterSegment {
    //NOTE: setters are required for deserialization
    public AndOr AndOr { get; set; } = AndOr.And;

    public Dictionary<string, ConnectedExpression> FilterExpressions { get; private init; } = [];

    public FilterSegment() { }

    public FilterSegment(FilterExpression filterExpression, AndOr? andOr = null) {
        AddExpression(filterExpression, andOr);
    }

    public void AddExpression(FilterExpression filterExpression, AndOr? andOr = null) {
        FilterExpressions.Add(filterExpression.Name, new ConnectedExpression(filterExpression, andOr ?? AndOr));
    }


    public void AddExpression(ConnectedExpression connectedExpression) {
        FilterExpressions.Add(connectedExpression.FilterExpression.Name, connectedExpression);
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
        foreach (var filterExpression in filterExpressions) {
            FilterExpressions.Add(filterExpression.FilterExpression.Name, filterExpression);
        }

        AndOr = andOr ?? AndOr.And;
    }

    public void AddExpression(ConnectedExpression<T> filterExpression) {
        FilterExpressions.Add(filterExpression.FilterExpression.Name, filterExpression);
    }

    public static bool TryParse(string json, out FilterSegment<T>? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<FilterSegment<T>>(json);
        return filterSegment is not null;
    }
}