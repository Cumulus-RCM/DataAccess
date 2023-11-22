using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib;
using DataAccess.Interfaces;

namespace DataAccess;

public abstract class DatabaseSaveStrategy : ISaveStrategy {
    public abstract Task<int> SaveAsync(IEnumerable<IDataChange> dataChanges);

    protected readonly IDbConnectionManager dbConnection;
    protected readonly IDatabaseMapper databaseMapper;

    protected DatabaseSaveStrategy(IDbConnectionManager dbConnection, IDatabaseMapper databaseMapper) {
        this.dbConnection = dbConnection;
        this.databaseMapper = databaseMapper;
    }
}