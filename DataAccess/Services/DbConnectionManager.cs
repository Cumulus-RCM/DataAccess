using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace DataAccess;

public sealed class DbConnectionManager {
    private static readonly IEnumerable<TimeSpan> delay = Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), retryCount: 5);
    private static readonly RetryPolicy retryPolicy = Policy.Handle<Exception>().WaitAndRetry(delay);
    private static string connectionString = "";

    public DbConnectionManager(IConfiguration config) {
        if (connectionString == "")
            connectionString = config["ConnectionStrings:DefaultConnection"] ?? "";
    }

    public DbConnectionManager(string connString) => connectionString = connString;

    private static readonly List<IDbConnection> connections = new();
    private static readonly object locker = new();

    public IDbConnection CreateConnection() {
        var conn = new SqlConnection(connectionString);
        conn.Disposed += (sender, _) => {
            if (sender is null) return; //I don't think this is possible
            lock (locker) {
                connections.Remove((IDbConnection) sender);
            }
        };
        retryPolicy.Execute(conn.Open);
        if (conn.State != ConnectionState.Open) {
            conn.Dispose();
            throw new Exception("Could not open Database.");
        }

        lock (locker) {
            connections.Add(conn);
        }

        return conn;
    }

    public void CloseAllConnections() {
        lock (locker) {
            foreach (var connection in connections.ToList()) {
                connection.Close();
                connection.Dispose();
            }
        }
    }
}