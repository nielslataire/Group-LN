using System.Globalization;
using System.Text.Json;
using BOCore;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

// gl-v2 — Instellingen/Kostprijzen materialen (design-handoff punt 52a/52b/52c, "CRM Instellingen.dc.html"): materialen, bouwkost % en formule koppelingen.
// De pagina zelf staat in KostprijsMaterialen() (InstellingenController.Budget.cs, UseGlV2Layout-ternary); hier de JSON-acties van het nieuwe scherm. Elke
// actie geeft de volledige stand terug zodat de pagina zich zonder herladen bijwerkt. Alle schrijfacties vragen schrijfrecht op Settings.KostprijsMaterialen.
public partial class InstellingenController
{
    /// <summary>De gl-v2-schermen tekenen zichzelf in JS uit één JSON-blok: het model van de view is die JSON als tekst.</summary>
    private ViewResult ViewJson(string view, object data) =>
        View(view, JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    private static bool TryParseBedrag(string? raw, out decimal waarde)
    {
        waarde = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim().Replace(" ", "").Replace(" ", "");
        if (s.Contains(',')) s = s.Replace(".", "").Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out waarde) && waarde >= 0;
    }

    /// <summary>Alle gegevens van de drie tabs. "gekoppeld" = het materiaal hangt aan een formule-slot (dan kan het niet verwijderd worden).</summary>
    private object KostprijsStand()
    {
        var materialen = _kostprijsService.GetMaterialen().Values ?? new();
        var koppelingen = _kostprijsService.GetFormulaKoppelingen().Values ?? new();
        var gekoppeld = koppelingen.Where(k => k.MateriaalId.HasValue).Select(k => k.MateriaalId!.Value).ToHashSet();
        return new
        {
            indexTypes = (_kostprijsService.GetIndexTypes().Values ?? new()).Select(i => new { id = i.Id, code = i.Code, naam = i.Naam }),
            groepen = (_kostprijsService.GetActivityGroepen().Values ?? new()).Select(g => new { id = g.ID, lot = g.Lot, naam = g.Name }),
            materialen = materialen.Select(m => new
            {
                id = m.Id, naam = m.Naam, eenheid = m.Eenheid, toepassing = m.Toepassingsgebied ?? "", prijs = m.ReferentiePrijs,
                datum = m.ReferentieDatum.ToString("yyyy-MM-dd"), categorieId = m.CategorieId, indexTypeId = m.KmIndexTypeId,
                indexCode = m.IndexTypeCode, volgorde = m.Volgorde, gekoppeld = gekoppeld.Contains(m.Id)
            }),
            pctGroepen = (_kostprijsService.GetPercentageGroepen().Values ?? new()).Select(g => new { id = g.Id, naam = g.Naam, volgorde = g.Volgorde }),
            percentages = (_kostprijsService.GetPercentages().Values ?? new()).Select(p => new
            {
                id = p.Id, groepId = p.GroepId, naam = p.Naam, percentage = p.Percentage, volgorde = p.Volgorde, systeem = p.IsSysteem, sleutel = p.Sleutel
            }),
            koppelingen = koppelingen.Select(k => new { id = k.Id, sleutel = k.Sleutel, omschrijving = k.Omschrijving, materiaalId = k.MateriaalId })
        };
    }

    private IActionResult KostprijsAntwoord(bool ok, string? bericht = null) => Json(new { ok, bericht, stand = KostprijsStand() });
    private static IActionResult KostprijsFout(string bericht) => new JsonResult(new { ok = false, bericht });
    private static string? EersteFout(Response r) => r.Messages.FirstOrDefault(m => m.Type == MessageType.Error)?.Message;

