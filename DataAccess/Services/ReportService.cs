using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib;
using Dapper;
using DataAccess.Shared;
using DataAccess.Shared.ReportService;
using Serilog;

namespace DataAccess.Services;

public class ReportService(IDataService dataService, IDbConnectionManager connectionManager) : IReportService {
    private Dictionary<string, ReportDefinition>? cachedReportDefinitions;

    public async Task<Response<(string reportName, string reportDescription)>> GetReportsAsync(bool forceRefresh) {
        await loadReportsAsync(forceRefresh).ConfigureAwait(false);
        var result = cachedReportDefinitions?.Select(kvp => (kvp.Key, kvp.Value.ReportDescription)).ToArray() ?? [];
        return new Response<(string, string)>(result);
    }

    private async Task loadReportsAsync(bool forceRefresh = false) {
        if (cachedReportDefinitions is null || forceRefresh) {
            cachedReportDefinitions = new Dictionary<string, ReportDefinition>();
            var response = await dataService.GetQueries<ReportDefinition>().GetAllAsync().ConfigureAwait(false);
            if (response.IsSuccess) {
                foreach (var reportInfo in response.Items)
                    cachedReportDefinitions.Add(reportInfo.ReportName, reportInfo);
            }
        }
    }

    public async Task<Response<ReportDefinition>> GetReportDefinitionAsync(string reportName) {
        await loadReportsAsync().ConfigureAwait(false);
        var reportInfo = cachedReportDefinitions?.GetValueOrDefault(reportName);
        return reportInfo is null ? Response<ReportDefinition>.Empty() : new Response<ReportDefinition>(reportInfo);
    }

    public async Task<Response<dynamic>> GetReportDataAsync(ReportDefinition reportDefinition, int pageSize = 0, int pageNum = 0) {
        try {
            var reader = new Reader(connectionManager, reportDefinition.ReportSql);
            var cnt = await reader.GetCountAsync(reportDefinition.Filter).ConfigureAwait(false);
            if (cnt == 0) return Response<dynamic>.Empty();
            var result = await reader.GetAllAsync(reportDefinition.Filter, pageSize, pageNum, reportDefinition.OrderBy).ConfigureAwait(false);
            return new Response<dynamic>(result, cnt);
        }
        catch (Exception ex) {
            Log.Error(ex, nameof(GetReportDataAsync));
            return Response<dynamic>.Empty(ex.Message);
        }
    }

    public async Task<long> GetCountAsync(ReportDefinition reportDefinition) {
        var sql = $"SELECT COUNT(*) FROM ({reportDefinition.ReportSql}) d";
        try {
            using var conn = connectionManager.CreateConnection();
            return await conn.ExecuteScalarAsync<long>(sql, reportDefinition.Filter);
        }
        catch (Exception exception) {
            Log.Error(exception, "Error in GetReportDataAsync: {0}", reportDefinition);
            return 0;
        }
    }
}