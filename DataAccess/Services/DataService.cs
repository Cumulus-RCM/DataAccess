using BaseLib;
using DataAccess.Interfaces;
using DataAccess.Shared;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public abstract class DataService : IDataService {
    private readonly IDbConnectionManager connectionManager;
    private readonly IDatabaseMapper databaseMapper;
    private readonly ILoggerFactory loggerFactory;

    protected DataService(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
    }

    public virtual ICrud<T> GetCrud<T>() where T : class => new Crud<T>(connectionManager, databaseMapper, loggerFactory);
}