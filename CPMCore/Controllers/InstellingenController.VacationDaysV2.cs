using BOCore;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen/Verlofdagen (design-handoff punt 50, "CRM Instellingen.dc.html"). De pagina zelf staat in Vacationdays()
// (InstellingenController.Algemeen.cs, UseGlV2Layout-ternary); hier de acties van het nieuwe scherm. Een algemene verlofdag is
// bouwverlof (Kind 0) of een feestdag (Kind 1, migratie 116). Enkel de algemene dagen (ProjectId leeg) zijn hier te beheren; de
// verlofdagen van een project (weerverlet) blijven buiten beeld. Elke actie geeft de volledige lijst terug zodat de kalender
// zich zonder eigen bijhouden van id's kan herschikken.
public partial class InstellingenController
{
    private object[] AlgemeneVerlofdagenJson()
    {
        var response = _projectService.GetVacationDaysGeneral();
        return response.Success
            ? response.Values.OrderBy(b => b.VacationDay).Select(b => (object)new
            {
                id = b.Id, year = b.VacationDay.Year, month = b.VacationDay.Month, day = b.VacationDay.Day, kind = (int)b.Kind
            }).ToArray()
            : Array.Empty<object>();
    }

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsHolidays)]
    public IActionResult VerlofdagenV2Dagen() => Json(AlgemeneVerlofdagenJson());

    // Zet een of meer dagen op bouwverlof (kind 0) of feestdag (kind 1). Een dag die al in de andere modus staat, wordt omgezet;
    // een dag die al in deze modus staat, blijft staan (een klik om te wissen is VerlofdagWisV2). Weekends worden overgeslagen.
    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsHolidays)]
    public IActionResult VerlofdagenZetV2(string[] dagen, byte kind)
    {
        if (kind > 1) return Json(new { ok = false, bericht = "Onbekend soort verlofdag." });
        var teZetten = new List<DateOnly>();
        foreach (var d in dagen ?? Array.Empty<string>())
            if (DateOnly.TryParseExact(d, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var datum)
                && datum.DayOfWeek != DayOfWeek.Saturday && datum.DayOfWeek != DayOfWeek.Sunday)
                teZetten.Add(datum);
        if (teZetten.Count == 0) return Json(new { ok = false, bericht = "Geen geldige dagen." });
        var fout = ZetAlgemeneVerlofdagen(teZetten, kind);
        return Json(new { ok = fout == null, bericht = fout, dagen = AlgemeneVerlofdagenJson() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsHolidays)]
    public IActionResult VerlofdagWisV2(int id)
    {
        var algemeen = _projectService.GetVacationDaysGeneral();
        if (!algemeen.Success || !algemeen.Values.Any(v => v.Id == id))
            return Json(new { ok = false, bericht = "Deze dag bestaat niet meer. Vernieuw de pagina.", dagen = AlgemeneVerlofdagenJson() });
        var response = _projectService.DeleteVacationDays(new List<int> { id });
        return Json(new { ok = response.Success, dagen = AlgemeneVerlofdagenJson() });
    }

    // Vult de 10 wettelijke feestdagen van een jaar in (ook als ze in het weekend vallen: dan staan ze er als stip op). Een dag die al
    // bouwverlof was, wordt feestdag. De rest van het jaar blijft ongewijzigd.
    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsHolidays)]
    public IActionResult FeestdagenInvullenV2(int jaar)
    {
        if (jaar < 2000 || jaar > 2100) return Json(new { ok = false, bericht = "Geef een jaar tussen 2000 en 2100." });
        var fout = ZetAlgemeneVerlofdagen(WettelijkeFeestdagen(jaar), 1);
        return Json(new { ok = fout == null, bericht = fout, aantal = WettelijkeFeestdagen(jaar).Count, dagen = AlgemeneVerlofdagenJson() });
    }

    private string? ZetAlgemeneVerlofdagen(IEnumerable<DateOnly> dagen, byte kind)
    {
        var bestaand = _projectService.GetVacationDaysGeneral();
        if (!bestaand.Success) return "De verlofdagen konden niet geladen worden.";
        var perDatum = bestaand.Values.ToDictionary(v => v.VacationDay, v => v);
        foreach (var datum in dagen.Distinct())
        {
            VacationDayBO bo;
            if (perDatum.TryGetValue(datum, out var huidig))
            {
                if (huidig.Kind == kind) continue;
                huidig.Kind = kind;
                bo = huidig;
            }
            else bo = new VacationDayBO { VacationDay = datum, Kind = kind };
            var r = _projectService.InsertUpdateVacationDay(bo);
            if (!r.Success) return "Opslaan is niet gelukt.";
        }
        return null;
    }

    /// <summary>De 10 wettelijke feestdagen in België: Nieuwjaar, Paasmaandag, Dag van de Arbeid, O.L.H. Hemelvaart, Pinkstermaandag,
    /// Nationale feestdag, O.L.V. Hemelvaart, Allerheiligen, Wapenstilstand en Kerstmis.</summary>
    internal static List<DateOnly> WettelijkeFeestdagen(int jaar)
    {
        // Pasen (Gregoriaans, Meeus/Jones/Butcher)
        int a = jaar % 19, b = jaar / 100, c = jaar % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7, m = (a + 11 * h + 22 * l) / 451;
        int maand = (h + l - 7 * m + 114) / 31, dag = ((h + l - 7 * m + 114) % 31) + 1;
        var pasen = new DateOnly(jaar, maand, dag);
        return new List<DateOnly>
        {
            new(jaar, 1, 1), pasen.AddDays(1), new(jaar, 5, 1), pasen.AddDays(39), pasen.AddDays(50),
            new(jaar, 7, 21), new(jaar, 8, 15), new(jaar, 11, 1), new(jaar, 11, 11), new(jaar, 12, 25)
        };
    }
}
