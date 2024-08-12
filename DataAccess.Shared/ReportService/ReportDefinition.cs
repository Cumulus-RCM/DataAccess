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
    public string? OrderByJson { get; set; } 
    public ICollection<ReportColumn> ColumnDefinitions { get; }
    public OrderBy OrderBy { get; }
    public Filter? Filter { get; set; }

    public ReportDefinition(IdPk Id, string ReportName, string ReportDescription, string ReportSql, string ColumnsJson, string OrderByJson) {
        this.Id = Id;
        this.ReportName = ReportName;
        this.ReportDescription = ReportDescription;
        this.ReportSql = ReportSql;
       this.ColumnsJson = ColumnsJson;

        if (ColumnsJson is null) throw new InvalidOperationException($"No ColumnsJson in ReportDefinition for :{ReportName}");

        try {
            ColumnDefinitions ??= JsonSerializer.Deserialize<ICollection<ReportColumn>>(ColumnsJson)
                                  ?? throw new InvalidOperationException($"No Columns Defined for Report:{ReportName}");

            OrderBy = JsonSerializer.Deserialize<OrderBy>(OrderByJson) ?? new OrderBy("1");
        }
        catch (Exception ex) {
            Log.Error(ex, "Error retrieving ReportDefinition Id:{Id}, ReportName:{ReportName}", Id, ReportName );
            throw;
        }
    }
}