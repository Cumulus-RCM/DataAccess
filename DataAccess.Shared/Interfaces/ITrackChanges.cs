namespace DataAccess.Shared; 

public interface ITrackChanges {
    bool IsNew { get; }
    void ApplyChanges();
    void CancelChanges();
}