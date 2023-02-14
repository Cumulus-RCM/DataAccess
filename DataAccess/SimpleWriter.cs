using DataAccess.Services;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class SimpleWriter : Writer {
    public SimpleWriter(DbConnectionManager dbConnection, DatabaseMapper databaseMapper, ILoggerFactory loggerFactory) : base(new SimpleSingleEntitySaveStrategy(dbConnection, databaseMapper,loggerFactory)) { }
}