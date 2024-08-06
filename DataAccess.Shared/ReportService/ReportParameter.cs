using System;

namespace DataAccess.Shared.ReportService;

public record ReportParameter  {
    public string ParameterName { get; private set; }
    public Type ParameterType { get; private set; }
    public string? ParameterPrompt { get; init; }
    public string InputFormat { get; init; } = "";
    public object? Value { get; set; }

    public ReportParameter(string parameterName, Type parameterType) {
        ParameterName = parameterName;
        ParameterType = parameterType;
        ParameterPrompt ??= ParameterName;
    }
}