namespace DataAccess;

public abstract class SaveStrategy {
    public abstract Task<int> SaveAsync(IEnumerable<IDataChange> dataChanges);

    protected readonly DbConnectionManager dbConnection;
    protected readonly DatabaseMapper databaseMapper;

    protected SaveStrategy(DbConnectionManager dbConnection, DatabaseMapper databaseMapper) {
        this.dbConnection = dbConnection;
        this.databaseMapper = databaseMapper;
    }
}