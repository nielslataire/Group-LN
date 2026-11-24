using DALCore.Models;

namespace ServiceCore.Planning;

/// <summary>De variabelen waarmee een sjabloon op één bouwdeel wordt uitgerold.</summary>
/// <summary>
/// <paramref name="Verdiepingen"/> = het aantal bouwlagen boven de grond, het gelijkvloers meegeteld (gelijkvloers + 2 verdiepingen = 3).
/// <paramref name="Terrassen"/> = de bouwlagen (1 = gelijkvloers, 2 = 1ste verdieping, ...) met terrassen of luifels bij de afdek; leeg of null = geen.
/// </summary>
public sealed record SjabloonVariabelen(int Verdiepingen = 1, bool Kelder = false, string? Dak = null, IReadOnlyCollection<int>? Terrassen = null);

/// <summary>Eén taak van een uitgerolde planning. <see cref="Sleutel"/> is uniek binnen de uitrol en gelijk voor "dezelfde" taak in een andere uitrol (estafette).</summary>
public sealed class UitgerolTaak
{
    public string Sleutel { get; init; } = "";
    public string Naam { get; init; } = "";
    public string? Beschrijving { get; init; }
    public int Duur { get; init; }
    public string? Code { get; init; }
    /// <summary>De bouwstaat die de taak oplevert en haar activiteit (migratie 158); worden op de planningtaak overgenomen.</summary>
    public int? BouwstaatId { get; init; }
    public int? ActivityId { get; init; }
    /// <summary>Naam van het tussenniveau (deelfase) in de fase, bv. "Gelijkvloers" of "1ste verdieping"; null = de taak staat rechtstreeks in de fase.</summary>
    public string? Subfase { get; init; }
    public bool Estafette { get; init; }
    public int EstafetteWacht { get; init; }
    public List<(string Sleutel, byte Type, int Wacht)> Voorgangers { get; } = new();
}

public sealed class UitgerolFase
{
    public string Naam { get; init; } = "";
    /// <summary>Code van het blok in het sjabloon; bij een blok dat per verdieping herhaald wordt is ze niet uniek en dus onbruikbaar om aan te koppelen.</summary>
    public string? Code { get; init; }
    public List<UitgerolTaak> Taken { get; } = new();
}

public sealed class SjabloonUitrolling
{
    public List<UitgerolFase> Fases { get; } = new();
    public List<string> Waarschuwingen { get; } = new();
}

/// <summary>
/// Zet een planningssjabloon om naar fases en taken voor één bouwdeel, volgens de variabelen: blokken met een voorwaarde (kelder, dak) vallen weg of
/// blijven, blokken per verdieping worden herhaald. Zuivere logica zonder database, zodat ze los te testen is.
/// </summary>
public static class PlanningSjabloonUitrol
{
    private sealed class Instantie
    {
        public required PlanningSjabloonBlok Blok { get; init; }
        public int Index { get; init; }          // 0 = niet herhaald, anders verdieping 1..n
        /// <summary>De taken van deze fase, elk met haar bouwlaag (0 = niet per bouwlaag).</summary>
        public required List<(PlanningSjabloonTaak T, int Laag)> Items { get; init; }
        public bool Bevat(PlanningSjabloonTaak t) => Items.Any(x => x.T == t);
        public string Sleutel(PlanningSjabloonTaak t, int laag) => Blok.Herhaling >= 1 ? $"{t.Id}#{laag}" : t.Id.ToString();
    }

    /// <summary>Herhaling van een blok: 0 = niet herhaald, 1 = een fase per bouwlaag, 2 = één fase met de taken per bouwlaag erin (bv. "Ruwbouw - Bovenbouw").</summary>
    public const byte PerBouwlaagEenFase = 2;

    public static bool VoorwaardeKlopt(PlanningSjabloonBlok b, SjabloonVariabelen v)
    {
        if (string.IsNullOrWhiteSpace(b.VoorwaardeVariabele)) return true;
        var waarde = (b.VoorwaardeWaarde ?? "").Trim().ToLowerInvariant();
        return b.VoorwaardeVariabele.Trim().ToLowerInvariant() switch
        {
            "kelder" => waarde is "ja" or "true" ? v.Kelder : (waarde is "nee" or "false" ? !v.Kelder : v.Kelder),
            "dak" => string.Equals(v.Dak ?? "", waarde, StringComparison.OrdinalIgnoreCase),
            "terras" => TerrasKlopt(b, v, null),
            _ => true,
        };
    }

