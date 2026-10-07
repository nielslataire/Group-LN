using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Traject;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.ProjectsDossiers)]
[Route("Projects/{projectId:int}/Dossiers")]
public class ProjectDossiersController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly IProjectDossierService _dossiers;
    private readonly INutsAansluitingService _nuts;

    public ProjectDossiersController(cpmRunningContext db, IProjectDossierService dossiers, INutsAansluitingService nuts)
    {
        _db = db;
        _dossiers = dossiers;
        _nuts = nuts;
    }

    private string? UserId => User.FindFirst(CpmClaims.UserId)?.Value;

    [HttpGet("~/Projects/Dossiers")]
    public IActionResult MenuRedirect()
    {
        AddMessage("info", "Selecteer eerst een project om dossiers te bekijken.", "Info");
        return RedirectToAction("Index", "Projecten");
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int projectId, [FromQuery] DossierFilterBO filters, string? tab = null, int? hl = null, int? kind = null)
    {
        var projectName = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync()
            ?? $"Project {projectId}";

        SetBreadcrumbs(projectName, projectId);
        SetPageHeader("ph ph-folder-open", $"{projectName} - Dossiers");
        if (UseGlV2)
            ViewData["BreadcrumbNode"] = ProjectCrumb(projectName, projectId); // gl-v2: de kruimel stopt bij het project, "Dossiers" is de titel (punt 13, regel 2)
        ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
        // Zelfde .gl-traject-tabrow als ProjectTraject/Index — die rij rekent op deze klasse om
        // theme.css's ongeconditioneerde .content-body-marge (10px) te neutraliseren op desktop,
        // anders staat de tabrij 10px te laag t.o.v. de topbar (zie DESIGN.md ".gl-traject-tabrow").
        ViewBag.ContentBodyClass = "gl-traject-flush";

        filters ??= new DossierFilterBO();
        var vm = new DossierIndexVm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Filter = filters,
            Dossiers = await _dossiers.Search(projectId, filters),
            ProjectUnits = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync()
        };
        if (UseGlV2)
        {
            vm.StartTab = tab;
            vm.HighlightDossierId = hl;
            vm.FilterKind = kind;
            await VulV2Index(vm);
            return View("IndexV2", vm);
        }
        return View(vm);
    }

    private bool UseGlV2 => ViewData["UseGlV2Layout"] as bool? == true;

    private static MvcBreadcrumbNode ProjectCrumb(string projectName, int projectId) =>
        new("Detail", "Projecten", projectName)
        {
            Parent = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = new MvcBreadcrumbNode("Index", "Home", "Dashboard") },
            RouteValues = new { projectid = projectId }
        };

    private async Task VulV2Index(DossierIndexVm vm)
    {
        var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        vm.CanWrite = ps.HasWrite(PermissionCodes.ProjectsDossiers);
        vm.CanDelete = ps.HasDelete(PermissionCodes.ProjectsDossiers);

        var nuts = await _db.ProjectNutsAansluiting.AsNoTracking()
            .Where(n => n.ProjectDossier.ProjectId == vm.ProjectId)
            .Include(n => n.NetbeheerderCompany).Include(n => n.ProjectDossier)
            .ToListAsync();
        foreach (var g in nuts.Where(n => n.NetbeheerderCompanyId.HasValue).GroupBy(n => n.NutsType))
        {
            var top = g.GroupBy(n => n.NetbeheerderCompanyId!.Value).OrderByDescending(x => x.Count()).First();
            vm.NetbeheerderPerType[g.Key] = (top.Key, top.First().NetbeheerderCompany?.BedrijfsNaam ?? "");
        }
        foreach (var n in nuts.Where(n => n.UnitId.HasValue && n.ProjectDossier.Status != (int)DossierStatus.Geannuleerd))
            vm.BestaandePerEenheidEnType.Add($"{n.UnitId}:{n.NutsType}");
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int projectId, int id)
    {
        var dossier = await _dossiers.GetById(projectId, id);
        if (dossier == null) return NotFound();

        var projectName = await _db.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync()
            ?? $"Project {projectId}";

        SetBreadcrumbs(projectName, projectId, dossier.Titel);
        if (UseGlV2)
        {
            // gl-v2 kruimel (design 31d): Project / Dossiers / "Elektriciteit · Lot 2" — de titel van het dossier staat al in de kop.
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Index", "ProjectDossiers", "Dossiers")
            {
                Parent = ProjectCrumb(projectName, projectId),
                RouteValues = new { projectId }
            };
        }
        var dossierIcon = dossier.DossierKind == (int)DossierKind.NutsAansluiting ? "ph ph-lightning" : "ph ph-folder-open";
        SetPageHeader(dossierIcon, dossier.Titel);

        var mijlpaalIds = await _db.ProjectDossierMijlpaal.Where(x => x.ProjectDossierId == id).Select(x => x.MijlpaalId).ToListAsync();
        var gekoppeld = await _db.Mijlpaal.Where(m => mijlpaalIds.Contains(m.Id)).ToListAsync();

        int? projecttrajectId = await _db.Projecttraject.Where(t => t.ProjectId == projectId).Select(t => t.Id).FirstOrDefaultAsync();
        var beschikbaar = projecttrajectId is int ptid
            ? await _db.Mijlpaal.Where(m => m.ProjecttrajectId == ptid && !mijlpaalIds.Contains(m.Id))
                .OrderBy(m => m.Volgorde).ToListAsync()
            : new List<Mijlpaal>();

        var vm = new DossierDetailsVm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Dossier = dossier,
            GekoppeldeMijlpalen = gekoppeld,
            BeschikbareMijlpalen = beschikbaar
        };

        if (dossier.DossierKind == (int)DossierKind.NutsAansluiting)
            vm.Nuts = await _nuts.GetById(projectId, id);
        vm.ProjectUnits = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync();

        if (UseGlV2)
        {
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            vm.CanWrite = ps.HasWrite(PermissionCodes.ProjectsDossiers);
            vm.CanDelete = ps.HasDelete(PermissionCodes.ProjectsDossiers);
            var userIds = dossier.Gebeurtenissen.Select(g => g.UserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
            if (userIds.Count > 0)
            {
                var users = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.UserId)).Select(u => new { u.UserId, u.Voornaam }).ToListAsync();
                foreach (var u in users) vm.Gebruikers[u.UserId] = u.Voornaam ?? "";
            }
            vm.CanSeeDocs = ps.HasRead(PermissionCodes.ProjectsDocuments);
            vm.CanUploadDocs = vm.CanSeeDocs && vm.CanWrite && ps.HasWrite(PermissionCodes.ProjectsDocuments);
            if (vm.CanSeeDocs)
            {
                var koppelingen = await _db.ProjectDossierDocument.AsNoTracking()
                    .Where(x => x.ProjectDossierId == id && x.ProjectDocId != null)
                    .Include(x => x.ProjectDoc).ThenInclude(p => p.Revisions)
                    .OrderByDescending(x => x.CreatedDate).ToListAsync();
                foreach (var k in koppelingen.Where(k => k.ProjectDoc != null))
                {
                    var doc = k.ProjectDoc;
                    var rev = doc.Revisions.FirstOrDefault(r => r.Id == doc.CurrentRevisionId) ?? doc.Revisions.OrderByDescending(r => r.RevisionNo).FirstOrDefault();
                    vm.Documenten.Add(new DossierDocumentV2
                    {
                        DocumentId = doc.Id,
                        Naam = doc.Name,
                        Datum = rev?.UploadedDate.ToLocalTime() ?? doc.CreatedDate?.ToLocalTime() ?? k.CreatedDate.ToLocalTime(),
                        Grootte = rev?.SizeBytes,
                        Extensie = System.IO.Path.GetExtension(rev?.OriginalFilename ?? rev?.Filename ?? doc.Filename ?? "").TrimStart('.'),
                        RevisionId = rev?.Id
                    });
                }
            }
            if (dossier.UnitId is int uid)
            {
                var eenheid = await _db.Units.AsNoTracking().Where(u => u.Id == uid).Select(u => new { u.Name, Klant = u.ClientAccount != null ? u.ClientAccount.Name : null }).FirstOrDefaultAsync();
                vm.EenheidKlant = eenheid == null ? null : eenheid.Name + (string.IsNullOrWhiteSpace(eenheid.Klant) ? "" : " · " + eenheid.Klant);
            }
            return View("DetailsV2", vm);
        }
        return View(vm);
    }

    // ── gl-v2 (design-handoff 31c/31d/31e/31f) ──────────────────────────────────────────────────────

    /// <summary>31c — "Nieuwe nutsaansluiting": een scherm i.p.v. een modal (18 velden plus werkstroom).</summary>
    [HttpGet("Nuts/Nieuw")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> NutsNieuw(int projectId, int? unitId, int? type)
    {
        var vm = await BouwNutsForm(projectId, null);
        vm.DefaultType = type ?? 0;
        vm.DefaultUnitId = unitId;
        if (await VoorstelNetbeheerder(projectId, vm.DefaultType) is { } nb) { vm.DefaultNetbeheerderId = nb.Id; vm.DefaultNetbeheerderNaam = nb.Naam; }
        SetNutsFormHeader(vm, "Nieuwe nutsaansluiting");
        return View("NutsFormV2", vm);
    }

    [HttpGet("Nuts/{id:int}/Bewerken")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> NutsBewerken(int projectId, int id)
    {
        var nuts = await _nuts.GetById(projectId, id);
        if (nuts == null) return NotFound();
        var vm = await BouwNutsForm(projectId, nuts);
        SetNutsFormHeader(vm, "Nutsaansluiting bewerken");
        return View("NutsFormV2", vm);
    }

    private async Task<NutsFormV2Vm> BouwNutsForm(int projectId, ProjectNutsAansluiting? nuts)
    {
        var projectName = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";
        var vm = new NutsFormV2Vm
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Bestaand = nuts,
            Dossier = nuts?.ProjectDossier,
            Units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync()
        };
        if (nuts != null)
        {
            var stappen = await _dossiers.GetSubstappen(nuts.ProjectDossierId);
            var lijst = DossierWeergave.ChecklistItems(stappen).ToList();
            vm.ChecklistTotaal = lijst.Count;
            vm.ChecklistKlaar = lijst.Count(s => s.Status == (int)DossierSubstapStatus.Afgerond || s.Status == (int)DossierSubstapStatus.NietVanToepassing);
        }
        return vm;
    }

    private async Task<(int Id, string Naam)?> VoorstelNetbeheerder(int projectId, int nutsType)
    {
        var top = await _db.ProjectNutsAansluiting.AsNoTracking()
            .Where(n => n.ProjectDossier.ProjectId == projectId && n.NutsType == nutsType && n.NetbeheerderCompanyId != null)
            .GroupBy(n => n.NetbeheerderCompanyId!.Value).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefaultAsync();
        if (top == 0) return null;
        var naam = await _db.CompanyInfo.AsNoTracking().Where(c => c.CompanyId == top).Select(c => c.BedrijfsNaam).FirstOrDefaultAsync();
        return naam == null ? null : (top, naam);
    }

    private void SetNutsFormHeader(NutsFormV2Vm vm, string title)
    {
        SetPageHeader("ph ph-lightning", title);
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Index", "ProjectDossiers", "Dossiers")
        {
            Parent = ProjectCrumb(vm.ProjectName, vm.ProjectId),
            RouteValues = new { projectId = vm.ProjectId }
        };
    }

    /// <summary>31d — de hoofdknop "volgende stap": zet de eerstvolgende lege werkstroomdatum op vandaag (of de opgegeven datum).</summary>
    [HttpPost("Nuts/{id:int}/Stap")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> NutsVolgendeStap(int projectId, int id, DateOnly? datum)
    {
        var nuts = await _nuts.GetById(projectId, id);
        if (nuts == null) return NotFound();
        var volgende = NutsVolgendeStapCode(nuts);
        if (volgende != null)
        {
            var stappen = await _dossiers.GetSubstappen(id);
            var stap = stappen.FirstOrDefault(s => string.Equals(s.Code, volgende, StringComparison.OrdinalIgnoreCase));
            if (stap != null) await _dossiers.ChangeSubstapStatus(id, stap.Id, (int)DossierSubstapStatus.Afgerond, datum ?? DateOnly.FromDateTime(DateTime.Today), UserId);
        }
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    internal static string? NutsVolgendeStapCode(ProjectNutsAansluiting n) =>
        !n.AanvraagVerstuurdOp.HasValue ? "AANVRAAG"
        : !n.OfferteOntvangenOp.HasValue ? "OFFERTE_ONTVANGEN"
        : !n.OfferteGoedgekeurdOp.HasValue ? "OFFERTE_GOEDGEKEURD"
        : !n.UitvoeringGevraagdOp.HasValue ? "UITVOERINGSDATUM_DOORGEGEVEN"
        : !n.UitgevoerdOp.HasValue ? "UITGEVOERD"
        : null;

    [HttpPost("{id:int}/Checklist/Toevoegen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> ChecklistToevoegen(int projectId, int id, string? naam)
    {
        if (await _dossiers.GetById(projectId, id) == null) return NotFound();
        await _dossiers.AddChecklistItem(id, naam ?? "", UserId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/Checklist/{substapId:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> ChecklistVerwijderen(int projectId, int id, int substapId)
    {
        if (await _dossiers.GetById(projectId, id) == null) return NotFound();
        await _dossiers.RemoveChecklistItem(id, substapId, UserId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    /// <summary>31f — "Ander dossier": modal (nieuw of bewerken), inhoud via AJAX.</summary>
    [HttpGet("Ander/Modal")]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> AnderModal(int projectId, int? id, int? kind)
    {
        var vm = new DossierAnderModalVm
        {
            ProjectId = projectId,
            DefaultKind = kind ?? (int)DossierKind.Omgevingsvergunning,
            Units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).ToListAsync(),
        };
        if (id is int did and > 0)
        {
            vm.Bestaand = await _dossiers.GetById(projectId, did);
            if (vm.Bestaand == null) return NotFound();
        }
        var ptid = await _db.Projecttraject.Where(t => t.ProjectId == projectId).Select(t => (int?)t.Id).FirstOrDefaultAsync();
        if (ptid.HasValue)
        {
            vm.Mijlpalen = await _db.Mijlpaal.AsNoTracking().Where(m => m.ProjecttrajectId == ptid && m.UnitId == null).OrderBy(m => m.Volgorde).ToListAsync();
            if (vm.Bestaand != null)
                vm.GekoppeldeMijlpaalId = await _db.ProjectDossierMijlpaal.Where(x => x.ProjectDossierId == vm.Bestaand.Id).Select(x => (int?)x.MijlpaalId).FirstOrDefaultAsync();
        }
        return PartialView("Modals/_ModalAnderV2", vm);
    }

    [HttpPost("Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Opslaan(int projectId, [FromForm] DossierUpsertBO dto, int? koppelMijlpaalId = null)
    {
        dto.ProjectId = projectId;
        // Afgehandeld ↔ afgehandeld-datum: een dossier dat afgehandeld wordt krijgt vandaag, een heropend dossier verliest de datum.
        if (dto.Status == (int)DossierStatus.Afgehandeld) dto.AfgehandeldDatum ??= DateOnly.FromDateTime(DateTime.Today);
        else dto.AfgehandeldDatum = null;
        int dossierId;
        bool nieuw = !(dto.Id is int id and > 0);
        if (!nieuw)
        {
            await _dossiers.Update(dto.Id!.Value, dto, UserId);
            dossierId = dto.Id!.Value;
        }
        else
            dossierId = (await _dossiers.Create(dto, UserId)).Id;

        // gl-v2 31f: "Koppel aan mijlpaal" — voegt de gekozen mijlpaal toe aan wat al gekoppeld is (een vergunning koppelt er zelf
        // een reeks). Ontkoppelen gebeurt per mijlpaal op het dossierdetail, nooit stilzwijgend vanuit dit formulier.
        if (koppelMijlpaalId is int mid and > 0)
        {
            var huidig = await _db.ProjectDossierMijlpaal.Where(x => x.ProjectDossierId == dossierId).Select(x => x.MijlpaalId).ToListAsync();
            if (!huidig.Contains(mid)) await _dossiers.LinkMijlpaal(dossierId, mid);
        }

        AddMessage("success", "Het dossier is opgeslagen.", "Opgeslagen");
        if (UseGlV2 && !nieuw) return RedirectToAction(nameof(Details), new { projectId, id = dossierId });
        return RedirectToAction(nameof(Index), new { projectId, tab = UseGlV2 ? "alle" : null, kind = UseGlV2 ? dto.DossierKind : (int?)null, hl = dossierId });
    }

    [HttpPost("Nuts/Opslaan")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> NutsOpslaan(int projectId, [FromForm] NutsAansluitingUpsertBO dto, bool opslaanEnNieuw = false)
    {
        dto.ProjectId = projectId;
        ProjectNutsAansluiting saved;
        if (dto.Id is int id and > 0)
            saved = await _nuts.Update(id, dto, UserId) ?? throw new InvalidOperationException("Nutsaansluiting niet gevonden.");
        else
            saved = await _nuts.Create(dto, UserId);

        AddMessage("success", "Het nutsaansluitingsdossier is opgeslagen.", "Opgeslagen");
        if (opslaanEnNieuw && UseGlV2) return RedirectToAction(nameof(NutsNieuw), new { projectId, type = dto.NutsType });
        return RedirectToAction(nameof(Details), new { projectId, id = saved.ProjectDossierId });
    }

    [HttpPost("Nuts/BulkAanmaken")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> NutsBulkAanmaken(int projectId, [FromForm] NutsAansluitingBulkCreateBO dto)
    {
        dto.ProjectId = projectId;
        var result = await _nuts.CreateBulkForUnits(dto, UserId);
        var msg = $"{result.AantalAangemaakt} nutsaansluitingsdossier(s) aangemaakt.";
        if (result.OvergeslagenEenheden.Count > 0)
            msg += $" Overgeslagen (bestond al voor dit type): {string.Join(", ", result.OvergeslagenEenheden)}.";
        AddMessage(result.OvergeslagenEenheden.Count > 0 ? "warning" : "success", msg, "Bulk aanmaken");
        return RedirectToAction(nameof(Index), new { projectId, tab = UseGlV2 ? "nuts" : null });
    }

    [HttpGet("Units/{unitId:int}/Meterdata")]
    public async Task<IActionResult> UnitMeterdata(int projectId, int unitId)
    {
        var (eanGas, eanElek, watermeter) = await _nuts.GetUnitMeterData(unitId);
        return Json(new { eanGas, eanElek, watermeter });
    }

    [HttpPost("{id:int}/Status")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Status(int projectId, int id, int nieuweStatus, string? opmerking)
    {
        var ok = await _dossiers.ChangeStatus(projectId, id, nieuweStatus, UserId, opmerking);
        if (!ok) AddMessage("danger", "Dossier niet gevonden.", "Fout");
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/Substap/{substapId:int}/Status")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> SubstapStatus(int projectId, int id, int substapId, int nieuweStatus, DateOnly? datum)
    {
        await _dossiers.ChangeSubstapStatus(id, substapId, nieuweStatus, datum, UserId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { ok = true });
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/Gebeurtenis")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Gebeurtenis(int projectId, int id, [FromForm] DossierGebeurtenisBO dto)
    {
        dto.ProjectDossierId = id;
        await _dossiers.AddGebeurtenis(dto, UserId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/KoppelMijlpaal")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> KoppelMijlpaal(int projectId, int id, int mijlpaalId)
    {
        await _dossiers.LinkMijlpaal(id, mijlpaalId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/OntkoppelMijlpaal")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> OntkoppelMijlpaal(int projectId, int id, int mijlpaalId)
    {
        await _dossiers.UnlinkMijlpaal(id, mijlpaalId);
        return RedirectToAction(nameof(Details), new { projectId, id });
    }

    [HttpPost("{id:int}/Verwijderen")]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsDossiers)]
    public async Task<IActionResult> Verwijderen(int projectId, int id)
    {
        await _dossiers.Delete(projectId, id, UserId);
        AddMessage("success", "Het dossier is verwijderd.", "Verwijderd");
        return RedirectToAction(nameof(Index), new { projectId });
    }

    private void SetBreadcrumbs(string projectName, int projectId, string? leaf = null)
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var projecten = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = dashboard };
        var detail = new MvcBreadcrumbNode("Detail", "Projecten", projectName) { Parent = projecten, RouteValues = new { projectid = projectId } };
        var lijst = new MvcBreadcrumbNode("Index", "ProjectDossiers", "Dossiers") { Parent = detail, RouteValues = new { projectId } };
        ViewData["BreadcrumbNode"] = leaf == null
            ? lijst
            : new MvcBreadcrumbNode("Details", "ProjectDossiers", leaf) { Parent = lijst, RouteValues = new { projectId } };
    }
}
