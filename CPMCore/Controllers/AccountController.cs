using CPMCore.Models;
using DALCore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

[Authorize]
public class AccountController : BaseController
{
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
}