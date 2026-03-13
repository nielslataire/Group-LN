using BOCore;
using DALCore.Models;

namespace FacadeCore;

public interface IConstructionIssueReportService
{
    Task<ConstructionIssueReport> CreateReportEntity(int projectId, int reportType, int responsiblePartyType, int? responsiblePartyId, string? responsibleOtherName, string? responsibleOtherEmail, List<int> issueIds, string? userId);
    Task<byte[]> GenerateReportPdf(int projectId, int reportId);
    Task<int> SendSelectedIssues(int projectId, ConstructionIssueSendRequestBO request, string? userId, string? comment = null);
    Task<int> SendReminder(int projectId, List<int> issueIds, string? userId);
}