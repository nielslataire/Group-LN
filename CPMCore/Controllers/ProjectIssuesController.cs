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
    public async Task<IActionResult> SendSelected(int projectId, ConstructionIssueSendRequestBO request)
    {
        var sent = await _reportService.SendSelectedIssues(projectId, request, User.FindFirst(CpmClaims.UserId)?.Value);
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
            MediaUrls = media.ToDictionary(x => x.Id, x => GetSignedAssetUrlByFileName(x.FileId, "issues"))
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
            var fileId = await UploadAssetToStorageAsync(file, "issues");
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


    [HttpPost("ChangeStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int projectId, int id, int newStatus, string? comment)
    {
        await _service.ChangeStatus(projectId, id, newStatus, comment, User.FindFirst(CpmClaims.UserId)?.Value);
        return RedirectToAction(nameof(Details), new { projectId, id });
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


    private async Task<List<ConstructionIssueMediaVm>> BuildMediaVm(int projectId, int issueId)
    {
        var media = await _service.GetMedia(projectId, issueId);
        return media
            .Select(x => new ConstructionIssueMediaVm
            {
                Id = x.Id,
                FileId = x.FileId,
                Url = GetSignedAssetUrlByFileName(x.FileId, "issues")
            })
            .ToList();
    }

    private async Task AddIssueMedia(int projectId, int issueId, List<IFormFile>? files)
    {
        if (files == null) return;

        foreach (var file in files.Where(x => x?.Length > 0))
        {
            var fileId = await UploadAssetToStorageAsync(file, "issues");
            if (!string.IsNullOrWhiteSpace(fileId))
            {
                await _service.AddMedia(projectId, issueId, fileId, (int)ConstructionIssueMediaType.Photo, User.FindFirst(CpmClaims.UserId)?.Value);
            }
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
        var readKey = _configuration["StorageApi:ReadApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(readKey))
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
}