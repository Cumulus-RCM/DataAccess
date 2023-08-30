using BaseLib;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataAccess;
public class WriterFactory : IWriterFactory{
    private readonly IDbConnectionManager connectionManager;
    private readonly IDatabaseMapper databaseMapper;
    private readonly ILoggerFactory loggerFactory;

    public WriterFactory(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper,ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;

    }

    public IWriter GetWriter() => new SimpleWriter(connectionManager, databaseMapper, loggerFactory);
}