using BOCore;
using CPMCore.Attributes;
using CPMCore.Models;
using CPMCore.Models.Home;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Definition;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using ServiceCore.Invoicing.Pdf.Templates;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace CPMCore.Controllers;

/// <summary>Instellingen-hub (Index), activiteiten en verlofdagen. Opgesplitst uit InstellingenController.cs (okt. 2026, structureren) - views in Views/Instellingen/Algemeen/. Zelfde partial class: alle private velden/services van InstellingenController.cs blijven gewoon bruikbaar.</summary>
public partial class InstellingenController
{
    [HttpGet]
    [Breadcrumb("Instellingen")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        SetPageHeader("bx bx-cog", "Instellingen");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };

        ViewData["BreadcrumbNode"] = instellingenIndex;

        // gl-v2 (design-handoff punt 24a, "CRM Instellingen.dc.html"): de legacy view berekent haar
        // canXxx-permissievlaggen zelf via @inject IPermissionService — IndexV2 heeft een echt
        // view-model nodig (een kaart kan nu ook een live status tonen, bv. Marktdata-status se
        // laatste crawl, wat een service-aanroep vergt die niet in de view thuishoort), dus die
        // opbouw gebeurt hier in de controller, enkel voor de gl-v2-tak.
        if (ViewData["UseGlV2Layout"] as bool? == true)
        {
            var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            await permissionService.EnsureLoadedAsync(ct);

            var vm = new InstellingenIndexV2Vm();

            var facturatie = new SettingsGroupVm { Key = "facturatie", Name = "Facturatie & boekhouding" };
            if (permissionService.HasRead(PermissionCodes.SettingsBillingCompanies))
            {
                facturatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-buildings",
                    Title = "Mijn bedrijven",
                    Description = "Beheer uitgevende vennootschappen, factuurlay-out, bankrekeningen en verzendgegevens.",
                    Href = Url.Action("IssuerCompanies", "Instellingen") ?? "#"
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsInvoiceTemplates))
            {
                facturatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-file-text",
                    Title = "Factuurtemplates",
                    Description = "Beheer factuurlay-outs die je per bedrijf kunt selecteren en hergebruiken.",
                    Href = Url.Action("InvoiceTemplates", "Instellingen") ?? "#"
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsBouwIndexen))
            {
                facturatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-chart-line-up",
                    Title = "Bouwindexen",
                    Description = "Beheer S-index, I-2021-index en ABEX-index voor je budgetberekeningen.",
                    Href = Url.Action("Bouwindexen", "Instellingen") ?? "#"
                });
            }
            if (facturatie.Items.Count > 0) vm.Groups.Add(facturatie);

            var team = new SettingsGroupVm { Key = "team", Name = "Team & toegang" };
            if (permissionService.HasRead(PermissionCodes.SettingsUsers))
            {
                team.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-users",
                    Title = "Gebruikers & rollen",
                    Description = "Beheer toegang van collega's, aannemers en gastgebruikers, en hou rolrechten up-to-date.",
                    Href = Url.Action("Index", "UserAdmin") ?? "#",
                    Chips = { new SettingsChipVm { Label = "Rollen", Href = Url.Action("Index", "AppRoles") ?? "#" } }
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsHolidays))
            {
                team.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-calendar-blank",
                    Title = "Verlofdagen",
                    Description = "Plan collectief verlof, feestdagen en weerverlet zodat projecten juist rekenen.",
                    Href = Url.Action("VacationDays", "Instellingen") ?? "#"
                });
            }
            if (team.Items.Count > 0) vm.Groups.Add(team);

            var calculatie = new SettingsGroupVm { Key = "calculatie", Name = "Calculatie" };
            if (permissionService.HasRead(PermissionCodes.SettingsActivities))
            {
                calculatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-list-bullets",
                    Title = "Activiteiten",
                    Description = "Beheer activiteiten en activiteitgroepen (loten) die in je calculaties terugkomen.",
                    Href = Url.Action("Activities", "Instellingen") ?? "#",
                    Chips = { new SettingsChipVm { Label = "Groepen", Href = Url.Action("Activities", "Instellingen") ?? "#" } }
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsKostprijsMaterialen))
            {
                calculatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-tag",
                    Title = "Kostprijzen materialen",
                    Description = "Beheer referentieprijzen per materiaaltype en bouwkostpercentages voor gebruik in budgetten.",
                    Href = Url.Action("KostprijsMaterialen", "Instellingen") ?? "#"
                });
                calculatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-calculator",
                    Title = "Budgetformules",
                    Description = "Bewerk de voorstel-formules per activiteit voor de budgetwizard, met parameters uit budget en materialen.",
                    Href = Url.Action("BudgetFormules", "Instellingen") ?? "#"
                });
                calculatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-currency-eur",
                    Title = "Prijsreferenties verkoop",
                    Description = "Bouw- en grondprijscodes (€/m²) met datum en bron, voor de verkooplijnen op stap 8 van de budgetwizard.",
                    Href = Url.Action("BudgetPrijsReferenties", "Instellingen") ?? "#"
                });
                calculatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-clock-counter-clockwise",
                    Title = "Referentieprojecten (nacalc)",
                    Description = "Werkelijke kost per activiteit van afgewerkte projecten, uit de app of uit Excel, om een budget mee te vergelijken op stap 6.",
                    Href = Url.Action("BudgetReferentieProjecten", "Instellingen") ?? "#",
                    Chips = { new SettingsChipVm { Label = "Excel-sjabloon", Href = Url.Action("BudgetReferentieSjabloon", "Instellingen") ?? "#" } }
                });
            }
            if (calculatie.Items.Count > 0) vm.Groups.Add(calculatie);

            var website = new SettingsGroupVm { Key = "website", Name = "Website" };
            if (permissionService.HasRead(PermissionCodes.SettingsBlogBeheer))
            {
                website.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-newspaper",
                    Title = "Blog artikelen",
                    Description = "Beheer nieuws- en blogartikelen die op de publieke website verschijnen, inclusief foto's en rijke inhoud.",
                    Href = Url.Action("Index", "BlogBeheer") ?? "#"
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsVacatureBeheer))
            {
                website.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-briefcase",
                    Title = "Vacatures",
                    Description = "Beheer de openstaande vacatures die op de publieke website verschijnen.",
                    Href = Url.Action("Index", "VacatureBeheer") ?? "#"
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsEmailTemplates))
            {
                website.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-envelope-simple",
                    Title = "E-mailtemplates",
                    Description = "Beheer voorgedefinieerde e-mailtemplates (onderwerp + tekst) om te versturen naar projectcontacten.",
                    Href = Url.Action("Index", "EmailTemplateBeheer") ?? "#"
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsHomeHeroProject))
            {
                website.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-image",
                    Title = "Home hero — uitgelicht project",
                    Description = "Kies het project dat uitgelicht wordt op de home-hero van de publieke website, met eigen kicker, titel en tekst.",
                    Href = Url.Action("Index", "HomeHeroProject") ?? "#"
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsCookieConsentStats))
            {
                website.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-cookie",
                    Title = "Cookiebanner — statistieken",
                    Description = "Hoeveel bezoekers de cookiebanner aanvaarden, weigeren of verlaten zonder keuze — los van Google Analytics gemeten.",
                    Href = Url.Action("Index", "CookieConsentStats") ?? "#"
                });
            }
            if (website.Items.Count > 0) vm.Groups.Add(website);

            var automatisatie = new SettingsGroupVm { Key = "automatisatie", Name = "Automatisatie & meldingen" };
            if (permissionService.HasRead(PermissionCodes.SettingsIssueNotifications))
            {
                automatisatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-bell",
                    Title = "Automatische puntmeldingen",
                    Description = "Beheer schema's voor automatische e-mailmeldingen van werfpunten naar aannemers.",
                    Href = Url.Action("Index", "IssueNotificationAdmin") ?? "#"
                });
            }
            if (permissionService.HasRead(PermissionCodes.SettingsMarketDataStatus))
            {
                var marketItem = new SettingsItemVm
                {
                    Icon = "ph-pulse",
                    Title = "Marktdata-status",
                    Description = "Bekijk of de laatste crawl-run van GroupLN.MarketData.Worker geslaagd is, per bron, en welke fouten er eventueel optraden.",
                    Href = Url.Action("MarketDataStatus", "Instellingen") ?? "#"
                };
                // 24a: "Status die je anders moet gaan zoeken ... staat op de kaart zelf" — enige kaart
                // met een goedkope, al bestaande databron voor een live status; andere kaarten (bv.
                // Mijn bedrijven se Octopus-token) hebben dat vandaag niet en tonen daarom geen meta.
                try
                {
                    var status = await _marketDataStatus.GetStatusAsync(ct: ct);
                    var actief = status.Sources.Where(s => s.IsActive).ToList();
                    var mislukt = actief.Any(s => s.LastFailedCrawlAt.HasValue
                        && (!s.LastSuccessfulCrawlAt.HasValue || s.LastFailedCrawlAt > s.LastSuccessfulCrawlAt));
                    var laatsteSucces = actief.Where(s => s.LastSuccessfulCrawlAt.HasValue)
                        .Select(s => s.LastSuccessfulCrawlAt!.Value)
                        .OrderByDescending(d => d)
                        .Cast<DateTime?>()
                        .FirstOrDefault();
                    if (mislukt)
                    {
                        marketItem.MetaTone = "is-danger";
                        marketItem.MetaLabel = "Laatste crawl mislukt";
                    }
                    else if (laatsteSucces.HasValue)
                    {
                        marketItem.MetaTone = "is-success";
                        marketItem.MetaLabel = "Laatste crawl: " + laatsteSucces.Value.ToString("dd/MM");
                    }
                }
                catch
                {
                    // Marktdata-databron (aparte connection string) even niet bereikbaar — de kaart
                    // blijft gewoon werken, enkel zonder statuslabel; MarketDataStatus zelf toont de
                    // volledige foutdetails.
                }
                automatisatie.Items.Add(marketItem);
            }
            if (permissionService.HasRead(PermissionCodes.SettingsTrajectSjablonen))
            {
                automatisatie.Items.Add(new SettingsItemVm
                {
                    Icon = "ph-git-branch",
                    Title = "Trajectsjablonen",
                    Description = "Beheer de standaardtrajecten met fases en mijlpalen per projecttype die gebruikt worden om het traject van een project op te bouwen.",
                    Href = Url.Action("Index", "TrajectSjabloonAdmin") ?? "#"
                });
            }
            if (automatisatie.Items.Count > 0) vm.Groups.Add(automatisatie);

            return View("IndexV2", vm);
        }

        return View();
    }


   



    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsActivities)]
    [Breadcrumb("Activiteiten")]
    public IActionResult Activities()
    {
        SetPageHeader("bx bx-cog", "Activiteiten");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };
        var instellingenActivities = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Activities", "Instellingen", "Activiteiten")
        {
            Parent = instellingenIndex
        };

        ViewData["BreadcrumbNode"] = instellingenActivities;

        var activitiesResponse = _activityService.GetActivities();
        var groupsResponse = _activityService.GetActivityGroups();

        var model = new ActivitySettingsViewModel
        {
            Activities = activitiesResponse.Values?.OrderBy(a => a.Name).ToList() ?? new List<ActivityBO>(),
            ActivityGroups = groupsResponse.Values?.OrderBy(g => g.Lot).ThenBy(g => g.Name).ToList() ?? new List<ActivityGroupBO>()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityCreate(string name, int groupId)
    {
        if (groupId <= 0)
        {
            TempData["Error"] = "Selecteer een activiteitgroep.";
            return RedirectToAction(nameof(Activities));
        }

        var activity = new ActivityBO
        {
            Name = name?.Trim() ?? string.Empty,
            Group = new ActivityGroupBO { ID = groupId }
        };

        var response = _activityService.InsertUpdate(activity);
        SetActivityResponseMessage(response, "Activiteit toegevoegd.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityUpdate(int id, string name, int groupId)
    {
        if (groupId <= 0)
        {
            TempData["Error"] = "Selecteer een activiteitgroep.";
            return RedirectToAction(nameof(Activities));
        }

        var activity = new ActivityBO
        {
            ID = id,
            Name = name?.Trim() ?? string.Empty,
            Group = new ActivityGroupBO { ID = groupId }
        };

        var response = _activityService.InsertUpdate(activity);
        SetActivityResponseMessage(response, "Activiteit bijgewerkt.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityDelete(int id)
    {
        var response = _activityService.Delete(new List<int> { id });
        SetActivityResponseMessage(response, "Activiteit verwijderd.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityGroupCreate(string name, int lot)
    {
        var group = new ActivityGroupBO
        {
            Name = name?.Trim() ?? string.Empty,
            Lot = lot
        };

        var response = _activityService.InsertUpdateGroup(group);
        SetActivityResponseMessage(response, "Activiteitgroep toegevoegd.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityGroupUpdate(int id, string name, int lot)
    {
        var group = new ActivityGroupBO
        {
            ID = id,
            Name = name?.Trim() ?? string.Empty,
            Lot = lot
        };

        var response = _activityService.InsertUpdateGroup(group);
        SetActivityResponseMessage(response, "Activiteitgroep bijgewerkt.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityGroupDelete(int id)
    {
        var response = _activityService.DeleteGroup(new List<int> { id });
        SetActivityResponseMessage(response, "Activiteitgroep verwijderd.");

        return RedirectToAction(nameof(Activities));
    }

    //VAKANTIEDAGEN
    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsHolidays)]
    [Breadcrumb("Vakantiedagen")]
    public ActionResult Vacationdays()
    {
        SetPageHeader("bx bx-cog", "Vakantiedagen");

        HomeModel model = new HomeModel();

        return View(model);
    }
    [HttpGet]
    public IActionResult GetVacationDays()
    {
        var response = _projectService.GetVacationDaysGeneral();

        var rows = response.Success
            ? response.Values.Select(b => new
            {
                id = b.Id,
                title = "verlofdag",
                year = b.VacationDay.Year,
                month = b.VacationDay.Month,
                day = b.VacationDay.Day
            }).ToArray()
            : Array.Empty<object>();

        return Json(rows); // of: return Ok(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public int AddVacationDay(DateOnly dag)
    {
        var day = new VacationDayBO { VacationDay = dag };
        var response = _projectService.InsertUpdateVacationDay(day);

        if (!response.Success) return 0;

        var idMsg = response.Messages.FirstOrDefault(m => m.Type == MessageType.Value)?.Message
                    ?? response.Messages.FirstOrDefault()?.Message;

        return int.TryParse(idMsg, out var id) ? id : 0;
    }

    [HttpPost]
    public bool DeleteVacationDay(int id)
    {
        var ids = new List<int> { id };
        var response = _projectService.DeleteVacationDays(ids);
        return response.Success;
    }
}
