using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.ContractorPortal;
using CPMCore.Services;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace CPMCore.Controllers;

[Authorize]
[Route("Werfportaal")]
public class ContractorPortalController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IConstructionIssueService _issueService;
    private readonly IConstructionIssueReportService _reportService;
    private readonly IConfiguration _configuration;

    public ContractorPortalController(
        cpmRunningContext db,
        IConstructionIssueService issueService,
        IConstructionIssueReportService reportService,
        IConfiguration configuration)
    {
        _db = db;
        _issueService = issueService;
        _reportService = reportService;
        _configuration = configuration;
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [HttpGet("")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var (companyIds, companyName, isFullAdmin, limitToContactIds) = await GetContractorContextAsync(ct);
        if (!companyIds.Any())
            return View(new ContractorDashboardVm { CompanyName = User.GetCpmDisplayName() ?? "" });

        var projectIds = await GetProjectIdsAsync(companyIds, limitToContactIds, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekEnd = today.AddDays(7);

        var issueQuery = _db.ConstructionIssue
            .AsNoTracking()
            .Where(i => projectIds.Contains(i.ProjectId))
            .Where(i => companyIds.Contains(i.ResponsiblePartyId ?? 0));

        var issues = await issueQuery
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
            .Include(p => p.PostalCode)
            .ToListAsync(ct);

        var doneStatuses = new[] { (int)ConstructionIssueStatus.Resolved, (int)ConstructionIssueStatus.Rejected };

        var activeProjects = projects
            .Where(p => p.StatusId != (int)ProjectStatusType.Opgeleverd)
            .Select(p =>
            {
                var pAll     = issues.Where(i => i.ProjectId == p.ProjectId).ToList();
                var pVg      = voortgangen.FirstOrDefault(v => v.ProjectId == p.ProjectId);
                return new ContractorProjectSummary
                {
                    ProjectId    = p.ProjectId,
                    Name         = p.ProjectName ?? $"Project {p.ProjectId}",
                    Address      = FormatAddress(p),
                    OpenCount    = pAll.Count(i => openStatuses.Contains(i.Status)),
                    TotalCount   = pAll.Count,
                    ResolvedCount= pAll.Count(i => doneStatuses.Contains(i.Status)),
                    ProgressPct  = pVg?.FysiekeVoortgangPct ?? 0m
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
        var (companyIds, _, isFullAdmin, limitToContactIds) = await GetContractorContextAsync(ct);
        var projectIds = companyIds.Any() ? await GetProjectIdsAsync(companyIds, limitToContactIds, ct) : new List<int>();

        var projects = await _db.Project
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.ProjectId))
            .Include(p => p.PostalCode)
            .ToListAsync(ct);

        List<ConstructionIssue> issues;
        if (!companyIds.Any())
        {
            issues = new List<ConstructionIssue>();
        }
        else
        {
            issues = await _db.ConstructionIssue.AsNoTracking()
                .Where(i => projectIds.Contains(i.ProjectId))
                .Where(i => companyIds.Contains(i.ResponsiblePartyId ?? 0))
                .ToListAsync(ct);
        }

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
        var (companyIds, _, isFullAdmin, limitToContactIds) = await GetContractorContextAsync(ct);

        // Verify contractor has access to this project
        var contractQuery = _db.Contract.Where(c => c.ProjectId == projectId && companyIds.Contains(c.CompanyId));
        if (!isFullAdmin && limitToContactIds != null && limitToContactIds.Any())
            contractQuery = contractQuery.Where(c => c.SiteManagerContactId.HasValue &&
                                                     limitToContactIds.Contains(c.SiteManagerContactId.Value));
        var hasAccess = companyIds.Any() && await contractQuery.AnyAsync(ct);
        if (!hasAccess)
            return Forbid();

        var project = await _db.Project
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Include(p => p.PostalCode)
            .FirstOrDefaultAsync(ct);
        if (project == null) return NotFound();

        var vg = await _db.ProjectVoortgang
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ProjectId == projectId, ct);

        var issueQuery = _db.ConstructionIssue
            .AsNoTracking()
            .Where(i => i.ProjectId == projectId)
            .Where(i => companyIds.Contains(i.ResponsiblePartyId ?? 0));

        var allIssues = await issueQuery
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .Include(i => i.ConstructionIssueMedia)
            .OrderBy(i => i.UnitId).ThenBy(i => i.Title)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var openStatuses = new[] { (int)ConstructionIssueStatus.Open, (int)ConstructionIssueStatus.Reopened };
        var doneStatuses = new[] { (int)ConstructionIssueStatus.Resolved, (int)ConstructionIssueStatus.Rejected };

        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');

        // Plan URLs voor issues die een locatie op plan hebben
        var planUnitIds = allIssues
            .Where(i => i.PlanDocumentId.HasValue && i.UnitId.HasValue)
            .Select(i => i.UnitId!.Value).Distinct().ToList();
        var planUnits = planUnitIds.Any()
            ? await _db.Units.AsNoTracking().Where(u => planUnitIds.Contains(u.Id)).ToListAsync(ct)
            : new List<Units>();
        var executionPlanIds = allIssues
            .Where(i => i.PlanDocumentId.HasValue && i.PlanDocumentId.Value > 0)
            .Select(i => i.PlanDocumentId!.Value).Distinct().ToList();
        var execPlans = executionPlanIds.Any()
            ? await _db.UnitExecutionPlan.AsNoTracking().Where(p => executionPlanIds.Contains(p.Id)).ToListAsync(ct)
            : new List<UnitExecutionPlan>();

        // Load comments for all issues (action=6 CommentAdded)
        var issueIds = allIssues.Select(i => i.Id).ToList();
        var historyComments = issueIds.Any()
            ? await _db.ConstructionIssueHistory
                .AsNoTracking()
                .Where(h => issueIds.Contains(h.IssueId) && h.Action == (int)ConstructionIssueHistoryAction.CommentAdded)
                .OrderBy(h => h.Timestamp)
                .ToListAsync(ct)
            : new List<ConstructionIssueHistory>();

        // Resolve author names
        var commentUserIds = historyComments
            .Where(h => h.UserId != null).Select(h => h.UserId!).Distinct().ToList();
        var commentUserData = commentUserIds.Any()
            ? await _db.Users.AsNoTracking()
                .Where(u => commentUserIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Id, u.Voornaam, u.Familienaam })
                .ToListAsync(ct)
            : new[] { new { UserId = "", Id = 0, Voornaam = "", Familienaam = "" } }.Take(0).ToList();
        var commentUserMap = commentUserData.ToDictionary(u => u.UserId, u => ($"{u.Voornaam} {u.Familienaam}".Trim(), u.Id));
        var commentUserIntIdList = commentUserData.Select(u => u.Id).ToList();
        var contractorUserIntIds = commentUserIntIdList.Any()
            ? (await _db.UserCompanyAccess.AsNoTracking()
                .Where(a => commentUserIntIdList.Contains(a.UserId))
                .Select(a => a.UserId).Distinct().ToListAsync(ct)).ToHashSet()
            : new HashSet<int>();

        var issueRows = allIssues.Select(i => {
            string? planUrl = null;
            if (i.PlanDocumentId.HasValue && i.UnitId.HasValue && i.PlanXnormalized.HasValue && i.PlanYnormalized.HasValue)
            {
                if (i.PlanDocumentId.Value == 0)
                {
                    var unit = planUnits.FirstOrDefault(u => u.Id == i.UnitId.Value);
                    if (!string.IsNullOrWhiteSpace(unit?.Plan))
                        planUrl = $"/Werfportaal/PlanImage?fileId={Uri.EscapeDataString(unit.Plan)}";
                }
                else
                {
                    var ep = execPlans.FirstOrDefault(p => p.Id == i.PlanDocumentId.Value);
                    if (!string.IsNullOrWhiteSpace(ep?.FileId))
                        planUrl = $"/Werfportaal/PlanImage?fileId={Uri.EscapeDataString(ep.FileId)}";
                }
            }

            var comments = historyComments
                .Where(h => h.IssueId == i.Id)
                .Select(h => {
                    var (authorName, authorIntId) = h.UserId != null && commentUserMap.TryGetValue(h.UserId, out var u) ? u : ("Onbekend", 0);
                    var photoUrls = new List<string>();
                    if (!string.IsNullOrWhiteSpace(h.NewValueJson))
                    {
                        try {
                            using var doc = System.Text.Json.JsonDocument.Parse(h.NewValueJson);
                            // Support both single photoUrl (legacy) and photoUrls array
                            if (doc.RootElement.TryGetProperty("photoUrls", out var arrEl) && arrEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                                photoUrls = arrEl.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).Cast<string>().ToList();
                            else if (doc.RootElement.TryGetProperty("photoUrl", out var singleEl))
                            {
                                var s = singleEl.GetString();
                                if (s != null) photoUrls.Add(s);
                            }
                        } catch { }
                    }
                    return new ContractorIssueComment
                    {
                        AuthorName   = string.IsNullOrWhiteSpace(authorName) ? "Onbekend" : authorName,
                        IsContractor = contractorUserIntIds.Contains(authorIntId),
                        Text         = h.Comment ?? "",
                        Timestamp    = h.Timestamp,
                        PhotoUrls    = photoUrls
                    };
                }).ToList();

            return new ContractorIssueRow
            {
                Id               = i.Id,
                Title            = i.Title ?? "",
                LocationText     = i.LocationText,
                RoomOrZone       = i.RoomOrZone,
                Status           = i.Status,
                Priority         = i.Priority,
                Description      = i.Description,
                DueDate          = i.DueDate,
                PlannedDate      = i.PlannedDate,
                IsOverdue        = openStatuses.Contains(i.Status) && i.DueDate.HasValue && i.DueDate.Value < today,
                CategoryName     = i.Category?.Name ?? "",
                MediaFileIds     = i.ConstructionIssueMedia.Select(m => m.FileId).ToList(),
                PlanDocumentId   = i.PlanDocumentId,
                PlanXnormalized  = i.PlanXnormalized,
                PlanYnormalized  = i.PlanYnormalized,
                PlanPageNumber   = i.PlanPageNumber,
                PlanImageUrl     = planUrl,
                Comments         = comments
            };
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
        var (companyIds, _, _, _) = await GetContractorContextAsync(ct);
        var issue = await _db.ConstructionIssue
            .Where(i => i.Id == issueId && i.ProjectId == projectId)
            .Where(i => companyIds.Contains(i.ResponsiblePartyId ?? 0))
            .FirstOrDefaultAsync(ct);

        if (issue == null) return Json(new { ok = false, error = "Punt niet gevonden." });

        var userId = User.GetCpmUserCode();
        var ok = await _issueService.ChangeStatus(projectId, issueId,
            (int)ConstructionIssueStatus.WaitingInspection, null, userId);

        return Json(new { ok });
    }

    // ── AJAX: opmerking toevoegen ────────────────────────────────────────────

    [HttpPost("Werven/{projectId:int}/Issues/{issueId:int}/Comment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int projectId, int issueId, [FromBody] AddCommentRequest req, CancellationToken ct)
    {
        var hasPhotos = req?.PhotoUrls?.Any(u => !string.IsNullOrWhiteSpace(u)) == true;
        if (string.IsNullOrWhiteSpace(req?.Comment) && !hasPhotos)
            return Json(new { ok = false, error = "Opmerking of foto vereist." });

        var (companyIds, _, _, _) = await GetContractorContextAsync(ct);
        var issue = await _db.ConstructionIssue
            .Where(i => i.Id == issueId && i.ProjectId == projectId)
            .Where(i => companyIds.Contains(i.ResponsiblePartyId ?? 0))
            .FirstOrDefaultAsync(ct);
        if (issue == null) return Json(new { ok = false, error = "Punt niet gevonden." });

        var cleanUrls = req?.PhotoUrls?.Where(u => !string.IsNullOrWhiteSpace(u)).ToList() ?? new List<string>();
        var photoJson = cleanUrls.Any()
            ? System.Text.Json.JsonSerializer.Serialize(new { photoUrls = cleanUrls })
            : null;

        await _issueService.AddHistory(issueId, (int)ConstructionIssueHistoryAction.CommentAdded,
            User.GetCpmUserCode(), null, photoJson, req.Comment);

        return Json(new { ok = true });
    }

    // ── AJAX: foto uploaden ──────────────────────────────────────────────────

    [HttpPost("Werven/{projectId:int}/Issues/{issueId:int}/UploadPhoto")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(int projectId, int issueId, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return Json(new { ok = false, error = "Geen bestand geselecteerd." });

        var (companyIds, _, _, _) = await GetContractorContextAsync(ct);
        var issue = await _db.ConstructionIssue
            .Where(i => i.Id == issueId && i.ProjectId == projectId)
            .Where(i => companyIds.Contains(i.ResponsiblePartyId ?? 0))
            .FirstOrDefaultAsync(ct);
        if (issue == null) return Json(new { ok = false, error = "Punt niet gevonden." });

        var fileId = await UploadToStorageAsync(file, "pictures", ct);
        if (string.IsNullOrWhiteSpace(fileId))
            return Json(new { ok = false, error = "Upload mislukt." });

        // Photo is attached to the comment — do NOT add to issue media separately
        var storageBase = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var url = $"{storageBase}/pictures/{Uri.EscapeDataString(System.IO.Path.GetFileName(fileId))}";
        return Json(new { ok = true, url, fileId });
    }

    // ── PDF rapport download ─────────────────────────────────────────────────

    [HttpGet("Rapport/{reportId:int}")]
    public async Task<IActionResult> DownloadRapport(int reportId, CancellationToken ct)
    {
        var (companyIds, _, _, _) = await GetContractorContextAsync(ct);
        if (!companyIds.Any()) return Forbid();

        var report = await _db.ConstructionIssueReport
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId
                && r.ResponsiblePartyId.HasValue
                && companyIds.Contains(r.ResponsiblePartyId.Value), ct);

        if (report == null) return NotFound();

        var bytes = await _reportService.GenerateReportPdf(report.ProjectId, reportId);
        return File(bytes, "application/pdf", $"Openstaande_punten_{reportId}_{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    // ── Plan image proxy ─────────────────────────────────────────────────────

    [HttpGet("PlanImage")]
    public async Task<IActionResult> PlanImage(string fileId, CancellationToken ct)
    {
        var safeFile = System.IO.Path.GetFileName(fileId ?? "");
        if (string.IsNullOrWhiteSpace(safeFile)) return NotFound();

        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var readKey = _configuration["StorageApi:ReadApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(readKey))
            return NotFound();

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("X-Api-Key", readKey);
        var signResp = await http.PostAsync($"{baseUrl}/api/assets/plans/{Uri.EscapeDataString(safeFile)}/sign", null, ct);
        if (!signResp.IsSuccessStatusCode) return NotFound();

        var payload = await signResp.Content.ReadAsStringAsync(ct);
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(payload);
        if (!jsonDoc.RootElement.TryGetProperty("url", out var urlEl)) return NotFound();

        var signedUrl = urlEl.GetString() ?? "";
        if (!signedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            signedUrl = baseUrl + signedUrl;

        var imgResp = await http.GetAsync(signedUrl, ct);
        if (!imgResp.IsSuccessStatusCode) return NotFound();

        var contentType = imgResp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var stream = await imgResp.Content.ReadAsStreamAsync(ct);
        return File(stream, contentType);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(List<int> companyIds, string companyName, bool isFullAdmin, List<int>? limitToContactIds)> GetContractorContextAsync(CancellationToken ct)
    {
        var userId = User.GetCpmUserId();
        if (userId == null) return (new List<int>(), "", false, null);

        var access = await _db.UserCompanyAccess
            .AsNoTracking()
            .Where(a => a.UserId == userId.Value)
            .Include(a => a.Company)
            .ToListAsync(ct);

        var companyIds  = access.Select(a => a.CompanyId).Distinct().ToList();
        var companyName = access.FirstOrDefault()?.Company?.BedrijfsNaam ?? User.GetCpmDisplayName() ?? "";
        var isFullAdmin = access.Any(a => a.Role == ContractorAccessRole.VolledigVerantwoordelijk);

        // Beperkte toegang: lijst van ContactIds waarvoor deze gebruiker SiteManager is
        List<int>? limitToContactIds = null;
        if (!isFullAdmin)
        {
            limitToContactIds = access
                .Where(a => a.Role == ContractorAccessRole.Beperkt && a.ContactId.HasValue)
                .Select(a => a.ContactId!.Value)
                .Distinct()
                .ToList();
        }

        return (companyIds, companyName, isFullAdmin, limitToContactIds);
    }

    private async Task<List<int>> GetProjectIdsAsync(List<int> companyIds, List<int>? limitToContactIds, CancellationToken ct)
    {
        var query = _db.Contract
            .AsNoTracking()
            .Where(c => companyIds.Contains(c.CompanyId));

        // Beperkt: enkel projecten waarvoor dit contact als SiteManager staat
        if (limitToContactIds != null && limitToContactIds.Any())
            query = query.Where(c => c.SiteManagerContactId.HasValue &&
                                     limitToContactIds.Contains(c.SiteManagerContactId.Value));

        return await query
            .Select(c => c.ProjectId)
            .Distinct()
            .ToListAsync(ct);
    }

    private static string FormatAddress(Project p)
    {
        var street = string.IsNullOrWhiteSpace(p.Number)
            ? p.Street : $"{p.Street} {p.Number}";
        var city = p.PostalCode != null
            ? $"{p.PostalCode.Postcode} {p.PostalCode.Gemeente}"
            : null;
        return string.Join(", ", new[] { street, city }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private async Task<string?> UploadToStorageAsync(IFormFile file, string folder, CancellationToken ct)
    {
        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var writeKey = _configuration["StorageApi:WriteApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
            return null;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("X-Api-Key", writeKey);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(folder), "folder");
        await using var fileStream = file.OpenReadStream();
        var fileContent = new StreamContent(fileStream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var response = await http.PostAsync($"{baseUrl}/api/assets/upload", content, ct);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("fileId", out var el) ? el.GetString() : null;
    }
}

public record AddCommentRequest(string? Comment, List<string>? PhotoUrls = null);
