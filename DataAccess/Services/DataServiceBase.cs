using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Dapper;
using DataAccess.Shared;

namespace DataAccess.Services;

public abstract record DataServiceBase : DataServiceUnitOfWork, IDataService {
    private readonly Lazy<Dictionary<Type, Type>> customQueries;
    
    protected DataServiceBase(IReaderFactory ReaderFactory, ISaveStrategy SaveStrategy, IDatabaseMapper DatabaseMapper, Assembly? customQueriesAssembly = null) : base(SaveStrategy,DatabaseMapper) {
        this.ReaderFactory = ReaderFactory;
        customQueries = new Lazy<Dictionary<Type, Type>>(getCustomQueriesFromAssembly(customQueriesAssembly));
    }

    static DataServiceBase() {
        SqlMapper.AddTypeHandler(new SqlTimeOnlyTypeHandler());
        SqlMapper.AddTypeHandler(new DapperSqlDateOnlyTypeHandler());
    }

    public IReaderFactory ReaderFactory { get; init; }

    public IQueries<T> GetQueries<T>() where T : class {
        var isCustom = customQueries.Value.TryGetValue(typeof(T), out var customCrudType);
        return (IQueries<T>) (isCustom
            ? Activator.CreateInstance(customCrudType!, ReaderFactory.GetReader<T>())!
            : new Queries<T>(ReaderFactory.GetReader<T>()));
    }
    
    protected Dictionary<Type, Type> getCustomQueriesFromAssembly(Assembly? assembly) {
        if (assembly is null) return new Dictionary<Type, Type>();
        var types = assembly.GetTypes();
        var custom = types.Where(t => typeof(ICustomQueries).IsAssignableFrom(t) && !t.IsInterface).ToList();
        return custom.Select(t => new KeyValuePair<Type,Type>(t.BaseType!.GetGenericArguments()[0], t)).ToDictionary(x=>x.Key, x=>x.Value);
    }
}

public class SqlTimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly> {
    public override void SetValue(IDbDataParameter parameter, TimeOnly time) {
        parameter.Value = time.ToString();
    }

    public override TimeOnly Parse(object value) {
        return TimeOnly.FromTimeSpan((TimeSpan)value);
    }
}

public class DapperSqlDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly> {
    public override void SetValue(IDbDataParameter parameter, DateOnly date)
        => parameter.Value = date.ToDateTime(new TimeOnly(0, 0));

    public override DateOnly Parse(object value)
        => DateOnly.FromDateTime((DateTime)value);
}