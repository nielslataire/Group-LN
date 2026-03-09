using DALCore.Models;
using BOCore;

namespace CPMCore.Models.Issues;

public class ConstructionIssueFormVm
{
    public int ProjectId { get; set; }
    public int? Id { get; set; }
    public ConstructionIssueUpsertBO Input { get; set; } = new();
    public List<ConstructionIssueCategory> Categories { get; set; } = new();
    public List<Units> Units { get; set; } = new();
    public List<IdNameBO> ResponsibleContractors { get; set; } = new();
    public List<ConstructionIssueMediaVm> ExistingMedia { get; set; } = new();
}

public class ConstructionIssueMediaVm
{
    public int Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string? Url { get; set; }
}