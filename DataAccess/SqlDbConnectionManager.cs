using BaseLib;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public sealed class SqlDbConnectionManager(string connectionString, ILoggerFactory loggerFactory)
    : DbConnectionManager<SqlConnection>(connectionString, loggerFactory) {
    public SqlDbConnectionManager(DbConnectionOptions dbConnectionOptions, ILoggerFactory loggerFactory) 
        : this(dbConnectionOptions.ConnectionString, loggerFactory) {}
}