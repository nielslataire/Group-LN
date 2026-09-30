using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Account;
using CPMCore.Services;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

[Authorize]
public class AccountController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IUserSignatureService _userSignatureService;
    private readonly GoogleLoginSettings _googleLogin;

    public AccountController(cpmRunningContext db, IUserSignatureService userSignatureService, GoogleLoginSettings googleLogin)
    {
        _db = db;
        _userSignatureService = userSignatureService;
        _googleLogin = googleLogin;
    }

    private static LoginType ParseLoginType(string? type) => type?.ToLowerInvariant() switch
    {
        "contractor" => LoginType.Contractor,
        "customer"   => LoginType.Customer,
        _            => LoginType.Internal
    };

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? type = null)
    {
        var loginType = ParseLoginType(type);
        var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~");
        return View(new EntraLoginViewModel
        {
            ReturnUrl = safeReturnUrl,
            Type = loginType,
            // Google enkel voor portaalgasten; interne medewerkers altijd via de organisatie-tenant.
            ShowGoogleLogin = _googleLogin.Enabled && loginType != LoginType.Internal
        });
    }

    /// <param name="provider">"google" voor het Google-schema; anders (default) Microsoft Entra.</param>
    /// <param name="type">Logintype (contractor/customer) — reist mee zodat een mislukte login
    /// terugkeert naar de juiste portaal-layout (zie LoginRedirect).</param>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult SignIn(string? returnUrl = null, string? provider = null, string? type = null)
    {
        var redirectUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        if (!string.IsNullOrWhiteSpace(type))
            properties.Items[LoginRedirect.LoginTypeItem] = type.ToLowerInvariant();

        if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
        {
            if (!_googleLogin.Enabled || ParseLoginType(type) == LoginType.Internal)
            {
                return Redirect(LoginRedirect.WithError(properties,
                    "Aanmelden met Google is niet beschikbaar. Gebruik uw Microsoft-account."));
            }
            return Challenge(properties, GoogleAuthDefaults.Scheme);
        }

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public IActionResult SignOut()
    {
        // Na uitloggen terug naar de loginpagina in de juiste portaal-layout.
        var loginType = User.GetCpmUserType() switch
        {
            "contractor" => "contractor",
            "customer"   => "customer",
            _            => null
        };
        var properties = new AuthenticationProperties
        {
            RedirectUri = loginType == null
                ? Url.Action(nameof(Login), "Account")
                : Url.Action(nameof(Login), "Account", new { type = loginType })
        };

        // Google kent geen RP-initiated logout (geen end_session_endpoint): enkel de eigen cookie
        // wissen. Voor Entra ook de tenant-sessie beëindigen zoals voorheen.
        if (User.IsGoogleLogin())
            return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme);

        return SignOut(
            properties,
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsContractor())
            return Redirect("/Werfportaal");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ProfilePhoto(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";

        var userId = User.GetCpmUserId();
        if (userId == null)
        {
            return Redirect(Url.Content("~/img/!logged-user.jpg"));
        }

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, ct);

        if (user?.Photo is { Length: > 0 } photo)
        {
            var contentType = string.IsNullOrWhiteSpace(user.PhotoContentType)
                ? "image/jpeg"
                : user.PhotoContentType;
            return File(photo, contentType);
        }

        return Redirect(Url.Content("~/img/!logged-user.jpg"));
    }

    [HttpGet]
    public IActionResult MijnHandtekening()
    {
        SetPageHeader("bx bx-envelope", "Mijn handtekening");

        var userId = User.GetCpmUserId();
        if (userId == null)
            return Forbid();

        var result = _userSignatureService.GetByUserId(userId.Value);
        var vm = new MijnHandtekeningVM
        {
            SignatureHtml = result.Value?.SignatureHtml,
            Format = string.IsNullOrWhiteSpace(result.Value?.SignatureFormat) ? "Visual" : result.Value.SignatureFormat
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MijnHandtekening(MijnHandtekeningVM vm)
    {
        var userId = User.GetCpmUserId();
        if (userId == null)
            return Forbid();

        var response = _userSignatureService.Save(userId.Value, vm.SignatureHtml ?? string.Empty, vm.Format);

        if (response.HasErrors)
            AddMessage("error", "Handtekening kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Handtekening opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(MijnHandtekening));
    }
}