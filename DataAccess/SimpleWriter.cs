using BaseLib;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class SimpleWriter : Writer {
    public SimpleWriter(IDbConnectionManager dbConnection, IDatabaseMapper databaseMapper, ILoggerFactory loggerFactory) : base(new SimpleSingleEntitySaveStrategy(dbConnection, databaseMapper,loggerFactory)) { }
}