using System.Threading.Tasks;
using DataAccess.Shared.ReportService;

namespace DataAccess.Shared;

public interface IReportService {
    Task<Response<(string reportName, string reportDescription)>> GetReportsAsync(bool forceRefresh = false);
    Task<Response<ReportDefinition>> GetReportDefinitionAsync(string reportName);
    Task<Response<dynamic>> GetReportDataAsync(ReportDefinition reportDefinition, int pageSize = 0, int pageNum = 0);
    Task<long> GetCountAsync(ReportDefinition reportDefinition);
}