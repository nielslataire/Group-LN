using CPMCore.Helpers;
using CPMCore.Models;
using DALCore.Models;
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

    public AccountController(cpmRunningContext db)
    {
        _db = db;
    }
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new EntraLoginViewModel
        {
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? Url.Content("~") : returnUrl
        });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult SignIn(string? returnUrl = null)
    {
        var redirectUrl = string.IsNullOrWhiteSpace(returnUrl) ? Url.Content("~") : returnUrl;
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
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ProfilePhoto(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";

        var userId = User.GetCpmUserId();
        if (userId == null)
        {
            return NotFound();
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

        return NotFound();
    }
}