using System.Globalization;
using CPMCore.Services.Verkoop;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // Budget: enkel als er een definitief budget is en deze eenheid aan een verkooplijn gekoppeld is.
        if (index.DefinitiefVersieId is int versieId)
        {
            var lijn = await _db.BudgetVerkoopLijn.AsNoTracking().FirstOrDefaultAsync(l => l.BudgetVersieId == versieId && l.UnitId == unit.UnitId);
            if (lijn != null)
            {
                var voorstel = await _verkoopVoorstelService.BerekenAsync(versieId);
                var p = await _berekeningService.GetOrCreateParamsAsync(versieId);
                var e = voorstel.Eenheden.FirstOrDefault(x => string.Equals((x.EenheidNaam ?? "").Trim(), (lijn.EenheidNaam ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
                if (e != null)
                    vm.Budget = new VerkoopBudgetInfo
                    {
                        VersieId = versieId, EenheidNaam = lijn.EenheidNaam ?? "", OppGereduceerd = e.OppGereduceerd, Grondopp = e.Grondopp, Kostprijs = e.Kostprijs, Minimum = e.MinimumVerkoopprijs,
                        MargePerc = voorstel.DoelMargePerc, Commissie = p.MakelaarsCommissiePerc ?? 0m, CodeGrond = lijn.CodeGrond, CodeBouw = lijn.CodeBouw,
                    };
            }
        }
        SetVerkoopBreadcrumb(projectid, index.ProjectName, unit.Naam + " berekenen", "SalesCalculateV2");
        SetPageHeader("bx bx-building-house", $"{index.ProjectName} - {unit.Naam} berekenen");
        ViewData["HelpScreenId"] = "projecten-eenheid-berekenen";
        return View("SalesCalculateV2", vm);
    }
}
