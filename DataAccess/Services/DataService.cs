using BaseLib;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public abstract class DataService : IDataService {
    private readonly DbConnectionManager connectionManager;
    private readonly DatabaseMapper databaseMapper;
    private readonly ILoggerFactory loggerFactory;

    protected DataService(DbConnectionManager connectionManager, DatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
    }

    public virtual ICrud<T> GetCrud<T>() where T : class => new Crud<T>(connectionManager, databaseMapper, loggerFactory);
}