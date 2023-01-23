using System.Linq.Expressions;
using System.Text.Json;

namespace DataAccess.Shared.FilterService;

public class FilterService {
    private readonly DatabaseMapper.DatabaseMapper databaseMapper;

    public FilterService(DatabaseMapper.DatabaseMapper databaseMapper) {
        this.databaseMapper = databaseMapper;
    }

    private string mapPropertyName<T>(string propertyName) {
        var tableInfo = databaseMapper.GetTableInfo<T>();
        return tableInfo.ColumnsMap.SingleOrDefault(x => x.PropertyName == propertyName)?.ColumnName ?? propertyName;
    }
    public Filter<T> Create<T>(Expression<Func<T, object>> propertyName, Operator oper) {
        var fe = new FilterExpression<T>(propertyName, oper);
        var mappedPropertyName = mapPropertyName<T>(fe.PropertyName);
        if (mappedPropertyName != fe.PropertyName) fe = new FilterExpression<T>(fe.PropertyName, oper, mappedPropertyName);
        return new Filter<T>(fe);
    }

    public Filter<T> Create<T>(string propertyName, Operator oper) {
        var mappedPropertyName = mapPropertyName<T>(propertyName);
        var fe = new FilterExpression<T>(propertyName, oper, mappedPropertyName);
        return new Filter<T>(fe);
    }

    public void Add<T>(Filter<T> filter, FilterExpression<T> filterExpression) {
        var mappedPropertyName = mapPropertyName<T>(filterExpression.PropertyName);
        var fe = new FilterExpression<T>(filterExpression.PropertyName, filterExpression.Operator, mappedPropertyName);
        filter.Add(fe);
    }
}

public static class FilterExt {
    public static void Add<T>(this Filter<T> filter, FilterExpression<T> filterExpression) {
        filter.Add(filterExpression);
    }
}

public class Filter<T> {
    public IEnumerable<FilterExpression<T>> Expressions => expressions;
    private List<FilterExpression<T>> expressions { get; } = new();

    public Filter() { }

    internal Filter(FilterExpression<T> expression) => Add(expression);

    internal void Add(FilterExpression<T> filterExpression) {
        expressions.Add(filterExpression);
    }
    public override string ToString() => string.Join("", expressions.Select(e=>e.ToString()));

    public static bool TryParse(string value, out Filter<T>? result) {
        result = JsonSerializer.Deserialize<Filter<T>>(value);
        return result is null;
    }
}