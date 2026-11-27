using DALCore.Models;
using FacadeCore;

namespace ServiceCore.Planning;

/// <summary>
/// Proefdraai van een planningssjabloon: rolt het uit zoals bij het toepassen (verdiepingen, voorwaarden, estafette) en rekent het door met de
/// echte <see cref="PlanningCalculator"/> op een gewone werkweek (zonder feestdagen of verlet). Zuivere logica, geen database.
/// </summary>
internal static class PlanningSjabloonProef
{
    // Een maandag; enkel de werkdagen tellen, de kalenderdatum niet.
    private static readonly DateOnly Start = new(2026, 1, 5);

    /// <summary>Voor het voorbeeld doen voorwaardelijke blokken mee: kelder aan, en het eerste daktype dat het sjabloon kent.</summary>
    internal static SjabloonVariabelen Voorbeeldvariabelen(PlanningSjabloon s, int verdiepingen, bool kelder = true)
    {
        var dak = s.Blokken.Where(b => string.Equals(b.VoorwaardeVariabele, "dak", StringComparison.OrdinalIgnoreCase)).Select(b => b.VoorwaardeWaarde).FirstOrDefault(w => !string.IsNullOrEmpty(w));
        // Een daktype kan ook als voorwaarde op een taak staan (bv. "dak:plat"); anders ontbreekt het hele dak in het voorbeeld.
        dak ??= s.Blokken.SelectMany(b => b.Taken).SelectMany(t => PlanningSjabloonUitrol.TaakVoorwaarden(t.Voorwaarde)).Where(v => v.Variabele == "dak").Select(v => v.Waarde).FirstOrDefault(w => !string.IsNullOrEmpty(w));
        var lagen = Math.Max(1, verdiepingen);
        // Terrassen of luifels: in het voorbeeld bij elke bouwlaag, zodat het langste verloop te zien is.
        // Gevelafwerkingen: in het voorbeeld alle, zodat het langste verloop te zien is.
        return new SjabloonVariabelen(lagen, kelder, dak, Enumerable.Range(1, lagen).ToArray(), GevelAfwerkingen.Alle);
    }

    private static int Nr(DateOnly d)
    {
        var n = 1;
        for (var x = Start; x < d; x = x.AddDays(1)) if (!WerkKalender.IsWeekend(x)) n++;
        return n;
    }

    private sealed record Rij(int Woning, UitgerolTaak Taak, int Id);

    private static (List<Rij> Rijen, PlanResultaat Res) Draai(SjabloonUitrolling uit, int woningen)
    {
        var rijen = new List<Rij>();
        var id = 0;
        var perSleutel = new Dictionary<(int, string), int>();
        var alle = uit.Fases.SelectMany(f => f.Taken).ToList();
        for (var u = 0; u < woningen; u++)
            foreach (var t in alle) { var r = new Rij(u, t, ++id); rijen.Add(r); perSleutel[(u, t.Sleutel)] = r.Id; }

        var taken = new List<PlanTaak>();
        foreach (var r in rijen)
        {
            var links = new List<PlanLink>();
            foreach (var (sleutel, type, wacht) in r.Taak.Voorgangers)
                if (perSleutel.TryGetValue((r.Woning, sleutel), out var vid)) links.Add(new PlanLink(vid, (PlanLinkType)Math.Min((int)type, 3), wacht));
            if (r.Taak.Estafette && r.Woning > 0 && perSleutel.TryGetValue((r.Woning - 1, r.Taak.Sleutel), out var vorig))
                links.Add(new PlanLink(vorig, PlanLinkType.FS, r.Taak.EstafetteWacht));
            taken.Add(new PlanTaak(r.Id, r.Taak.Duur, links));
        }
        return (rijen, PlanningCalculator.Bereken(taken, Start, new WerkKalender(), metVerlet: false));
    }

    /// <summary>Doorrekenen van een sjabloon (met tijdelijke of echte id's) voor <paramref name="woningen"/> woningen van <paramref name="verdiepingen"/> verdiepingen.</summary>
    internal static PlanningSjabloonProefUitkomst Rekenen(PlanningSjabloon s, int woningen, int verdiepingen, bool kelder = true)
        => Rekenen(s, woningen, Voorbeeldvariabelen(s, verdiepingen, kelder));

