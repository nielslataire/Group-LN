using BOCore;
using DALCore.Models;

namespace FacadeCore;

public interface IConstructionIssueReportService
{
    Task<ConstructionIssueReport> CreateReportEntity(int projectId, int reportType, int responsiblePartyType, int? responsiblePartyId, string? responsibleOtherName, string? responsibleOtherEmail, List<int> issueIds, string? userId);
    /// <summary>allStatuses: geen statusbeperking (afdrukken vanuit de lijst: de gebruiker koos zelf wat er in staat); anders enkel de statussen die naar een aannemer gaan.</summary>
    Task<byte[]> GenerateReportPdf(int projectId, int reportId, bool allStatuses = false);
    /// <summary>Afdrukmodal van de puntenlijst (design-handoff 40u): puntenlijst, per aannemer of rondgang.</summary>
    Task<byte[]> GeneratePrintPdf(int projectId, ConstructionIssuePrintOptions options);
    Task<int> SendSelectedIssues(int projectId, ConstructionIssueSendRequestBO request, string? userId, string? comment = null);
    Task<int> SendReminder(int projectId, List<int> issueIds, string? userId);
}