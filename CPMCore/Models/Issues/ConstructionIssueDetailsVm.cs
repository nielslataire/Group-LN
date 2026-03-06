using DALCore.Models;

namespace CPMCore.Models.Issues;

public class ConstructionIssueDetailsVm
{
    public int ProjectId { get; set; }
    public ConstructionIssue Issue { get; set; } = new();
    public List<ConstructionIssueNotification> Notifications { get; set; } = new();
    public Dictionary<int, string?> MediaUrls { get; set; } = new();
}