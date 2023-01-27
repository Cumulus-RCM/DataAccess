using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.Json;
using DataAccess.Shared.Helpers;

namespace DataAccess.Shared.FilterService;

public record FilterExpression(string PropertyName, Operator Operator, string? ColumnName = null, bool IsString = false) {
    public string PropertyName { get; protected set; } = PropertyName;
    public Operator Operator { get; protected set; } = Operator;
    public string? ColumnName { get; set; } = ColumnName ?? PropertyName;
    public bool IsString { get; set; } = IsString;

    public override string ToString() => $"{ColumnName ?? PropertyName} {Operator.DisplayName} {(IsString ? "'" : "")}{Operator.PreTemplate}@{PropertyName}{Operator.PostTemplate}{(IsString ? "'" : "")}";

    public static bool TryParse(string value, out FilterExpression? result) {
        result = JsonSerializer.Deserialize<FilterExpression>(value);
        return result is null;
    }
}

public record FilterExpression<T> : FilterExpression {
    public FilterExpression(string propertyName, Operator oper, string? columnName = null) : base(propertyName, oper, columnName) {
      var propertyInfo = typeof(T).GetProperty(propertyName);
      if (propertyInfo is null) throw new ArgumentException($"Property: {propertyName} NOT found on {typeof(T).Name}");
      IsString = propertyInfo.PropertyType == typeof(string);
    }

    public FilterExpression(Expression<Func<T,object>> propertyNameExpression, Operator oper) : this(MemberHelpers.GetMemberName(propertyNameExpression), oper){ }

    public override string ToString() {
        var (pre, post) = stringifyTemplates();
        return $"{ColumnName ?? PropertyName} {Operator.DisplayName} {pre}@{PropertyName}{post}";

        (string pre, string post) stringifyTemplates() {
            if (!IsString) return ("", "");
            var before = string.IsNullOrWhiteSpace(Operator.PreTemplate) ? "" : $"'{Operator.PreTemplate}' + ";
            var after = string.IsNullOrWhiteSpace(Operator.PostTemplate) ? "" : $" + '{Operator.PostTemplate}'";
            return (before, after);
        }
    }
}
