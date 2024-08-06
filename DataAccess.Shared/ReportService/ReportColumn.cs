using System;

namespace DataAccess.Shared.ReportService;

public record ReportColumn(string ColumnName, Type ColumnType, string? ColumnFormat = null, string? ColumnTitle = null) {
    public string ColumnTitle { get; init; } = ColumnTitle ?? ColumnName;
}