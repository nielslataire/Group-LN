using BOCore;
using DALCore.Models;

namespace CPMCore.Models.Issues;

public class ConstructionIssueIndexVm
{
    public int ProjectId { get; set; }
    public ConstructionIssueFilterBO Filters { get; set; } = new();
    public List<ConstructionIssue> Issues { get; set; } = new();
    public List<ConstructionIssueCategory> Categories { get; set; } = new();
    public List<Units> Units { get; set; } = new();
    public ConstructionIssueFormVm Form { get; set; } = new();
}