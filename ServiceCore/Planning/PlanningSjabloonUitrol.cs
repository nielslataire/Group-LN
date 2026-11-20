using DALCore.Models;

namespace ServiceCore.Planning;

/// <summary>De variabelen waarmee een sjabloon op één bouwdeel wordt uitgerold.</summary>
public sealed record SjabloonVariabelen(int Verdiepingen = 1, bool Kelder = false, string? Dak = null);

/// <summary>Eén taak van een uitgerolde planning. <see cref="Sleutel"/> is uniek binnen de uitrol en gelijk voor "dezelfde" taak in een andere uitrol (estafette).</summary>
public sealed class UitgerolTaak
{
    public string Sleutel { get; init; } = "";
    public string Naam { get; init; } = "";
    public string? Beschrijving { get; init; }
    public int Duur { get; init; }
    public string? Code { get; init; }
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
        public required List<PlanningSjabloonTaak> Taken { get; init; }
        public string Sleutel(PlanningSjabloonTaak t) => Blok.Herhaling == 1 ? $"{t.Id}#{Index}" : t.Id.ToString();
    }

    public static bool VoorwaardeKlopt(PlanningSjabloonBlok b, SjabloonVariabelen v)
    {
        if (string.IsNullOrWhiteSpace(b.VoorwaardeVariabele)) return true;
        var waarde = (b.VoorwaardeWaarde ?? "").Trim().ToLowerInvariant();
        return b.VoorwaardeVariabele.Trim().ToLowerInvariant() switch
        {
            "kelder" => waarde is "ja" or "true" ? v.Kelder : (waarde is "nee" or "false" ? !v.Kelder : v.Kelder),
            "dak" => string.Equals(v.Dak ?? "", waarde, StringComparison.OrdinalIgnoreCase),
            _ => true,
        };
    }

    public static SjabloonUitrolling Rol(PlanningSjabloon sjabloon, SjabloonVariabelen v)
    {
        var uit = new SjabloonUitrolling();
        var blokken = sjabloon.Blokken.OrderBy(b => b.Volgorde).ThenBy(b => b.Id).ToList();

        var instanties = new List<Instantie>();
        foreach (var b in blokken)
        {
            if (!VoorwaardeKlopt(b, v)) continue;
            var taken = b.Taken.OrderBy(t => t.Volgorde).ThenBy(t => t.Id).ToList();
            if (b.Herhaling == 1)
            {
                var n = Math.Max(1, v.Verdiepingen);
                for (var i = 1; i <= n; i++) instanties.Add(new Instantie { Blok = b, Index = i, Taken = taken });
            }
            else instanties.Add(new Instantie { Blok = b, Index = 0, Taken = taken });
        }

        var blokPerCode = blokken.Where(b => !string.IsNullOrWhiteSpace(b.Code)).GroupBy(b => b.Code!.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());
        var taakPerCode = blokken.SelectMany(b => b.Taken.Select(t => (Blok: b, Taak: t))).Where(x => !string.IsNullOrWhiteSpace(x.Taak.Code))
            .GroupBy(x => x.Taak.Code!.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());

        // Welke instantie van het doelblok hoort bij de verwijzende instantie: dezelfde verdieping als beide herhaald zijn, anders de laatste.
        Instantie? Kies(PlanningSjabloonBlok doel, Instantie van)
        {
            var kandidaten = instanties.Where(i => i.Blok.Id == doel.Id).ToList();
            if (kandidaten.Count == 0) return null;
            if (doel.Herhaling == 1 && van.Blok.Herhaling == 1)
                return kandidaten.FirstOrDefault(k => k.Index == van.Index) ?? kandidaten[^1];
            return kandidaten[^1];
        }

        foreach (var inst in instanties)
        {
            var naam = inst.Blok.Naam;
            if (inst.Blok.Herhaling == 1 && Math.Max(1, v.Verdiepingen) > 1) naam += " verdieping " + inst.Index;
            var fase = new UitgerolFase { Naam = naam, Code = inst.Blok.Herhaling == 1 && Math.Max(1, v.Verdiepingen) > 1 ? null : (string.IsNullOrWhiteSpace(inst.Blok.Code) ? null : inst.Blok.Code.Trim()) };

            foreach (var t in inst.Taken)
            {
                var taak = new UitgerolTaak
                {
                    Sleutel = inst.Sleutel(t), Naam = t.Naam, Beschrijving = t.Beschrijving, Duur = Math.Max(0, t.Duur),
                    Code = t.Code, Estafette = t.Estafette, EstafetteWacht = t.EstafetteWacht,
                };
                void Voeg(string sleutel, byte type, int wacht)
                {
                    if (sleutel == taak.Sleutel || taak.Voorgangers.Any(x => x.Sleutel == sleutel)) return;
                    taak.Voorgangers.Add((sleutel, type, wacht));
                }

                foreach (var vg in t.Voorgangers)
                {
                    var code = (vg.VoorgangerCode ?? "").Trim().ToLowerInvariant();
                    if (taakPerCode.TryGetValue(code, out var tt))
                    {
                        var doel = Kies(tt.Blok, inst);
                        if (doel == null) continue; // voorwaardelijk blok dat nu niet meedoet
                        Voeg(doel.Sleutel(tt.Taak), vg.Type, vg.WachtDagen);
                    }
                    else if (blokPerCode.TryGetValue(code, out var bb))
                    {
                        var doel = Kies(bb, inst);
                        if (doel == null) continue;
                        foreach (var dt in doel.Taken) Voeg(doel.Sleutel(dt), vg.Type, vg.WachtDagen);
                    }
                    else uit.Waarschuwingen.Add($"Taak “{t.Naam}”: voorganger met code “{vg.VoorgangerCode}” bestaat niet in het sjabloon en is overgeslagen.");
                }

                // Volgende verdieping start na de vorige verdieping van hetzelfde blok.
                if (inst.Blok.Herhaling == 1 && inst.Blok.VolgtVorigeVerdieping && inst.Index > 1 && t == inst.Taken[0])
                {
                    var vorige = instanties.FirstOrDefault(i => i.Blok.Id == inst.Blok.Id && i.Index == inst.Index - 1);
                    if (vorige != null) foreach (var vt in vorige.Taken) Voeg(vorige.Sleutel(vt), 0, 0);
                }

                fase.Taken.Add(taak);
            }
            uit.Fases.Add(fase);
        }
        return uit;
    }
}
