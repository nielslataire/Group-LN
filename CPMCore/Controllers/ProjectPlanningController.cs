using System.Text.Json;
using BOCore;
using CPMCore.Attributes;
using CPMCore.Helpers;
using CPMCore.Models.Planning;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

/// <summary>
/// Bouwplanning van een project: taken en fases met duur in werkdagen en voorgangers, doorgerekend met de werkkalender
/// (feestdagen, bouwverlof, verlet). De pagina stuurt elke wijziging als <see cref="PlanningCommando"/> en krijgt de herberekende
/// staat terug; de rekenlogica staat in <c>ServiceCore.Planning</c>.
/// </summary>
[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.ProjectsPlanning)]
[Route("Projects/{projectId:int}/Planning")]
public class ProjectPlanningController : BaseController
{
    private static readonly JsonSerializerOptions JsonOpties = new(JsonSerializerDefaults.Web);

    private readonly cpmRunningContext _db;
    private readonly IPlanningService _planning;
    private readonly IPlanningSjabloonService _sjablonen;

    public ProjectPlanningController(cpmRunningContext db, IPlanningService planning, IPlanningSjabloonService sjablonen)
    {
        _db = db;
        _planning = planning;
        _sjablonen = sjablonen;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("~/Projects/Planning")]
    public IActionResult MenuRedirect()
    {
        AddMessage("info", "Selecteer eerst een project om de planning te bekijken.", "Info");
        return RedirectToAction("Index", "Projecten");
    }

    [HttpGet("")]
    [Breadcrumb("Planning")]
    public async Task<IActionResult> Index(int projectId, int? planningId = null)
    {
        var projectInfo = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId).Select(p => new { p.ProjectName, p.ProjectCode }).FirstOrDefaultAsync();
        if (projectInfo == null) return NotFound();
        var projectName = projectInfo.ProjectName;