    // ── Materialen (52a) ──────────────────────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult KostprijsMateriaalOpslaanV2(int id, int categorieId, string? naam, string? eenheid, string? toepassing, int indexTypeId, string? prijs, string? datum)
    {
        naam = naam?.Trim() ?? "";
        eenheid = eenheid?.Trim() ?? "";
        if (naam.Length == 0) return KostprijsFout("Geef een naam in.");
        if (naam.Length > 200) return KostprijsFout("De naam mag maximaal 200 tekens hebben.");
        if (eenheid.Length == 0 || eenheid.Length > 20) return KostprijsFout("Kies een eenheid.");
        if (!TryParseBedrag(prijs, out var p)) return KostprijsFout("Geef een geldige referentieprijs in (0 of meer).");
        if (!DateTime.TryParseExact(datum, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return KostprijsFout("Geef een geldige referentiedatum in.");
        if (!(_kostprijsService.GetActivityGroepen().Values ?? new()).Any(g => g.ID == categorieId)) return KostprijsFout("Kies een activiteitgroep.");
        if (!(_kostprijsService.GetIndexTypes().Values ?? new()).Any(i => i.Id == indexTypeId)) return KostprijsFout("Kies een indextype.");
        var toep = toepassing is "Woning" or "Appartement" or "Gemeenschappelijk" ? toepassing : "";

        var alle = _kostprijsService.GetMaterialen().Values ?? new();
        var bestaand = id > 0 ? alle.FirstOrDefault(m => m.Id == id) : null;
        if (id > 0 && bestaand == null) return KostprijsFout("Dit materiaal bestaat niet meer. Vernieuw de pagina.");
        // Volgorde: een bestaand materiaal houdt de zijne; een nieuw komt achteraan in zijn groep (de pagina sorteert op naam).
        var volgorde = bestaand?.Volgorde ?? (alle.Where(m => m.CategorieId == categorieId).Select(m => m.Volgorde).DefaultIfEmpty(0).Max() + 1);

        var r = _kostprijsService.InsertUpdateMateriaal(new KostprijsMateriaalBO
        {
            Id = id, CategorieId = categorieId, Naam = naam, Eenheid = eenheid, Toepassingsgebied = toep, KmIndexTypeId = indexTypeId,
            ReferentiePrijs = p, ReferentieDatum = d, Volgorde = volgorde
        });
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Opslaan is niet gelukt.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult KostprijsMateriaalVerwijderV2(int id)
    {
        var r = _kostprijsService.DeleteMateriaal(id);
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Verwijderen is niet gelukt.");
    }

    // ── Bouwkost % (52b) ──────────────────────────────────────────────────────────────────────────

    private sealed record PctWijziging(int Id, string? Percentage);

    /// <summary>Eén Opslaan voor alle percentages van de pagina: vaste parameters en bijkomende percentages. Enkel het percentage verandert; naam, groep en
    /// volgorde van een rij blijven zoals ze waren.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BouwkostOpslaanV2(string? wijzigingen)
    {
        List<PctWijziging> lijst;
        try { lijst = JsonSerializer.Deserialize<List<PctWijziging>>(wijzigingen ?? "[]", new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new(); }
        catch (JsonException) { return KostprijsFout("De wijzigingen konden niet gelezen worden."); }
        var huidig = (_kostprijsService.GetPercentages().Values ?? new()).ToDictionary(p => p.Id);
        var teSchrijven = new List<BouwkostPercentageBO>();
        foreach (var w in lijst)
        {
            if (!huidig.TryGetValue(w.Id, out var bo)) return KostprijsFout("Een percentage bestaat niet meer. Vernieuw de pagina.");
            if (!TryParseBedrag(w.Percentage, out var pct) || pct > 100) return KostprijsFout($"“{bo.Naam}”: geef een percentage tussen 0 en 100 in.");
            bo.Percentage = Math.Round(pct, 2);
            teSchrijven.Add(bo);
        }
        foreach (var bo in teSchrijven)
        {
            var r = _kostprijsService.InsertUpdatePercentage(bo);
            if (!r.Success) return KostprijsAntwoord(false, EersteFout(r) ?? "Opslaan is niet gelukt.");
        }
        return KostprijsAntwoord(true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BouwkostGroepOpslaanV2(int id, string? naam)
    {
        naam = naam?.Trim() ?? "";
        if (naam.Length == 0) return KostprijsFout("Geef een naam in voor de groep.");
        if (naam.Length > 100) return KostprijsFout("De naam mag maximaal 100 tekens hebben.");
        var groepen = _kostprijsService.GetPercentageGroepen().Values ?? new();
        if (groepen.Any(g => g.Id != id && string.Equals(g.Naam, naam, StringComparison.OrdinalIgnoreCase))) return KostprijsFout("Er bestaat al een groep met deze naam.");
        var bestaand = id > 0 ? groepen.FirstOrDefault(g => g.Id == id) : null;
        if (id > 0 && bestaand == null) return KostprijsFout("Deze groep bestaat niet meer. Vernieuw de pagina.");
        var r = _kostprijsService.InsertUpdatePercentageGroep(new BouwkostPercentageGroepBO { Id = id, Naam = naam, Volgorde = bestaand?.Volgorde ?? (groepen.Select(g => g.Volgorde).DefaultIfEmpty(0).Max() + 1) });
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Opslaan is niet gelukt.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BouwkostGroepVerwijderV2(int id)
    {
        if ((_kostprijsService.GetPercentages().Values ?? new()).Any(p => p.GroepId == id))
            return KostprijsAntwoord(false, "Een groep met percentages verwijder je pas als ze leeg is.");
        var r = _kostprijsService.DeletePercentageGroep(id);
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Verwijderen is niet gelukt.");
    }

    /// <summary>De volgorde van de groepen na slepen: de lijst id's in de nieuwe volgorde.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BouwkostGroepenVolgordeV2(int[]? ids)
    {
        var groepen = (_kostprijsService.GetPercentageGroepen().Values ?? new()).ToDictionary(g => g.Id);
        var volgorde = 1;
        foreach (var id in (ids ?? Array.Empty<int>()).Distinct())
        {
            if (!groepen.TryGetValue(id, out var g)) continue;
            g.Volgorde = volgorde++;
            var r = _kostprijsService.InsertUpdatePercentageGroep(g);
            if (!r.Success) return KostprijsAntwoord(false, EersteFout(r) ?? "Opslaan is niet gelukt.");
        }
        return KostprijsAntwoord(true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BouwkostItemOpslaanV2(int id, int groepId, string? naam, string? percentage)
    {
        naam = naam?.Trim() ?? "";
        if (naam.Length == 0) return KostprijsFout("Geef een naam in voor het percentage.");
        if (naam.Length > 200) return KostprijsFout("De naam mag maximaal 200 tekens hebben.");
        if (!TryParseBedrag(percentage, out var pct) || pct > 100) return KostprijsFout("Geef een percentage tussen 0 en 100 in.");
        if (!(_kostprijsService.GetPercentageGroepen().Values ?? new()).Any(g => g.Id == groepId)) return KostprijsFout("Kies een groep.");
        var alle = _kostprijsService.GetPercentages().Values ?? new();
        var bestaand = id > 0 ? alle.FirstOrDefault(p => p.Id == id) : null;
        if (id > 0 && bestaand == null) return KostprijsFout("Dit percentage bestaat niet meer. Vernieuw de pagina.");
        if (bestaand?.IsSysteem == true) return KostprijsFout("Dit is een vaste parameter: enkel het percentage kan gewijzigd worden.");
        var r = _kostprijsService.InsertUpdatePercentage(new BouwkostPercentageBO
        {
            Id = id, GroepId = groepId, Naam = naam, Percentage = Math.Round(pct, 2),
            Volgorde = bestaand != null && bestaand.GroepId == groepId ? bestaand.Volgorde : alle.Where(p => p.GroepId == groepId).Select(p => p.Volgorde).DefaultIfEmpty(0).Max() + 1
        });
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Opslaan is niet gelukt.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult BouwkostItemVerwijderV2(int id)
    {
        var r = _kostprijsService.DeletePercentage(id);
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Verwijderen is niet gelukt.");
    }

    // ── Formule koppelingen (52c) ─────────────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult KoppelingOpslaanV2(string? sleutel, int? materiaalId)
    {
        if (string.IsNullOrWhiteSpace(sleutel)) return KostprijsFout("Onbekend formule-slot.");
        if (materiaalId.HasValue && !(_kostprijsService.GetMaterialen().Values ?? new()).Any(m => m.Id == materiaalId.Value)) return KostprijsFout("Dit materiaal bestaat niet meer.");
        var r = _kostprijsService.SaveFormulaKoppeling(sleutel, materiaalId);
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Opslaan is niet gelukt.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult KoppelingAanmakenV2(string? naam, int? materiaalId)
    {
        if (string.IsNullOrWhiteSpace(naam)) return KostprijsFout("Geef een omschrijving in voor het nieuwe slot.");
        if (naam.Trim().Length > 200) return KostprijsFout("De omschrijving mag maximaal 200 tekens hebben.");
        var r = _kostprijsService.CreateFormulaKoppeling(naam, materiaalId);
        return Json(new { ok = r.Success, bericht = r.Success ? null : EersteFout(r) ?? "Aanmaken is niet gelukt.", sleutel = r.Values?.FirstOrDefault()?.Sleutel, stand = KostprijsStand() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsKostprijsMaterialen)]
    public IActionResult KoppelingVerwijderV2(int id)
    {
        var r = _kostprijsService.DeleteFormulaKoppeling(id);
        return KostprijsAntwoord(r.Success, r.Success ? null : EersteFout(r) ?? "Verwijderen is niet gelukt.");
    }
}
