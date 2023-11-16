using BaseLib;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataAccess;
public class DatabaseWriterFactory : IWriterFactory{
    private readonly IDbConnectionManager connectionManager;
    private readonly IDatabaseMapper databaseMapper;
    private readonly ILoggerFactory loggerFactory;
    private readonly ISaveStrategy saveStrategy;

    public DatabaseWriterFactory(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper, ISaveStrategy saveStrategy, ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;
        this.saveStrategy = saveStrategy;
    }

    public IWriter GetWriter() => new SimpleDatabaseWriter(connectionManager, databaseMapper,saveStrategy, loggerFactory);
}