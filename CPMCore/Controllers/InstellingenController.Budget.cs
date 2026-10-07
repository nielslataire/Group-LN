using BOCore;
using BOCore.Budget;
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

/// <summary>Budget-instellingen: kostprijzen materialen, budgetformules, bouwindexen. Opgesplitst uit InstellingenController.cs (okt. 2026, structureren) - views in Views/Instellingen/Budget/. Zelfde partial class: alle private velden/services van InstellingenController.cs blijven gewoon bruikbaar.</summary>
public partial class InstellingenController
{
    // ─── Kostprijzen materialen ─────────────────────────────────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    [Breadcrumb("Kostprijzen materialen")]
    public IActionResult KostprijsMaterialen()
    {
        SetPageHeader("bx bx-cog", "Kostprijzen materialen");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };
        var instellingenKM = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("KostprijsMaterialen", "Instellingen", "Kostprijs materialen")
        {
            Parent = instellingenIndex
        };

        ViewData["BreadcrumbNode"] = instellingenKM;

        var vm = new KostprijsMaterialenViewModel
        {
            IndexTypes         = _kostprijsService.GetIndexTypes().Values         ?? new(),
            ActivityGroepen    = _kostprijsService.GetActivityGroepen().Values    ?? new(),
            Materialen         = _kostprijsService.GetMaterialen().Values         ?? new(),
            PercentageGroepen  = _kostprijsService.GetPercentageGroepen().Values  ?? new(),
            Percentages        = _kostprijsService.GetPercentages().Values        ?? new(),
            FormulaKoppelingen = _kostprijsService.GetFormulaKoppelingen().Values ?? new()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsMateriaalCreate(int categorieId, string naam, string eenheid, string toepassingsgebied, int kmIndexTypeId, decimal referentiePrijs, DateTime referentieDatum, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdateMateriaal(new KostprijsMateriaalBO { CategorieId = categorieId, Naam = naam, Eenheid = eenheid, Toepassingsgebied = toepassingsgebied, KmIndexTypeId = kmIndexTypeId, ReferentiePrijs = referentiePrijs, ReferentieDatum = referentieDatum, Volgorde = volgorde }), "Materiaal aangemaakt.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsMateriaalUpdate(int id, int categorieId, string naam, string eenheid, string toepassingsgebied, int kmIndexTypeId, decimal referentiePrijs, DateTime referentieDatum, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdateMateriaal(new KostprijsMateriaalBO { Id = id, CategorieId = categorieId, Naam = naam, Eenheid = eenheid, Toepassingsgebied = toepassingsgebied, KmIndexTypeId = kmIndexTypeId, ReferentiePrijs = referentiePrijs, ReferentieDatum = referentieDatum, Volgorde = volgorde }), "Materiaal opgeslagen.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsMateriaalDelete(int id)
    {
        var r = _kostprijsService.DeleteMateriaal(id);
        SetActivityResponseMessage(r, "Materiaal verwijderd.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost]
    public IActionResult FormulaKoppelingOpslaanAjax([FromBody] FormulaKoppelingAjaxRequest req)
    {
        if (req == null) return BadRequest();
        var r = _kostprijsService.SaveFormulaKoppeling(req.Sleutel, req.MateriaalId);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, message = r.Success ? "Koppeling opgeslagen." : fout });
    }

    [HttpGet]
    public IActionResult GetFormulaKoppelingen()
    {
        var lijst = _kostprijsService.GetFormulaKoppelingen().Values ?? new();
        return Json(lijst.Select(k => new {
            k.Sleutel, k.Omschrijving, k.MateriaalId, k.MateriaalNaam,
            k.MateriaalReferentiePrijs, k.MateriaalEenheid
        }));
    }

    [HttpPost]
    public IActionResult FormulaKoppelingAanmaken([FromBody] FormulaKoppelingAanmakenRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Naam)) return BadRequest();
        var r = _kostprijsService.CreateFormulaKoppeling(req.Naam, req.MateriaalId);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        var k = r.Values?.FirstOrDefault();
        return Json(new
        {
            ok = r.Success,
            message = r.Success ? "Formule-koppeling aangemaakt." : fout,
            koppeling = k == null ? null : new
            {
                k.Id, k.Sleutel, k.Omschrijving, k.MateriaalId, k.MateriaalNaam,
                k.MateriaalReferentiePrijs, k.MateriaalEenheid
            }
        });
    }

