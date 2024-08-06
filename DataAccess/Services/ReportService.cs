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

    public async Task<Response<dynamic>> GetReportDataAsync(ReportDefinition reportDefinition, int pageSize = 0, int pageNum = 1) {
        if (pageNum <= 0) pageNum = 1;
        var offsetFetchClause = pageSize > 0
            ? $"OFFSET {pageSize * (pageNum - 1)} ROWS FETCH NEXT {pageSize} ROW ONLY"
            : "";
        var sql = $"{reportDefinition.ReportSql} {offsetFetchClause}";
        var parameters = reportDefinition.GetDynamicParameters();
        try {
            using var conn = connectionManager.CreateConnection();
            var result = await conn.QueryAsync(sql, parameters).ConfigureAwait(false);
            return new Response<dynamic>(result);
        }
        catch (Exception exception) {
            Log.Error(exception, "Error in GetReportDataAsync:{0}", reportDefinition);
            return Response<dynamic>.Fail(exception);
        }
    }

    public async Task<long> GetCountAsync(ReportDefinition reportDefinition) {
        var sql = $"SELECT COUNT(*) FROM ({reportDefinition.ReportSql}) d";
        var parameters = reportDefinition.GetDynamicParameters();
        try {
            using var conn = connectionManager.CreateConnection();
            return await conn.ExecuteScalarAsync<long>(sql, parameters);
        }
        catch (Exception exception) {
            Log.Error(exception, "Error in GetReportDataAsync: {0}", reportDefinition);
            return 0;
        }
    }
}