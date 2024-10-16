using BaseLib;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DataAccess;

public sealed class SqlDbConnectionManager(string connectionString, ILoggerFactory loggerFactory)
    : DbConnectionManager<SqlConnection>(connectionString, loggerFactory);