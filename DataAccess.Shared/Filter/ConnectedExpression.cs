using System.Text.Json;

namespace DataAccess.Shared;

public record ConnectedExpression {
    public FilterExpression FilterExpression { get; set; }
    public AndOr AndOr { get; init; } = AndOr.And;

    public ConnectedExpression(FilterExpression filterExpression, AndOr andOr) {
        this.FilterExpression = filterExpression;
        this.AndOr = andOr;
    }

    public static bool TryParse(string json, out ConnectedExpression? filterSegment) {
        filterSegment = JsonSerializer.Deserialize<ConnectedExpression>(json);
        return filterSegment is not null;
    }
}

public record ConnectedExpression<T> : ConnectedExpression {
    public ConnectedExpression(FilterExpression<T> filterExpression, AndOr andOr) : base(filterExpression, andOr) { }
}