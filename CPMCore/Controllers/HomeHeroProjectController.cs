using BOCore;
using CPMCore.Attributes;
using SmartBreadcrumbs.Nodes;
using CPMCore.Attributes;
using CPMCore.Models.Instellingen;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartBreadcrumbs.Nodes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsHomeHeroProject)]
public class HomeHeroProjectController : BaseController
{
    private readonly IHomeHeroProjectService _heroService;
    private readonly cpmRunningContext _db;
    private readonly IConfiguration _configuration;

    public HomeHeroProjectController(IHomeHeroProjectService heroService, cpmRunningContext db, IConfiguration configuration)
    {
        _heroService = heroService;
        _db = db;
        _configuration = configuration;
    }

    [HttpGet]
    [Breadcrumb("Home hero")]
    public async Task<IActionResult> Index(bool classic = false)
    {
        SetPageHeader("bx bx-carousel", "Home hero — uitgelicht project");
        if (classic) ViewData["UseGlV2Layout"] = false;
        ZetBreadcrumb();

        var bo = await _heroService.GetAsync();
        var vm = bo != null
            ? new HomeHeroProjectVM
            {
                ProjectId = bo.ProjectId,
                Kicker = bo.Kicker,
                Titel = bo.Titel,
                Tekst = bo.Tekst,
                ProjectTitelOverride = bo.ProjectTitelOverride
            }
            : new HomeHeroProjectVM();

        VulProjectOpties(vm);
        if (ViewData["UseGlV2Layout"] as bool? == true)
        {
            if (vm.ProjectId > 0) ViewData["HeroMedia"] = await ProjectMediaAsync(vm.ProjectId);
            return View("IndexV2", vm);
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HomeHeroProjectVM vm)
    {
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-carousel", "Home hero — uitgelicht project");
            ZetBreadcrumb();
            VulProjectOpties(vm);
            return ViewData["UseGlV2Layout"] as bool? == true ? View("IndexV2", vm) : View(vm);
        }

        await _heroService.SaveAsync(new HomeHeroProjectBO
        {
            ProjectId = vm.ProjectId,
            Kicker = vm.Kicker,
            Titel = vm.Titel,
            Tekst = vm.Tekst,
            ProjectTitelOverride = vm.ProjectTitelOverride
        });

        AddMessage("success", "Instellingen opgeslagen.", "Opgeslagen");
        return RedirectToAction(nameof(Index));
    }

    // Vult kicker/titel/tekst/projecttitel voor op vanuit de commerciële tekst van het gekozen project.
    [HttpGet]
    public async Task<IActionResult> ProjectTekst(int projectId)
    {
        var project = await _db.Set<Project>()
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => new { p.ProjectName, p.CommercialTitleNl, p.CommercialTextNl })
            .FirstOrDefaultAsync();

        if (project == null)
            return NotFound();

        var projectTitel = !string.IsNullOrWhiteSpace(project.CommercialTitleNl) ? project.CommercialTitleNl : project.ProjectName;

        return Json(new
        {
            titel = projectTitel,
            tekst = StripHtml(project.CommercialTextNl),
            projectTitel,
            media = await ProjectMediaAsync(projectId)
        });
    }

    // Instellingen > Website > Home hero ("Website" is enkel een sectielabel op Instellingen/Index, zie het ontwerp).
    private void ZetBreadcrumb()
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var website = new MvcBreadcrumbNode("Index", "Instellingen", "Website") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Index", "HomeHeroProject", "Home hero") { Parent = website };
    }

    // Standaardfoto of -video van het project, met dezelfde bron als de homepage van de website (pictures/800/ of videos/).
    private async Task<object?> ProjectMediaAsync(int projectId)
    {
        var pic = await _db.Set<Project>().AsNoTracking().Where(p => p.ProjectId == projectId && p.DefaultPicture != null)
            .Select(p => new { p.DefaultPicture.Name, p.DefaultPicture.MediaType }).FirstOrDefaultAsync();
        if (pic == null || string.IsNullOrWhiteSpace(pic.Name)) return null;
        var basis = (_configuration["URL:ImageWebURL"] ?? "").TrimEnd('/');
        var ext = System.IO.Path.GetExtension(pic.Name)?.ToLowerInvariant();
        var video = pic.MediaType == 1 || ext is ".mp4" or ".webm" or ".mov" or ".avi";
        return new { url = video ? $"{basis}/videos/{pic.Name}" : $"{basis}/pictures/800/{pic.Name}", video };
    }

    private void VulProjectOpties(HomeHeroProjectVM vm)
    {
        vm.ProjectOpties = _db.Set<Project>()
            .Where(p => p.ProjectSalesSettings.Any(s => s.SaleVisible == true))
            .OrderBy(p => p.ProjectName)
            .Select(p => new SelectListItem(p.ProjectName, p.ProjectId.ToString()))
            .ToList();
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
