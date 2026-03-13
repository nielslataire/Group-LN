using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Issues;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace CPMCore.Controllers;

[Authorize(Policy = "Permission:Projecten")]
[Route("Projects/{projectId:int}/Issues")]
public class ProjectsIssuesController : BaseController
{
    private readonly IConstructionIssueService _service;
    private readonly IConstructionIssueReportService _reportService;
    private readonly cpmRunningContext _db;
    private readonly IConfiguration _configuration;

    public ProjectsIssuesController(IConstructionIssueService service, IConstructionIssueReportService reportService, cpmRunningContext db, IConfiguration configuration)
    {
        _service = service;
        _reportService = reportService;
        _db = db;
        _configuration = configuration;
    }

    [HttpGet("~/Projects/Issues")]
    public IActionResult MenuRedirect()
    {
        AddMessage("info", "Selecteer eerst een project om punten te beheren.", "Info");
        return RedirectToAction("Index", "Projecten");
    }

    [HttpGet("")]
    [Breadcrumb("Punten")]
    public async Task<IActionResult> Index(int projectId, [FromQuery] ConstructionIssueFilterBO filters)
    {
        var formVm = await BuildVm(projectId);
        var vm = new ConstructionIssueIndexVm
        {
            ProjectId = projectId,
            Filters = filters,
            Categories = formVm.Categories,
            Units = formVm.Units,
            Form = formVm,
            Issues = await _service.Search(projectId, filters)
        };
        return View(vm);
    }

