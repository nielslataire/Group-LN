namespace FacadeCore;

public interface IIssueNotificationSchedulerService
{
    /// <summary>
    /// Checks all active schedules and fires any that are due.
    /// Called by the background service and the external HTTP trigger.
    /// </summary>
    /// <param name="triggeredBy">Label stored in IssueNotificationRun.TriggeredBy.</param>
    Task RunDueJobsAsync(string triggeredBy = "scheduler");

    /// <summary>
    /// Immediately sends a manual batch for a given schedule, ignoring the next-run timer.
    /// </summary>
    Task RunManualAsync(int scheduleId, string triggeredBy = "manual");
}
