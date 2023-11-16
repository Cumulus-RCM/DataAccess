using BaseLib;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public class SimpleDatabaseWriter : Writer {
    public SimpleDatabaseWriter(IDbConnectionManager dbConnection, IDatabaseMapper databaseMapper, ISaveStrategy saveStrategy, ILoggerFactory loggerFactory) : base(saveStrategy) { }
}