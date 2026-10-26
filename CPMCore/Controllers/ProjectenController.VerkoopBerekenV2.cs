using System.Globalization;
using CPMCore.Services.Verkoop;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 45c): Eenheid berekenen. Enkel rekenen en bekijken: alles rekent live in de browser, er wordt niets bewaard.
public partial class ProjectenController
{
    [HttpGet]
    public async Task<IActionResult> SalesCalculateV2(int projectid, int? unitId)
    {
        if (ViewData["UseGlV2Layout"] as bool? != true) return RedirectToAction("DetailSales", new { projectid });
        var index = await Verkoop.GetIndexAsync(projectid);
        if (index.Rijen.Count == 0) { AddMessage("error", "Dit project heeft nog geen eenheden om te berekenen.", "Fout"); return RedirectToAction("DetailSales", new { projectid }); }
        // verkochte eenheden zijn vergrendeld: we vallen terug op de eerste die nog niet verkocht is
        var unit = index.Rijen.FirstOrDefault(r => r.UnitId == unitId && r.Status != "Verkocht") ?? index.Rijen.FirstOrDefault(r => r.Status != "Verkocht") ?? index.Rijen.First();
        var be = CultureInfo.GetCultureInfo("nl-BE");
        VerkoopCode Code(DALCore.Models.BudgetPrijsReferentie p) => new(p.Code, (p.PrijsType == "Grond" ? "G" : "B") + "-" + p.Code.ToString("00") + " · " + p.Omschrijving + " · € " + p.PrijsPerM2.ToString("N0", be) + "/m²", p.PrijsPerM2, p.Omschrijving ?? "");
        var refs = await _db.BudgetPrijsReferentie.AsNoTracking().Where(p => !p.Gearchiveerd && (p.ProjectId == null || p.ProjectId == projectid)).OrderBy(p => p.Code).ThenBy(p => p.ProjectId.HasValue).ToListAsync();
        var vm = new VerkoopBerekenVm
        {
            ProjectId = projectid, ProjectName = index.ProjectName, Units = index.Rijen, Unit = unit, Instellingen = await Verkoop.GetInstellingenAsync(projectid),
            CodesGrond = refs.Where(p => p.PrijsType == "Grond").Select(Code).ToList(), CodesBouw = refs.Where(p => p.PrijsType == "Bouw").Select(Code).ToList(),
        };

        vm.Budget = await VerkoopBudgetInfoAsync(index.DefinitiefVersieId, unit.UnitId);
        SetVerkoopBreadcrumb(projectid, index.ProjectName, unit.Naam + " berekenen", "SalesCalculateV2");
        SetPageHeader("bx bx-building-house", $"{index.ProjectName} - {unit.Naam} berekenen");
        ViewData["HelpScreenId"] = "projecten-eenheid-berekenen";
        return View("SalesCalculateV2", vm);
    }

    /// <summary>Wat het definitieve budget (stap 8 en 9) over een eenheid weet; enkel als er een definitief budget is en de eenheid aan een verkooplijn gekoppeld is.</summary>
    private async Task<VerkoopBudgetInfo?> VerkoopBudgetInfoAsync(int? versieId, int unitId)
    {
        if (versieId is not int vid) return null;
        var lijn = await _db.BudgetVerkoopLijn.AsNoTracking().FirstOrDefaultAsync(l => l.BudgetVersieId == vid && l.UnitId == unitId);
        if (lijn == null) return null;
        var voorstel = await _verkoopVoorstelService.BerekenAsync(vid);
        var p = await _berekeningService.GetOrCreateParamsAsync(vid);
        var e = voorstel.Eenheden.FirstOrDefault(x => string.Equals((x.EenheidNaam ?? "").Trim(), (lijn.EenheidNaam ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
        if (e == null) return null;
        return new VerkoopBudgetInfo
        {
            VersieId = vid, EenheidNaam = lijn.EenheidNaam ?? "", OppGereduceerd = e.OppGereduceerd, Grondopp = e.Grondopp, Kostprijs = e.Kostprijs, Minimum = e.MinimumVerkoopprijs,
            MargePerc = voorstel.DoelMargePerc, Commissie = p.MakelaarsCommissiePerc ?? 0m, CodeGrond = lijn.CodeGrond, CodeBouw = lijn.CodeBouw,
        };
    }

    /// <summary>45d/45e — Prijsberekening (PDF): één pagina per eenheid, versie koper of intern. <paramref name="grond"/> en <paramref name="bouw"/> (enkel bij één eenheid) nemen de eigen bedragen van de Berekenen-pagina over.</summary>
    [HttpGet]
    public async Task<IActionResult> SalesCalculationPdfV2(int projectid, string? ids, bool intern = false, bool kostenKoper = true, bool detail = true, bool overzicht = true, string? grond = null, string? bouw = null)
    {
        var index = await Verkoop.GetIndexAsync(projectid);
        var gekozen = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0).ToHashSet();
        var rijen = gekozen.Count == 0 ? index.Rijen : index.Rijen.Where(r => gekozen.Contains(r.UnitId)).ToList();
        if (rijen.Count == 0) { AddMessage("error", "Er zijn geen eenheden om te berekenen.", "Fout"); return RedirectToAction("DetailSales", new { projectid }); }
        var inst = await Verkoop.GetInstellingenAsync(projectid);
        var eenheden = new List<CPMCore.Documents.GlV2.PrijsberekeningEenheid>();
        foreach (var r in rijen)
        {
            var g = r.Grond; var b = r.Bouw;
            if (rijen.Count == 1 && grond != null) g = VerkoopService.ParseBedrag(grond);
            if (rijen.Count == 1 && bouw != null) b = VerkoopService.ParseBedrag(bouw);
            var koper = (g == r.Grond && b == r.Bouw) ? r.Koper : VerkoopService.Bereken(g, b, r.IsAppartement, inst);
            var budget = intern ? await VerkoopBudgetInfoAsync(index.DefinitiefVersieId, r.UnitId) : null;
            eenheden.Add(new CPMCore.Documents.GlV2.PrijsberekeningEenheid(r, koper, budget));
        }
        var company = await HttpContext.RequestServices.GetRequiredService<CPMCore.Services.Signing.ChangeOrderPdfBuilder>().BuildGlV2CompanyForProjectAsync(projectid);
        var fonts = CPMCore.Documents.GlV2.GlV2PdfFonts.EnsureRegistered(HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(), HttpContext.RequestServices.GetRequiredService<ILogger<ProjectenController>>());
        var pdf = new CPMCore.Documents.GlV2.PrijsberekeningDocumentV2(index.ProjectName, eenheden, new CPMCore.Documents.GlV2.PrijsberekeningOpties(intern, kostenKoper, detail, overzicht), company, fonts).GeneratePdf();
        var naam = rijen.Count == 1 ? rijen[0].Naam : "eenheden";
        var proj = string.Concat(index.ProjectName.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')).Trim().Replace(' ', '_');
        return File(pdf, "application/pdf", $"Prijsberekening_{proj}_{naam.Replace(' ', '_')}{(intern ? "_intern" : "")}_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
