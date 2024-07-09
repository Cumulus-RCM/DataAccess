using System.Collections.Generic;
using System.Linq;

namespace DataAccess.Shared;

public static class Pager {
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int pageSize, int currentPage) =>
        pageSize == 0
            ? source
            : source.Skip((currentPage - 1) * pageSize).Take(pageSize);
}