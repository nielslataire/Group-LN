using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Account;
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

    public AccountController(cpmRunningContext db, IUserSignatureService userSignatureService)
    {
        _db = db;
        _userSignatureService = userSignatureService;
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? type = null)
    {
        var loginType = type?.ToLowerInvariant() switch
        {
            "contractor" => LoginType.Contractor,
            "customer"   => LoginType.Customer,
            _            => LoginType.Internal
        };
        var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~");
        return View(new EntraLoginViewModel
        {
            ReturnUrl = safeReturnUrl,
            Type = loginType
        });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult SignIn(string? returnUrl = null)
    {
        var redirectUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public IActionResult SignOut()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Login), "Account")
        };

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