using System.Text.Json;

namespace DataAccess.Shared.ReportService;

public class ReportParameter : ParameterValue {
    public string? Prompt { get; init; }
    public string InputFormat { get; init; } = "";

    private object? _value;
    public object? Value {
        get => _value;
        set => _value = value is JsonElement ? null : value;
    }

    public ReportParameter(string name, object value, string typeName) : base(name, value.ToString() ?? "null", typeName) {
        this.Value = value;
    }
}