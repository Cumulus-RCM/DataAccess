using System.Linq.Expressions;

namespace DataAccess.Shared.Helpers;

internal class MemberHelpers {
    private const string expressionCannotBeNullMessage = "The expression cannot be null.";
    private const string invalidExpressionMessage = "Invalid expression.";

    public static string GetMemberName<T>(Expression<Func<T, object>> expression) => getMemberName(expression.Body);

    private static string getMemberName(Expression expression) => expression switch {
        null => throw new ArgumentException(expressionCannotBeNullMessage),
        MemberExpression memberExpression => memberExpression.Member.Name,
        MethodCallExpression methodCallExpression => methodCallExpression.Method.Name,
        UnaryExpression unaryExpression => getMemberName(unaryExpression),
        _ => throw new ArgumentException(invalidExpressionMessage)
    };

    private static string getMemberName(UnaryExpression unaryExpression) {
        if (unaryExpression.Operand is MethodCallExpression methodExpression) return methodExpression.Method.Name;
        return ((MemberExpression)unaryExpression.Operand).Member.Name;
    }
}

public static class NameReaderExtensions {
    private const string expressionCannotBeNullMessage = "The expression cannot be null.";
    private const string invalidExpressionMessage = "Invalid expression.";

    public static string GetMemberName<T>(this T instance, Expression<Func<T, object>> expression) => getMemberName(expression.Body);

    public static List<string> GetMemberNames<T>(this T instance, params Expression<Func<T, object>>[] expressions) => expressions.Select(cExpression => getMemberName(cExpression.Body)).ToList();

    public static string GetMemberName<T>(this T instance, Expression<Action<T>> expression) => getMemberName(expression.Body);

    private static string getMemberName(Expression expression) {
        return expression switch {
            null => throw new ArgumentException(expressionCannotBeNullMessage),
            MemberExpression memberExpression => memberExpression.Member.Name,
            MethodCallExpression methodCallExpression => methodCallExpression.Method.Name,
            UnaryExpression unaryExpression => getMemberName(unaryExpression),
            _ => throw new ArgumentException(invalidExpressionMessage)
        };

    }

    private static string getMemberName(UnaryExpression unaryExpression) {
        if (unaryExpression.Operand is MethodCallExpression) {
            var methodExpression = (MethodCallExpression)unaryExpression.Operand;
            return methodExpression.Method.Name;
        }

        return ((MemberExpression)unaryExpression.Operand).Member.Name;
    }
}