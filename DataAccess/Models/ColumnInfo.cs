namespace DataAccess.Models; 

public class ColumnInfo {
    public string ColumnName { get; init; }
    public string PropertyName { get; init; } 
    public bool IsSkipByDefault { get; init; }
    public bool CanWrite { get; init; }
    public bool IsPrimaryKey { get; init; }
    public string Alias => PropertyName.Equals(ColumnName,StringComparison.InvariantCultureIgnoreCase) ? string.Empty : $" {PropertyName}";

    public ColumnInfo(string columnName, string? propertyName = null, bool? isIsSkipByDefault = false, bool? canWrite = true, bool? isPrimaryKey = false) {
        ColumnName = columnName;
        PropertyName = propertyName ?? ColumnName;
        IsSkipByDefault = isIsSkipByDefault ?? false;
        CanWrite = canWrite ?? true;
        IsPrimaryKey = isPrimaryKey ?? false;
    }
}