    /// <summary>
    /// De voorwaarde van een taak: een lijst "variabele:waarde" gescheiden door komma's, alle voorwaarden moeten kloppen.
    /// kelder:ja|nee · dak:hellend|plat · terras:ja|nee (per bouwlaag) · laag:eerste|laatste|boven (eerste = het gelijkvloers, boven = niet het gelijkvloers).
    /// Zo kan één blok per bouwlaag taken bevatten die enkel op het gelijkvloers, enkel zonder kelder of enkel bij terrassen voorkomen.
    /// </summary>
    public static IEnumerable<(string Variabele, string Waarde)> TaakVoorwaarden(string? voorwaarde)
    {
        foreach (var deel in (voorwaarde ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var i = deel.IndexOf(':');
            if (i <= 0) continue;
            yield return (deel[..i].Trim().ToLowerInvariant(), deel[(i + 1)..].Trim().ToLowerInvariant());
        }
    }

    /// <summary>Klopt de voorwaarde van de taak voor deze bouwlaag? <paramref name="index"/> 0 = het blok wordt niet per bouwlaag herhaald.</summary>
    private static bool TaakKlopt(PlanningSjabloonTaak t, SjabloonVariabelen v, int index, int aantal)
    {
        foreach (var (naam, waarde) in TaakVoorwaarden(t.Voorwaarde))
        {
            var ja = !(waarde is "nee" or "false");
            switch (naam)
            {
                case "kelder": if (v.Kelder != ja) return false; break;
                case "dak": if (!string.Equals(v.Dak ?? "", waarde, StringComparison.OrdinalIgnoreCase)) return false; break;
                case "terras":
                    var heeft = index == 0 ? (v.Terrassen?.Count ?? 0) > 0 : v.Terrassen?.Contains(index) == true;
                    if (heeft != ja) return false; break;
                case "grond": // de bouwlaag rust op de grond: het gelijkvloers zonder kelder
                    if ((index <= 1 && !v.Kelder) != ja) return false; break;
                case "laag":
                    var eerste = index <= 1; var laatste = index == 0 || index == aantal;
                    var klopt = waarde switch { "eerste" => eerste, "laatste" => laatste, "boven" => !eerste, "eenmalig" => index == 0, _ => true };
                    if (!klopt) return false; break;
            }
        }
        return true;
    }

    /// <summary>Gebruikt het sjabloon deze variabele (kelder, dak, terras) in een blokvoorwaarde of in de voorwaarde van een taak? Dan vraagt het toepassen ernaar.</summary>
    public static bool GebruiktVariabele(PlanningSjabloon s, string variabele)
        => s.Blokken.Any(b => string.Equals((b.VoorwaardeVariabele ?? "").Trim(), variabele, StringComparison.OrdinalIgnoreCase)
                              || b.Taken.Any(t => TaakVoorwaarden(t.Voorwaarde).Any(v => v.Variabele == variabele || (variabele == "kelder" && v.Variabele == "grond"))));

    /// <summary>Een taak die in een blok "per bouwlaag in één fase" maar één keer voorkomt (bv. het gevelmetselwerk of de predallen van het dak), zonder bouwlaag in de naam.</summary>
    private static bool IsEenmalig(PlanningSjabloonTaak t) => TaakVoorwaarden(t.Voorwaarde).Any(v => v.Variabele == "laag" && v.Waarde == "eenmalig");

    /// <summary>Voorloop van een voorgangercode die naar dezelfde taak in de vorige bouwlaag wijst (bv. "VORIGE:AFDEK").</summary>
    public const string VorigePrefix = "vorige:";

    /// <summary>Voorloop van een voorgangercode die naar dezelfde taak in de eerste bouwlaag wijst (bv. "EERSTE:METSELWERK_RUWBOUW"); bruikbaar vanuit elke taak.</summary>
    public const string EerstePrefix = "eerste:";

    private static bool IsTerras(PlanningSjabloonBlok b) => string.Equals((b.VoorwaardeVariabele ?? "").Trim(), "terras", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Terrassen of luifels: "ja" = het blok geldt enkel voor de bouwlagen met een terras of luifel, "nee" = enkel voor de andere.
    /// Zonder bouwlaag (blok dat niet per verdieping herhaald wordt) telt "is er ergens een terras of luifel".
    /// </summary>
    private static bool TerrasKlopt(PlanningSjabloonBlok b, SjabloonVariabelen v, int? bouwlaag)
    {
        var terrassen = v.Terrassen ?? Array.Empty<int>();
        var waarde = (b.VoorwaardeWaarde ?? "").Trim().ToLowerInvariant();
        var wil = !(waarde is "nee" or "false");
        var heeft = bouwlaag == null ? terrassen.Count > 0 : terrassen.Contains(bouwlaag.Value);
        return wil ? heeft : !heeft;
    }

    /// <summary>De bouwlaag zoals wij ze noemen: 1 = gelijkvloers, 2 = 1ste verdieping, 3 = 2de verdieping, ...</summary>
    public static string BouwlaagNaam(int bouwlaag) => bouwlaag <= 1 ? "gelijkvloers" : SchijfSjabloonUitrol.Rangtelwoord(bouwlaag - 1) + " verdieping";

    public static SjabloonUitrolling Rol(PlanningSjabloon sjabloon, SjabloonVariabelen v)
    {
        var uit = new SjabloonUitrolling();
        var blokken = sjabloon.Blokken.OrderBy(b => b.Volgorde).ThenBy(b => b.Id).ToList();
        var n = Math.Max(1, v.Verdiepingen);

        var instanties = new List<Instantie>();
        foreach (var b in blokken)
        {
            var terras = IsTerras(b);
            if (!terras && !VoorwaardeKlopt(b, v)) continue;
            var alle = b.Taken.OrderBy(t => t.Volgorde).ThenBy(t => t.Id).ToList();
            if (b.Herhaling == 1)
            {
                for (var i = 1; i <= n; i++)
                {
                    if (terras && !TerrasKlopt(b, v, i)) continue;   // terrassen en luifels: enkel de bouwlagen die ze hebben
                    var items = alle.Where(t => TaakKlopt(t, v, i, n)).Select(t => (t, i)).ToList();
                    if (alle.Count > 0 && items.Count == 0) continue;   // alle taken vallen op deze bouwlaag weg: geen lege fase
                    instanties.Add(new Instantie { Blok = b, Index = i, Items = items });
                }
            }
            else if (b.Herhaling == PerBouwlaagEenFase)
            {
                // Eén fase: per bouwlaag de taken die erbij horen, daarna de taken die maar één keer voorkomen (gevel, dak).
                if (terras && !TerrasKlopt(b, v, null)) continue;
                var items = new List<(PlanningSjabloonTaak T, int Laag)>();
                for (var i = 1; i <= n; i++)
                    foreach (var t in alle)
                        if (!IsEenmalig(t) && TaakKlopt(t, v, i, n)) items.Add((t, i));
                foreach (var t in alle)
                    if (IsEenmalig(t) && TaakKlopt(t, v, 0, n)) items.Add((t, 0));
                if (alle.Count > 0 && items.Count == 0) continue;
                instanties.Add(new Instantie { Blok = b, Index = 0, Items = items });
            }
            else
            {
                if (terras && !TerrasKlopt(b, v, null)) continue;
                var items = alle.Where(t => TaakKlopt(t, v, 0, n)).Select(t => (t, 0)).ToList();
                if (alle.Count > 0 && items.Count == 0) continue;
                instanties.Add(new Instantie { Blok = b, Index = 0, Items = items });
            }
        }

        var blokPerCode = blokken.Where(b => !string.IsNullOrWhiteSpace(b.Code)).GroupBy(b => b.Code!.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());
        var taakPerCode = blokken.SelectMany(b => b.Taken.Select(t => (Blok: b, Taak: t))).Where(x => !string.IsNullOrWhiteSpace(x.Taak.Code))
            .GroupBy(x => x.Taak.Code!.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());

        // Alle plaatsen (fase + bouwlaag) waar een taak in de uitrol voorkomt.
        List<(Instantie Inst, int Laag)> Plaatsen(PlanningSjabloonTaak taak)
            => instanties.SelectMany(i => i.Items.Where(x => x.T == taak).Select(x => (i, x.Laag))).ToList();

        // Een taak kan in sommige bouwlagen wegvallen (voorwaarde per taak): kies de bouwlaag waar ze wel staat.
        // Dezelfde bouwlaag als beide per bouwlaag herhaald worden, anders de laatste.
        string? SleutelVan(PlanningSjabloonBlok doel, PlanningSjabloonTaak taak, int vanLaag, PlanningSjabloonBlok vanBlok)
        {
            var kandidaten = Plaatsen(taak);
            if (kandidaten.Count == 0) return null;
            var gekozen = kandidaten[^1];
            if (doel.Herhaling >= 1 && vanBlok.Herhaling >= 1 && vanLaag >= 1 && vanLaag <= n)
            {
                // Beide per bouwlaag: dezelfde bouwlaag. Bestaat de taak daar niet (bv. predallen op het gelijkvloers zonder kelder), dan valt de verwijzing weg.
                var zelfde = kandidaten.Where(k => k.Laag == vanLaag).ToList();
                if (zelfde.Count == 0) return null;
                gekozen = zelfde[0];
            }
            return gekozen.Inst.Sleutel(taak, gekozen.Laag);
        }

        // De taken van een heel blok: de bouwlaag van de verwijzende taak, anders de laatste bouwlaag.
        IEnumerable<string> SleutelsVanBlok(PlanningSjabloonBlok doel, int vanLaag, PlanningSjabloonBlok vanBlok)
        {
            var kandidaten = instanties.Where(i => i.Blok.Id == doel.Id).SelectMany(i => i.Items.Select(x => (Inst: i, x.T, x.Laag))).ToList();
            if (kandidaten.Count == 0) return Array.Empty<string>();
            var lagen = kandidaten.Select(k => k.Laag).Distinct().ToList();
            var laag = doel.Herhaling >= 1 && vanBlok.Herhaling >= 1 && vanLaag > 0 && lagen.Contains(vanLaag)
                ? vanLaag
                : (lagen.Any(l => l > 0) ? lagen.Max() : 0);
            // Bij een blok per verdieping (een fase per bouwlaag) hoort de bouwlaag bij de fase, niet bij de taak.
            if (doel.Herhaling == 1)
            {
                var fase = kandidaten.Where(k => k.Laag == laag).Select(k => k.Inst).FirstOrDefault() ?? kandidaten[^1].Inst;
                return fase.Items.Select(x => fase.Sleutel(x.T, x.Laag)).ToList();
            }
            return kandidaten.Where(k => k.Laag == laag).Select(k => k.Inst.Sleutel(k.T, k.Laag)).ToList();
        }

        foreach (var inst in instanties)
        {
            var naam = inst.Blok.Naam;
            if (inst.Blok.Herhaling == 1 && n > 1) naam += " " + BouwlaagNaam(inst.Index);
            var fase = new UitgerolFase { Naam = naam, Code = inst.Blok.Herhaling == 1 && n > 1 ? null : (string.IsNullOrWhiteSpace(inst.Blok.Code) ? null : inst.Blok.Code.Trim()) };

            foreach (var (t, laag) in inst.Items)
            {
                var taakNaam = inst.Blok.Herhaling == PerBouwlaagEenFase && laag > 0 && n > 1 ? $"{t.Naam} {BouwlaagNaam(laag)}" : t.Naam;
                var taak = new UitgerolTaak
                {
                    Sleutel = inst.Sleutel(t, laag), Naam = taakNaam, Beschrijving = t.Beschrijving, Duur = Math.Max(0, t.Duur),
                    Code = t.Code, Estafette = t.Estafette, EstafetteWacht = t.EstafetteWacht, BouwstaatId = t.BouwstaatId, ActivityId = t.ActivityId,
                    Subfase = inst.Blok.Herhaling == PerBouwlaagEenFase && laag > 0 && n > 1 ? char.ToUpperInvariant(BouwlaagNaam(laag)[0]) + BouwlaagNaam(laag)[1..] : null,
                };
                void Voeg(string sleutel, byte type, int wacht)
                {
                    if (sleutel == taak.Sleutel || taak.Voorgangers.Any(x => x.Sleutel == sleutel)) return;
                    taak.Voorgangers.Add((sleutel, type, wacht));
                }

                // De bouwlaag van deze taak; een taak die maar één keer voorkomt (dak) komt na de bovenste bouwlaag.
                var vanLaag = inst.Blok.Herhaling == 1 ? inst.Index : (inst.Blok.Herhaling == PerBouwlaagEenFase ? (laag > 0 ? laag : n + 1) : 0);

                foreach (var vg in t.Voorgangers)
                {
                    var code = (vg.VoorgangerCode ?? "").Trim().ToLowerInvariant();
                    // "eerste:metselwerk_ruwbouw" = het metselwerk van de eerste bouwlaag, vanuit elke taak (bv. het gevelmetselwerk start na het metselwerk van de eerste bouwlaag).
                    if (code.StartsWith(EerstePrefix, StringComparison.Ordinal))
                    {
                        code = code[EerstePrefix.Length..];
                        if (!taakPerCode.TryGetValue(code, out var et) || et.Blok.Herhaling < 1) continue;
                        var eerste = Plaatsen(et.Taak).OrderBy(p => p.Laag).FirstOrDefault();
                        if (eerste.Inst != null) Voeg(eerste.Inst.Sleutel(et.Taak, eerste.Laag), vg.Type, vg.WachtDagen);
                        continue;
                    }
                    // "vorige:metselwerk_ruwbouw" = het metselwerk van de vorige bouwlaag (de eerste bouwlaag heeft geen vorige en slaat dit over).
                    if (code.StartsWith(VorigePrefix, StringComparison.Ordinal))
                    {
                        code = code[VorigePrefix.Length..];
                        if (inst.Blok.Herhaling < 1 || vanLaag <= 1 || !taakPerCode.TryGetValue(code, out var vt) || vt.Blok.Herhaling < 1) continue;
                        var vorige = Plaatsen(vt.Taak).FirstOrDefault(p => p.Laag == vanLaag - 1 || (p.Inst.Blok.Herhaling == 1 && p.Inst.Index == vanLaag - 1));
                        if (vorige.Inst != null) Voeg(vorige.Inst.Sleutel(vt.Taak, vorige.Laag), vg.Type, vg.WachtDagen);
                        continue;
                    }
                    if (taakPerCode.TryGetValue(code, out var tt))
                    {
                        var sleutel = SleutelVan(tt.Blok, tt.Taak, vanLaag, inst.Blok);
                        if (sleutel == null) continue; // voorwaardelijk blok of taak die nu niet meedoet
                        Voeg(sleutel, vg.Type, vg.WachtDagen);
                    }
                    else if (blokPerCode.TryGetValue(code, out var bb))
                    {
                        foreach (var s in SleutelsVanBlok(bb, vanLaag, inst.Blok)) Voeg(s, vg.Type, vg.WachtDagen);
                    }
                    else uit.Waarschuwingen.Add($"Taak “{t.Naam}”: voorganger met code “{vg.VoorgangerCode}” bestaat niet in het sjabloon en is overgeslagen.");
                }

                // Volgende verdieping start na de vorige verdieping van hetzelfde blok.
                if (inst.Blok.Herhaling == 1 && inst.Blok.VolgtVorigeVerdieping && inst.Index > 1 && inst.Items.Count > 0 && inst.Items[0].T == t)
                {
                    var vorige = instanties.FirstOrDefault(i => i.Blok.Id == inst.Blok.Id && i.Index == inst.Index - 1);
                    if (vorige != null) foreach (var (vt, vl) in vorige.Items) Voeg(vorige.Sleutel(vt, vl), 0, 0);
                }

                fase.Taken.Add(taak);
            }
            uit.Fases.Add(fase);
        }
        return uit;
    }
}
