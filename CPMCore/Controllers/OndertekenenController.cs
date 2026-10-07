using CPMCore.Configuration;
using FacadeCore.Signing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CPMCore.Controllers;

/// <summary>
/// Publieke ondertekenpagina, fase 2 van de signingmodule (ONDERTEKENEN_VOORSTEL.md §5.2,
/// ONDERTEKENEN_VOORTGANG.md "Fase 2"). Route <c>/tekenen</c> — bewust niet <c>/ondertekenen</c>,
/// dat is nog de route van de andere, live ondertekenflow (<see cref="SigningController"/>) tot de
/// cutover. Geen <c>BaseController</c> (geen ingelogde gebruiker), <c>[AllowAnonymous]</c> omdat de
/// globale fallback-policy anders overal authenticatie eist. De sessie leeft server-side
/// (<c>SigningAccessToken.SessionId</c>); deze controller bewaart enkel dat guid in een cookie.
/// </summary>
[AllowAnonymous]
[Route("tekenen")]
public class OndertekenenController : Controller
{
    private const string SessionCookieName = "gl_tekenen_sid";

    private readonly ISigningService _signing;
    private readonly FeatureFlagsOptions _features;
    private readonly ILogger<OndertekenenController> _logger;

    public OndertekenenController(ISigningService signing, IOptions<FeatureFlagsOptions> features, ILogger<OndertekenenController> logger)
    {
        _signing = signing;
        _features = features.Value;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!_features.EnableSigning)
        {
            context.Result = NotFound();
        }
    }

    private SigningRequestContext Ctx(Guid? sessionId = null) => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString(),
        sessionId);

    private Guid? SessionIdFromCookie() =>
        Guid.TryParse(Request.Cookies[SessionCookieName], out var id) ? id : null;

    private void SetSessionCookie(Guid sessionId) => Response.Cookies.Append(SessionCookieName, sessionId.ToString(), new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/tekenen",
        MaxAge = TimeSpan.FromHours(12),
    });

    private void ClearSessionCookie() => Response.Cookies.Delete(SessionCookieName, new CookieOptions { Path = "/tekenen" });

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Link inwisselen
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    [HttpGet("{token}")]
    [EnableRateLimiting("signing-open")]
    public async Task<IActionResult> Index(string token, CancellationToken ct)
    {
        var result = await _signing.RedeemTokenAsync(token, Ctx(), ct);
        if (!result.Success || result.SessionId is null)
            return View("Invalid");

        SetSessionCookie(result.SessionId.Value);
        return RedirectToAction(nameof(Document));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Document / ondertekenen (enkel via de sessiecookie, nooit via een id in de URL)
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    [HttpGet("document")]
    [EnableRateLimiting("signing-open")]
    public async Task<IActionResult> Document(CancellationToken ct)
    {
        var sessionId = SessionIdFromCookie();
        var session = sessionId is null ? null : await _signing.GetSessionAsync(sessionId.Value, ct);
        if (session is null)
        {
            ClearSessionCookie();
            return View("Expired");
        }
        ViewData["Title"] = session.Title;
        ViewData["PublicKicker"] = session.SignerName;
        ViewData["PublicKickerPlain"] = true;
        ViewData["PublicBrand"] = session.IssuerName;   // design-handoff 36: rechtsboven de naam van wie tekent, niet de documentsoort
        return View(session);
    }

    [HttpGet("document/bestand")]
    [EnableRateLimiting("signing-open")]
    public async Task<IActionResult> Bestand(int? bijlage, CancellationToken ct)
    {
        var sessionId = SessionIdFromCookie();
        if (sessionId is null) return NotFound();
        var content = await _signing.GetSessionDocumentAsync(sessionId.Value, bijlage, Ctx(sessionId), ct);
        if (content is null) return NotFound();
        Response.Headers["Content-Disposition"] = CPMCore.Services.Signing.ContentDispositionHelper.Inline(content.FileName);
        return File(content.Content, content.ContentType);
    }

    // Bewust géén publieke "/document/audit"-actie: het auditrapport is enkel intern in te zien/te
    // downloaden (SigningAdminController.Download) — de ondertekenaar krijgt het niet aangeboden.
    [HttpGet("document/finaal")]
    [EnableRateLimiting("signing-open")]
    public async Task<IActionResult> Finaal(CancellationToken ct)
    {
        var sessionId = SessionIdFromCookie();
        if (sessionId is null) return NotFound();
        var content = await _signing.GetSessionFinalDocumentAsync(sessionId.Value, auditReport: false, Ctx(sessionId), ct);
        if (content is null) return NotFound();
        // Inline (niet "attachment"): dit bestand wordt ook in de preview-iframe getoond zodra het
        // dossier voltooid is, niet enkel via de downloadknop.
        Response.Headers["Content-Disposition"] = CPMCore.Services.Signing.ContentDispositionHelper.Inline(content.FileName);
        return File(content.Content, content.ContentType);
    }

    [HttpPost("document/code")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("signing-otp-request")]
    public async Task<IActionResult> Code(CancellationToken ct)
    {
        var sessionId = SessionIdFromCookie();
        if (sessionId is null) return Json(new { ok = false, error = "Uw sessie is verlopen." });
        var r = await _signing.RequestVerificationAsync(sessionId.Value, Ctx(sessionId), ct);
        return Json(new { ok = r.Success, destinationMasked = r.DestinationMasked, expiresAt = r.ExpiresAt, error = r.Error });
    }

    public sealed record VerifyCodePost(string Code);

    [HttpPost("document/verifieer")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("signing-otp-verify")]
    public async Task<IActionResult> Verifieer([FromBody] VerifyCodePost body, CancellationToken ct)
    {
        var sessionId = SessionIdFromCookie();
        if (sessionId is null) return Json(new { ok = false, error = "Uw sessie is verlopen." });
        var r = await _signing.VerifyCodeAsync(sessionId.Value, body.Code ?? "", Ctx(sessionId), ct);
        return Json(new { ok = r.Success, locked = r.Locked, attemptsLeft = r.AttemptsLeft, error = r.Error });
    }

    /// <summary><c>SignatureImagePng</c> is de canvas-tekening als data-URL (<c>data:image/png;base64,...</c>
    /// of kaal base64) — verplicht: Niels wil de knop "Ondertekenen" pas bruikbaar zodra er getekend is,
    /// en dat wordt hier ook server-side afgedwongen, niet enkel via de uitgeschakelde knop in de UI.</summary>
    public sealed record SignPost(bool ConsentAccepted, string IdempotencyKey, string? SignatureImagePng);

    [HttpPost("document/tekenen")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("signing-sign")]
    public async Task<IActionResult> Tekenen([FromBody] SignPost body, CancellationToken ct)
    {
        var sessionId = SessionIdFromCookie();
        if (sessionId is null) return Json(new { ok = false, error = "Uw sessie is verlopen." });

        byte[]? signatureImage;
        try { signatureImage = DecodeSignatureImage(body.SignatureImagePng); }
        catch (FormatException) { return Json(new { ok = false, error = "De handtekening kon niet gelezen worden. Teken ze opnieuw." }); }
        if (signatureImage is null) return Json(new { ok = false, error = "Zet eerst uw handtekening in het vak." });

        var request = new SignRequest(sessionId.Value, body.ConsentAccepted, signatureImage, body.IdempotencyKey ?? Guid.NewGuid().ToString("N"));
        var r = await _signing.SignAsync(request, Ctx(sessionId), ct);
        return Json(new { ok = r.Success, alreadySigned = r.AlreadySigned, caseCompleted = r.CaseCompleted, error = r.Error });
    }

    private static byte[]? DecodeSignatureImage(string? dataUrlOrBase64)
    {
        if (string.IsNullOrWhiteSpace(dataUrlOrBase64)) return null;
        var comma = dataUrlOrBase64.IndexOf(',');
        var base64 = comma >= 0 && dataUrlOrBase64.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? dataUrlOrBase64[(comma + 1)..]
            : dataUrlOrBase64;
        var bytes = Convert.FromBase64String(base64);
        return bytes.Length == 0 ? null : bytes;
    }

    public sealed record DeclinePost(string Reason);

    [HttpPost("document/weigeren")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("signing-sign")]
    public async Task<IActionResult> Weigeren([FromBody] DeclinePost body, CancellationToken ct)
    {
        var sessionId = SessionIdFromCookie();
        if (sessionId is null) return Json(new { ok = false, error = "Uw sessie is verlopen." });
        var r = await _signing.DeclineAsync(new DeclineRequest(sessionId.Value, body.Reason ?? ""), Ctx(sessionId), ct);
        return Json(new { ok = r.Success, error = r.Error });
    }
}
