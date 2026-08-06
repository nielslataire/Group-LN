using BOCore;
using CPMCore.Models.Instellingen;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

    public HomeHeroProjectController(IHomeHeroProjectService heroService, cpmRunningContext db)
    {
        _heroService = heroService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        SetPageHeader("bx bx-carousel", "Home hero — uitgelicht project");

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
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HomeHeroProjectVM vm)
    {
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-carousel", "Home hero — uitgelicht project");
            VulProjectOpties(vm);
            return View(vm);
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
            projectTitel
        });
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
