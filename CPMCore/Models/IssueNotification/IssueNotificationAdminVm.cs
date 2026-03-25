using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.IssueNotification;

// ── Index ──────────────────────────────────────────────────────

public class IssueNotificationIndexVm
{
    public List<ScheduleRowVm> Schedules { get; set; } = new();
    public List<RunRowVm> RecentRuns { get; set; } = new();
}

public class ScheduleRowVm
{
    public int Id { get; set; }
    public string SupplierName { get; set; }      // null = alle aannemers
    public string ProjectName { get; set; }       // null = alle projecten
    public int ReminderFrequencyDays { get; set; }
    public int? ReminderDayOfWeek { get; set; }
    public int ReminderHour { get; set; }
    public int BrusselsReminderHour { get; set; }
    public bool SendEveningUpdate { get; set; }
    public int EveningUpdateHour { get; set; }
    public int BrusselsEveningUpdateHour { get; set; }
    public bool IsActive { get; set; }
    public DateTime? NextReminderRun { get; set; }
    public DateOnly? LastEveningRun { get; set; }
    public string Notes { get; set; }
}

public class RunRowVm
{
    public int Id { get; set; }
    public int? ScheduleId { get; set; }
    public string RunTypeLabel { get; set; }
    public string TriggeredBy { get; set; }
    public DateTime StartedDate { get; set; }
    public DateTime? FinishedDate { get; set; }
    public string StatusLabel { get; set; }
    public string StatusCssClass { get; set; }
    public int EmailsSent { get; set; }
    public string ErrorMessage { get; set; }
}

// ── Edit ───────────────────────────────────────────────────────

public class ScheduleEditVm
{
    public int Id { get; set; }

    [Display(Name = "Aannemer / bedrijf")]
    public int? SupplierId { get; set; }

    [Display(Name = "Project")]
    public int? ProjectId { get; set; }

    [Display(Name = "Herinnering om de X dagen")]
    [Range(0, 365)]
    public int ReminderFrequencyDays { get; set; } = 7;

    [Display(Name = "Dag van de week")]
    public int? ReminderDayOfWeek { get; set; } = 1; // 1 = Monday

    [Display(Name = "Dagelijkse update versturen")]
    public bool SendEveningUpdate { get; set; } = true;

    [Display(Name = "Uur dagelijkse update (UTC)")]
    [Range(0, 23)]
    public int EveningUpdateHour { get; set; } = 18;

    // Display/input in Brussels local time – controller converts to/from UTC
    [Display(Name = "Uur dagelijkse update (Brussels)")]
    [Range(0, 23)]
    public int BrusselsHour { get; set; } = 19;

    [Display(Name = "Uur herinnering (UTC)")]
    [Range(0, 23)]
    public int ReminderHour { get; set; } = 8;

    [Display(Name = "Uur herinnering (Brussels)")]
    [Range(0, 23)]
    public int BrusselsReminderHour { get; set; } = 9;

    [Display(Name = "Actief")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Notities")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Select lists
    public List<SelectItem> Suppliers { get; set; } = new();
    public List<SelectItem> Projects { get; set; } = new();
}

public class SelectItem
{
    public int Id { get; set; }
    public string Name { get; set; }
}
