using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;

namespace DataAccess.Shared;

public class OrderBy {
     public List<OrderByExpression> OrderByExpressions { get; set; } = [];

     public OrderBy() { }

     public OrderBy(OrderByExpression orderByExpression) {
         OrderByExpressions.Add(orderByExpression);
     }

     public OrderBy(IEnumerable<OrderByExpression> orderByExpressions) => OrderByExpressions.AddRange(orderByExpressions);

     public OrderBy(string propertyName, OrderDirection? orderDirection = null) {
         OrderByExpressions.Add(new OrderByExpression(propertyName, orderDirection));
     }
     public static OrderBy? FromJson(string? json) => string.IsNullOrWhiteSpace(json)
         ? null
         : JsonSerializer.Deserialize<OrderBy>(json);

     public string AsJson() => JsonSerializer.Serialize(this);

     public static bool TryParse(string json, out OrderBy? orderBy) {
         orderBy = JsonSerializer.Deserialize<OrderBy>(json);
         return orderBy is not null;
     }
}

public record OrderByExpression {
    public string PropertyName { get; set; }
    public OrderDirection OrderDirection { get; set; }

    public OrderByExpression() { }

    public OrderByExpression(string propertyName, OrderDirection? orderDirection = null) {
        PropertyName = propertyName;
        OrderDirection = orderDirection ?? OrderDirection.Ascending;
    }

    public string AsJson() => JsonSerializer.Serialize(this);

    public Func<T, object> ToLinqExpression<T>() {
        var param = Expression.Parameter(typeof(T), "x");
        var body = Expression.Property(param, PropertyName);
        var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(body, typeof(object)), param);
        return lambda.Compile();
    }


    public static bool TryParse(string json, out OrderByExpression? orderByExpression) {
        orderByExpression = JsonSerializer.Deserialize<OrderByExpression>(json);
        return orderByExpression is not null;
    }
}

public record OrderByExpression<T> : OrderByExpression {
    public OrderByExpression(string propertyName, OrderDirection? dir = null) : base(propertyName, dir) {
        var _ = typeof(T).GetProperty(propertyName) ?? throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
    }

    public OrderByExpression(Expression<Func<T, object>> propertyNameExpression, OrderDirection? orderDirection = null) : 
        this(MemberHelpers.GetMemberName(propertyNameExpression), orderDirection) {
    }

    public static bool TryParse(string value, out OrderByExpression<T>? result) {
        result = JsonSerializer.Deserialize<OrderByExpression<T>>(value);
        return result is null;
    }
}