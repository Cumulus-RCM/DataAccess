using System.Linq.Expressions;

namespace DataAccess.Shared.Helpers;

internal class MemberHelpers {
    public const string EXPRESSION_CANNOT_BE_NULL_MESSAGE = "The expression cannot be null.";
    public const string INVALID_EXPRESSION_MESSAGE = "Invalid expression.";

    public static string GetMemberName<T>(Expression<Func<T, object>> expression) => getMemberName(expression.Body);
    public static Type GetMemberType<T>(Expression<Func<T, object>> expression) => getMemberType(expression.Body);


    private static string getMemberName(Expression expression) => expression switch {
        null => throw new ArgumentException(EXPRESSION_CANNOT_BE_NULL_MESSAGE),
        MemberExpression memberExpression => memberExpression.Member.Name,
        MethodCallExpression methodCallExpression => methodCallExpression.Method.Name,
        UnaryExpression unaryExpression => getMemberName(unaryExpression),
        _ => throw new ArgumentException(INVALID_EXPRESSION_MESSAGE)
    };

    private static Type getMemberType(Expression expression) {
        return expression switch {
            null => throw new ArgumentException(MemberHelpers.EXPRESSION_CANNOT_BE_NULL_MESSAGE),
            MemberExpression memberExpression => memberExpression.Member.GetType(),
            MethodCallExpression methodCallExpression => methodCallExpression.Method.GetType(),
            UnaryExpression unaryExpression => getMemberName(unaryExpression).GetType(),
            _ => throw new ArgumentException(MemberHelpers.INVALID_EXPRESSION_MESSAGE)
        };
    }

    private static string getMemberName(UnaryExpression unaryExpression) {
        if (unaryExpression.Operand is MethodCallExpression methodExpression) return methodExpression.Method.Name;
        return ((MemberExpression)unaryExpression.Operand).Member.Name;
    }
}

public static class NameReaderExtensions {
    public static string GetMemberName<T>(this T instance, Expression<Func<T, object>> expression) => getMemberName(expression.Body);

    public static List<string> GetMemberNames<T>(this T instance, params Expression<Func<T, object>>[] expressions) => expressions.Select(cExpression => getMemberName(cExpression.Body)).ToList();

    public static string GetMemberName<T>(this T instance, Expression<Action<T>> expression) => getMemberName(expression.Body);

    public static Type GetMemberType<T>(this T instance, Expression<Func<T, object>> expression) => getMemberType(expression.Body);

    public static List<Type> GetMemberTypes<T>(this T instance, params Expression<Func<T, object>>[] expressions) => expressions.Select(cExpression => getMemberType(cExpression.Body)).ToList();

    public static Type GetMemberType<T>(this T instance, Expression<Action<T>> expression) => getMemberType(expression.Body);


    private static string getMemberName(Expression expression) {
        return expression switch {
            null => throw new ArgumentException(MemberHelpers.EXPRESSION_CANNOT_BE_NULL_MESSAGE),
            MemberExpression memberExpression => memberExpression.Member.Name,
            MethodCallExpression methodCallExpression => methodCallExpression.Method.Name,
            UnaryExpression unaryExpression => getMemberName(unaryExpression),
            _ => throw new ArgumentException(MemberHelpers.INVALID_EXPRESSION_MESSAGE)
        };
    }

    private static Type getMemberType(Expression expression) {
        return expression switch {
            null => throw new ArgumentException(MemberHelpers.EXPRESSION_CANNOT_BE_NULL_MESSAGE),
            MemberExpression memberExpression => memberExpression.Member.GetType(),
            MethodCallExpression methodCallExpression => methodCallExpression.Method.GetType(),
            UnaryExpression unaryExpression => getMemberName(unaryExpression).GetType(),
            _ => throw new ArgumentException(MemberHelpers.INVALID_EXPRESSION_MESSAGE)
        };
    }

    private static string getMemberName(UnaryExpression unaryExpression) {
        if (unaryExpression.Operand is MethodCallExpression methodExpression) return methodExpression.Method.Name;
        return ((MemberExpression)unaryExpression.Operand).Member.Name;
    }
}