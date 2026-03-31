using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.ContractorPortal;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

[Authorize]
[Route("Portaal")]
public class ContractorPortalController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IConstructionIssueService _issueService;
    private readonly IConfiguration _configuration;

    public ContractorPortalController(
        cpmRunningContext db,
        IConstructionIssueService issueService,
        IConfiguration configuration)
    {
        _db = db;
        _issueService = issueService;
        _configuration = configuration;
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [HttpGet("")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var (companyIds, companyName) = await GetContractorContextAsync(ct);
        if (!companyIds.Any())
            return View(new ContractorDashboardVm { CompanyName = User.GetCpmDisplayName() ?? "" });

        var projectIds = await GetProjectIdsAsync(companyIds, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekEnd = today.AddDays(7);

        var issues = await _db.ConstructionIssue
            .AsNoTracking()
            .Where(i => projectIds.Contains(i.ProjectId) && companyIds.Contains(i.ResponsiblePartyId ?? 0))
            .Include(i => i.Project)
            .Include(i => i.Unit)
            .OrderByDescending(i => i.CreatedDate)
            .ToListAsync(ct);

        var openStatuses = new[] { (int)ConstructionIssueStatus.Open, (int)ConstructionIssueStatus.Reopened };
        var openIssues = issues.Where(i => openStatuses.Contains(i.Status)).ToList();

        // Progress: average of project voortgang for contractor's projects
        var voortgangen = await _db.ProjectVoortgang
            .AsNoTracking()
            .Where(v => projectIds.Contains(v.ProjectId))
            .ToListAsync(ct);
        var avgProgress = voortgangen.Any()
            ? voortgangen.Average(v => (double)v.FysiekeVoortgangPct)
            : 0;

        // Active project summaries (non-delivered)
        var projects = await _db.Project
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.ProjectId))
            .Include(p => p.PostalCodeNavigation)
            .ToListAsync(ct);

        var activeProjects = projects
            .Where(p => p.StatusId != (int)ProjectStatusType.Opgeleverd)
            .Select(p =>
            {
                var pIssues = issues.Where(i => i.ProjectId == p.ProjectId && openStatuses.Contains(i.Status)).ToList();
                var pVg = voortgangen.FirstOrDefault(v => v.ProjectId == p.ProjectId);
                return new ContractorProjectSummary
                {
                    ProjectId   = p.ProjectId,
                    Name        = p.ProjectName ?? $"Project {p.ProjectId}",
                    Address     = FormatAddress(p),
                    OpenCount   = pIssues.Count,
                    ProgressPct = pVg?.FysiekeVoortgangPct ?? 0m
                };
            })
            .OrderBy(p => p.Name)
            .Take(5)
            .ToList();

        var recentIssues = issues
            .Take(10)
            .Select(i => new ContractorRecentIssue
            {
                IssueId     = i.Id,
                ProjectId   = i.ProjectId,
                Title       = i.Title ?? "",
                ProjectName = i.Project?.ProjectName ?? $"Project {i.ProjectId}",
                UnitName    = i.Unit?.Name,
                Status      = i.Status,
                CreatedDate = i.CreatedDate
            })
            .ToList();

        var vm = new ContractorDashboardVm
        {
            CompanyName              = companyName,
            TotalOpen                = issues.Count(i => openStatuses.Contains(i.Status)),
            TotalOverdue             = openIssues.Count(i => i.DueDate.HasValue && i.DueDate.Value < today),
            TotalWaitingInspection   = issues.Count(i => i.Status == (int)ConstructionIssueStatus.WaitingInspection),
            TotalThisWeek            = openIssues.Count(i => i.DueDate.HasValue && i.DueDate.Value >= today && i.DueDate.Value <= weekEnd),
            OverallProgressPct       = (decimal)Math.Round(avgProgress, 1),
            ActiveProjects           = activeProjects,
            RecentIssues             = recentIssues
        };

        return View(vm);
    }

    // ── Werven ───────────────────────────────────────────────────────────────

    [HttpGet("Werven")]
    public async Task<IActionResult> Werven(string filter = "alle", string? search = null, CancellationToken ct = default)
    {
        var (companyIds, _) = await GetContractorContextAsync(ct);
        var projectIds = companyIds.Any() ? await GetProjectIdsAsync(companyIds, ct) : new List<int>();

        var projects = await _db.Project
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.ProjectId))
            .Include(p => p.PostalCodeNavigation)
            .ToListAsync(ct);

        var issues = companyIds.Any()
            ? await _db.ConstructionIssue
                .AsNoTracking()
                .Where(i => projectIds.Contains(i.ProjectId) && companyIds.Contains(i.ResponsiblePartyId ?? 0))
                .ToListAsync(ct)
            : new List<ConstructionIssue>();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var voortgangen = await _db.ProjectVoortgang
            .AsNoTracking()
            .Where(v => projectIds.Contains(v.ProjectId))
            .ToListAsync(ct);

        var openStatuses = new[] { (int)ConstructionIssueStatus.Open, (int)ConstructionIssueStatus.Reopened };
        var doneStatuses = new[] { (int)ConstructionIssueStatus.Resolved, (int)ConstructionIssueStatus.Rejected };

        var cards = projects.Select(p =>
        {
            var pIssues = issues.Where(i => i.ProjectId == p.ProjectId).ToList();
            var vg = voortgangen.FirstOrDefault(v => v.ProjectId == p.ProjectId);
            var isActive = p.StatusId != (int)ProjectStatusType.Opgeleverd;
            return new ContractorWerfCard
            {
                ProjectId   = p.ProjectId,
                Name        = p.ProjectName ?? $"Project {p.ProjectId}",
                Address     = FormatAddress(p),
                OpenCount   = pIssues.Count(i => openStatuses.Contains(i.Status)),
                TotalCount  = pIssues.Count,
                ResolvedCount = pIssues.Count(i => doneStatuses.Contains(i.Status)),
                UnitCount   = pIssues.Select(i => i.UnitId).Distinct().Count(u => u != null),
                OverdueCount = pIssues.Count(i => openStatuses.Contains(i.Status) && i.DueDate.HasValue && i.DueDate.Value < today),
                ProgressPct = vg?.FysiekeVoortgangPct ?? 0m,
                IsActive    = isActive
            };
        }).ToList();

        // Filter
        if (filter == "actief")
            cards = cards.Where(c => c.IsActive).ToList();
        else if (filter == "afgerond")
            cards = cards.Where(c => !c.IsActive).ToList();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
            cards = cards.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        cards = cards.OrderByDescending(c => c.IsActive).ThenBy(c => c.Name).ToList();

        return View(new ContractorWervenVm { Projects = cards, Filter = filter, Search = search });
    }

    // ── Werf Detail ──────────────────────────────────────────────────────────

    [HttpGet("Werven/{projectId:int}")]
    public async Task<IActionResult> WerfDetail(int projectId, CancellationToken ct)
    {
        var (companyIds, _) = await GetContractorContextAsync(ct);

        // Verify contractor has access to this project
        var hasAccess = companyIds.Any() &&
            await _db.Contract.AnyAsync(c => c.ProjectId == projectId && companyIds.Contains(c.CompanyId), ct);
        if (!hasAccess)
            return Forbid();

        var project = await _db.Project
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Include(p => p.PostalCodeNavigation)
            .FirstOrDefaultAsync(ct);
        if (project == null) return NotFound();

        var vg = await _db.ProjectVoortgang
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ProjectId == projectId, ct);

        var allIssues = await _db.ConstructionIssue
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId && companyIds.Contains(i.ResponsiblePartyId ?? 0))
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .Include(i => i.ConstructionIssueMedia)
            .OrderBy(i => i.UnitId).ThenBy(i => i.Title)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var openStatuses = new[] { (int)ConstructionIssueStatus.Open, (int)ConstructionIssueStatus.Reopened };
        var doneStatuses = new[] { (int)ConstructionIssueStatus.Resolved, (int)ConstructionIssueStatus.Rejected };

        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');

        var issueRows = allIssues.Select(i => new ContractorIssueRow
        {
            Id           = i.Id,
            Title        = i.Title ?? "",
            LocationText = i.LocationText,
            RoomOrZone   = i.RoomOrZone,
            Status       = i.Status,
            Priority     = i.Priority,
            Description  = i.Description,
            DueDate      = i.DueDate,
            PlannedDate  = i.PlannedDate,
            IsOverdue    = openStatuses.Contains(i.Status) && i.DueDate.HasValue && i.DueDate.Value < today,
            CategoryName = i.Category?.Name ?? "",
            MediaFileIds = i.ConstructionIssueMedia.Select(m => m.FileId).ToList()
        }).ToList();

        // Group by unit
        var groups = issueRows
            .GroupBy(r => allIssues.First(i => i.Id == r.Id).UnitId)
            .Select(g =>
            {
                var unitName = g.Key.HasValue
                    ? allIssues.First(i => i.UnitId == g.Key).Unit?.Name ?? $"Eenheid {g.Key}"
                    : "Algemeen";
                return new ContractorIssueGroup
                {
                    GroupName = unitName,
                    UnitId    = g.Key,
                    Issues    = g.ToList()
                };
            })
            .OrderBy(g => g.UnitId == null ? 0 : 1)
            .ThenBy(g => g.GroupName)
            .ToList();

        var vm = new ContractorWerfDetailVm
        {
            ProjectId              = projectId,
            Name                   = project.ProjectName ?? $"Project {projectId}",
            Address                = FormatAddress(project),
            ProgressPct            = vg?.FysiekeVoortgangPct ?? 0m,
            OpenCount              = allIssues.Count(i => openStatuses.Contains(i.Status)),
            InProgressCount        = allIssues.Count(i => i.Status == (int)ConstructionIssueStatus.InProgress),
            WaitingInspectionCount = allIssues.Count(i => i.Status == (int)ConstructionIssueStatus.WaitingInspection),
            ResolvedCount          = allIssues.Count(i => doneStatuses.Contains(i.Status)),
            IssueGroups            = groups,
            StorageBaseUrl         = baseUrl
        };

        return View(vm);
    }

    // ── AJAX: markeer punt als klaar voor controle ────────────────────────────

    [HttpPost("Werven/{projectId:int}/Issues/{issueId:int}/Resolve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveIssue(int projectId, int issueId, CancellationToken ct)
    {
        var (companyIds, _) = await GetContractorContextAsync(ct);
        var issue = await _db.ConstructionIssue
            .FirstOrDefaultAsync(i => i.Id == issueId && i.ProjectId == projectId && companyIds.Contains(i.ResponsiblePartyId ?? 0), ct);

        if (issue == null) return Json(new { ok = false, error = "Punt niet gevonden." });

        var userId = User.GetCpmUserCode();
        var ok = await _issueService.ChangeStatus(projectId, issueId,
            (int)ConstructionIssueStatus.WaitingInspection, null, userId);

        return Json(new { ok });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(List<int> companyIds, string companyName)> GetContractorContextAsync(CancellationToken ct)
    {
        var userId = User.GetCpmUserId();
        if (userId == null) return (new List<int>(), "");

        var access = await _db.UserCompanyAccess
            .AsNoTracking()
            .Where(a => a.UserId == userId.Value)
            .Include(a => a.Company)
            .ToListAsync(ct);

        var companyIds = access.Select(a => a.CompanyId).Distinct().ToList();
        var companyName = access.FirstOrDefault()?.Company?.BedrijfsNaam ?? User.GetCpmDisplayName() ?? "";
        return (companyIds, companyName);
    }

    private async Task<List<int>> GetProjectIdsAsync(List<int> companyIds, CancellationToken ct)
    {
        return await _db.Contract
            .AsNoTracking()
            .Where(c => companyIds.Contains(c.CompanyId))
            .Select(c => c.ProjectId)
            .Distinct()
            .ToListAsync(ct);
    }

    private static string FormatAddress(Project p)
    {
        var street = string.IsNullOrWhiteSpace(p.Number)
            ? p.Street : $"{p.Street} {p.Number}";
        var city = p.PostalCodeNavigation != null
            ? $"{p.PostalCodeNavigation.Postcode} {p.PostalCodeNavigation.Gemeente}"
            : null;
        return string.Join(", ", new[] { street, city }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
