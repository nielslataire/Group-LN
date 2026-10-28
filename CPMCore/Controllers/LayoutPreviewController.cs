using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

/// <summary>
/// gl-v2 layout-pilot (design-handoff/): zet/wist de per-sessie preview-cookie die
/// BaseController.OnActionExecuting leest. Geen eigen views of business-logica — puur de
/// aan/uit-schakelaar voor de nieuwe shell, bedoeld om na goedkeuring weer te verwijderen.
/// </summary>
[Authorize]
[Route("LayoutPreview")]
public class LayoutPreviewController : BaseController
{
    [HttpGet("On")]
    public IActionResult On(string? returnUrl = null)
    {
        Response.Cookies.Append(GlV2PreviewCookie, "1", new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(365),
            HttpOnly = false
        });
        return RedirectToSafeUrl(returnUrl);
    }

    [HttpGet("Off")]
    public IActionResult Off(string? returnUrl = null)
    {
        // "0" i.p.v. de cookie te wissen: als gl-v2 de standaard is (Layout:GlV2Default) moet de keuze voor klassiek blijven gelden.
        Response.Cookies.Append(GlV2PreviewCookie, "0", new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(365),
            HttpOnly = false
        });
        return RedirectToSafeUrl(returnUrl);
    }

    private IActionResult RedirectToSafeUrl(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(referer))
            return Redirect(referer);

        return RedirectToAction("Index", "Home");
    }
}
