using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Issues;

public class IssueNotificationSchedulerService : IIssueNotificationSchedulerService
{
    private readonly cpmRunningContext _db;
    private readonly IIssueNotificationSenderService _sender;

    // Run-type constants kept local to avoid coupling to enum
    private const int RunTypeReminder = 0;
    private const int RunTypeEvening = 1;
    private const int RunTypeManual = 2;

    private const int StatusRunning = 0;
    private const int StatusSuccess = 1;
    private const int StatusError = 2;

    public IssueNotificationSchedulerService(
        cpmRunningContext db,
        IIssueNotificationSenderService sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task RunDueJobsAsync(string triggeredBy = "scheduler")
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var schedules = await _db.IssueNotificationSchedule
            .Where(x => x.IsActive)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            // ── Reminder ──────────────────────────────────────────
            if (schedule.ReminderFrequencyDays > 0
                && schedule.NextReminderRun.HasValue
                && schedule.NextReminderRun.Value <= now)
            {
                await ExecuteJobAsync(schedule, RunTypeReminder, triggeredBy,
                    async () =>
                    {
                        var sent = await _sender.SendNotificationsAsync(
                            scheduleId: schedule.Id,
                            eveningOnly: false,
                            filterSupplierId: schedule.SupplierId,
                            filterProjectId: schedule.ProjectId);
                        return sent;
                    });

                // Advance next run date – aligneer op het geconfigureerde uur
                schedule.NextReminderRun = now.Date.AddDays(schedule.ReminderFrequencyDays).AddHours(schedule.ReminderHour);
                schedule.ModifiedDate = now;
            }

            // ── Evening update ────────────────────────────────────
            if (schedule.SendEveningUpdate
                && now.Hour >= schedule.EveningUpdateHour
                && (schedule.LastEveningRun == null || schedule.LastEveningRun < today))
            {
                await ExecuteJobAsync(schedule, RunTypeEvening, triggeredBy,
                    async () =>
                    {
                        var sent = await _sender.SendNotificationsAsync(
                            scheduleId: schedule.Id,
                            eveningOnly: true,
                            filterSupplierId: schedule.SupplierId,
                            filterProjectId: schedule.ProjectId);
                        return sent;
                    });

                schedule.LastEveningRun = today;
                schedule.ModifiedDate = now;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task RunManualAsync(int scheduleId, string triggeredBy = "manual")
    {
        var schedule = await _db.IssueNotificationSchedule.FindAsync(scheduleId);
        if (schedule == null)
            throw new InvalidOperationException($"Schedule {scheduleId} niet gevonden.");

        await ExecuteJobAsync(schedule, RunTypeManual, triggeredBy,
            async () => await _sender.SendNotificationsAsync(
                scheduleId: schedule.Id,
                eveningOnly: false,
                filterSupplierId: schedule.SupplierId,
                filterProjectId: schedule.ProjectId));

        await _db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────────────────────

    private async Task ExecuteJobAsync(
        IssueNotificationSchedule schedule,
        int runType,
        string triggeredBy,
        Func<Task<int>> work)
    {
        var run = new IssueNotificationRun
        {
            ScheduleId = schedule.Id,
            RunType = runType,
            TriggeredBy = triggeredBy,
            StartedDate = DateTime.UtcNow,
            Status = StatusRunning
        };

        _db.IssueNotificationRun.Add(run);
        await _db.SaveChangesAsync(); // persist Running record immediately

        try
        {
            run.EmailsSent = await work();
            run.Status = StatusSuccess;
        }
        catch (Exception ex)
        {
            run.Status = StatusError;
            run.ErrorMessage = ex.Message;
        }
        finally
        {
            run.FinishedDate = DateTime.UtcNow;
        }

        // SaveChanges is called by the caller after updating schedule timestamps
    }
}
