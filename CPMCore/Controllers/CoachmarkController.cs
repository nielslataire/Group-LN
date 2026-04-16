using CPMCore.Helpers;
using CPMCore.Models.Coachmarks;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

/// <summary>
/// JSON API voor coachmark-statusbeheer vanuit de frontend.
/// Alle endpoints verwachten een JSON body en retourneren JSON.
/// </summary>
[Authorize]
[Route("Coachmark")]
public sealed class CoachmarkController : BaseController
{
    private readonly ICoachmarkService _coachmarks;

    public CoachmarkController(ICoachmarkService coachmarks)
    {
        _coachmarks = coachmarks;
    }

    // POST /Coachmark/MarkShown
    [HttpPost("MarkShown")]
    public async Task<IActionResult> MarkShown(
        [FromBody] CoachmarkMarkShownRequest request, CancellationToken ct)
    {
        var userId = User.GetCpmUserId();
        if (userId is null || string.IsNullOrWhiteSpace(request.StateKey))
            return BadRequest();

        await _coachmarks.MarkShownAsync(request.StateKey, userId.Value, ct);
        return Ok(new { success = true });
    }

    // POST /Coachmark/Dismiss
    [HttpPost("Dismiss")]
    public async Task<IActionResult> Dismiss(
        [FromBody] CoachmarkDismissRequest request, CancellationToken ct)
    {
        var userId = User.GetCpmUserId();
        if (userId is null || string.IsNullOrWhiteSpace(request.StateKey))
            return BadRequest();

        await _coachmarks.DismissAsync(request.StateKey, userId.Value, ct);
        return Ok(new { success = true });
    }

    // POST /Coachmark/Complete
    [HttpPost("Complete")]
    public async Task<IActionResult> Complete(
        [FromBody] CoachmarkCompleteRequest request, CancellationToken ct)
    {
        var userId = User.GetCpmUserId();
        if (userId is null || string.IsNullOrWhiteSpace(request.StateKey))
            return BadRequest();

        await _coachmarks.CompleteAsync(request.StateKey, userId.Value, ct);
        return Ok(new { success = true });
    }

    // POST /Coachmark/AdvanceStep
    [HttpPost("AdvanceStep")]
    public async Task<IActionResult> AdvanceStep(
        [FromBody] CoachmarkAdvanceStepRequest request, CancellationToken ct)
    {
        var userId = User.GetCpmUserId();
        if (userId is null || string.IsNullOrWhiteSpace(request.StateKey))
            return BadRequest();

        await _coachmarks.AdvanceStepAsync(request.StateKey, userId.Value, request.Step, ct);
        return Ok(new { success = true });
    }
}
