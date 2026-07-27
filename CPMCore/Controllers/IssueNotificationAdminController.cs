using BOCore;
using CPMCore.Models.IssueNotification;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsIssueNotifications)]
[Route("Instellingen/PuntMeldingen")]
public class IssueNotificationAdminController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IIssueNotificationSchedulerService _scheduler;
    private readonly IIssueNotificationSenderService _sender;
    private readonly IEntraGuestInvitationService _inviteService;
    private readonly IConfiguration _configuration;

    public IssueNotificationAdminController(
        cpmRunningContext db,
        IIssueNotificationSchedulerService scheduler,
        IIssueNotificationSenderService sender,
        IEntraGuestInvitationService inviteService,
        IConfiguration configuration)
    {
        _db = db;
        _scheduler = scheduler;
        _sender = sender;
        _inviteService = inviteService;
        _configuration = configuration;
    }

    // ── BREADCRUMBS ───────────────────────────────────────────────

    private void SetBreadcrumbs(string? leafTitle = null)
    {
        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };
        var issueNotifIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "IssueNotificationAdmin", "Puntmeldingen")
        {
            Parent = instellingenIndex
        };

        if (leafTitle == null)
        {
            ViewData["BreadcrumbNode"] = issueNotifIndex;
        }
        else
        {
            var leaf = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "IssueNotificationAdmin", leafTitle)
            {
                Parent = issueNotifIndex
            };
            ViewData["BreadcrumbNode"] = leaf;
        }
    }

    // ── INDEX ─────────────────────────────────────────────────────

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var schedules = await _db.IssueNotificationSchedule
            .OrderBy(x => x.SupplierId.HasValue ? 1 : 0)
            .ThenBy(x => x.Id)
            .ToListAsync();

        var supplierIds = schedules.Where(x => x.SupplierId.HasValue).Select(x => x.SupplierId!.Value).Distinct().ToList();
        var projectIds = schedules.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId!.Value).Distinct().ToList();

        var supplierNames = await _db.CompanyInfo
            .Where(x => supplierIds.Contains(x.CompanyId))
            .ToDictionaryAsync(x => x.CompanyId, x => x.BedrijfsNaam);

        var projectNames = await _db.Project
            .Where(x => projectIds.Contains(x.ProjectId))
            .ToDictionaryAsync(x => x.ProjectId, x => x.ProjectName);

        var recentRuns = await _db.IssueNotificationRun
            .OrderByDescending(x => x.StartedDate)
            .Take(30)
            .ToListAsync();

        var vm = new IssueNotificationIndexVm
        {
            Schedules = schedules.Select(s => new ScheduleRowVm
            {
                Id = s.Id,
                SupplierName = s.SupplierId.HasValue
                    ? supplierNames.GetValueOrDefault(s.SupplierId.Value, $"Bedrijf #{s.SupplierId}")
                    : null,
                ProjectName = s.ProjectId.HasValue
                    ? projectNames.GetValueOrDefault(s.ProjectId.Value, $"Project #{s.ProjectId}")
                    : null,
                ReminderFrequencyDays = s.ReminderFrequencyDays,
                ReminderDayOfWeek = s.ReminderDayOfWeek,
                ReminderHour = s.ReminderHour,
                BrusselsReminderHour = UtcToBrusselsHour(s.ReminderHour),
                SendEveningUpdate = s.SendEveningUpdate,
                EveningUpdateHour = s.EveningUpdateHour,
                BrusselsEveningUpdateHour = UtcToBrusselsHour(s.EveningUpdateHour),
                IsActive = s.IsActive,
                NextReminderRun = s.NextReminderRun.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(s.NextReminderRun.Value, BrusselsTz)
                    : null,
                LastEveningRun = s.LastEveningRun,
                Notes = s.Notes
            }).ToList(),

            RecentRuns = recentRuns.Select(MapRun).ToList()
        };

        SetBreadcrumbs();
        SetPageHeader("bx bx-bell", "Automatische puntmeldingen");
        return View(vm);
    }

    // ── CREATE ────────────────────────────────────────────────────

    [HttpGet("Nieuw")]
    public async Task<IActionResult> Create()
    {
        var vm = new ScheduleEditVm();
        await PopulateSelectLists(vm);
        vm.BrusselsHour = UtcToBrusselsHour(vm.EveningUpdateHour);
        vm.BrusselsReminderHour = UtcToBrusselsHour(vm.ReminderHour);
        SetBreadcrumbs("Nieuw schema");
        SetPageHeader("bx bx-bell", "Nieuw schema");
        return View("Edit", vm);
    }

    [HttpPost("Nieuw")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsIssueNotifications)]
    public async Task<IActionResult> CreatePost(ScheduleEditVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(vm);
            SetPageHeader("bx bx-bell", "Nieuw schema");
            return View("Edit", vm);
        }

        var entity = new IssueNotificationSchedule
        {
            SupplierId = vm.SupplierId,
            ProjectId = vm.ProjectId,
            ReminderFrequencyDays = vm.ReminderFrequencyDays,
            ReminderDayOfWeek = vm.ReminderFrequencyDays > 0 ? vm.ReminderDayOfWeek : null,
            ReminderHour = BrusselsToUtcHour(vm.BrusselsReminderHour),
            SendEveningUpdate = vm.SendEveningUpdate,
            EveningUpdateHour = BrusselsToUtcHour(vm.BrusselsHour),
            IsActive = vm.IsActive,
            Notes = vm.Notes,
            CreatedDate = DateTime.UtcNow,
            NextReminderRun = vm.ReminderFrequencyDays > 0
                ? ComputeNextReminderRun(vm.ReminderFrequencyDays, vm.ReminderFrequencyDays > 0 ? vm.ReminderDayOfWeek : null, BrusselsToUtcHour(vm.BrusselsReminderHour))
                : null
        };

        _db.IssueNotificationSchedule.Add(entity);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Schema aangemaakt.";
        return RedirectToAction(nameof(Index));
    }

    // ── EDIT ──────────────────────────────────────────────────────

    [HttpGet("{id:int}/Bewerken")]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await _db.IssueNotificationSchedule.FindAsync(id);
        if (entity == null) return NotFound();

        var vm = new ScheduleEditVm
        {
            Id = entity.Id,
            SupplierId = entity.SupplierId,
            ProjectId = entity.ProjectId,
            ReminderFrequencyDays = entity.ReminderFrequencyDays,
            ReminderDayOfWeek = entity.ReminderDayOfWeek ?? 1,
            ReminderHour = entity.ReminderHour,
            SendEveningUpdate = entity.SendEveningUpdate,
            EveningUpdateHour = entity.EveningUpdateHour,
            IsActive = entity.IsActive,
            Notes = entity.Notes
        };
        await PopulateSelectLists(vm);
        vm.BrusselsHour = UtcToBrusselsHour(vm.EveningUpdateHour);
        vm.BrusselsReminderHour = UtcToBrusselsHour(vm.ReminderHour);
        SetBreadcrumbs("Schema bewerken");
        SetPageHeader("bx bx-bell", "Schema bewerken");
        return View(vm);
    }

    [HttpPost("{id:int}/Bewerken")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsIssueNotifications)]
    public async Task<IActionResult> EditPost(int id, ScheduleEditVm vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectLists(vm);
            SetPageHeader("bx bx-bell", "Schema bewerken");
            return View("Edit", vm);
        }

        var entity = await _db.IssueNotificationSchedule.FindAsync(id);
        if (entity == null) return NotFound();

        entity.SupplierId = vm.SupplierId;
        entity.ProjectId = vm.ProjectId;
        entity.ReminderFrequencyDays = vm.ReminderFrequencyDays;
        entity.ReminderDayOfWeek = vm.ReminderFrequencyDays > 0 ? vm.ReminderDayOfWeek : null;
        entity.ReminderHour = BrusselsToUtcHour(vm.BrusselsReminderHour);
        entity.SendEveningUpdate = vm.SendEveningUpdate;
        entity.EveningUpdateHour = BrusselsToUtcHour(vm.BrusselsHour);
        entity.IsActive = vm.IsActive;
        entity.Notes = vm.Notes;
        entity.ModifiedDate = DateTime.UtcNow;

        // Herbereken NextReminderRun op basis van geconfigureerde dag en uur
        if (entity.ReminderFrequencyDays == 0)
            entity.NextReminderRun = null;
        else
            entity.NextReminderRun = ComputeNextReminderRun(entity.ReminderFrequencyDays, entity.ReminderDayOfWeek, entity.ReminderHour);

        await _db.SaveChangesAsync();

        TempData["Success"] = "Schema opgeslagen.";
        return RedirectToAction(nameof(Index));
    }

    // ── DELETE ────────────────────────────────────────────────────

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsIssueNotifications)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.IssueNotificationSchedule.FindAsync(id);
        if (entity != null)
        {
            _db.IssueNotificationSchedule.Remove(entity);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Schema verwijderd.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ── MANUAL TRIGGER ────────────────────────────────────────────

    [HttpPost("{id:int}/NuVersturen")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNow(int id)
    {
        try
        {
            await _scheduler.RunManualAsync(id, "admin-ui");
            return Json(new { success = true, message = "Batch manueel verstuurd." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ── TEST EMAIL ────────────────────────────────────────────────

    [HttpPost("TestEmail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTestEmail()
    {
        const string testRecipient = "niels.lataire@groupln.be";
        try
        {
            await _sender.SendTestEmailAsync(testRecipient);
            return Json(new { success = true, message = $"Testmail verstuurd naar {testRecipient}." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ── TEST UITNODIGINGSMAIL ─────────────────────────────────────

    [HttpPost("TestUitnodiging")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendTestInviteEmail()
    {
        const string testRecipient = "niels.lataire@groupln.be";
        var appBaseUrl = (_configuration["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        try
        {
            var result = await _inviteService.SendTestInviteEmailAsync(testRecipient, appBaseUrl);
            if (result.Success)
                return Json(new { success = true, message = $"Test uitnodigingsmail verstuurd naar {testRecipient}." });
            return Json(new { success = false, message = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ── DELETE MODAL ──────────────────────────────────────────────

    [HttpGet("{id:int}/VerwijderModal")]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsIssueNotifications)]
    public async Task<IActionResult> PartialDeleteScheduleModal(int id)
    {
        var entity = await _db.IssueNotificationSchedule.FindAsync(id);
        if (entity == null) return NotFound();

        var parts = new List<string>();
        if (entity.SupplierId.HasValue)
        {
            var supplier = await _db.CompanyInfo.FindAsync(entity.SupplierId.Value);
            if (supplier != null) parts.Add(supplier.BedrijfsNaam);
        }
        if (entity.ProjectId.HasValue)
        {
            var project = await _db.Project.FindAsync(entity.ProjectId.Value);
            if (project != null) parts.Add(project.ProjectName);
        }

        ViewBag.ScheduleId = id;
        ViewBag.ScheduleName = parts.Count > 0 ? string.Join(" / ", parts) : "globale standaard";
        return PartialView("Modals/_DeleteScheduleModal");
    }

    // ── LOGS ──────────────────────────────────────────────────────

    [HttpGet("Logs")]
    public async Task<IActionResult> Logs(int page = 1)
    {
        const int pageSize = 50;
        var total = await _db.IssueNotificationRun.CountAsync();
        var runs = await _db.IssueNotificationRun
            .OrderByDescending(x => x.StartedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        SetBreadcrumbs("Logs");
        SetPageHeader("bx bx-bell", "Uitvoeringslogboek");
        return View(runs.Select(MapRun).ToList());
    }

    // ── HELPERS ───────────────────────────────────────────────────

    private async Task PopulateSelectLists(ScheduleEditVm vm)
    {
        vm.Suppliers = await _db.CompanyInfo
            .OrderBy(x => x.BedrijfsNaam)
            .Select(x => new SelectItem { Id = x.CompanyId, Name = x.BedrijfsNaam })
            .ToListAsync();

        vm.Projects = await _db.Project
            .OrderBy(x => x.ProjectName)
            .Select(x => new SelectItem { Id = x.ProjectId, Name = x.ProjectName })
            .ToListAsync();
    }

    private static RunRowVm MapRun(IssueNotificationRun r) => new()
    {
        Id = r.Id,
        ScheduleId = r.ScheduleId,
        RunTypeLabel = r.RunType switch { 0 => "Herinnering", 1 => "Dagelijkse update", _ => "Manueel" },
        TriggeredBy = r.TriggeredBy,
        StartedDate = TimeZoneInfo.ConvertTimeFromUtc(r.StartedDate, BrusselsTz),
        FinishedDate = r.FinishedDate.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(r.FinishedDate.Value, BrusselsTz) : null,
        StatusLabel = r.Status switch { 0 => "Bezig", 1 => "Geslaagd", _ => "Fout" },
        StatusCssClass = r.Status switch { 0 => "badge bg-secondary", 1 => "badge bg-success", _ => "badge bg-danger" },
        EmailsSent = r.EmailsSent,
        ErrorMessage = r.ErrorMessage
    };

    // ── NEXT REMINDER RUN HELPER ──────────────────────────────────

    private static DateTime ComputeNextReminderRun(int frequencyDays, int? dayOfWeek, int reminderHourUtc)
    {
        var now = DateTime.UtcNow;
        var todayAtHour = now.Date.AddHours(reminderHourUtc);

        if (dayOfWeek.HasValue)
        {
            var targetDow = (DayOfWeek)dayOfWeek.Value;
            int daysUntil = ((int)targetDow - (int)now.DayOfWeek + 7) % 7;

            if (daysUntil == 0)
            {
                // Vandaag is de geconfigureerde dag
                if (now < todayAtHour)
                    return todayAtHour; // uur nog niet voorbij: run vandaag
                else
                    return todayAtHour.AddDays(frequencyDays); // uur voorbij: volgende cyclus
            }

            return now.Date.AddDays(daysUntil).AddHours(reminderHourUtc);
        }
        else
        {
            if (now < todayAtHour)
                return todayAtHour;
            return todayAtHour.AddDays(frequencyDays);
        }
    }

    // ── BRUSSELS TIME HELPERS ─────────────────────────────────────
    private static TimeZoneInfo BrusselsTz => TimeZoneInfo.FindSystemTimeZoneById(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Central European Standard Time"
            : "Europe/Brussels");

    private static int UtcToBrusselsHour(int utcHour)
    {
        var utcNow = DateTime.UtcNow;
        var utcDt = DateTime.SpecifyKind(utcNow.Date.AddHours(utcHour), DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcDt, BrusselsTz).Hour;
    }

    private static int BrusselsToUtcHour(int brusselsHour)
    {
        var brusselsNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrusselsTz);
        var brusselsDt = DateTime.SpecifyKind(brusselsNow.Date.AddHours(brusselsHour), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(brusselsDt, BrusselsTz).Hour;
    }
}
