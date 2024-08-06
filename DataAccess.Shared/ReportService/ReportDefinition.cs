using System.Collections.Generic;
using System.Text.Json;
using Dapper;

namespace DataAccess.Shared.ReportService;

public record ReportDefinition(IdPk Id, string ReportName, string ReportDescription, string ReportSql, string? ReportParametersJson) {
    private DynamicParameters? dynamicParameters;

    public DynamicParameters GetDynamicParameters() {
        if (dynamicParameters is not null) return dynamicParameters;
        dynamicParameters = new DynamicParameters();
        var reportParameters = ReportParametersJson is null
            ? []
            : JsonSerializer.Deserialize<ICollection<ReportParameter>>(ReportParametersJson) ?? [];
        foreach(var reportParameter in reportParameters) dynamicParameters.Add(reportParameter.ParameterName, reportParameter.Value);
        return dynamicParameters;
    }
}