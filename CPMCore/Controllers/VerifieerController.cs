using CPMCore.Configuration;
using FacadeCore.Signing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CPMCore.Controllers;

/// <summary>
/// Publieke verificatiepagina (fase 3, ONDERTEKENEN_VOORSTEL.md §9.4): wie een ondertekend document
/// (of de QR erop) in handen heeft, kan hier nagaan dat het dossier echt bestaat en voltooid is —
/// zonder in te loggen, en zonder namen/IP's/interne id's te tonen (<see cref="PublicVerificationView"/>
/// is daar al op ontworpen). Route <c>/verifieer/{id}</c> lag al gereserveerd in
/// <c>SigningSecurityHeadersMiddleware</c>; de link stond al op het document maar was tot nu dood.
/// </summary>
[AllowAnonymous]
[Route("verifieer")]
public class VerifieerController : Controller
{
    private readonly ISigningService _signing;
    private readonly FeatureFlagsOptions _features;

    public VerifieerController(ISigningService signing, IOptions<FeatureFlagsOptions> features)
    {
        _signing = signing;
        _features = features.Value;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!_features.EnableSigning)
        {
            context.Result = NotFound();
        }
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting("signing-verify-page")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        var view = await _signing.GetPublicVerificationAsync(id, ct);
        if (view is null)
        {
            ViewData["Title"] = "Niet gevonden";
            return View("NotFound");
        }
        ViewData["Title"] = view.DocumentTypeLabel;
        ViewData["PublicBrand"] = view.IssuerName;
        return View(view);
    }
}
