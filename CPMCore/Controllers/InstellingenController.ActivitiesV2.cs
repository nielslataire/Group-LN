using BOCore;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen/Activiteiten en Activiteitgroepen (design-handoff punt 51a/51b, "CRM Instellingen.dc.html"). De pagina zelf staat in
// Activities() (InstellingenController.Algemeen.cs, UseGlV2Layout-ternary); hier de JSON-acties van het nieuwe scherm. Elke actie geeft de
// volledige stand terug (groepen met aantal + alle activiteiten) zodat de pagina zich zonder herladen kan bijwerken. Het lot van een groep
// is haar sleutel (uniek); het lotnummer van een activiteit is lot + eigen volgnummer binnen de groep ("5.11", migratie 117).
public partial class InstellingenController
{
    private object ActiviteitenStand()
    {
        var groepen = _activityService.GetActivityGroups();
        var activiteiten = _activityService.GetActivities();
        var acts = activiteiten.Success ? activiteiten.Values.ToList() : new List<ActivityBO>();
        return new
        {
            groepen = (groepen.Success ? groepen.Values : new List<ActivityGroupBO>())
                .OrderBy(g => g.Lot).ThenBy(g => g.Name)
                .Select(g => new { id = g.ID, lot = g.Lot, naam = g.Name, aantal = acts.Count(a => a.Group != null && a.Group.ID == g.ID) }).ToArray(),
            activiteiten = acts.Select(a => new { id = a.ID, naam = a.Name, groepId = a.Group?.ID ?? 0, volgnr = a.Volgnr }).ToArray()
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsActivities)]
    public IActionResult ActiviteitOpslaanV2(int id, string? naam, int groepId, int? volgnr)
    {
        naam = naam?.Trim() ?? "";
        if (naam.Length == 0) return Json(new { ok = false, bericht = "Geef een naam in." });
        if (naam.Length > 250) return Json(new { ok = false, bericht = "De naam mag maximaal 250 tekens hebben." });
        var alleGroepen = _activityService.GetActivityGroups();
        var groep = alleGroepen.Success ? alleGroepen.Values.FirstOrDefault(g => g.ID == groepId) : null;
        if (groep == null) return Json(new { ok = false, bericht = "Kies een activiteitgroep." });
        if (volgnr is <= 0) return Json(new { ok = false, bericht = "Het nummer moet groter zijn dan 0." });

        var bestaand = _activityService.GetActivities();
        if (bestaand.Success && volgnr.HasValue && bestaand.Values.Any(a => a.ID != id && a.Group?.ID == groepId && a.Volgnr == volgnr))
            return Json(new { ok = false, bericht = $"Nummer {groep.Lot}.{volgnr} is al in gebruik in deze groep." });

        var bo = new ActivityBO { ID = id, Name = naam, Volgnr = volgnr, Group = new ActivityGroupBO { ID = groepId } };
        var response = _activityService.InsertUpdate(bo);
        return Json(new { ok = response.Success, bericht = response.Success ? null : "Opslaan is niet gelukt.", stand = ActiviteitenStand() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsActivities)]
    public IActionResult ActiviteitVerwijderV2(int id)
    {
        try
        {
            var response = _activityService.Delete(new List<int> { id });
            if (response.Success) return Json(new { ok = true, stand = ActiviteitenStand() });
        }
        catch (Exception) { /* verwijzingen vanuit budgetten, contracten of facturen: zie de melding hieronder */ }
        return Json(new { ok = false, bericht = "Deze activiteit wordt gebruikt in een budget, contract of factuur en kan niet verwijderd worden.", stand = ActiviteitenStand() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsActivities)]
    public IActionResult ActiviteitgroepOpslaanV2(int id, string? naam, int lot)
    {
        naam = naam?.Trim() ?? "";
        if (naam.Length == 0) return Json(new { ok = false, bericht = "Geef een naam in." });
        if (naam.Length > 100) return Json(new { ok = false, bericht = "De naam mag maximaal 100 tekens hebben." });
        if (lot <= 0 || lot > 999) return Json(new { ok = false, bericht = "Geef een lot tussen 1 en 999." });
        var groepen = _activityService.GetActivityGroups();
        if (groepen.Success && groepen.Values.Any(g => g.ID != id && g.Lot == lot))
            return Json(new { ok = false, bericht = $"Lot {lot} bestaat al. Kies een ander lot." });

        var response = _activityService.InsertUpdateGroup(new ActivityGroupBO { ID = id, Name = naam, Lot = lot });
        return Json(new { ok = response.Success, bericht = response.Success ? null : "Opslaan is niet gelukt.", stand = ActiviteitenStand() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsActivities)]
    public IActionResult ActiviteitgroepVerwijderV2(int id)
    {
        var activiteiten = _activityService.GetActivities();
        if (activiteiten.Success && activiteiten.Values.Any(a => a.Group?.ID == id))
            return Json(new { ok = false, bericht = "Deze groep bevat nog activiteiten. Verplaats ze eerst naar een andere groep.", stand = ActiviteitenStand() });
        try
        {
            var response = _activityService.DeleteGroup(new List<int> { id });
            if (response.Success) return Json(new { ok = true, stand = ActiviteitenStand() });
        }
        catch (Exception) { /* de groep hangt aan een planning of ander record */ }
        return Json(new { ok = false, bericht = "Deze groep wordt nog gebruikt en kan niet verwijderd worden.", stand = ActiviteitenStand() });
    }
}