    [HttpPost]
    public IActionResult FormulaKoppelingVerwijderen([FromBody] int id)
    {
        var r = _kostprijsService.DeleteFormulaKoppeling(id);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, message = r.Success ? "Koppeling verwijderd." : fout });
    }

    // ─── Budgetformules (voorstellen BudgetActivityLijnen) ─────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsCalculatie)]
    [Breadcrumb("Budgetformules")]
    public async Task<IActionResult> BudgetFormules()
    {
        SetPageHeader("bx bx-math", "Budgetformules");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };
        var formulesNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("BudgetFormules", "Instellingen", "Budgetformules")
        {
            Parent = instellingenIndex
        };
        ViewData["BreadcrumbNode"] = formulesNode;

        var vm = new BudgetFormulesViewModel
        {
            Formules     = await _budgetFormules.GetFormulesAsync(),
            Activiteiten = await _budgetFormules.GetActiviteitenMetLotAsync(),
            TestVersies  = await _budgetFormules.GetTestVersiesAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgetFormuleParameters(int? versieId)
    {
        var parameters = await _budgetFormules.GetParametersAsync(versieId);
        return Json(parameters.Select(p => new { p.Naam, p.Omschrijving, p.Eenheid, p.Categorie, p.Waarde }));
    }

    [HttpPost]
    public async Task<IActionResult> BudgetFormuleOpslaan([FromBody] BudgetFormuleOpslaanRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Formule)) return BadRequest();
        var r = await _budgetFormules.SaveAsync(req.ActivityId, req.Formule, req.Omschrijving, req.Actief);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        var f = r.Value;
        return Json(new
        {
            ok = r.Success,
            message = r.Success ? "Formule opgeslagen." : fout,
            formule = f == null ? null : new
            {
                f.Id, f.ActivityId, f.ActivityOmschrijving, f.LotNaam, f.LotNummer,
                f.Formule, f.Omschrijving, f.Actief, laatstGewijzigd = f.LaatstGewijzigd.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> BudgetFormuleDelete([FromBody] int id)
    {
        var r = await _budgetFormules.DeleteAsync(id);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, message = r.Success ? "Formule verwijderd." : fout });
    }

    [HttpPost]
    public async Task<IActionResult> BudgetFormuleTest([FromBody] BudgetFormuleTestRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Formule) || req.VersieId <= 0) return BadRequest();
        var r = await _budgetFormules.TestAsync(req.VersieId, req.Formule);
        return Json(new
        {
            ok              = r.Ok,
            fout            = r.Fout,
            totaal          = r.Totaal,
            perEenheid      = r.PerEenheid,
            aantalEenheden  = r.AantalEenheden,
            onbekendeParams = r.OnbekendeParams,
            termen          = r.Termen.Select(t => new { t.ExprNamen, t.ExprWaarden, t.Waarde })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageGroepCreate(string naam, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentageGroep(new BouwkostPercentageGroepBO { Naam = naam, Volgorde = volgorde }), "Groep aangemaakt.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageGroepUpdate(int id, string naam, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentageGroep(new BouwkostPercentageGroepBO { Id = id, Naam = naam, Volgorde = volgorde }), "Groep opgeslagen.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageGroepDelete(int id)
    {
        SetActivityResponseMessage(_kostprijsService.DeletePercentageGroep(id), "Groep verwijderd.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageCreate(int groepId, string naam, decimal percentage, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentage(new BouwkostPercentageBO { GroepId = groepId, Naam = naam, Percentage = percentage, Volgorde = volgorde }), "Percentage aangemaakt.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageUpdate(int id, int groepId, string naam, decimal percentage, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentage(new BouwkostPercentageBO { Id = id, GroepId = groepId, Naam = naam, Percentage = percentage, Volgorde = volgorde }), "Percentage opgeslagen.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageDelete(int id)
    {
        SetActivityResponseMessage(_kostprijsService.DeletePercentage(id), "Percentage verwijderd.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpGet]
    public IActionResult KostprijsUpdatePreview(int projectId)
    {
        ViewBag.ProjectId = projectId;
        SetPageHeader("bx bx-cog", "Kostprijzen bijwerken — preview");
        return View(_kostprijsService.GetUpdatePreview(projectId).Values ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsUpdateBevestigen(int projectId)
    {
        SetActivityResponseMessage(_kostprijsService.BevestigUpdate(projectId), "Kostprijzen bijgewerkt.");
        return RedirectToAction("Details", "Project", new { id = projectId });
    }

    // ─── Prijsreferenties verkoop (€/m²-codes voor stap 8 budgetwizard) ───────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    [Breadcrumb("Prijsreferenties verkoop")]
    public IActionResult BudgetPrijsReferenties()
    {
        SetPageHeader("bx bx-euro", "Prijsreferenties verkoop");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("BudgetPrijsReferenties", "Instellingen", "Prijsreferenties verkoop") { Parent = instellingenIndex };

        var projecten = _projectService.GetProjectsForList();
        var vm = new BudgetPrijsReferentiesViewModel
        {
            Referenties = _prijsReferenties.GetAlle().Values ?? new(),
            Projecten   = (projecten.Success && projecten.Values != null ? projecten.Values : new())
                            .OrderBy(p => p.Name).Select(p => new IdNameBO { ID = p.Id, Display = p.Name }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BudgetPrijsReferentieCreate(string prijsType, int code, decimal prijsPerM2, string omschrijving, DateTime? datum, string bron, int? projectId)
    {
        SetActivityResponseMessage(_prijsReferenties.InsertUpdate(new BudgetPrijsReferentieBO
        {
            PrijsType = prijsType, Code = code, PrijsPerM2 = prijsPerM2, Omschrijving = omschrijving, Datum = datum, Bron = bron,
            ProjectId = projectId > 0 ? projectId : null
        }), "Prijsreferentie aangemaakt.");
        return RedirectToAction(nameof(BudgetPrijsReferenties));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BudgetPrijsReferentieUpdate(int id, string prijsType, int code, decimal prijsPerM2, string omschrijving, DateTime? datum, string bron, int? projectId)
    {
        SetActivityResponseMessage(_prijsReferenties.InsertUpdate(new BudgetPrijsReferentieBO
        {
            Id = id, PrijsType = prijsType, Code = code, PrijsPerM2 = prijsPerM2, Omschrijving = omschrijving, Datum = datum, Bron = bron,
            ProjectId = projectId > 0 ? projectId : null
        }), "Prijsreferentie opgeslagen.");
        return RedirectToAction(nameof(BudgetPrijsReferenties));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BudgetPrijsReferentieDelete(int id)
    {
        SetActivityResponseMessage(_prijsReferenties.Delete(id), "Prijsreferentie verwijderd.");
        return RedirectToAction(nameof(BudgetPrijsReferenties));
    }

    // ─── Referentieprojecten voor de nacalculatie (stap 6 budgetwizard) ────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    [Breadcrumb("Referentieprojecten")]
    public async Task<IActionResult> BudgetReferentieProjecten()
    {
        SetPageHeader("bx bx-history", "Referentieprojecten (nacalc)");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("BudgetReferentieProjecten", "Instellingen", "Referentieprojecten") { Parent = instellingenIndex };

        var projecten = _projectService.GetProjectsForList();
        var vm = new BudgetReferentieProjectenViewModel
        {
            Referenties = await _referentieProjecten.GetAlleAsync(),
            Projecten   = (projecten.Success && projecten.Values != null ? projecten.Values : new())
                            .OrderBy(p => p.Name).Select(p => new IdNameBO { ID = p.Id, Display = p.Name }).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BudgetReferentieSjabloon()
        => File(_referentieProjecten.MaakSjabloon(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "referentieproject-sjabloon.xlsx");

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    public async Task<IActionResult> BudgetReferentieProjectImportExcel(IFormFile bestand, string naam, DateTime? datum, int aantalEenheden, decimal? oppervlakteGBA, string opmerking)
    {
        if (bestand == null || bestand.Length == 0)
        {
            TempData["Error"] = "Kies een Excel-bestand (.xlsx).";
            return RedirectToAction(nameof(BudgetReferentieProjecten));
        }
        await using var stream = bestand.OpenReadStream();
        var r = await _referentieProjecten.ImportExcelAsync(stream, naam, datum, aantalEenheden, oppervlakteGBA, opmerking);
        SetReferentieMessages(r);
        return RedirectToAction(nameof(BudgetReferentieProjecten));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    public async Task<IActionResult> BudgetReferentieProjectImportProject(int projectId, string naam, DateTime? datum, string opmerking)
    {
        var r = await _referentieProjecten.ImportUitProjectAsync(projectId, naam, datum, opmerking);
        SetReferentieMessages(r);
        return RedirectToAction(nameof(BudgetReferentieProjecten));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    public async Task<IActionResult> BudgetReferentieProjectDelete(int id)
    {
        SetActivityResponseMessage(await _referentieProjecten.DeleteAsync(id), "Referentieproject verwijderd.");
        return RedirectToAction(nameof(BudgetReferentieProjecten));
    }

    /// <summary>Import-resultaat: succes én waarschuwingen (niet-herkende rijen) samen tonen, fouten apart.</summary>
    private void SetReferentieMessages(Response r)
    {
        var fouten = r.Messages.Where(m => m.Type == MessageType.Error).Select(m => m.Message).ToList();
        var rest   = r.Messages.Where(m => m.Type != MessageType.Error).Select(m => m.Message).ToList();
        if (fouten.Count > 0) TempData["Error"]   = string.Join(" ", fouten);
        if (rest.Count   > 0) TempData["Message"] = string.Join(" ", rest);
    }

    // ─── Bouwindexen ────────────────────────────────────────────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsBouwIndexen)]
    [Breadcrumb("Bouwindexen")]
    public async Task<IActionResult> Bouwindexen()
    {
        SetPageHeader("bx bx-cog", "Bouwindexen");

        var dashboard       = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen    = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var bouwindexen     = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Bouwindexen", "Instellingen", "Bouwindexen") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = bouwindexen;

        var model = new CPMCore.Models.Instellingen.BouwindexenModel
        {
            SIndexen     = await _bouwIndex.GetGefilterdAsync("S"),
            I2021Indexen = await _bouwIndex.GetGefilterdAsync("I2021"),
            IPlusIndexen = await _bouwIndex.GetGefilterdAsync("I+"),
            AbexIndexen  = await _bouwIndex.GetGefilterdAsync("ABEX")
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SyncSIndex()
    {
        var result = await _sIndexScraper.ScrapeAsync();
        var herlaad = result.Geslaagd && (result.AantalNieuw > 0 || result.AantalBijgewerkt > 0);
        return Json(new { ok = herlaad, waarschuwing = !herlaad && result.Geslaagd, bericht = result.Samenvatting });
    }

    [HttpPost]
    public async Task<IActionResult> SyncI2021()
    {
        var result = await _i2021Sync.SyncAsync();
        var herlaad = result.Geslaagd && (result.AantalNieuw > 0 || result.AantalBijgewerkt > 0);
        return Json(new { ok = herlaad, waarschuwing = !herlaad && result.Geslaagd, bericht = result.Samenvatting });
    }

    [HttpGet]
    public async Task<IActionResult> GetBouwIndexen(string indexType)
    {
        var lijst = await _bouwIndex.GetGefilterdAsync(indexType);
        return Json(lijst);
    }

    [HttpPost]
    public async Task<IActionResult> OpslaanBouwIndex([FromBody] DALCore.Models.BouwIndex index)
    {
        try
        {
            await _bouwIndex.OpslaanAsync(index);
            return Json(new { ok = true });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, bericht = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> VerwijderBouwIndex(int id)
    {
        var ok = await _bouwIndex.VerwijderenAsync(id);
        return Json(new { ok });
    }

    [HttpPost]
    public async Task<IActionResult> SetActiefBouwIndex(string indexType, int id)
    {
        await _bouwIndex.SetActiefAsync(indexType, id);
        return Json(new { ok = true });
    }
}
