using System.Text.Json;

namespace DataAccess.Shared.ReportService;

public record ReportParameter  {
    public required string ParameterName { get; init; }
    public required string ParameterTypeName { get; init; }
    private string? parameterPrompt;
    public string? ParameterPrompt { 
        get => parameterPrompt ?? ParameterName;
        private set => parameterPrompt = value;
    } 
    public string InputFormat { get; init; } = "";

    private object? _value;
    public object? Value {
        get => _value;
        set => _value = value is JsonElement ? null : value;
    }
}