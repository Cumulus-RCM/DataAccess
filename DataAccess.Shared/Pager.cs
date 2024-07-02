using System.Collections.Generic;
using System.Linq;

namespace DataAccess.Shared;

public static class Pager {
    public static IEnumerable<T> Page<T>(IEnumerable<T> source, int pageSize, int currentPage) => 
        source.Skip((currentPage - 1) * pageSize).Take(pageSize);
}