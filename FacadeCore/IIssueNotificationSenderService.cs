namespace FacadeCore;

public interface IIssueNotificationSenderService
{
    /// <summary>
    /// Sends notification emails to all responsible parties with open issues.
    /// </summary>
    /// <param name="scheduleId">Optional schedule context used for run logging.</param>
    /// <param name="eveningOnly">
    /// When true only issues created or updated today are included (evening digest).
    /// </param>
    /// <param name="filterSupplierId">Restrict sending to a specific company.</param>
    /// <param name="filterProjectId">Restrict sending to a specific project.</param>
    /// <returns>Number of emails sent.</returns>
    Task<int> SendNotificationsAsync(
        int? scheduleId = null,
        bool eveningOnly = false,
        int? filterSupplierId = null,
        int? filterProjectId = null);
}
