using System.Linq.Expressions;
using System.Text.Json;
using DataAccess.Shared.Helpers;

namespace DataAccess.Shared.FilterService;

public record FilterExpression(string PropertyName, Operator Operator, string? ColumnName = null) {
    public string PropertyName { get; protected set; } = PropertyName;
    public Operator Operator { get; protected set; } = Operator;
    public string? ColumnName { get; set; } = ColumnName ?? PropertyName;

    public override string ToString() => $"{ColumnName ?? PropertyName} {Operator.DisplayName} {Operator.PreTemplate}@{PropertyName}{Operator.PostTemplate}";

    public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator oper, string? columnName = null) : base(propertyName, oper, columnName) {
      if (columnName is null && typeof(T).GetProperty(propertyName) is null) throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
    }

    public FilterExpression(Expression<Func<T,object>> propertyName, Operator oper) : base(MemberHelpers.GetMemberName(propertyName), oper){ }

    public override string ToString() => $"{ColumnName ?? PropertyName} {Operator.DisplayName} {Operator.PreTemplate}@{PropertyName}{Operator.PostTemplate}";

}
