using BOCore;
using CPMCore.Models.Instellingen;
using DALCore.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen/Bouwindexen (design-handoff punt 48a-d, "CRM Instellingen.dc.html"). De pagina zelf staat in
// Bouwindexen() (InstellingenController.Budget.cs, UseGlV2Layout-ternary); hier enkel de schrijfacties van het nieuwe scherm,
// die anders dan de oude acties een schrijfrecht eisen, dubbele waarden weigeren en de geldende waarde bijhouden.
public partial class InstellingenController
{
    private static readonly string[] BouwIndexTypes = { "S", "I2021", "I+", "ABEX" };

    private static bool TryParseIndexWaarde(string? raw, out decimal waarde)
    {
        waarde = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim().Replace(" ", "").Replace(" ", "");
        // nl-BE: "1.071,00" of "38,8970"; ook "38.897" (punt als decimaal) accepteren.
        if (s.Contains(',')) s = s.Replace(".", "").Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out waarde) && waarde > 0;
    }

    /// <summary>Nieuwe of aangepaste indexwaarde. Eén waarde per (type, jaar, maand) — bij S ook per categorie; ABEX heeft geen maand.
    /// Een bestaande waarde behoudt haar bron, jaar/maand en geldend-markering; een nieuwe krijgt bron "Manueel".
    /// ABEX: de waarde van het meest recente jaar wordt automatisch de geldende.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsBouwIndexen)]
    public async Task<IActionResult> BouwindexOpslaanV2(int id, string indexType, int? jaar, int? maand, string? waarde, string? opmerking)
    {
        if (!BouwIndexTypes.Contains(indexType)) return Json(new { ok = false, bericht = "Onbekend type index." });
        if (!TryParseIndexWaarde(waarde, out var w)) return Json(new { ok = false, bericht = "Geef een geldige waarde in, groter dan 0." });

        var bestaande = await _bouwIndex.GetAlleIndexenAsync(indexType);
        var huidig = id > 0 ? bestaande.FirstOrDefault(x => x.Id == id) : null;
        if (id > 0 && huidig == null) return Json(new { ok = false, bericht = "Deze waarde bestaat niet meer. Vernieuw de pagina." });

        var j = huidig?.Jaar ?? jaar;
        var m = indexType == "ABEX" ? null : (huidig?.Maand ?? maand);
        if (j is null or < 1990 or > 2100) return Json(new { ok = false, bericht = "Geef een jaar in tussen 1990 en 2100." });
        if (indexType != "ABEX" && m is null or < 1 or > 12) return Json(new { ok = false, bericht = "Kies een maand." });

        if (huidig == null && bestaande.Any(x => x.Jaar == j && x.Maand == m))
            return Json(new { ok = false, bericht = indexType == "ABEX" ? $"Er is al een ABEX voor {j}." : $"Er is al een waarde voor {m:00}/{j}." });

        var rij = huidig ?? new BouwIndex
        {
            IndexType = indexType,
            Jaar = j,
            Maand = m,
            IsActief = false,
            Bron = "Manueel",
            Categorie = indexType == "S" ? "Categorie A" : null,
            SubCategorie = indexType == "S" ? "2B" : null,
            GeldigVanaf = m.HasValue ? new DateTime(j.Value, m.Value, 1) : null
        };
        rij.IndexWaarde = Math.Round(w, 4);
        rij.Opmerking = string.IsNullOrWhiteSpace(opmerking) ? null : opmerking.Trim();
        await _bouwIndex.OpslaanAsync(rij);

        if (indexType == "ABEX" && rij.Id > 0)
        {
            var maxJaar = Math.Max(bestaande.Where(x => x.Id != rij.Id).Select(x => x.Jaar ?? 0).DefaultIfEmpty(0).Max(), j.Value);
            if (j.Value == maxJaar) await _bouwIndex.SetActiefAsync("ABEX", rij.Id);
        }
        return Json(new { ok = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsBouwIndexen)]
    public async Task<IActionResult> BouwindexVerwijderV2(int id)
        => Json(new { ok = await _bouwIndex.VerwijderenAsync(id) });

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsBouwIndexen)]
    public async Task<IActionResult> BouwindexGeldendV2(string indexType, int id)
    {
        if (!BouwIndexTypes.Contains(indexType)) return Json(new { ok = false });
        await _bouwIndex.SetActiefAsync(indexType, id);
        return Json(new { ok = true });
    }
}
