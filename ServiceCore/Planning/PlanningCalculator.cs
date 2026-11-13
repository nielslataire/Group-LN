namespace ServiceCore.Planning;

/// <summary>Soort afhankelijkheid (zelfde waarden als <c>PlanningAfhankelijkheid.Type</c>).</summary>
public enum PlanLinkType : byte
{
    /// <summary>Einde van de voorganger, dan begin van de opvolger.</summary>
    FS = 0,
    /// <summary>Begin, dan begin.</summary>
    SS = 1,
    /// <summary>Einde, dan einde.</summary>
    FF = 2,
    /// <summary>Begin, dan einde.</summary>
    SF = 3,
}

/// <summary>Beperking op een taak (zelfde waarden als <c>PlanningTaak.BeperkingType</c>).</summary>
public enum PlanBeperking : byte
{
    ZoVroegMogelijk = 0,
    MoetBeginnenOp = 1,
    NietEerderBeginnenDan = 2,
    NietLaterBeginnenDan = 3,
    NietEerderEindigenDan = 4,
    NietLaterEindigenDan = 5,
    MoetEindigenOp = 6,
}

/// <summary>Voorganger van een taak. <paramref name="WachtDagen"/> in werkdagen, ook negatief (overlap).</summary>
public sealed record PlanLink(int VoorgangerId, PlanLinkType Type = PlanLinkType.FS, int WachtDagen = 0);

/// <param name="Duur">Duur in werkdagen; 0 = mijlpaal.</param>
/// <param name="WerkelijkEinde">Werkelijk einde (<c>PlanningTaak.AfgerondOp</c>); de opvolgers rekenen vanaf deze datum.</param>
public sealed record PlanTaak(
    int Id,
    int Duur,
    IReadOnlyList<PlanLink> Voorgangers,
    PlanBeperking Beperking = PlanBeperking.ZoVroegMogelijk,
    DateOnly? BeperkingDatum = null,
    DateOnly? Deadline = null,
    DateOnly? WerkelijkStart = null,
    DateOnly? WerkelijkEinde = null,
    bool IsAfgerond = false);

/// <summary>Resultaat per taak.</summary>
public sealed class PlanTaakResultaat
{
    public int Id { get; init; }
    public DateOnly Start { get; set; }
    public DateOnly Einde { get; set; }

    /// <summary>Speling in werkdagen (0 = kan niet opschuiven zonder het projecteinde te verschuiven).</summary>
    public int Speling { get; set; }

    /// <summary>Op het kritieke pad: speling 0 (of negatief) en nog niet afgerond.</summary>
    public bool IsKritiek { get; set; }

    /// <summary>De voorganger die de start bepaalt (null = projectstart, beperking of werkelijke datum).</summary>
    public int? BepalendeVoorgangerId { get; set; }

    public bool InCyclus { get; set; }

    public List<PlanWaarschuwing> Waarschuwingen { get; } = new();
}

public enum PlanWaarschuwingType
{
    /// <summary>Het einde valt na de deadline van de taak (Dagen = werkdagen te laat).</summary>
    DeadlineOverschreden,
    /// <summary>Een "niet later dan"-beperking wordt overschreden (Dagen = werkdagen te laat).</summary>
    BeperkingOverschreden,
    /// <summary>De taak heeft geen voorganger en start op de eerste projectdag.</summary>
    GeenVoorganger,
    /// <summary>De taak zit in een cyclus.</summary>
    Cyclus,
}

public sealed record PlanWaarschuwing(PlanWaarschuwingType Type, int Dagen = 0);

public sealed class PlanResultaat
{
    public Dictionary<int, PlanTaakResultaat> Taken { get; } = new();

    /// <summary>Taak-id's die in een cyclus zitten (wachten op elkaar). Hun datums zijn niet betrouwbaar.</summary>
    public List<int> Cyclus { get; } = new();

    public DateOnly? Start { get; set; }
    public DateOnly? Einde { get; set; }
}

