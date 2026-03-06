using System.Net.Http.Headers;
using System.Text.Json;
using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Issues;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;

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
        var vm = new ConstructionIssueIndexVm
        {
            ProjectId = projectId,
            Filters = filters,
            Categories = await _service.GetCategories(),
            Units = await _service.GetProjectUnits(projectId),
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
    public async Task<IActionResult> Create(int projectId)
    {
        return View("CreateEdit", await BuildVm(projectId));
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int projectId, ConstructionIssueFormVm vm)
    {
        vm.Input.ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor;
        if (!ModelState.IsValid) return View("CreateEdit", await BuildVm(projectId, vm.Input));
        await _service.Create(projectId, vm.Input, User.FindFirst(CpmClaims.UserId)?.Value);
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int projectId, int id)
    {
        var issue = await _service.GetById(projectId, id);
        if (issue == null) return NotFound();
        var vm = await BuildVm(projectId, new ConstructionIssueUpsertBO
        {
            Title = issue.Title,
            Description = issue.Description,
            LocationText = issue.LocationText,
            CategoryId = issue.CategoryId,
            BuildingPart = issue.BuildingPart,
            RoomOrZone = issue.RoomOrZone,
            UnitId = issue.UnitId,
            IssueType = issue.IssueType,
            IssuePhase = issue.IssuePhase,
            Priority = issue.Priority,
            Status = issue.Status,
            ResponsiblePartyType = issue.ResponsiblePartyType,
            ResponsiblePartyId = issue.ResponsiblePartyId,
            ResponsibleOtherName = issue.ResponsibleOtherName,
            ResponsibleOtherEmail = issue.ResponsibleOtherEmail,
            DueDate = issue.DueDate
        });
        vm.Id = id;
        return View("CreateEdit", vm);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int projectId, int id, ConstructionIssueFormVm vm)
    {
        vm.Input.ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor;
        if (!ModelState.IsValid) return View("CreateEdit", await BuildVm(projectId, vm.Input));
        var updated = await _service.Update(projectId, id, vm.Input, User.FindFirst(CpmClaims.UserId)?.Value);
        if (updated == null) return NotFound();
        return RedirectToAction(nameof(Details), new { projectId, id });
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

    [HttpPost("ChangeStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int projectId, int id, int newStatus, string? comment)
    {
        await _service.ChangeStatus(projectId, id, newStatus, comment, User.FindFirst(CpmClaims.UserId)?.Value);
        return RedirectToAction(nameof(Details), new { projectId, id });
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