    /// <summary>Doorrekenen met de echte keuzes van een gebouw (kelder, dak, terrassen) in plaats van het langste voorbeeld.</summary>
    internal static PlanningSjabloonProefUitkomst Rekenen(PlanningSjabloon s, int woningen, SjabloonVariabelen variabelen)
    {
        woningen = Math.Clamp(woningen, 1, 20);
        var uit = new PlanningSjabloonProefUitkomst();
        var rol = PlanningSjabloonUitrol.Rol(s, variabelen);
        if (rol.Fases.Sum(f => f.Taken.Count) == 0) return uit;

        var codeNaarBlok = s.Blokken.SelectMany(b => b.Taken.Select(t => (Code: (t.Code ?? "").ToUpperInvariant(), Blok: b))).Where(x => x.Code.Length > 0)
            .GroupBy(x => x.Code).ToDictionary(g => g.Key, g => g.First().Blok);

        // Eén woning: voor de blokken, de start per taak en het kritieke pad.
        var (rijen1, res1) = Draai(rol, 1);
        int StartVan(Rij r) => res1.Taken.TryGetValue(r.Id, out var x) ? Nr(x.Start) : 1;
        int EindeVan(Rij r) => res1.Taken.TryGetValue(r.Id, out var x) ? Math.Max(Nr(x.Start), Nr(x.Einde)) : 1;

        uit.Werkdagen = rijen1.Count == 0 ? 0 : rijen1.Max(EindeVan);
        foreach (var blok in s.Blokken.OrderBy(b => b.Volgorde).ThenBy(b => b.Id))
        {
            var leden = rijen1.Where(r => codeNaarBlok.TryGetValue((r.Taak.Code ?? "").ToUpperInvariant(), out var b) && ReferenceEquals(b, blok)).ToList();
            if (leden.Count == 0) continue;
            var s0 = leden.Min(StartVan);
            var e0 = leden.Max(EindeVan);
            var pb = new PlanningSjabloonProefBlok { Code = blok.Code ?? "", Naam = blok.Naam ?? "", Start = s0, Einde = e0, Duur = Math.Max(0, e0 - s0 + 1) };
            if (leden.Any(r => r.Taak.Subfase != null))
                foreach (var deel in leden.GroupBy(r => r.Taak.Subfase ?? "Eenmalig").Select(g => new { g.Key, Van = g.Min(StartVan), Tot = g.Max(EindeVan) }).OrderBy(d => d.Key == "Eenmalig" ? 1 : 0).ThenBy(d => d.Van))
                    pb.Delen.Add(new PlanningSjabloonProefDeel { Naam = deel.Key, Start = deel.Van, Einde = deel.Tot, Duur = Math.Max(0, deel.Tot - deel.Van + 1) });
            uit.Blokken.Add(pb);
        }
        foreach (var g in rijen1.GroupBy(r => r.Taak.Code ?? "").Where(g => g.Key.Length > 0))
            uit.Taken.Add(new PlanningSjabloonProefTaak
            {
                Code = g.Key,
                Start = g.Min(StartVan),
                Kritiek = g.Any(r => res1.Taken.TryGetValue(r.Id, out var x) && x.IsKritiek && !x.InCyclus && r.Taak.Duur > 0),
            });
        uit.Kritiek = rijen1.Where(r => r.Taak.Duur > 0 && res1.Taken.TryGetValue(r.Id, out var x) && x.IsKritiek && !x.InCyclus)
            .OrderBy(StartVan).Select(r => r.Taak.Naam).Distinct().ToList();
        uit.Maanden = (int)Math.Round(uit.Werkdagen / 5.0 * 7 / 30.4);

        // Alle woningen samen, met de estafette.
        var (rijenN, resN) = Draai(rol, woningen);
        for (var u = 0; u < woningen; u++)
        {
            var van = rijenN.Where(r => r.Woning == u && resN.Taken.ContainsKey(r.Id)).ToList();
            if (van.Count == 0) continue;
            uit.Woningen.Add(new PlanningSjabloonProefWoning
            {
                Start = van.Min(r => Nr(resN.Taken[r.Id].Start)),
                Einde = van.Max(r => Math.Max(Nr(resN.Taken[r.Id].Start), Nr(resN.Taken[r.Id].Einde))),
            });
        }
        uit.WerkdagenAlle = uit.Woningen.Count == 0 ? 0 : uit.Woningen.Max(w => w.Einde);
        if (res1.Cyclus.Count > 0)
        {
            var namen = rijen1.Where(r => res1.Cyclus.Contains(r.Id)).Select(r => r.Taak.Naam).Distinct().Take(3);
            uit.Meldingen.Add(new PlanningSjabloonProefMelding { Ernst = "rood", Titel = "Kring", Tekst = string.Join(" → ", namen) + " wachten op elkaar" });
        }
        return uit;
    }
}
