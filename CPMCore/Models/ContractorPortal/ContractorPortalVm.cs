namespace CPMCore.Models.ContractorPortal;

// ── Dashboard ────────────────────────────────────────────────────────────────

public class ContractorDashboardVm
{
    public string CompanyName { get; set; } = "";
    public int TotalOpen { get; set; }
    public int TotalOverdue { get; set; }
    public int TotalWaitingInspection { get; set; }
    public int TotalThisWeek { get; set; }
    public decimal OverallProgressPct { get; set; }
    public List<ContractorProjectSummary> ActiveProjects { get; set; } = new();
    public List<ContractorRecentIssue> RecentIssues { get; set; } = new();
}

public class ContractorProjectSummary
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public int OpenCount { get; set; }
    public decimal ProgressPct { get; set; }
}

public class ContractorRecentIssue
{
    public int IssueId { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string? UnitName { get; set; }
    public int Status { get; set; }
    public DateTime CreatedDate { get; set; }
}

// ── Werven ───────────────────────────────────────────────────────────────────

public class ContractorWervenVm
{
    public List<ContractorWerfCard> Projects { get; set; } = new();
    public string Filter { get; set; } = "alle";
    public string? Search { get; set; }
}

public class ContractorWerfCard
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public int OpenCount { get; set; }
    public int TotalCount { get; set; }
    public int ResolvedCount { get; set; }
    public int UnitCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal ProgressPct { get; set; }
    public bool IsActive { get; set; }
}

// ── Werf Detail ──────────────────────────────────────────────────────────────

public class ContractorWerfDetailVm
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal ProgressPct { get; set; }
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int WaitingInspectionCount { get; set; }
    public int ResolvedCount { get; set; }
    public List<ContractorIssueGroup> IssueGroups { get; set; } = new();
    public string? StorageBaseUrl { get; set; }
}

public class ContractorIssueGroup
{
    public string GroupName { get; set; } = "";
    public int? UnitId { get; set; }
    public List<ContractorIssueRow> Issues { get; set; } = new();
}

public class ContractorIssueRow
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? LocationText { get; set; }
    public string? RoomOrZone { get; set; }
    public int Status { get; set; }
    public int Priority { get; set; }
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? PlannedDate { get; set; }
    public bool IsOverdue { get; set; }
    public string CategoryName { get; set; } = "";
    public List<string> MediaFileIds { get; set; } = new();
    public int? PlanDocumentId { get; set; }
    public decimal? PlanXnormalized { get; set; }
    public decimal? PlanYnormalized { get; set; }
    public string? PlanImageUrl { get; set; }
    public bool HasPlan => PlanDocumentId.HasValue && PlanXnormalized.HasValue && PlanYnormalized.HasValue && !string.IsNullOrWhiteSpace(PlanImageUrl);
    public List<ContractorIssueComment> Comments { get; set; } = new();
}

public class ContractorIssueComment
{
    public string? AuthorName { get; set; }
    public bool IsContractor { get; set; }  // true = verstuurd vanuit portaal
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? PhotoUrl { get; set; }
}
