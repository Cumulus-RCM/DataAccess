using System.Linq.Expressions;

namespace DataAccess {
    internal static class ExpressionExt {
        public static string ToSql(this IEnumerable<Expression> expressions) {
            return "";
        }
    }
}
