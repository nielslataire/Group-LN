using CPMCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CPMCore.Controllers;

/// <summary>
/// External HTTP trigger endpoint — call via cron-job.org as a reliability backup.
/// Example: GET /api/trigger/issue-notifications?key=YOUR_SECRET_KEY
/// </summary>
[AllowAnonymous]
[Route("api/trigger")]
public class TriggerController : Controller
{
    private readonly IssueNotificationHostedService _hostedService;
    private readonly IConfiguration _configuration;

    public TriggerController(
        IssueNotificationHostedService hostedService,
        IConfiguration configuration)
    {
        _hostedService = hostedService;
        _configuration = configuration;
    }

    [HttpGet("issue-notifications")]
    public async Task<IActionResult> TriggerIssueNotifications([FromQuery] string key)
    {
        var expectedKey = _configuration["TriggerKeys:IssueNotifications"];
        if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
            return Unauthorized(new { error = "Ongeldige sleutel." });

        await _hostedService.RunJobsAsync("http-trigger");
        return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
    }

    /// <summary>Keep-alive ping — returns 200 to prevent IIS app pool recycling.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "alive", timestamp = DateTime.UtcNow });
}