        // De kruimel stopt bij de projectnaam; "Planning" is al de paginatitel (zoals Traject).
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Detail", "Projecten", projectName)
        {
            Parent = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = new MvcBreadcrumbNode("Index", "Home", "Dashboard") },
            RouteValues = new { projectid = projectId }
        };
        SetPageHeader("ph ph-chart-bar-horizontal", "Planning");
        ViewData["HelpScreenId"] = "planning-index";

        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var staat = await _planning.Laad(projectId, planningId);
        return View("IndexV2", new PlanningIndexVm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            ProjectCode = projectInfo.ProjectCode,
            CanWrite = ps.HasWrite(PermissionCodes.ProjectsPlanning),
            CanDelete = ps.HasDelete(PermissionCodes.ProjectsPlanning),
            CanLinkTraject = ps.HasWrite(PermissionCodes.ProjectsTraject),
            CanSaveTemplate = ps.HasWrite(PermissionCodes.SettingsPlanningSjablonen),
            CanDeleteTemplate = ps.HasDelete(PermissionCodes.SettingsPlanningSjablonen),
            StaatJson = JsonSerializer.Serialize(staat, JsonOpties),
        });
    }

    /// <summary>Eén wijziging aan de planning; het antwoord bevat de volledig herberekende staat.</summary>
    [HttpPost("Commando")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> Commando(int projectId, [FromBody] PlanningCommando commando)
    {
        if (commando == null) return BadRequest();

        // Verwijderen vraagt het verwijderrecht (zoals Traject en Dossiers).
        if ((commando.Type == "verwijder" || commando.Type == "planningVerwijder")
            && !HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasDelete(PermissionCodes.ProjectsPlanning))
            return Forbid();

        var uitkomst = await _planning.Voer(projectId, commando, UserId);
        return Json(uitkomst, JsonOpties);
    }

    /// <summary>
    /// Koppelt een trajectmijlpaal aan een taak of fase van de planning (of haalt de koppeling weg). Vraagt naast schrijfrecht op de planning
    /// ook schrijfrecht op het Traject: de mijlpaal in het Traject volgt voortaan de planning.
    /// </summary>
    [HttpPost("Koppeling")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> Koppeling(int projectId, [FromBody] PlanningKoppelCommando commando)
    {
        if (commando == null) return BadRequest();
        if (!HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsTraject))
            return Forbid();

        var uitkomst = await _planning.Koppel(projectId, commando, UserId);
        return Json(uitkomst, JsonOpties);
    }

    /// <summary>Koppelt een betalingsschijf aan een taak of fase van de planning (of haalt de koppeling weg).</summary>
    [HttpPost("SchijfKoppeling")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> SchijfKoppeling(int projectId, [FromBody] PlanningSchijfKoppelCommando commando)
    {
        if (commando == null) return BadRequest();
        return Json(await _planning.KoppelSchijf(projectId, commando, UserId), JsonOpties);
    }

    /// <summary>"Schijf bereikt" markeren voor de eenheden van de planning (of terugzetten). Vraagt ook schrijfrecht op de facturatie.</summary>
    [HttpPost("SchijfMarkeren")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> SchijfMarkeren(int projectId, [FromBody] PlanningSchijfMarkeerCommando commando)
    {
        if (commando == null) return BadRequest();
        if (!HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsInvoicing))
            return Forbid();
        return Json(await _planning.MarkeerSchijf(projectId, commando, UserId), JsonOpties);
    }

    public sealed class SjabloonBewaarVerzoek
    {
        public int? PlanningId { get; set; }
        public string Naam { get; set; } = "";
        public string? Beschrijving { get; set; }
    }

    public sealed class SjabloonVerwijderVerzoek
    {
        public int Id { get; set; }
    }

    // ── Projectsjabloon: planning, traject en schijven in één keer, aangepast aan het gebouw (migratie 156) ──

    public sealed class ProjectSjabloonVerzoek
    {
        public int SjabloonId { get; set; }
        public ProjectSjabloonAntwoorden Antwoorden { get; set; } = new();
    }

    public sealed class ProjectSjabloonAiVerzoek
    {
        public string Beschrijving { get; set; } = "";
    }

    [HttpGet("ProjectSjablonen")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> ProjectSjablonen(int projectId)
        => Json(await HttpContext.RequestServices.GetRequiredService<IProjectSjabloonService>().Keuzes(), JsonOpties);

    /// <summary>Wat het projectsjabloon met deze antwoorden zou doen. Bewaart niets.</summary>
    [HttpPost("ProjectSjabloonVoorbeeld")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> ProjectSjabloonVoorbeeld(int projectId, [FromBody] ProjectSjabloonVerzoek verzoek)
    {
        if (verzoek == null) return BadRequest();
        if ((verzoek.Antwoorden?.VervangPlanning ?? false) && !HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasDelete(PermissionCodes.ProjectsPlanning))
            return Json(new ProjectSjabloonResultaat { Fout = "De bestaande planning vervangen vraagt het recht Verwijderen op Planning. Je rol heeft dat niet." }, JsonOpties);
        return Json(await HttpContext.RequestServices.GetRequiredService<IProjectSjabloonService>().Voorbeeld(projectId, verzoek.SjabloonId, verzoek.Antwoorden ?? new()), JsonOpties);
    }

    /// <summary>Past het projectsjabloon toe. Naast schrijfrecht op de planning is schrijfrecht op facturatie nodig, want de schijven komen in de betalingsgroepen.</summary>
    [HttpPost("ProjectSjabloonToepassen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> ProjectSjabloonToepassen(int projectId, [FromBody] ProjectSjabloonVerzoek verzoek)
    {
        if (verzoek == null) return BadRequest();
        if (!HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsInvoicing))
            return Json(new ProjectSjabloonResultaat { Fout = "Voor een projectsjabloon heb je ook schrijfrecht op de facturatie nodig, want de schijven komen in de betalingsgroepen." }, JsonOpties);
        if ((verzoek.Antwoorden?.VervangPlanning ?? false) && !HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasDelete(PermissionCodes.ProjectsPlanning))
            return Json(new ProjectSjabloonResultaat { Fout = "De bestaande planning vervangen vraagt het recht Verwijderen op Planning. Je rol heeft dat niet." }, JsonOpties);
        return Json(await HttpContext.RequestServices.GetRequiredService<IProjectSjabloonService>().Toepassen(projectId, verzoek.SjabloonId, verzoek.Antwoorden ?? new(), UserId), JsonOpties);
    }

    /// <summary>De AI vult de antwoorden in uit een beschrijving van het gebouw. Enkel een voorstel: er wordt niets bewaard of toegepast.</summary>
    [HttpPost("ProjectSjabloonAi")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> ProjectSjabloonAi(int projectId, [FromBody] ProjectSjabloonAiVerzoek verzoek, CancellationToken ct)
    {
        if (verzoek == null) return BadRequest();

        // Privacy: niets wat persoonlijk kan zijn gaat naar de externe AI. Namen van mensen kunnen we niet herkennen; daarom staat dat ook in de uitleg bij het veld.
        var persoonlijk = ServiceCore.Planning.ProjectSjabloonAiVerwerker.ControleerPrivacy(verzoek.Beschrijving);
        if (persoonlijk.Count > 0)
            return Json(new ProjectSjabloonAiVoorstel { Fout = "In de beschrijving lijkt " + string.Join(", ", persoonlijk) + " te staan. Haal dat weg: er gaan geen persoonsgegevens, adressen of bedragen naar de AI." }, JsonOpties);

        var sjablonen = HttpContext.RequestServices.GetRequiredService<IProjectSjabloonService>();
        var context = await sjablonen.AiContext(projectId);
        var voorstel = await HttpContext.RequestServices.GetRequiredService<IProjectSjabloonAiService>().Voorstel(verzoek.Beschrijving, context, ct);
        // De duur komt uit de planning, niet van de AI.
        if (voorstel.Ok && voorstel.SjabloonId is int gekozen) voorstel.Controle = await sjablonen.Controle(projectId, gekozen, voorstel.Antwoorden);
        return Json(voorstel, JsonOpties);
    }

    /// <summary>Bewaart de getoonde planning als sjabloon. Vraagt naast schrijfrecht op de planning het recht op planningssjablonen.</summary>
    [HttpPost("SjabloonBewaren")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> SjabloonBewaren(int projectId, [FromBody] SjabloonBewaarVerzoek verzoek)
    {
        if (verzoek == null) return BadRequest();
        if (!HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.SettingsPlanningSjablonen))
            return Forbid();
        return Json(await _sjablonen.BewaarVanPlanning(projectId, verzoek.PlanningId, verzoek.Naam, verzoek.Beschrijving, UserId), JsonOpties);
    }

    [HttpPost("SjabloonVerwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> SjabloonVerwijderen(int projectId, [FromBody] SjabloonVerwijderVerzoek verzoek)
    {
        if (verzoek == null) return BadRequest();
        if (!HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasDelete(PermissionCodes.SettingsPlanningSjablonen))
            return Forbid();
        var fout = await _sjablonen.Verwijder(verzoek.Id);
        return Json(new { ok = fout == null, fout }, JsonOpties);
    }

    /// <summary>
    /// Stap 1 en 2 van "Planning importeren": leest een MS Project-XML in en geeft het voorbeeld terug (niets wordt bewaard).
    /// De pagina stuurt het resultaat daarna als commando <c>importeren</c> terug.
    /// </summary>
    [HttpPost("ImportControle")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsPlanning)]
    public async Task<IActionResult> ImportControle(int projectId, IFormFile? bestand)
    {
        if (bestand == null || bestand.Length == 0) return Json(new FacadeCore.PlanningImportBestand { Fout = "Kies een bestand." }, JsonOpties);
        if (bestand.Length > 10 * 1024 * 1024) return Json(new FacadeCore.PlanningImportBestand { Fout = "Het bestand is groter dan 10 MB." }, JsonOpties);
        var naam = Path.GetFileName(bestand.FileName);
        if (naam.EndsWith(".mpp", StringComparison.OrdinalIgnoreCase))
            return Json(new FacadeCore.PlanningImportBestand { Bestandsnaam = naam, Fout = "Een .mpp-bestand kan niet rechtstreeks. Open het in MS Project, kies Bestand › Opslaan als › XML-indeling (*.xml) en kies dat bestand." }, JsonOpties);
        if (!naam.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return Json(new FacadeCore.PlanningImportBestand { Bestandsnaam = naam, Fout = "Enkel MS Project-XML (.xml) kan nu ingelezen worden." }, JsonOpties);
        using var stream = bestand.OpenReadStream();
        var uitkomst = ServiceCore.Planning.MsProjectXmlParser.Lees(stream, naam);
        if (uitkomst.Ok && uitkomst.Resources.Count > 0)
        {
            // Bedrijven om resources aan te koppelen: enkel die met een contract in dit project.
            var inProject = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId).Select(c => c.CompanyId).Distinct().ToListAsync();
            var bedrijven = await _db.CompanyInfo.AsNoTracking().Where(c => inProject.Contains(c.CompanyId) && c.BedrijfsNaam != null && c.BedrijfsNaam != "")
                .OrderBy(c => c.BedrijfsNaam).Select(c => new { c.CompanyId, c.BedrijfsNaam }).ToListAsync();
            uitkomst.Bedrijven = bedrijven.Select(b => new FacadeCore.PlanningUitvoerderOptie { Id = b.CompanyId, Naam = b.BedrijfsNaam!, InProject = true }).ToList();
        }
        return Json(uitkomst, JsonOpties);
    }

    [HttpGet("Staat")]
    public async Task<IActionResult> Staat(int projectId, int? planningId = null) => Json(await _planning.Laad(projectId, planningId), JsonOpties);
}
