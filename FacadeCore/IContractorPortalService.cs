namespace FacadeCore;

public interface IContractorPortalService
{
    Task PushIssueUpdateAsync(int issueId);
}