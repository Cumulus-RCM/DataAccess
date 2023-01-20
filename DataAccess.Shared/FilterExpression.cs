using System.Linq.Expressions;
using System.Text.Json;
using DataAccess.Shared.Helpers;

namespace DataAccess.Shared;

public class Filter<T> {
    public IEnumerable<FilterExpression<T>> Expressions => expressions;
    private List<FilterExpression<T>> expressions { get; } = new();

    public Filter() { }

    public Filter(FilterExpression<T> expression) => Add(expression);

    public Filter(Expression<Func<T,object>> propertyName, Operator oper) {
        var fe = new FilterExpression<T>(propertyName, oper);
        Add(fe);
    }

    public Filter(string propertyName, Operator oper) {
        var fe = new FilterExpression<T>(propertyName, oper);
        Add(fe);
    }

    public void Add(FilterExpression<T> filterExpression) {
       expressions.Add(filterExpression);
    }
    public override string ToString() => string.Join("", expressions.Select(e=>e.ToString()));

    public static bool TryParse(string value, out Filter<T>? result) {
        result = JsonSerializer.Deserialize<Filter<T>>(value);
        return result is null;
    }
}

public record FilterExpression(string PropertyName, Operator Operator) {
    public string PropertyName { get; protected set; } = PropertyName;
    public Operator Operator { get; protected set; } = Operator;

    public override string ToString() => $"{PropertyName} {Operator.DisplayName} {Operator.PreTemplate}@{PropertyName}{Operator.PostTemplate}";

    public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator oper) : base(propertyName, oper) {
      if (typeof(T).GetProperty(propertyName) is null) throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
    }

    public FilterExpression(Expression<Func<T,object>> propertyName, Operator oper) : base(MemberHelpers.GetMemberName(propertyName), oper){ }

    public override string ToString() => $"{PropertyName} {Operator.DisplayName} {Operator.PreTemplate}@{PropertyName}{Operator.PostTemplate}";

}