    [HttpPost("BulkUpdate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpdate(int projectId, ConstructionIssueBulkUpdateBO dto)
    {
        var updated = await _service.BulkUpdate(projectId, dto, User.FindFirst(CpmClaims.UserId)?.Value);
        AddMessage("success", $"{updated} punt(en) bijgewerkt.", "Geslaagd");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("SendSelected")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSelected(int projectId, ConstructionIssueSendRequestBO request, string? comment)
    {
        var sent = await _reportService.SendSelectedIssues(projectId, request, User.FindFirst(CpmClaims.UserId)?.Value, comment);
        AddMessage(sent > 0 ? "success" : "error", sent > 0 ? $"{sent} punt(en) verzonden." : "Geen punten verzonden (geen geldige e-mail).", sent > 0 ? "Geslaagd" : "Fout");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("Reminder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reminder(int projectId, List<int> issueIds)
    {
        var sent = await _reportService.SendReminder(projectId, issueIds, User.FindFirst(CpmClaims.UserId)?.Value);
        AddMessage(sent > 0 ? "success" : "error", sent > 0 ? $"{sent} herinnering(en) verzonden." : "Geen herinneringen verzonden.", sent > 0 ? "Geslaagd" : "Fout");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("DownloadPdf")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadPdf(int projectId, List<int> issueIds, int reportType = 0)
    {
        if (issueIds == null || issueIds.Count == 0)
            return RedirectToAction(nameof(Index), new { projectId });

        var first = await _service.GetById(projectId, issueIds.First());
        if (first == null)
            return RedirectToAction(nameof(Index), new { projectId });

        var report = await _reportService.CreateReportEntity(projectId, reportType, first.ResponsiblePartyType, first.ResponsiblePartyId, first.ResponsibleOtherName, first.ResponsibleOtherEmail, issueIds, User.FindFirst(CpmClaims.UserId)?.Value);
        var bytes = await _reportService.GenerateReportPdf(projectId, report.Id);
        return File(bytes, "application/pdf", $"puntenlijst_{projectId}_{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    [HttpGet("Create")]
    public IActionResult Create(int projectId)
    {
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int projectId, ConstructionIssueFormVm vm, List<IFormFile>? mediaFiles)
    {
        vm.Input.ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor;
        mediaFiles = ResolveMediaFiles(mediaFiles);
        if (!ModelState.IsValid)
        {
            PreserveMediaValidation(ModelState);
            AddMessage("error", "Punt kon niet opgeslagen worden. Controleer de ingevulde velden.", "Fout");
            return RedirectToAction(nameof(Index), new { projectId });
        }

        var created = await _service.Create(projectId, vm.Input, User.FindFirst(CpmClaims.UserId)?.Value);
        await AddIssueMedia(projectId, created.Id, mediaFiles);
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpGet("Edit/{id:int}")]
    public IActionResult Edit(int projectId, int id)
    {
        return RedirectToAction(nameof(Index), new { projectId });
    }
    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int projectId, int id, ConstructionIssueFormVm vm, List<IFormFile>? mediaFiles, List<int>? deleteMediaIds)
    {
        vm.Input.ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor;
        mediaFiles = ResolveMediaFiles(mediaFiles);
        if (!ModelState.IsValid)
        {
            PreserveMediaValidation(ModelState);
            AddMessage("error", "Punt kon niet opgeslagen worden. Controleer de ingevulde velden.", "Fout");
            return RedirectToAction(nameof(Index), new { projectId });
        }

        var updated = await _service.Update(projectId, id, vm.Input, User.FindFirst(CpmClaims.UserId)?.Value);
        if (updated == null) return NotFound();

        if (deleteMediaIds != null)
        {
            foreach (var mediaId in deleteMediaIds.Distinct())
            {
                await _service.DeleteMedia(projectId, id, mediaId, User.FindFirst(CpmClaims.UserId)?.Value);
            }
        }

        await AddIssueMedia(projectId, id, mediaFiles);
        return RedirectToAction(nameof(Index), new { projectId });
    }



    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int projectId, int id)
    {
        var issue = await _service.GetById(projectId, id);
        if (issue == null) return NotFound();

        var media = await _service.GetMedia(projectId, id);
        var vm = new ConstructionIssueDetailsVm
        {
            ProjectId = projectId,
            Issue = issue,
            Notifications = await _service.GetNotifications(projectId, id),
            MediaUrls = media.ToDictionary(x => x.Id, x => GetSignedAssetUrlByFileName(x.FileId, "pictures"))
        };
        return View(vm);
    }

    [HttpPost("UploadMedia/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadMedia(int projectId, int id, List<IFormFile> files, int mediaType = 0)
    {
        if (files == null || files.Count == 0)
        {
            AddMessage("error", "Geen bestanden geselecteerd.", "Fout");
            return RedirectToAction(nameof(Details), new { projectId, id });
        }

        foreach (var file in files.Where(x => x?.Length > 0))
        {
            var fileId = await UploadAssetToStorageAsync(file, "pictures");
            if (!string.IsNullOrWhiteSpace(fileId))
            {
                await _service.AddMedia(projectId, id, fileId, mediaType, User.FindFirst(CpmClaims.UserId)?.Value);
            }
        }

        AddMessage("success", "Bijlage(n) toegevoegd.", "Geslaagd");
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("DeleteMedia/{id:int}/{mediaId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMedia(int projectId, int id, int mediaId)
    {
        await _service.DeleteMedia(projectId, id, mediaId, User.FindFirst(CpmClaims.UserId)?.Value);
        AddMessage("success", "Bijlage verwijderd.", "Geslaagd");
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("SyncMedia/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncMedia(int projectId, int id, List<IFormFile>? files, List<int>? deleteMediaIds)
    {
        if (deleteMediaIds != null)
        {
            foreach (var mediaId in deleteMediaIds.Distinct())
            {
                await _service.DeleteMedia(projectId, id, mediaId, User.FindFirst(CpmClaims.UserId)?.Value);
            }
        }

        if (files != null && files.Count > 0)
        {
            await AddIssueMedia(projectId, id, files);
        }

        AddMessage("success", "Bijlagen bijgewerkt.", "Geslaagd");
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpGet("ModalDelete/{id:int}")]
    public async Task<IActionResult> ModalDelete(int projectId, int id)
    {
        var issue = await _service.GetById(projectId, id);
        if (issue == null) return NotFound();

        ViewBag.ProjectId = projectId;
        return PartialView("Modals/_ModalDeleteIssue", new IdNameBO
        {
            ID = id,
            Display = issue.Title
        });
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int projectId, int id)
    {
        var deleted = await _service.Delete(projectId, id, User.FindFirst(CpmClaims.UserId)?.Value);
        AddMessage(deleted ? "success" : "error", deleted ? "Punt verwijderd." : "Punt niet gevonden.", deleted ? "Geslaagd" : "Fout");
        return RedirectToAction(nameof(Index), new { projectId });
    }


    [HttpPost("ChangeStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int projectId, int id, int newStatus, string? comment)
    {
        await _service.ChangeStatus(projectId, id, newStatus, comment, User.FindFirst(CpmClaims.UserId)?.Value);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpGet("DetailData/{id:int}")]
    public async Task<IActionResult> DetailData(int projectId, int id)
    {
        var issue = await _service.GetById(projectId, id);
        if (issue == null) return NotFound();

        var media = await BuildMediaVm(projectId, id);
        var notifications = await _service.GetNotifications(projectId, id);
        var plan = await ResolveIssuePlan(projectId, issue);

        return Json(new
        {
            issue.Id,
            issue.Title,
            issue.Description,
            issue.LocationText,
            issue.BuildingPart,
            issue.RoomOrZone,
            unitName = issue.Unit?.Name,
            categoryName = issue.Category?.Name,
            issueType = issue.IssueType,
            issueTypeLabel = GetEnumDisplayName<ConstructionIssueType>(issue.IssueType),
            issuePhase = issue.IssuePhase,
            issuePhaseLabel = GetEnumDisplayName<ConstructionIssuePhase>(issue.IssuePhase),
            priority = issue.Priority,
            priorityLabel = GetEnumDisplayName<ConstructionIssuePriority>(issue.Priority),
            status = issue.Status,
            statusLabel = GetEnumDisplayName<ConstructionIssueStatus>(issue.Status),
            issue.ResponsiblePartyId,
            issue.ResponsibleOtherName,
            issue.ResponsibleOtherEmail,
            dueDate = issue.DueDate?.ToString("yyyy-MM-dd"),
            planDocumentId = issue.PlanDocumentId,
            planPageNumber = issue.PlanPageNumber,
            planXnormalized = issue.PlanXnormalized,
            planYnormalized = issue.PlanYnormalized,
            plan,
            media = media.Select(m => new { m.Id, m.Url }),
            history = issue.ConstructionIssueHistory
                .OrderByDescending(h => h.Timestamp)
                .Select(h => new
                {
                    h.Id,
                    h.Action,
                    actionLabel = GetEnumDisplayName<ConstructionIssueHistoryAction>(h.Action),
                    h.Comment,
                    timestamp = h.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                    h.OldValueJson,
                    h.NewValueJson
                }),
            notifications = notifications
                .OrderByDescending(n => n.SentDate)
                .Select(n => new
                {
                    n.Id,
                    n.RecipientEmail,
                    sentDate = n.SentDate.ToString("dd/MM/yyyy HH:mm"),
                    n.IsReminder
                })
        });
    }


    [HttpGet("EditData/{id:int}")]
    public async Task<IActionResult> EditData(int projectId, int id)
    {
        var issue = await _service.GetById(projectId, id);
        if (issue == null) return NotFound();

        var media = await BuildMediaVm(projectId, id);
        return Json(new
        {
            issue.Id,
            issue.Title,
            issue.Description,
            issue.LocationText,
            issue.CategoryId,
            issue.BuildingPart,
            issue.RoomOrZone,
            issue.UnitId,
            issue.IssueType,
            issue.IssuePhase,
            issue.Priority,
            issue.Status,
            issue.ResponsiblePartyType,
            issue.ResponsiblePartyId,
            issue.ResponsibleOtherName,
            issue.ResponsibleOtherEmail,
            issue.PlanDocumentId,
            issue.PlanPageNumber,
            issue.PlanXnormalized,
            issue.PlanYnormalized,
            dueDate = issue.DueDate?.ToString("yyyy-MM-dd"),
            media = media.Select(m => new { m.Id, m.Url })
        });
    }

    [HttpPost("SendPreview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPreview(int projectId, [FromForm] IssueSendPreviewRequest request)
    {
        var query = _db.ConstructionIssue
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId);

        var selectedIds = (request.IssueIds ?? new List<int>()).Distinct().ToList();
        var scope = (request.Scope ?? "all").Trim().ToLowerInvariant();

        if (scope == "selected")
        {
            if (!selectedIds.Any())
            {
                return Json(new { units = Array.Empty<object>(), responsibles = Array.Empty<object>(), totalIssueCount = 0, totalResponsibleCount = 0 });
            }

            query = query.Where(x => selectedIds.Contains(x.Id));
        }

        if (scope == "unit" && request.UnitId.HasValue)
        {
            query = query.Where(x => x.UnitId == request.UnitId.Value);
        }

        var issues = await query
            .Select(x => new
            {
                x.Id,
                x.UnitId,
                UnitName = x.Unit != null ? x.Unit.Name : null,
                x.ResponsiblePartyType,
                x.ResponsiblePartyId,
                x.ResponsibleOtherName,
                x.ResponsibleOtherEmail
            })
            .ToListAsync();

        var companyIds = issues
            .Where(x => x.ResponsiblePartyId.HasValue)
            .Select(x => x.ResponsiblePartyId!.Value)
            .Distinct()
            .ToList();

        var companyInfo = await _db.CompanyInfo
            .AsNoTracking()
            .Where(x => companyIds.Contains(x.CompanyId))
            .Select(x => new { x.CompanyId, x.BedrijfsNaam, x.Email, x.InvoiceEmail })
            .ToListAsync();

        var companyLookup = companyInfo.ToDictionary(
            x => x.CompanyId,
            x => new
            {
                Name = string.IsNullOrWhiteSpace(x.BedrijfsNaam) ? $"Bedrijf #{x.CompanyId}" : x.BedrijfsNaam,
                Email = !string.IsNullOrWhiteSpace(x.InvoiceEmail) ? x.InvoiceEmail : x.Email
            });

        string BuildResponsibleKey(int type, int? partyId, string? otherName, string? otherEmail)
            => $"{type}|{partyId?.ToString() ?? ""}|{otherName ?? ""}|{otherEmail ?? ""}";

        var units = issues
            .Where(x => x.UnitId.HasValue)
            .GroupBy(x => new { UnitId = x.UnitId!.Value, UnitName = x.UnitName ?? $"Eenheid {x.UnitId!.Value}" })
            .Select(g => new { id = g.Key.UnitId, name = g.Key.UnitName, issueCount = g.Select(i => i.Id).Distinct().Count() })
            .OrderBy(x => x.name)
            .ToList();

        if (scope == "unit" && !request.UnitId.HasValue)
        {
            return Json(new { units, responsibles = Array.Empty<object>(), totalIssueCount = 0, totalResponsibleCount = 0 });
        }

        var responsibles = issues
                   .Select(x =>
                   {
                       var company = x.ResponsiblePartyId.HasValue && companyLookup.TryGetValue(x.ResponsiblePartyId.Value, out var found)
                           ? found
                           : null;

                       var email = !string.IsNullOrWhiteSpace(x.ResponsibleOtherEmail)
                           ? x.ResponsibleOtherEmail
                           : company?.Email;

                       var name = !string.IsNullOrWhiteSpace(x.ResponsibleOtherName)
                           ? x.ResponsibleOtherName
                           : company?.Name ?? "Onbekend";

                       var aggregateKey = x.ResponsiblePartyId.HasValue
                           ? $"party:{x.ResponsiblePartyId.Value}"
                           : $"email:{(email ?? string.Empty).Trim().ToLowerInvariant()}";

                       return new
                       {
                           aggregateKey,
                           originalKey = BuildResponsibleKey(x.ResponsiblePartyType, x.ResponsiblePartyId, x.ResponsibleOtherName, x.ResponsibleOtherEmail),
                           name,
                           email,
                           issueId = x.Id
                       };
                   })
                   .Where(x => !string.IsNullOrWhiteSpace(x.email))
                   .GroupBy(x => x.aggregateKey)
                   .Select(g =>
                   {
                       var issueIds = g.Select(x => x.issueId).Distinct().ToList();
                       var first = g.First();
                       return new
                       {
                           key = first.originalKey,
                           name = first.name,
                           email = first.email,
                           issueCount = issueIds.Count,
                           issueIds
                       };
                   })
                   .OrderBy(x => x.name)
                   .ToList();

        var totalIssueCount = issues.Select(x => x.Id).Distinct().Count();
        return Json(new
        {
            units,
            responsibles,
            totalIssueCount,
            totalResponsibleCount = responsibles.Count
        });
    }

    [HttpGet("ResponsibleInfo/{companyId:int}")]
    public async Task<IActionResult> ResponsibleInfo(int projectId, int companyId)
    {
        var contract = await _db.Contract
            .Include(c => c.SiteManagerContact)
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.CompanyId == companyId);

        if (contract == null)
        {
            return Json(new { name = string.Empty, email = string.Empty, hasResponsible = false });
        }

        var responsibleName = string.Join(" ", new[] { contract.SiteManagerContact?.ContactVoornaam, contract.SiteManagerContact?.ContactNaam }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        var responsibleEmail = contract.SiteManagerContact?.Email;
        var hasResponsible = !string.IsNullOrWhiteSpace(responsibleName) || !string.IsNullOrWhiteSpace(responsibleEmail);

        if (!hasResponsible)
        {
            responsibleName = contract.Company?.BedrijfsNaam ?? string.Empty;
            responsibleEmail = !string.IsNullOrWhiteSpace(contract.Company?.InvoiceEmail)
                ? contract.Company.InvoiceEmail
                : (contract.Company?.Email ?? string.Empty);
        }

        return Json(new
        {
            name = responsibleName ?? string.Empty,
            email = responsibleEmail ?? string.Empty,
            hasResponsible
        });
    }

    [HttpPost("UploadUnitExecutionPlan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadUnitExecutionPlan(int projectId, int unitId, string? planName, IFormFile? planFile)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == unitId);
        if (unit == null)
            return NotFound();

        if (planFile == null || planFile.Length == 0)
            return BadRequest(new { success = false, message = "Geen planbestand geselecteerd." });

        var extension = Path.GetExtension(planFile.FileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "Alleen PDF bestanden zijn toegelaten." });

        var fileId = await UploadAssetToStorageAsync(planFile, "plans");
        if (string.IsNullOrWhiteSpace(fileId))
            return StatusCode(500, new { success = false, message = "Uploaden van het plan is mislukt." });

        var resolvedName = string.IsNullOrWhiteSpace(planName)
            ? Path.GetFileNameWithoutExtension(planFile.FileName)
            : planName.Trim();

        _db.UnitExecutionPlan.Add(new UnitExecutionPlan
        {
            UnitId = unitId,
            Name = resolvedName,
            FileId = fileId,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = User.FindFirst(CpmClaims.UserId)?.Value
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }


    [HttpGet("UnitPlans/{unitId:int}")]
    public async Task<IActionResult> UnitPlans(int projectId, int unitId)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == unitId);
        if (unit == null) return Json(Array.Empty<object>());

        var plans = new List<(int id, string name, string url)>();

        if (!string.IsNullOrWhiteSpace(unit.Plan))
        {
            var unitPlanUrl = GetSignedAssetUrlByFileName(unit.Plan, "plans");
            if (!string.IsNullOrWhiteSpace(unitPlanUrl) && unitPlanUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var proxyUrl = Url.Action(nameof(PlanContent), new { projectId, fileId = unit.Plan });
                plans.Add((0, $"{unit.Name} - plan", proxyUrl ?? unitPlanUrl));
            }
        }

        var unitExecutionPlans = await _db.UnitExecutionPlan
            .Where(d => d.UnitId == unitId && d.DeletedDate == null)
            .OrderBy(d => d.Name)
            .ThenByDescending(d => d.CreatedDate)
            .ToListAsync();

        foreach (var doc in unitExecutionPlans)
        {
            var docUrl = GetSignedAssetUrlByFileName(doc.FileId, "plans");
            if (!string.IsNullOrWhiteSpace(docUrl))
            {
                var proxyUrl = Url.Action(nameof(PlanContent), new { projectId, fileId = doc.FileId });
                plans.Add((doc.Id, doc.Name, proxyUrl ?? docUrl));
            }
        }

        return Json(plans
            .GroupBy(x => x.url)
            .Select(x => new { id = x.First().id, name = x.First().name, url = x.Key })
            .ToList());
    }


    [HttpGet("PlanContent")]
    public async Task<IActionResult> PlanContent(int projectId, string fileId)
    {
        var safeFileId = Path.GetFileName(fileId ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileId)) return NotFound();

        var projectExists = await _db.Project.AnyAsync(p => p.ProjectId == projectId);
        if (!projectExists) return NotFound();

        var signedUrl = GetSignedAssetUrlByFileName(safeFileId, "plans");
        if (string.IsNullOrWhiteSpace(signedUrl)) return NotFound();

        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(signedUrl);
        if (!response.IsSuccessStatusCode) return NotFound();

        await using var sourceStream = await response.Content.ReadAsStreamAsync();
        var memory = new MemoryStream();
        await sourceStream.CopyToAsync(memory);
        memory.Position = 0;

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "application/pdf";

        return File(memory, contentType, enableRangeProcessing: true);
    }

    private async Task<object?> ResolveIssuePlan(int projectId, ConstructionIssue issue)
    {
        if (!issue.UnitId.HasValue || !issue.PlanDocumentId.HasValue)
            return null;

        var unit = await _db.Units.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == issue.UnitId.Value);
        if (unit == null)
            return null;

        if (issue.PlanDocumentId.Value == 0 && !string.IsNullOrWhiteSpace(unit.Plan))
        {
            var planUrl = Url.Action(nameof(PlanContent), new { projectId, fileId = unit.Plan });
            return new { id = 0, name = $"{unit.Name} - plan", url = planUrl };
        }

        var executionPlan = await _db.UnitExecutionPlan
            .Where(x => x.UnitId == issue.UnitId.Value && x.Id == issue.PlanDocumentId.Value && x.DeletedDate == null)
            .FirstOrDefaultAsync();

        if (executionPlan == null)
            return null;

        var url = Url.Action(nameof(PlanContent), new { projectId, fileId = executionPlan.FileId });
        return new { id = executionPlan.Id, name = executionPlan.Name, url };
    }

    private static string GetEnumDisplayName<TEnum>(int value) where TEnum : Enum
    {
        var enumValue = (TEnum)Enum.ToObject(typeof(TEnum), value);
        var member = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
        var displayAttribute = member?.GetCustomAttributes(typeof(DisplayAttribute), false).OfType<DisplayAttribute>().FirstOrDefault();
        return displayAttribute?.Name ?? enumValue.ToString();
    }


    private async Task<List<ConstructionIssueMediaVm>> BuildMediaVm(int projectId, int issueId)
    {
        var media = await _service.GetMedia(projectId, issueId);
        return media
            .Select(x => new ConstructionIssueMediaVm
            {
                Id = x.Id,
                FileId = x.FileId,
                Url = GetSignedAssetUrlByFileName(x.FileId, "pictures")
            })
            .ToList();
    }

    private List<IFormFile>? ResolveMediaFiles(List<IFormFile>? mediaFiles)
    {
        if (mediaFiles is { Count: > 0 })
            return mediaFiles;

        if (Request?.Form?.Files is not { Count: > 0 })
            return mediaFiles;

        return Request.Form.Files
            .Where(file => file?.Length > 0)
            .ToList();
    }


    private async Task AddIssueMedia(int projectId, int issueId, List<IFormFile>? files)
    {
        if (files == null) return;

        foreach (var file in files.Where(x => x?.Length > 0))
        {
            var fileId = await UploadIssueMediaToStorageAsync(file);
            if (!string.IsNullOrWhiteSpace(fileId))
            {
                await _service.AddMedia(projectId, issueId, fileId, (int)ConstructionIssueMediaType.Photo, User.FindFirst(CpmClaims.UserId)?.Value);
            }
        }
    }

    private async Task<string?> UploadIssueMediaToStorageAsync(IFormFile file)
    {
        var uploaded = await UploadAssetToStorageAsync(file, "pictures");
        if (!string.IsNullOrWhiteSpace(uploaded))
            return uploaded;

        if (file.Length <= 0 || string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            await using var source = file.OpenReadStream();
            using var image = await Image.LoadAsync(source);
            await using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 90 });
            output.Position = 0;

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = $"issue_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            return await UploadAssetToStorageAsync(output, $"{baseName}.jpg", "image/jpeg", "pictures");
        }
        catch
        {
            return null;
        }
    }

    private static void PreserveMediaValidation(ModelStateDictionary modelState)
    {
        var keysToRemove = modelState.Keys
            .Where(k => k.Contains("mediaFiles", StringComparison.OrdinalIgnoreCase) || k.Contains("deleteMediaIds", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            modelState.Remove(key);
        }
    }

    private async Task<ConstructionIssueFormVm> BuildVm(int projectId, ConstructionIssueUpsertBO? dto = null)
    {
        return new ConstructionIssueFormVm
        {
            ProjectId = projectId,
            Input = dto ?? new ConstructionIssueUpsertBO { Status = (int)ConstructionIssueStatus.Open, Priority = (int)ConstructionIssuePriority.Normal, ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor },
            Categories = await _service.GetCategories(),
            Units = await _service.GetProjectUnits(projectId),
            ResponsibleContractors = await _db.Contract
                .Where(c => c.ProjectId == projectId)
                .Select(c => new { c.CompanyId, Name = c.Company.BedrijfsNaam })
                .Distinct()
                .OrderBy(c => c.Name)
                .Select(c => new IdNameBO { ID = c.CompanyId, Display = c.Name })
                .ToListAsync()
        };
    }

    private string? GetSignedAssetUrlByFileName(string fileName, string folder)
    {
        var safeFileName = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName)) return null;

        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        if (string.Equals(folder, "pictures", StringComparison.OrdinalIgnoreCase))
            return $"{baseUrl}/pictures/{Uri.EscapeDataString(safeFileName)}";

        var readKey = _configuration["StorageApi:ReadApiKey"];
        if (string.IsNullOrWhiteSpace(readKey))
            return null;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", readKey);

        var signUrl = $"{baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(safeFileName)}/sign";
        var response = httpClient.PostAsync(signUrl, content: null).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) return null;

        var payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var jsonDoc = JsonDocument.Parse(payload);
        if (!jsonDoc.RootElement.TryGetProperty("url", out var urlElement)) return null;

        var relativeOrAbsolute = urlElement.GetString();
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;

        return relativeOrAbsolute.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? relativeOrAbsolute
            : $"{baseUrl}{relativeOrAbsolute}";
    }

    private async Task<string?> UploadAssetToStorageAsync(IFormFile file, string folder)
    {
        await using var fileStream = file.OpenReadStream();
        return await UploadAssetToStorageAsync(fileStream, file.FileName, file.ContentType, folder);
    }

    private async Task<string?> UploadAssetToStorageAsync(Stream fileStream, string originalFileName, string? contentType, string folder)
    {
        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var writeKey = _configuration["StorageApi:WriteApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
            return null;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", writeKey);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(folder), "folder");

        var fileContent = new StreamContent(fileStream);
        if (!string.IsNullOrWhiteSpace(contentType))
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", originalFileName);

        var response = await httpClient.PostAsync($"{baseUrl}/api/assets/upload", content);
        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(payload);
        if (!jsonDoc.RootElement.TryGetProperty("fileName", out var fileNameElement)) return null;
        return fileNameElement.GetString();
    }

    public sealed class IssueSendPreviewRequest
    {
        public string? Scope { get; set; }
        public int? UnitId { get; set; }
        public List<int>? IssueIds { get; set; }
    }
}

