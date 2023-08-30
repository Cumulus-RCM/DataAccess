using BaseLib;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataAccess;
public class ReaderFactory : IReaderFactory {
    private readonly IDbConnectionManager connectionManager;
    private readonly IDatabaseMapper databaseMapper;
    private readonly ILoggerFactory loggerFactory;

    public ReaderFactory(IDbConnectionManager connectionManager, IDatabaseMapper databaseMapper,ILoggerFactory loggerFactory) {
        this.connectionManager = connectionManager;
        this.databaseMapper = databaseMapper;
        this.loggerFactory = loggerFactory;

    }
    public virtual Reader<T> GetReader<T>() where T : class => new Reader<T>(connectionManager, databaseMapper, loggerFactory);
}