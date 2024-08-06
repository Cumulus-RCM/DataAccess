using System;
using System.Collections.Generic;
using System.Text.Json;
using Dapper;

namespace DataAccess.Shared.ReportService;

public record ReportDefinition {
    public IdPk Id { get; init; }
    public string ReportName { get; init; }
    public string ReportDescription { get; init; }
    public string ReportSql { get; init; }
    public string? ReportParametersJson { get; init; }
    public string ColumnsJson { get; init; }
    public ICollection<ReportParameter> Parameters { get; }
    public ICollection<ReportColumn> ColumnDefinitions { get; }

    public ReportDefinition(IdPk Id, string ReportName, string ReportDescription, string ReportSql, string? ReportParametersJson, string ColumnsJson) {
        this.Id = Id;
        this.ReportName = ReportName;
        this.ReportDescription = ReportDescription;
        this.ReportSql = ReportSql;
        this.ReportParametersJson = ReportParametersJson;
        this.ColumnsJson = ColumnsJson;

        Parameters ??= ReportParametersJson is null ? []
            : JsonSerializer.Deserialize<ICollection<ReportParameter>>(ReportParametersJson) ?? [];
        
        if (ColumnsJson is null) throw new InvalidOperationException($"No ColumnsJson in ReportDefinition for :{ReportName}");
        ColumnDefinitions ??= JsonSerializer.Deserialize<ICollection<ReportColumn>>(ColumnsJson) 
                    ?? throw new InvalidOperationException($"No Columns Defined for Report:{ReportName}");
    }

    public DynamicParameters DynamicParameters() {
        var dynamicParameters = new DynamicParameters();
        foreach(var parameter in Parameters) dynamicParameters.Add(parameter.ParameterName, parameter.Value);
        return dynamicParameters;
    }
}