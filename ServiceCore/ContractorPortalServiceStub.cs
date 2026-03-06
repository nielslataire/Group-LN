using FacadeCore;

namespace ServiceCore.Stubs;

public class ContractorPortalServiceStub : IContractorPortalService
{
    public Task PushIssueUpdateAsync(int issueId) => Task.CompletedTask;
}