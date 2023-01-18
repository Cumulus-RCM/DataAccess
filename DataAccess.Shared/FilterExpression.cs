using System.Linq.Expressions;
using DataAccess.Shared.Helpers;

namespace DataAccess.Shared;

public class Filter<T> {
    public IEnumerable<FilterExpression<T>> Expressions => expressions;
    private List<FilterExpression<T>> expressions { get; } = new();

    public Filter() { }

    public Filter(FilterExpression<T> expression) => Add(expression);

    public Filter(Expression<Func<T,object>> propertyName, Operator oper, string? value = null) {
        var fe = new FilterExpression<T>(propertyName, oper, value);
        Add(fe);
    }

    public Filter(string propertyName, Operator oper, string? value=null) {
        var fe = new FilterExpression<T>(propertyName, oper, value);
        Add(fe);
    }

    public void Add(FilterExpression<T> filterExpression) {
       expressions.Add(filterExpression);
    }
    public override string ToString() => string.Join("", expressions.Select(e=>e.ToString()));
}

public record FilterExpression<T> {
    public string PropertyName { get; }
    public Operator Operator { get; }
    public object? Value { get; }

    private FilterExpression(string propertyName, Operator oper, bool skipCheck, object? value) {
        if (!skipCheck) if (typeof(T).GetProperty(propertyName) is null) throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
        PropertyName = propertyName;
        Operator = oper;
        Value = value;
    }

    public FilterExpression(string propertyName, Operator oper, object? value = null) : this(propertyName, oper, false, value) { }

    public FilterExpression(Expression<Func<T,object>> propertyName, Operator oper, object? value = null) : this(MemberHelpers.GetMemberName(propertyName), oper, true, value){ }

    public override string ToString() {
        return Value is null
            ? $"{PropertyName} {Operator.DisplayName} @{PropertyName}"
            : $"{PropertyName} {Operator.DisplayName} {Value}";
    }
}