/// <summary>
/// Rekent een bouwplanning door in werkdagen (FS/SS/FF/SF met wachttijd, beperkingen, werkelijke data) en bepaalt
/// speling en kritiek pad. Zuivere logica zonder database: de werkkalender en de taken komen binnen als parameters.
/// </summary>
public static class PlanningCalculator
{
    public static PlanResultaat Bereken(IReadOnlyList<PlanTaak> taken, DateOnly projectStart, WerkKalender kalender, bool metVerlet = true)
    {
        var res = new PlanResultaat();
        if (taken.Count == 0) return res;

        var byId = taken.ToDictionary(t => t.Id);
        var start0 = kalender.OpOfNa(projectStart, metVerlet);

        // Voorgangers die niet (meer) bestaan negeren we stilzwijgend.
        IEnumerable<PlanLink> Links(PlanTaak t) => t.Voorgangers.Where(l => byId.ContainsKey(l.VoorgangerId) && l.VoorgangerId != t.Id);

        // ── Topologische volgorde (Kahn); wat overblijft zit in een cyclus ──
        var inGraad = taken.ToDictionary(t => t.Id, t => Links(t).Select(l => l.VoorgangerId).Distinct().Count());
        var opvolgers = taken.ToDictionary(t => t.Id, _ => new List<int>());
        foreach (var t in taken)
            foreach (var vid in Links(t).Select(l => l.VoorgangerId).Distinct())
                opvolgers[vid].Add(t.Id);

        var volgorde = new List<int>();
        var wachtrij = new Queue<int>(taken.Where(t => inGraad[t.Id] == 0).Select(t => t.Id));
        while (wachtrij.Count > 0)
        {
            var id = wachtrij.Dequeue();
            volgorde.Add(id);
            foreach (var o in opvolgers[id])
                if (--inGraad[o] == 0) wachtrij.Enqueue(o);
        }
        var cyclusIds = taken.Select(t => t.Id).Except(volgorde).ToList();

        // ── Voorwaartse berekening ──
        var eerste = taken[0].Id;
        foreach (var id in volgorde)
        {
            var t = byId[id];
            var r = new PlanTaakResultaat { Id = id };
            res.Taken[id] = r;

            var d = Math.Max(0, t.Duur);
            var start = start0;
            int? bepalend = null;

            foreach (var l in Links(t))
            {
                var v = res.Taken[l.VoorgangerId];
                DateOnly kandidaat;
                switch (l.Type)
                {
                    case PlanLinkType.SS:
                        kandidaat = kalender.OpOfNa(kalender.Voeg(v.Start, l.WachtDagen, metVerlet), metVerlet);
                        break;
                    case PlanLinkType.FF:
                        kandidaat = StartVanEinde(kalender, kalender.Voeg(v.Einde, l.WachtDagen, metVerlet), d, metVerlet);
                        break;
                    case PlanLinkType.SF:
                        kandidaat = StartVanEinde(kalender, kalender.Voeg(v.Start, l.WachtDagen, metVerlet), d, metVerlet);
                        break;
                    default: // FS: de dag na het einde, plus wachttijd
                        kandidaat = kalender.OpOfNa(kalender.Voeg(v.Einde, 1 + l.WachtDagen, metVerlet), metVerlet);
                        break;
                }
                if (kandidaat > start) { start = kandidaat; bepalend = l.VoorgangerId; }
            }

            // Beperkingen
            if (t.BeperkingDatum is DateOnly bd)
            {
                switch (t.Beperking)
                {
                    case PlanBeperking.MoetBeginnenOp:
                        start = kalender.OpOfNa(bd, metVerlet); bepalend = null; break;
                    case PlanBeperking.NietEerderBeginnenDan:
                        var b1 = kalender.OpOfNa(bd, metVerlet);
                        if (b1 > start) { start = b1; bepalend = null; }
                        break;
                    case PlanBeperking.MoetEindigenOp:
                        start = StartVanEinde(kalender, kalender.OpOfNa(bd, metVerlet), d, metVerlet); bepalend = null; break;
                    case PlanBeperking.NietEerderEindigenDan:
                        var b2 = StartVanEinde(kalender, kalender.OpOfNa(bd, metVerlet), d, metVerlet);
                        if (b2 > start) { start = b2; bepalend = null; }
                        break;
                }
            }

            // Werkelijke data overschrijven de planning; de opvolgers rekenen vanaf het werkelijke einde.
            if (t.WerkelijkStart is DateOnly ws) { start = ws; bepalend = null; }
            var einde = kalender.Einde(start, d, metVerlet);
            if (t.WerkelijkEinde is DateOnly we)
            {
                einde = we;
                if (we < start) start = we;
            }

            r.Start = start;
            r.Einde = einde;
            r.BepalendeVoorgangerId = bepalend;

            // Waarschuwingen
            if (!Links(t).Any() && t.Beperking == PlanBeperking.ZoVroegMogelijk && t.WerkelijkStart is null && id != eerste)
                r.Waarschuwingen.Add(new PlanWaarschuwing(PlanWaarschuwingType.GeenVoorganger));
            if (t.Deadline is DateOnly dl && einde > dl)
                r.Waarschuwingen.Add(new PlanWaarschuwing(PlanWaarschuwingType.DeadlineOverschreden, kalender.Tel(dl, einde, metVerlet)));
            if (t.BeperkingDatum is DateOnly nl)
            {
                if (t.Beperking == PlanBeperking.NietLaterBeginnenDan && start > nl)
                    r.Waarschuwingen.Add(new PlanWaarschuwing(PlanWaarschuwingType.BeperkingOverschreden, kalender.Tel(nl, start, metVerlet)));
                else if (t.Beperking == PlanBeperking.NietLaterEindigenDan && einde > nl)
                    r.Waarschuwingen.Add(new PlanWaarschuwing(PlanWaarschuwingType.BeperkingOverschreden, kalender.Tel(nl, einde, metVerlet)));
            }
        }

        // Cyclus: datums onbetrouwbaar, maar de taak blijft zichtbaar.
        foreach (var id in cyclusIds)
        {
            var t = byId[id];
            var r = new PlanTaakResultaat { Id = id, Start = start0, Einde = kalender.Einde(start0, Math.Max(0, t.Duur), metVerlet), InCyclus = true };
            r.Waarschuwingen.Add(new PlanWaarschuwing(PlanWaarschuwingType.Cyclus));
            res.Taken[id] = r;
            res.Cyclus.Add(id);
        }

        res.Start = res.Taken.Values.Min(x => x.Start);
        res.Einde = res.Taken.Values.Max(x => x.Einde);

        // ── Achterwaartse berekening: speling en kritiek pad ──
        var projEinde = res.Einde!.Value;
        var lateEinde = new Dictionary<int, DateOnly>();
        foreach (var id in Enumerable.Reverse(volgorde))
        {
            var t = byId[id];
            var d = Math.Max(0, t.Duur);
            DateOnly le = projEinde;
            foreach (var oid in opvolgers[id])
            {
                if (!lateEinde.ContainsKey(oid)) continue; // opvolger in een cyclus
                var o = byId[oid];
                var od = Math.Max(0, o.Duur);
                var oLateEinde = lateEinde[oid];
                var oLateStart = LateStart(kalender, oLateEinde, od, metVerlet);
                foreach (var l in Links(o).Where(l => l.VoorgangerId == id))
                {
                    DateOnly kandidaat = l.Type switch
                    {
                        PlanLinkType.SS => kalender.Einde(kalender.Voeg(oLateStart, -l.WachtDagen, metVerlet), d, metVerlet),
                        PlanLinkType.FF => kalender.Voeg(oLateEinde, -l.WachtDagen, metVerlet),
                        PlanLinkType.SF => kalender.Einde(kalender.Voeg(oLateEinde, -l.WachtDagen, metVerlet), d, metVerlet),
                        _ => kalender.Voeg(oLateStart, -(1 + l.WachtDagen), metVerlet),
                    };
                    if (kandidaat < le) le = kandidaat;
                }
            }
            lateEinde[id] = le;

            var r = res.Taken[id];
            r.Speling = le >= r.Einde ? kalender.Tel(r.Einde, le, metVerlet) : -kalender.Tel(le, r.Einde, metVerlet);
            r.IsKritiek = r.Speling <= 0 && !t.IsAfgerond && t.WerkelijkEinde is null;
        }

        return res;
    }

    /// <summary>Startdag van een taak van <paramref name="duur"/> werkdagen die op <paramref name="einde"/> eindigt.</summary>
    private static DateOnly StartVanEinde(WerkKalender k, DateOnly einde, int duur, bool metVerlet)
        => duur <= 1 ? k.OpOfNa(einde, metVerlet) : k.Voeg(k.OpOfNa(einde, metVerlet), -(duur - 1), metVerlet);

    private static DateOnly LateStart(WerkKalender k, DateOnly lateEinde, int duur, bool metVerlet)
        => duur <= 1 ? lateEinde : k.Voeg(lateEinde, -(duur - 1), metVerlet);
}
