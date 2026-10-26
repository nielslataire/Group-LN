using CPMCore.Attributes;
using CPMCore.Models.Instellingen;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;
using System.Linq;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 55c/55d/55e): enkel extra weergave-acties; de bestaande acties (Bewerken, Verwijderen, Taak/Vereiste/Voordeel/Stap…, Sollicitatie…) blijven ongewijzigd
// en worden door de V2-views gebruikt. ?classic=true houdt de klassieke pagina's bereikbaar.
public partial class VacatureBeheerController
{
    private bool IsV2 => ViewData["UseGlV2Layout"] as bool? == true;

    // Dashboard › Instellingen › Vacatures (› leaf)
    private MvcBreadcrumbNode VacaturesNode()
    {
        var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        return new MvcBreadcrumbNode("Index", "VacatureBeheer", "Vacatures") { Parent = instellingen };
    }

    private IActionResult IndexV2()
    {
        ViewData["BreadcrumbNode"] = VacaturesNode();
        var vacatures = _vacatureService.GetVacatures(alleenGepubliceerd: false).Values ?? new();
        var sol = _sollicitatieService.GetSollicitaties(null).Values ?? new();
        var vm = new VacatureListV2Vm { Vacatures = vacatures };
        foreach (var g in sol.Where(s => s.VacatureId.HasValue).GroupBy(s => s.VacatureId!.Value))
            vm.Sollicitaties[g.Key] = (g.Count(), g.Count(s => !s.IsGelezen));
        vm.TotaalNieuw = sol.Count(s => !s.IsGelezen);
        return View("IndexV2", vm);
    }

    // Na een mislukte POST zijn de puntenlijsten niet mee gepost: voor een bestaande vacature opnieuw laden, zodat ze niet verdwijnen.
    private VacatureEditVM HervulLijsten(VacatureEditVM vm)
    {
        if (vm.ID == 0) return vm;
        var bo = _vacatureService.GetVacatureById(vm.ID).Value;
        if (bo == null) return vm;
        vm.TaakItems = bo.TaakItems;
        vm.VereisteItems = bo.VereisteItems;
        vm.VoordeelItems = bo.VoordeelItems;
        vm.SollicitatieStapItems = bo.SollicitatieStapItems;
        return vm;
    }

    private IActionResult BewerkenV2(VacatureEditVM edit)
    {
        var vacatures = VacaturesNode();
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Bewerken", "VacatureBeheer", edit.ID == 0 ? "Nieuwe vacature" : "Bewerken") { Parent = vacatures };
        var vm = new VacatureEditV2Vm { Edit = edit, PreviewUrl = ViewBag.PreviewUrl as string };
        if (edit.ID != 0)
            vm.AantalSollicitaties = (_sollicitatieService.GetSollicitaties(edit.ID).Values ?? new()).Count;
        return View("BewerkenV2", vm);
    }

    private IActionResult SollicitatiesV2(VacatureSollicitatieListVM vm)
    {
        var vacatures = VacaturesNode();
        if (vm.VacatureId.HasValue)
        {
            var vacature = new MvcBreadcrumbNode("Bewerken", "VacatureBeheer", string.IsNullOrWhiteSpace(vm.VacatureTitel) ? "Vacature" : vm.VacatureTitel) { Parent = vacatures };
            vacature.RouteValues = new { id = vm.VacatureId.Value };
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Sollicitaties", "VacatureBeheer", "Sollicitaties") { Parent = vacature };
        }
        else
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode("Sollicitaties", "VacatureBeheer", "Sollicitaties") { Parent = vacatures };
        return View("SollicitatiesV2", vm);
    }

    // Een sollicitatie openen in het zijpaneel markeert ze als gelezen (zoals het downloaden van het cv dat al deed).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SollicitatieGelezenV2(int id, bool gelezen)
    {
        var response = _sollicitatieService.MarkeerGelezen(id, gelezen);
        return Json(new { ok = !response.HasErrors });
    }
}
