using BOCore;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen/Budgetformules (design-handoff punt 53a, "CRM Instellingen.dc.html"). De pagina zelf staat in BudgetFormules()
// (InstellingenController.Budget.cs, UseGlV2Layout-ternary). Opslaan en verwijderen krijgen hier eigen acties met schrijfrecht en antiforgery;
// testen (BudgetFormuleTest) en de parameterlijst (GetBudgetFormuleParameters) zijn alleen-lezen en blijven die van het oude scherm.
public partial class InstellingenController
{
    private static object FormuleJson(ServiceCore.Budget.BudgetActivityFormuleInfo f) => new
    {
        id = f.Id, activityId = f.ActivityId, activityOmschrijving = f.ActivityOmschrijving, lotNaam = f.LotNaam, lotNummer = f.LotNummer,
        formule = f.Formule, omschrijving = f.Omschrijving, actief = f.Actief,
        laatstGewijzigd = f.LaatstGewijzigd.ToLocalTime().ToString("dd/MM/yyyy 'om' HH:mm")
    };

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsCalculatie)]
    public async Task<IActionResult> BudgetFormuleOpslaanV2(int activityId, string? formule, string? omschrijving, bool actief)
    {
        if (activityId <= 0) return Json(new { ok = false, bericht = "Kies eerst een activiteit." });
        if (string.IsNullOrWhiteSpace(formule)) return Json(new { ok = false, bericht = "De formule is leeg." });
        if ((omschrijving ?? "").Length > 300) return Json(new { ok = false, bericht = "Het label mag maximaal 300 tekens hebben." });
        var r = await _budgetFormules.SaveAsync(activityId, formule, omschrijving, actief);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, bericht = r.Success ? null : fout ?? "Opslaan is niet gelukt.", formule = r.Value == null ? null : FormuleJson(r.Value) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsCalculatie)]
    public async Task<IActionResult> BudgetFormuleVerwijderV2(int id)
    {
        var r = await _budgetFormules.DeleteAsync(id);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, bericht = r.Success ? null : fout ?? "Verwijderen is niet gelukt." });
    }
}
