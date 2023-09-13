using System.Linq.Expressions;
using System.Text.Json;

namespace DataAccess.Shared;

public record class OrderByExpression {
    public string PropertyName { get; }
    public OrderDirection OrderDirection { get; }

    public OrderByExpression(string propertyName, OrderDirection? orderDirection = null) {
        PropertyName = propertyName;
        OrderDirection = orderDirection ?? OrderDirection.Ascending;
    }
}

public record OrderByExpression<T> : OrderByExpression {
    public OrderByExpression(string propertyName, OrderDirection? dir = null) : base(propertyName, dir) {
        var _ = typeof(T).GetProperty(propertyName) ?? throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
    }

    public OrderByExpression(Expression<Func<T, object>> propertyNameExpression, OrderDirection? orderDirection = null) : 
        this(MemberHelpers.GetMemberName(propertyNameExpression), orderDirection) {
    }


    public static bool TryParse(string value, out OrderByExpression? result) {
        result = JsonSerializer.Deserialize<OrderByExpression<T>>(value);
        return result is null;
    }
}