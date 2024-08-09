using System;

namespace DataAccess.Shared.ReportService;

public record ReportColumn(string ColumnName, string DataTypeName, string? ColumnFormat = null, string? ColumnTitle = null, bool IsFilterable = true) {
    public string ColumnTitle { get; init; } = ColumnTitle ?? ColumnName;
    public Type DataType => Type.GetType(DataTypeName) ?? typeof(string);
}