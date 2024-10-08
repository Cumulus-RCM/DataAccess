using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class ReaderFactory : IReaderFactory{
    private readonly IDbConnectionManager connectionManager;
    private readonly IDatabaseMapper databaseMapper;
    protected Lazy<Dictionary<Type, Type>> customReaders = new(new Dictionary<Type, Type>());
    private readonly ILoggerFactory loggerFactory;

    public ReaderFactory(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory ) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
    }

    public IReader<T> GetReader<T>() where T : class {
        var isCustom = customReaders.Value.TryGetValue(typeof(T), out var customReaderType);
        var reader= isCustom
            ? (Activator.CreateInstance(customReaderType!, connectionManager, databaseMapper, loggerFactory) as IReader<T>)!
            : new Reader<T>(connectionManager, databaseMapper, loggerFactory);
        return reader;
    }

    protected Dictionary<Type, Type> getCustomReadersFromAssembly(Assembly assembly) {
        var types = assembly.GetTypes();
        var custom = types.Where(t => typeof(ICustomReader).IsAssignableFrom(t) && !t.IsInterface).ToList();
        return custom.Select(t => new KeyValuePair<Type,Type>(t.BaseType!.GetGenericArguments()[0], t)).ToDictionary(x=>x.Key, x=>x.Value);
    }
}