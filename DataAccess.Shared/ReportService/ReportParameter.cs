using System;

namespace DataAccess.Shared.ReportService;

public record ReportParameter(string ParameterName, Type ParameterType)  {
    public object? Value { get; set; }
}