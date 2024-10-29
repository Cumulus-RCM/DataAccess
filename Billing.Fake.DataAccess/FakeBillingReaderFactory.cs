using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Shared;

namespace Billing.Fake.DataAccess;

public class FakeBillingReaderFactory : IReaderFactory {
    private readonly FakeBillingDataFactory fakeBillingDataFactory;
    private readonly Lazy<Dictionary<Type, Type>> customReaders;

    public FakeBillingReaderFactory(int collectionSize = 10) {
        fakeBillingDataFactory = new FakeBillingDataFactory(collectionSize);
        customReaders = new Lazy<Dictionary<Type, Type>>(getCustomReaders);
    }

    private Dictionary<Type, Type> getCustomReaders() {
        var assembly = typeof(FakeFacilityReader).Assembly;
        var types = assembly.GetTypes();
        var custom = types.Where(t => typeof(ICustomReader).IsAssignableFrom(t) && !t.IsInterface).ToList();
        return custom.Select(t => new KeyValuePair<Type, Type>(t.BaseType!.GetGenericArguments()[0], t)).ToDictionary(x => x.Key, x => x.Value);
    }

    public IReader<T> GetReader<T>() where T : class {
        var isCustom = customReaders.Value.TryGetValue(typeof(T), out var customReaderType);
        return isCustom
            ? (Activator.CreateInstance(customReaderType!, fakeBillingDataFactory) as IReader<T>)!
            : new FakeBillingReader<T>(fakeBillingDataFactory);
    }
}

public class FakeBillingReader<T>(FakeBillingDataFactory fakeBillingDataFactory) : IReader<T>
    where T : class {
    protected readonly FakeBillingDataFactory fakeBillingDataFactory = fakeBillingDataFactory;

    public Task<IReadOnlyCollection<T>> GetAllAsync(Filter? filter = null, int pageSize = 0, int pageNum = 0, OrderBy? orderBy = null,
        IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameters = null) {
        if (filter is not null) setItemPropertiesUsingFilter(filter);
        var items = fakeBillingDataFactory.GetItems<T>();
        var filteredItems = filter is null ? items : items.Where(filter.ToLinqExpression<T>()).ToList();
        var orderedItems = applyOrderedByExpressions(filteredItems, orderBy);

        var skipNum = pageNum == 0 ? 0 : pageSize * (pageNum - 1);
        IReadOnlyCollection<T> result = 
            pageSize == 0
                ? orderedItems.ToList().AsReadOnly()
                : orderedItems.Skip(skipNum).Take(pageSize).ToList().AsReadOnly();
        return Task.FromResult(result);

        static IOrderedEnumerable<T> applyOrderedByExpressions(IReadOnlyCollection<T> list, OrderBy? orderBy) {
            var orderedItems = list.OrderBy(_ => 1);
            var orderByOrderByExpressions = orderBy?.OrderByExpressions ?? Enumerable.Empty<OrderByExpression>();
            foreach (var orderExpression in orderByOrderByExpressions) {
                var orderByExpression = orderExpression.ToLinqExpression<T>();
                orderedItems = orderExpression.OrderDirection == OrderDirection.Ascending
                    ? orderedItems.ThenBy(orderByExpression)
                    : orderedItems.ThenByDescending(orderByExpression);
            }

            return orderedItems;
        }
    }

    private void setItemPropertiesUsingFilter(Filter filter) {
        foreach (var connectedExpression in filter.Segments.First().FilterExpressions.Values) {
            var expression = connectedExpression.FilterExpression;
            if (!expression.PropertyName.Contains('_')) return; // only set foreign key properties

            var propName = expression.PropertyName;
            var prop = typeof(T).GetProperty(propName);
            var propValue = TypeDescriptor.GetConverter(prop!.PropertyType);

            var items = fakeBillingDataFactory.GetItems<T>();
            foreach (var item in items) {
                prop.SetValue(item, propValue);
            }
        }
    }

    public Task<T?> GetByPkAsync(string pkValue, IReadOnlyCollection<string>? columnNames = null, ParameterValues? parameterValues = null) => Task.FromResult(fakeBillingDataFactory.GetItems<T>().First())!;

    public Task<int> GetCountAsync(Filter? filter = null, ParameterValues? parameterValues = null) => Task.FromResult(fakeBillingDataFactory.GetItems<T>().Count);
}