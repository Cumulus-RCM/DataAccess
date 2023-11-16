namespace DataAccess.Interfaces;

public interface ISaveStrategy {
    Task<int> SaveAsync(IEnumerable<IDataChange> dataChanges);
}