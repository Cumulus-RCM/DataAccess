using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib;
using DataAccess.Shared;

namespace DataAccess;

public abstract class DatabaseSaveStrategy(IDbConnectionManager dbConnection, IDatabaseMapper databaseMapper)
    : ISaveStrategy {
    public abstract Task<SaveResult> SaveAsync(IEnumerable<IDataChange> dataChanges);

    protected readonly IDbConnectionManager dbConnection = dbConnection;
    protected readonly IDatabaseMapper databaseMapper = databaseMapper;
}