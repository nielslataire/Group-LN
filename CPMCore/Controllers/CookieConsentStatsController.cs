using BOCore;
using CPMCore.Attributes;
using CPMCore.Models.Instellingen;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;
using System;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsCookieConsentStats)]
public class CookieConsentStatsController : BaseController
{
    private readonly ICookieConsentStatsService _statsService;

    public CookieConsentStatsController(ICookieConsentStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet]
    [Breadcrumb("Cookiebanner")]
    public async Task<IActionResult> Index()
    {
        SetPageHeader("bx bx-cookie", "Cookiebanner — statistieken",
            description: "Hoeveel bezoekers de cookiebanner op de publieke website aanvaarden, weigeren of geen keuze maken. Los van Google Analytics gemeten — die laadt zelf pas ná toestemming.");

        // Instellingen > Website > Cookiebanner. "Website" heeft geen eigen pagina (het is een
        // sectielabel op Instellingen/Index), dus die knoop wijst terug naar diezelfde actie —
        // zelfde aanpak als de andere Instellingen-onderdelen, enkel met de extra sectielaag
        // die op de Instellingen-pagina zelf ook zichtbaar is.
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var website = new MvcBreadcrumbNode("Index", "Instellingen", "Website") { Parent = instellingenIndex };
        var cookieConsentStats = new MvcBreadcrumbNode("Index", "CookieConsentStats", "Cookiebanner") { Parent = website };
        ViewData["BreadcrumbNode"] = cookieConsentStats;

        var now = DateTime.UtcNow;
        var last30 = await _statsService.GetStatsAsync(now.AddDays(-30), now.AddMinutes(1));
        var allTime = await _statsService.GetStatsAsync(DateTime.MinValue, now.AddMinutes(1));

        var vm = new CookieConsentStatsVM
        {
            Last30Days = new CookieConsentStatsPeriodVM
            {
                Titel = "Laatste 30 dagen",
                Subtitel = $"{last30.PeriodStartUtc.ToLocalTime():dd/MM/yyyy} — {now.ToLocalTime():dd/MM/yyyy}",
                Stats = last30
            },
            AllTime = new CookieConsentStatsPeriodVM
            {
                Titel = "Sinds het begin van de meting",
                Subtitel = "Alle geregistreerde gebeurtenissen",
                Stats = allTime
            }
        };

        return View(vm);
    }
}
