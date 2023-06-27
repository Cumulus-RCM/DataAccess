using BaseLib;
using DataAccess.Interfaces;

namespace DataAccess;

public abstract class SaveStrategy {
    public abstract Task<int> SaveAsync(IEnumerable<IDataChange> dataChanges);

    protected readonly IDbConnectionManager dbConnection;
    protected readonly IDatabaseMapper databaseMapper;

    protected SaveStrategy(IDbConnectionManager dbConnection, IDatabaseMapper databaseMapper) {
        this.dbConnection = dbConnection;
        this.databaseMapper = databaseMapper;
    }
}