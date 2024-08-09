using System;
using System.Collections.Generic;
using System.Text.Json;
using Dapper;
using Serilog;

namespace DataAccess.Shared.ReportService;

public record ReportDefinition {
    public IdPk Id { get; init; }
    public string ReportName { get; init; }
    public string ReportDescription { get; init; }
    public string ReportSql { get; init; }
    public string? ReportParametersJson { get; init; }
    public string ColumnsJson { get; init; }
    public string? OrderByJson { get; init; } = "";
    public ICollection<ReportParameter> Parameters { get; } = [];
    public ICollection<ReportColumn> ColumnDefinitions { get; }
    public OrderBy OrderBy { get; }
    public Filter Filter { get;} = new Filter();

    public ReportDefinition(IdPk Id, string ReportName, string ReportDescription, string ReportSql, string? ReportParametersJson, string ColumnsJson, string OrderByJson) {
        this.Id = Id;
        this.ReportName = ReportName;
        this.ReportDescription = ReportDescription;
        this.ReportSql = ReportSql;
        this.ReportParametersJson = ReportParametersJson;
        this.ColumnsJson = ColumnsJson;

        try
        {
            if (!string.IsNullOrWhiteSpace(ReportParametersJson))
                Parameters = JsonSerializer.Deserialize<ICollection<ReportParameter>>(ReportParametersJson);
        }
        catch (JsonException ex)
        {
            Log.Error($"Error Deserializing Parameters for Report:{ReportName}", ex);
        }

        if (ColumnsJson is null) throw new InvalidOperationException($"No ColumnsJson in ReportDefinition for :{ReportName}");
        ColumnDefinitions ??= JsonSerializer.Deserialize<ICollection<ReportColumn>>(ColumnsJson)
                              ?? throw new InvalidOperationException($"No Columns Defined for Report:{ReportName}");
        OrderBy = string.IsNullOrEmpty(OrderByJson) ? new OrderBy("1") : JsonSerializer.Deserialize<OrderBy>(OrderByJson) ;
    }

    public DynamicParameters DynamicParameters() {
        var dynamicParameters = new DynamicParameters();
        foreach (var parameter in Parameters) {
            dynamicParameters.Add(parameter.ParameterName, parameter.Value is JsonElement ? null : parameter.Value);
        }
        return dynamicParameters;
    }
}