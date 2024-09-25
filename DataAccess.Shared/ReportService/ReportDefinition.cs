using System;
using System.Collections.Generic;
using System.Text.Json;
using Serilog;

namespace DataAccess.Shared.ReportService;

public record ReportDefinition {
    public IdPk Id { get; init; }
    public string ReportName { get; init; }
    public string ReportDescription { get; init; }
    public string ReportSql { get; init; }
    public string ColumnsJson { get; init; }
    public string? OrderByJson { get; init; }
    public string? ParameterValuesJson { get; init; }
    public bool IsActive { get; init; }

    [ColumnInfo(isSkipByDefault: true, canWrite: false)]
    public ICollection<ReportColumn> ColumnDefinitions { get; private set; }

    [ColumnInfo(isSkipByDefault: true, canWrite: false)]
    public OrderBy OrderBy { get; set; }

    [ColumnInfo(isSkipByDefault: true, canWrite: false)]
    public Filter? Filter { get; set; }
    
    [ColumnInfo(isSkipByDefault: true, canWrite: false)]
    public ParameterValues? ParameterValues { get; private set; }

    public ReportDefinition() { }

    public ReportDefinition(IdPk Id, string ReportName, string ReportDescription, string ReportSql, string ColumnsJson, string OrderByJson) {
        this.Id = Id;
        this.ReportName = ReportName;
        this.ReportDescription = ReportDescription;
        this.ReportSql = ReportSql;
        this.ColumnsJson = ColumnsJson;
        this.ParameterValuesJson = ParameterValuesJson;

        Load();
    }

    public void Load() {
        try {
            ColumnDefinitions = JsonSerializer.Deserialize<ICollection<ReportColumn>>(ColumnsJson)
                                  ?? throw new InvalidOperationException($"No Columns Defined for Report:{ReportName}");
            OrderBy = OrderBy.FromJson(OrderByJson) ?? new OrderBy("1");
            ParameterValues = ParameterValues.FromJson(ParameterValuesJson);
        }
        catch (Exception ex) {
            Log.Error(ex, "Error retrieving ReportDefinition Id:{Id}, ReportName:{ReportName}", Id, ReportName);
            throw;
        }
    }
}