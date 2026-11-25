using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>
/// Projectsjablonen (migratie 156): beheer en toepassen. Toepassen vult enkel aan wat nog leeg is: een traject als het project er nog geen heeft,
/// een planning voor eenheden zonder planning, schijven in betalingsgroepen zonder (gebruikte) schijven. Wat al bestaat, blijft ongemoeid.
/// </summary>
public sealed class ProjectSjabloonService : IProjectSjabloonService
{
    private readonly cpmRunningContext _db;
    private readonly IPlanningService _planning;
    private readonly ITrajectInstantiationService _traject;
    private readonly ISchijfSjabloonService _schijven;

    public ProjectSjabloonService(cpmRunningContext db, IPlanningService planning, ITrajectInstantiationService traject, ISchijfSjabloonService schijven)
    {
        _db = db; _planning = planning; _traject = traject; _schijven = schijven;
    }

    // ── beheer ──

    public async Task<List<ProjectSjabloonBeheerRij>> BeheerLijst()
    {
        var lijst = await _db.ProjectSjabloon.AsNoTracking().OrderBy(s => s.Naam).ToListAsync();
        var planningen = await _db.PlanningSjabloon.AsNoTracking().Include(s => s.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).ToDictionaryAsync(s => s.Id);
        var trajecten = await _db.TrajectSjabloon.AsNoTracking().Include(s => s.Fases).ThenInclude(f => f.Mijlpalen).ToDictionaryAsync(s => s.Id);
        var schijven = await _db.SchijfSjabloon.AsNoTracking().Include(s => s.Schijven).ToDictionaryAsync(s => s.Id);
        var gebruikerIds = lijst.Select(s => s.ModifiedByUserId ?? s.CreatedByUserId).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
        var namen = gebruikerIds.Count == 0 ? new Dictionary<string, string>()
            : (await _db.AspNetUsers1.AsNoTracking().Where(u => gebruikerIds.Contains(u.Id)).Select(u => new { u.Id, u.Forename, u.Name }).ToListAsync())
                .ToDictionary(u => u.Id, u => string.IsNullOrWhiteSpace(u.Forename) ? (u.Name ?? "") : u.Forename);
        return lijst.Select(s =>
        {
            var r = new ProjectSjabloonBeheerRij
            {
                Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, IsActief = s.IsActief, GewijzigdOp = s.ModifiedDate ?? s.CreatedDate,
                StandaardGebouw = GebouwTekst(s.StandaardVerdiepingen, s.StandaardKelder, s.StandaardDak),
            };
            var door = s.ModifiedByUserId ?? s.CreatedByUserId;
            if (door != null && namen.TryGetValue(door, out var dn)) r.GewijzigdDoor = dn;
            if (s.PlanningSjabloonId is int p && planningen.TryGetValue(p, out var pl))
            {
                r.PlanningId = p; r.PlanningNaam = pl.Naam;
                var wd = 0; try { wd = PlanningSjabloonProef.Rekenen(pl, 1, Math.Max(1, s.StandaardVerdiepingen)).Werkdagen; } catch { /* een kapot sjabloon blijft in de lijst staan */ }
                r.PlanningKern = $"{pl.Blokken.Count} {(pl.Blokken.Count == 1 ? "blok" : "blokken")}{(wd > 0 ? " · " + wd + " wd" : "")}";
            }
            if (s.TrajectSjabloonId is int t && trajecten.TryGetValue(t, out var tr))
            {
                r.TrajectId = t; r.TrajectNaam = tr.Naam;
                var n = tr.Fases.Sum(f => f.Mijlpalen.Count);
                r.TrajectKern = $"{n} {(n == 1 ? "mijlpaal" : "mijlpalen")}";
            }
            if (s.SchijfSjabloonId is int c && schijven.TryGetValue(c, out var sc))
            {
                r.SchijvenId = c; r.SchijvenNaam = sc.Naam;
                r.SchijvenKern = $"{sc.Schijven.Count} {(sc.Schijven.Count == 1 ? "schijf" : "schijven")}";
            }
            return r;
        }).ToList();
    }

    /// <summary>"2 verd. · hellend dak · kelder": het standaardgebouw in woorden.</summary>
    private static string GebouwTekst(int verdiepingen, bool kelder, string? dak)
    {
        var delen = new List<string> { $"{Math.Max(1, verdiepingen)} verd." };
        if (dak == "hellend") delen.Add("hellend dak"); else if (dak == "plat") delen.Add("plat dak");
        if (kelder) delen.Add("kelder");
        return string.Join(" · ", delen);
    }

    public async Task<ProjectSjabloonOverzicht> Overzicht(int? planningId, int? trajectId, int? schijfId, int verdiepingen, bool kelder, string? dak)
    {
        verdiepingen = Math.Max(1, Math.Min(verdiepingen, 20));
        var uit = new ProjectSjabloonOverzicht();
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (planningId is int pid)
        {
            var pl = await _db.PlanningSjabloon.AsNoTracking().Include(x => x.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).FirstOrDefaultAsync(x => x.Id == pid);
            if (pl != null)
            {
                var proef = PlanningSjabloonProef.Rekenen(pl, 1, verdiepingen);
                // Blokken die niet voor dit gebouw gelden, vallen weg
                bool Geldt(PlanningSjabloonBlok b)
                {
                    var v = (b.VoorwaardeVariabele ?? "").ToLowerInvariant(); var w = (b.VoorwaardeWaarde ?? "").ToLowerInvariant();
                    if (v == "kelder") return kelder == (w == "ja");
                    if (v == "dak") return dak == null || dak == w;
                    return true;
                }
                var blokken = pl.Blokken.Where(Geldt).ToList();
                foreach (var b in pl.Blokken)
                {
                    if (!string.IsNullOrWhiteSpace(b.Code)) codes.Add(b.Code!);
                    foreach (var t in b.Taken) if (!string.IsNullOrWhiteSpace(t.Code)) codes.Add(t.Code!);
                }
                var verloop = string.Join(" · ", proef.Blokken.Where(x => blokken.Any(b => string.Equals(b.Code, x.Code, StringComparison.OrdinalIgnoreCase))).Select(x => $"{x.Naam} {x.Duur} wd"));
                uit.Planning = new ProjectSjabloonOverzichtPlanning
                {
                    Naam = pl.Naam, Blokken = pl.Blokken.Count, Taken = pl.Blokken.Sum(b => b.Taken.Count), Werkdagen = proef.Werkdagen,
                    PerVerdieping = pl.Blokken.Any(b => b.Herhaling >= 1), Fases = blokken.Count, Verloop = verloop,
                };
            }
        }
        if (trajectId is int tid)
        {
            var tr = await _db.TrajectSjabloon.AsNoTracking().Include(x => x.Fases).ThenInclude(f => f.Mijlpalen).FirstOrDefaultAsync(x => x.Id == tid);
            if (tr != null)
            {
                var mijl = tr.Fases.SelectMany(f => f.Mijlpalen).ToList();
                uit.Traject = new ProjectSjabloonOverzichtTraject
                {
                    Naam = tr.Naam, Mijlpalen = mijl.Count, Omschrijving = tr.Omschrijving,
                    Koppelingen = mijl.Count(m => !string.IsNullOrWhiteSpace(m.Code) && codes.Contains(m.Code)),
                };
            }
        }
        if (schijfId is int sid)
        {
            var sc = await _db.SchijfSjabloon.AsNoTracking().Include(x => x.Schijven).FirstOrDefaultAsync(x => x.Id == sid);
            if (sc != null)
            {
                var voorbeeld = await _schijven.Voorbeeld(sid, verdiepingen, kelder, dak);
                var rij = sc.Schijven.OrderBy(x => x.Volgorde).ThenBy(x => x.Id).ToList();
                var nietIn = uit.Planning == null ? new List<string>()
                    : rij.Select(x => (x.PlanningCode ?? "").Trim()).Where(c => c.Length > 0 && !codes.Contains(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var perVerd = rij.Count(x => x.PerVerdieping);
                var metVoorwaarde = rij.Count(x => !string.IsNullOrWhiteSpace(x.VoorwaardeVariabele));
                uit.Schijven = new ProjectSjabloonOverzichtSchijven
                {
                    Naam = sc.Naam, Aantal = rij.Count, Vast = VastKort(rij),
                    Toelichting = string.Join(", ", new[] { perVerd > 0 ? $"{perVerd} per verdieping" : "", metVoorwaarde > 0 ? $"{metVoorwaarde} enkel met kelder of dak" : "" }.Where(x => x.Length > 0)),
                    CodesNietInPlanning = nietIn, Voorstel = voorbeeld?.Schijven ?? new List<SchijfVoorstel>(), Totaal = voorbeeld?.Totaal ?? 0m,
                };
                if (nietIn.Count > 0)
                    uit.Meldingen.Add($"{string.Join(" en ", nietIn)} uit {sc.Naam} {(nietIn.Count == 1 ? "vindt" : "vinden")} geen taak in {uit.Planning!.Naam}: die schijven komen er zonder koppeling aan de planning.");
            }
        }
        return uit;
    }

    private static string VastKort(List<SchijfSjabloonSchijf> rij)
    {
        var vast = rij.Where(x => x.VastPercentage != null).ToList();
        if (vast.Count == 0) return "";
        var som = vast.Sum(x => x.VastPercentage!.Value);
        var laatste = rij.Count > 0 && rij[^1].VastPercentage != null;
        return $"vast {som.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("nl-BE"))} %{(laatste ? " oplevering" : "")}";
    }

    public async Task<List<ProjectSjabloonKeuze>> Keuzes()
    {
        var lijst = await _db.ProjectSjabloon.AsNoTracking().Where(s => s.IsActief).OrderBy(s => s.Naam).ToListAsync();
        var planning = await _db.PlanningSjabloon.AsNoTracking().Select(p => new { p.Id, N = p.Blokken.Count }).ToDictionaryAsync(x => x.Id, x => x.N);
        var traject = await _db.TrajectSjabloon.AsNoTracking().Select(p => new { p.Id, N = p.Fases.Count }).ToDictionaryAsync(x => x.Id, x => x.N);
        var schijven = await _db.SchijfSjabloon.AsNoTracking().Select(p => new { p.Id, N = p.Schijven.Count }).ToDictionaryAsync(x => x.Id, x => x.N);
        return lijst.Select(s =>
        {
            var delen = new List<string>();
            if (s.PlanningSjabloonId is int p && planning.TryGetValue(p, out var np)) delen.Add($"planning {np} {(np == 1 ? "fase" : "fases")}");
            if (s.TrajectSjabloonId is int t && traject.TryGetValue(t, out var nt)) delen.Add($"traject {nt} {(nt == 1 ? "fase" : "fases")}");
            if (s.SchijfSjabloonId is int c && schijven.TryGetValue(c, out var nc)) delen.Add($"{nc} {(nc == 1 ? "schijf" : "schijven")}");
            return new ProjectSjabloonKeuze { Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, Verdiepingen = Math.Max(1, s.StandaardVerdiepingen), Kelder = s.StandaardKelder, Dak = s.StandaardDak, Kern = delen.Count == 0 ? null : string.Join(" · ", delen) };
        }).ToList();
    }

    public async Task<ProjectSjabloonOpties> Opties() => new()
    {
        Planning = await _db.PlanningSjabloon.AsNoTracking().Where(s => s.IsActief).OrderBy(s => s.Naam).Select(s => new SjabloonOptie { Id = s.Id, Naam = s.Naam }).ToListAsync(),
        Traject = await _db.TrajectSjabloon.AsNoTracking().Where(s => s.IsActief).OrderBy(s => s.Naam).Select(s => new SjabloonOptie { Id = s.Id, Naam = s.Naam }).ToListAsync(),
        Schijven = await _db.SchijfSjabloon.AsNoTracking().Where(s => s.IsActief).OrderBy(s => s.Naam).Select(s => new SjabloonOptie { Id = s.Id, Naam = s.Naam }).ToListAsync(),
    };

    public async Task<ProjectSjabloonBewerk?> Haal(int id)
    {
        var s = await _db.ProjectSjabloon.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return null;
        return new ProjectSjabloonBewerk
        {
            Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, PlanningSjabloonId = s.PlanningSjabloonId, TrajectSjabloonId = s.TrajectSjabloonId,
            SchijfSjabloonId = s.SchijfSjabloonId, SchijfSjabloonBijgebouwId = s.SchijfSjabloonBijgebouwId, PlanningSjabloonProjectId = s.PlanningSjabloonProjectId, WoningenStartNa = s.WoningenStartNa, StandaardVerdiepingen = s.StandaardVerdiepingen, StandaardKelder = s.StandaardKelder, StandaardDak = s.StandaardDak, IsActief = s.IsActief,
        };
    }

    public async Task<ProjectSjabloonBewerkUitkomst> Bewaar(ProjectSjabloonBewerk s, string? userId)
    {
        var fouten = new List<string>();
        var naam = (s.Naam ?? "").Trim();
        if (naam.Length == 0) fouten.Add("Geef het sjabloon een naam.");
        if (naam.Length > 200) fouten.Add("De naam is te lang.");
        if (s.PlanningSjabloonId == null && s.PlanningSjabloonProjectId == null && s.TrajectSjabloonId == null && s.SchijfSjabloonId == null) fouten.Add("Kies minstens één sjabloon: planning, traject of schijven.");
        if (s.StandaardVerdiepingen < 1 || s.StandaardVerdiepingen > 20) fouten.Add("Het standaard aantal verdiepingen ligt tussen 1 en 20.");
        var dak = string.IsNullOrWhiteSpace(s.StandaardDak) ? null : s.StandaardDak.Trim().ToLowerInvariant();
        if (dak != null && dak is not ("hellend" or "plat")) fouten.Add("Het dak is hellend of plat.");
        if (s.PlanningSjabloonId is int p && !await _db.PlanningSjabloon.AnyAsync(x => x.Id == p)) fouten.Add("Het gekozen planningssjabloon bestaat niet meer.");
        if (s.PlanningSjabloonProjectId is int pp && !await _db.PlanningSjabloon.AnyAsync(x => x.Id == pp)) fouten.Add("Het gekozen planningssjabloon voor het project zelf bestaat niet meer.");
        var startNa = string.IsNullOrWhiteSpace(s.WoningenStartNa) ? null : s.WoningenStartNa.Trim().ToUpperInvariant();
        if (startNa != null && !await _db.Bouwstaat.AnyAsync(x => x.Code == startNa)) fouten.Add("De bouwstaat waarna de woningen starten bestaat niet.");
        if (s.TrajectSjabloonId is int t && !await _db.TrajectSjabloon.AnyAsync(x => x.Id == t)) fouten.Add("Het gekozen trajectsjabloon bestaat niet meer.");
        if (s.SchijfSjabloonId is int c && !await _db.SchijfSjabloon.AnyAsync(x => x.Id == c)) fouten.Add("Het gekozen schijvensjabloon bestaat niet meer.");
        if (s.SchijfSjabloonBijgebouwId is int cb && !await _db.SchijfSjabloon.AnyAsync(x => x.Id == cb)) fouten.Add("Het gekozen schijvensjabloon voor parkings en bergingen bestaat niet meer.");
        if (fouten.Count == 0 && await _db.ProjectSjabloon.AnyAsync(x => x.Id != s.Id && x.Naam == naam)) fouten.Add("Er bestaat al een projectsjabloon met deze naam.");
        if (fouten.Count > 0) return new ProjectSjabloonBewerkUitkomst { Fouten = fouten };

        ProjectSjabloon e;
        if (s.Id > 0)
        {
            e = await _db.ProjectSjabloon.FirstOrDefaultAsync(x => x.Id == s.Id) ?? throw new InvalidOperationException("Het sjabloon bestaat niet meer.");
            e.ModifiedDate = DateTime.UtcNow; e.ModifiedByUserId = userId;
        }
        else
        {
            e = new ProjectSjabloon { CreatedDate = DateTime.UtcNow, CreatedByUserId = userId };
            _db.ProjectSjabloon.Add(e);
        }
        e.Naam = naam; e.Beschrijving = string.IsNullOrWhiteSpace(s.Beschrijving) ? null : s.Beschrijving.Trim();
        e.PlanningSjabloonId = s.PlanningSjabloonId; e.TrajectSjabloonId = s.TrajectSjabloonId; e.SchijfSjabloonId = s.SchijfSjabloonId; e.SchijfSjabloonBijgebouwId = s.SchijfSjabloonBijgebouwId;
        e.PlanningSjabloonProjectId = s.PlanningSjabloonProjectId; e.WoningenStartNa = startNa;
        e.StandaardVerdiepingen = s.StandaardVerdiepingen; e.StandaardKelder = s.StandaardKelder; e.StandaardDak = dak; e.IsActief = s.IsActief;
        await _db.SaveChangesAsync();
        return new ProjectSjabloonBewerkUitkomst { Ok = true, Id = e.Id };
    }

    public async Task<string?> Verwijder(int id)
    {
        var s = await _db.ProjectSjabloon.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return "Het sjabloon bestaat niet meer.";
        _db.ProjectSjabloon.Remove(s);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> ZetActief(int id, bool actief, string? userId)
    {
        var s = await _db.ProjectSjabloon.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return "Het sjabloon bestaat niet meer.";
        s.IsActief = actief; s.ModifiedDate = DateTime.UtcNow; s.ModifiedByUserId = userId;
        await _db.SaveChangesAsync();
        return null;
    }

    // ── AI: anonieme context en controle ──

    public async Task<ProjectSjabloonAiContext> AiContext(int projectId)
    {
        var ctx = new ProjectSjabloonAiContext();
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => new { p.ExecutionDays }).FirstOrDefaultAsync();
        ctx.MaxWerkdagen = project?.ExecutionDays is > 0 ? project.ExecutionDays : null;
        ctx.AantalEenheden = await _db.Units.AsNoTracking().CountAsync(u => u.ProjectId == projectId);
        ctx.HeeftPlanning = await _db.PlanningSectie.AsNoTracking().AnyAsync(s => s.ProjectId == projectId && s.IsActief);
        ctx.HeeftTraject = await _db.Projecttraject.AsNoTracking().AnyAsync(t => t.ProjectId == projectId);
        ctx.HeeftSchijven = await _db.InvoicingPaymentStages.AsNoTracking().AnyAsync(s => s.Group.ProjectId == projectId);

        var rijen = await BeheerLijst();
        var planningen = await _db.PlanningSjabloon.AsNoTracking().Include(s => s.Blokken).ThenInclude(b => b.Taken).ToDictionaryAsync(s => s.Id);
        foreach (var r in rijen.Where(x => x.IsActief))
        {
            var k = new ProjectSjabloonAiKeuze { Id = r.Id, Naam = r.Naam, Beschrijving = r.Beschrijving, Planning = null, Traject = r.TrajectKern, Schijven = r.SchijvenKern };
            var sj = await _db.ProjectSjabloon.AsNoTracking().FirstAsync(x => x.Id == r.Id);
            k.Bouwlagen = Math.Max(1, sj.StandaardVerdiepingen); k.Kelder = sj.StandaardKelder; k.Dak = sj.StandaardDak;
            if (sj.PlanningSjabloonId is int pid && planningen.TryGetValue(pid, out var pl))
            {
                bool Heeft(string v) => PlanningSjabloonUitrol.GebruiktVariabele(pl, v);
                k.HeeftBouwlagen = pl.Blokken.Any(b => b.Herhaling >= 1);
                k.HeeftKelder = Heeft("kelder"); k.HeeftDak = Heeft("dak"); k.HeeftTerras = Heeft("terras");
                k.Planning = r.PlanningKern + ": " + string.Join(", ", pl.Blokken.OrderBy(b => b.Volgorde).Select(b => b.Naam));
            }
            ctx.Keuzes.Add(k);
        }
        return ctx;
    }

    public async Task<string?> Controle(int projectId, int sjabloonId, ProjectSjabloonAntwoorden antwoorden)
    {
        var sj = await _db.ProjectSjabloon.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sjabloonId && x.IsActief);
        if (sj?.PlanningSjabloonId is not int pid) return null;
        var pl = await _db.PlanningSjabloon.AsNoTracking().Include(x => x.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).FirstOrDefaultAsync(x => x.Id == pid);
        if (pl == null) return null;
        var a = Schoon(antwoorden);
        var eenheden = await _db.Units.AsNoTracking().CountAsync(u => u.ProjectId == projectId);
        var woningen = a.EenPlanningVoorAlles ? 1 : Math.Clamp(Math.Max(1, eenheden), 1, 20);
        var proef = PlanningSjabloonProef.Rekenen(pl, woningen, new SjabloonVariabelen(a.Verdiepingen, a.Kelder, a.Dak, a.Terrassen));
        var wd = woningen > 1 ? proef.WerkdagenAlle : proef.Werkdagen;
        if (wd <= 0) return null;
        var max = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.ExecutionDays).FirstOrDefaultAsync();
        var tekst = woningen > 1 ? $"Alle {woningen} woningen samen, met de estafette, duren ongeveer {wd} werkdagen." : $"Dit bouwdeel duurt ongeveer {wd} werkdagen.";
        if (max is > 0)
            tekst += wd <= max
                ? $" De uitvoeringstermijn van het project is {max} werkdagen: dat past, met {max - wd} werkdagen marge."
                : $" De uitvoeringstermijn van het project is {max} werkdagen: dit plan zit er {wd - max} werkdagen boven.";
        else tekst += " Het project heeft nog geen uitvoeringstermijn om tegen te vergelijken.";
        return tekst + " (Berekend met de werkkalender, zonder weerverlet; de startdatum volgt later.)";
    }

    // ── toepassen ──

    private static ProjectSjabloonAntwoorden Schoon(ProjectSjabloonAntwoorden a)
    {
        var dak = string.IsNullOrWhiteSpace(a?.Dak) ? null : a.Dak.Trim().ToLowerInvariant();
        return new ProjectSjabloonAntwoorden
        {
            Verdiepingen = Math.Max(1, Math.Min(a?.Verdiepingen ?? 1, 20)), Kelder = a?.Kelder ?? false,
            Dak = dak is "hellend" or "plat" ? dak : null, EenPlanningVoorAlles = a?.EenPlanningVoorAlles ?? false,
            Terrassen = (a?.Terrassen ?? new List<int>()).Where(t => t >= 1).Distinct().OrderBy(t => t).Take(20).ToList(),
            InHoofdplanning = a?.InHoofdplanning ?? false,
        };
    }

    /// <summary>
    /// Betalingsgroepen waarvan alle eenheden een parking of berging zijn: eenheden met een type buiten de typegroepen woning (1) en commercieel (4), zoals de documentenmodule ze ook onderscheidt.
    /// Een groep met ook maar één woning, een commerciële ruimte of een eenheid zonder type telt als gewone groep.
    /// </summary>
    private async Task<HashSet<int>> BijgebouwGroepen(int projectId)
    {
        var eenheden = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId && u.PaymentGroupId != null)
            .Select(u => new { Groep = u.PaymentGroupId!.Value, TypeGroep = u.Type != null ? (int?)u.Type.GroupId : null }).ToListAsync();
        return eenheden.GroupBy(e => e.Groep).Where(g => g.All(e => e.TypeGroep is int t && t != 1 && t != 4)).Select(g => g.Key).ToHashSet();
    }

    private static string Lijstje(IEnumerable<string> namen, int max = 5)
    {
        var l = namen.ToList();
        return string.Join(", ", l.Take(max)) + (l.Count > max ? $" en {l.Count - max} andere" : "");
    }

    public Task<ProjectSjabloonResultaat> Voorbeeld(int projectId, int sjabloonId, ProjectSjabloonAntwoorden antwoorden)
        => Verwerk(projectId, sjabloonId, antwoorden, uitvoeren: false, userId: null);

    public Task<ProjectSjabloonResultaat> Toepassen(int projectId, int sjabloonId, ProjectSjabloonAntwoorden antwoorden, string? userId)
        => Verwerk(projectId, sjabloonId, antwoorden, uitvoeren: true, userId);

    private async Task<ProjectSjabloonResultaat> Verwerk(int projectId, int sjabloonId, ProjectSjabloonAntwoorden antwoorden, bool uitvoeren, string? userId)
    {
        var res = new ProjectSjabloonResultaat();
        var sj = await _db.ProjectSjabloon.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sjabloonId && x.IsActief);
        if (sj == null) { res.Fout = "Het projectsjabloon bestaat niet meer of is inactief."; return res; }
        if (!await _db.Project.AnyAsync(p => p.ProjectId == projectId)) { res.Fout = "Het project bestaat niet meer."; return res; }
        var a = Schoon(antwoorden);

        // ── planning van het project zelf (hoofdplanning, bv. de wegenis): enkel als de hoofdplanning nog leeg is
        DateOnly? woningenStart = null;
        if (sj.PlanningSjabloonProjectId is int projectPlanningId)
        {
            var projNaam = await _db.PlanningSjabloon.AsNoTracking().Where(x => x.Id == projectPlanningId).Select(x => x.Naam).FirstOrDefaultAsync();
            if (projNaam == null) res.Opmerkingen.Add("Het planningssjabloon voor het project zelf bestaat niet meer: er komt geen planning voor het project.");
            else
            {
                // De hoofdplanning wordt pas bij de eerste schrijfactie aangemaakt: bestaat ze nog niet, dan is ze leeg (tenzij er losse fasen zonder planning zijn).
                var hp = await _db.BouwPlanning.AsNoTracking().Where(b => b.ProjectId == projectId && b.IsHoofd && b.IsActief).Select(b => new { b.Id }).FirstOrDefaultAsync();
                var hpId = hp?.Id;
                var hpLeeg = !await _db.PlanningSectie.AnyAsync(s => s.ProjectId == projectId && s.IsActief && (s.BouwPlanningId == null || (hpId != null && s.BouwPlanningId == hpId)));
                if (!hpLeeg) res.Opmerkingen.Add($"De hoofdplanning heeft al fasen of taken: de planning voor het project zelf ({projNaam}) is niet toegepast.");
                else if (!uitvoeren) res.Stappen.Add($"Planning “{projNaam}” komt in de hoofdplanning (voor het project zelf)." + (string.IsNullOrEmpty(sj.WoningenStartNa) ? "" : " De woningen starten daarna: zie de startdatum van hun planning."));
                else
                {
                    if (hpId == null)
                    {
                        var nieuweHoofd = new BouwPlanning { ProjectId = projectId, Naam = "Hoofdplanning", IsHoofd = true, Volgorde = 0, IsActief = true, CreatedDate = DateTime.UtcNow };
                        _db.BouwPlanning.Add(nieuweHoofd);
                        await _db.SaveChangesAsync();
                        hpId = nieuweHoofd.Id;
                    }
                    var staat0 = await _planning.Laad(projectId, 0);
                    var uit0 = await _planning.Voer(projectId, new PlanningCommando
                    {
                        Type = "sjabloonToepassen", PlanningId = 0, Versie = staat0.Versie,
                        Toepassing = new PlanningSjabloonToepassing { SjabloonId = projectPlanningId, Planningen = new List<PlanningToepassingPlanning> { new() { PlanningId = hpId, Verdiepingen = 1 } } },
                    }, userId);
                    if (!uit0.Ok) { res.Fout = "De planning voor het project zelf kon niet gemaakt worden: " + uit0.Fout; return res; }
                    res.Stappen.Add($"Planning “{projNaam}” toegepast in de hoofdplanning.");
                    foreach (var w in uit0.Waarschuwingen ?? new List<string>()) res.Opmerkingen.Add(w);

                    // De woningen starten de dag na de laatste taak die de gekozen bouwstaat oplevert (bv. na de verharding van de wegenis)
                    if (!string.IsNullOrEmpty(sj.WoningenStartNa))
                    {
                        var bsId = await _db.Bouwstaat.AsNoTracking().Where(b => b.Code == sj.WoningenStartNa && b.ProjectId == null).Select(b => (int?)b.Id).FirstOrDefaultAsync();
                        var staat1 = await _planning.Laad(projectId, hpId);
                        var taakIds = staat1.Rijen.Where(r => r.Soort == "taak").Select(r => r.Id).ToList();
                        var levert = bsId == null ? new HashSet<int>() : (await _db.PlanningTaak.AsNoTracking().Where(t => taakIds.Contains(t.Id) && t.BouwstaatId == bsId).Select(t => t.Id).ToListAsync()).ToHashSet();
                        var einden = staat1.Rijen.Where(r => r.Soort == "taak" && levert.Contains(r.Id) && r.Einde != null)
                            .Select(r => DateOnly.TryParse(r.Einde, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : (DateOnly?)null)
                            .Where(d => d != null).Select(d => d!.Value).ToList();
                        if (einden.Count == 0) res.Opmerkingen.Add("Geen taak in de planning van het project levert de bouwstaat waarna de woningen starten: de woningen starten op de aanvangsdatum van de bouwwerken.");
                        else
                        {
                            woningenStart = new WerkKalender().OpOfNa(einden.Max().AddDays(1));   // de eerstvolgende werkdag (verlof en verlet vangt de planning zelf op)
                            res.Stappen.Add($"De woningen starten op {woningenStart:dd/MM/yyyy}, de dag na “{sj.WoningenStartNa}” in de planning van het project. Je kan de startdatum per planning aanpassen.");
                        }
                    }
                }
            }
        }

        // ── planning: eenheden zonder planning
        if (sj.PlanningSjabloonId is int planningSjabloonId)
        {
            var planningIds = await _db.BouwPlanning.AsNoTracking().Where(b => b.ProjectId == projectId && b.IsActief).Select(b => b.Id).ToListAsync();
            var metPlanning = (await _db.BouwPlanningEenheid.AsNoTracking().Where(e => planningIds.Contains(e.BouwPlanningId)).Select(e => e.UnitId).ToListAsync()).ToHashSet();
            var eenheden = (await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).Select(u => new { u.Id, u.Name }).ToListAsync())
                .Where(u => !metPlanning.Contains(u.Id)).ToList();
            var planningNaam = await _db.PlanningSjabloon.AsNoTracking().Where(x => x.Id == planningSjabloonId).Select(x => x.Naam).FirstOrDefaultAsync();
            if (planningNaam == null) res.Opmerkingen.Add("Het planningssjabloon van dit projectsjabloon bestaat niet meer: er komt geen planning.");
            else if (eenheden.Count == 0) res.Opmerkingen.Add("Er zijn geen eenheden zonder planning (of er zijn nog geen eenheden): er komt geen nieuwe planning.");
            else
            {
                // In de hoofdplanning kan enkel als die nog leeg is (geen fasen); anders een aparte planning.
                if (a.EenPlanningVoorAlles && a.InHoofdplanning) await _planning.Laad(projectId, 0);   // zorgt dat de hoofdplanning bestaat
                var hoofd = a.EenPlanningVoorAlles && a.InHoofdplanning
                    ? await _db.BouwPlanning.AsNoTracking().Where(b => b.ProjectId == projectId && b.IsHoofd && b.IsActief).Select(b => new { b.Id }).FirstOrDefaultAsync()
                    : null;
                // Verwijderde (inactieve) fasen tellen niet mee: de hoofdplanning is dan voor de gebruiker leeg.
                var hoofdLeeg = hoofd != null && !await _db.PlanningSectie.AnyAsync(s => s.ProjectId == projectId && s.IsActief && (s.BouwPlanningId == hoofd.Id || s.BouwPlanningId == null));
                var inHoofd = hoofd != null && hoofdLeeg;
                if (a.EenPlanningVoorAlles && a.InHoofdplanning && !inHoofd)
                    res.Opmerkingen.Add(hoofd == null
                        ? "Er is nog geen hoofdplanning voor dit project: de planning komt in een aparte planning."
                        : "De hoofdplanning heeft al fasen of taken: de planning komt in een aparte planning.");
                var rijen = inHoofd
                    ? new List<PlanningToepassingPlanning> { new() { PlanningId = hoofd!.Id, Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak, Terrassen = a.Terrassen } }
                    : a.EenPlanningVoorAlles
                    ? new List<PlanningToepassingPlanning> { new() { EenheidIds = eenheden.Select(u => u.Id).ToList(), Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak, Terrassen = a.Terrassen } }
                    : eenheden.Select(u => new PlanningToepassingPlanning { EenheidIds = new List<int> { u.Id }, Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak, Terrassen = a.Terrassen }).ToList();
                if (woningenStart != null && !inHoofd) foreach (var r in rijen) r.StartDatum = woningenStart.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                var tekst = inHoofd ? $"in de hoofdplanning, voor {eenheden.Count} {(eenheden.Count == 1 ? "eenheid" : "eenheden")} ({Lijstje(eenheden.Select(u => u.Name ?? ""))})"
                          : a.EenPlanningVoorAlles ? $"één planning voor {eenheden.Count} {(eenheden.Count == 1 ? "eenheid" : "eenheden")} ({Lijstje(eenheden.Select(u => u.Name ?? ""))})"
                                                   : $"een planning voor elk van de {eenheden.Count} {(eenheden.Count == 1 ? "eenheid" : "eenheden")} zonder planning ({Lijstje(eenheden.Select(u => u.Name ?? ""))})";
                if (!uitvoeren) res.Stappen.Add($"Planning “{planningNaam}”: {tekst}.");
                else
                {
                    var staat = await _planning.Laad(projectId, 0);
                    var uit = await _planning.Voer(projectId, new PlanningCommando
                    {
                        Type = "sjabloonToepassen", PlanningId = 0, Versie = staat.Versie,
                        Toepassing = new PlanningSjabloonToepassing { SjabloonId = planningSjabloonId, Planningen = rijen },
                    }, userId);
                    if (!uit.Ok) { res.Fout = "De planning kon niet gemaakt worden: " + uit.Fout; return res; }
                    res.Stappen.Add($"Planning “{planningNaam}” toegepast: {tekst}.");
                    foreach (var w in uit.Waarschuwingen ?? new List<string>()) res.Opmerkingen.Add(w);
                }
            }
        }

        // ── traject
        if (sj.TrajectSjabloonId is int trajectSjabloonId)
        {
            var trajectNaam = await _db.TrajectSjabloon.AsNoTracking().Where(x => x.Id == trajectSjabloonId).Select(x => x.Naam).FirstOrDefaultAsync();
            if (trajectNaam == null) res.Opmerkingen.Add("Het trajectsjabloon van dit projectsjabloon bestaat niet meer: er komt geen traject.");
            else if (await _db.Projecttraject.AnyAsync(t => t.ProjectId == projectId)) res.Opmerkingen.Add("Het project heeft al een traject: dat blijft ongewijzigd.");
            else if (!uitvoeren) res.Stappen.Add($"Traject “{trajectNaam}” wordt aangemaakt.");
            else
            {
                try
                {
                    await _traject.Instantiate(new BOCore.TrajectInstantiatieBO { ProjectId = projectId, TrajectSjabloonId = trajectSjabloonId }, userId);
                    res.Stappen.Add($"Traject “{trajectNaam}” aangemaakt.");
                }
                catch (Exception ex) { res.Opmerkingen.Add("Het traject kon niet aangemaakt worden: " + ex.Message); }
            }
        }

        // ── schijven: betalingsgroepen zonder (gebruikte) schijven.
        // Een groep met enkel parkings en bergingen krijgt het schijvensjabloon voor bijgebouwen (als dat gekozen is), alle andere groepen het gewone.
        if (sj.SchijfSjabloonId is int || sj.SchijfSjabloonBijgebouwId is int)
        {
            var hoofd = sj.SchijfSjabloonId is int hid ? await _schijven.Voorbeeld(hid, a.Verdiepingen, a.Kelder, a.Dak) : null;
            var bij = sj.SchijfSjabloonBijgebouwId is int bid ? await _schijven.Voorbeeld(bid, a.Verdiepingen, a.Kelder, a.Dak) : null;
            if (sj.SchijfSjabloonId != null && hoofd == null) res.Opmerkingen.Add("Het schijvensjabloon van dit projectsjabloon bestaat niet meer of is leeg: er komen geen schijven voor de gewone groepen.");
            if (sj.SchijfSjabloonBijgebouwId != null && bij == null) res.Opmerkingen.Add("Het schijvensjabloon voor parkings en bergingen bestaat niet meer of is leeg: die groepen krijgen het gewone schijvensjabloon.");
            if (hoofd != null) res.Schijven = hoofd.Schijven;
            foreach (var w in (hoofd?.Waarschuwingen ?? new List<string>()).Concat(bij?.Waarschuwingen ?? new List<string>())) res.Opmerkingen.Add(w);

            var groepen = await _db.InvoicingPaymentGroup.Include(g => g.InvoicingPaymentStages).Where(g => g.ProjectId == projectId).OrderBy(g => g.Name).ToListAsync();
            var stageIds = groepen.SelectMany(g => g.InvoicingPaymentStages.Select(s => s.Id)).ToList();
            var bereikt = (await _db.UnitPaymentStageReached.AsNoTracking().Where(r => stageIds.Contains(r.PaymentStageId)).Select(r => r.PaymentStageId).ToListAsync()).ToHashSet();
            var gefactureerd = (await _db.InvoicesDetails.AsNoTracking().Where(x => x.PaymentStageId != null && stageIds.Contains(x.PaymentStageId.Value)).Select(x => x.PaymentStageId!.Value).ToListAsync()).ToHashSet();
            bool Leeg(InvoicingPaymentGroup g) => g.InvoicingPaymentStages.Count <= 1 && !g.InvoicingPaymentStages.Any(s => bereikt.Contains(s.Id) || gefactureerd.Contains(s.Id));
            var bijgebouwGroepen = await BijgebouwGroepen(projectId);

            if (groepen.Count == 0) res.Opmerkingen.Add("Er zijn nog geen betalingsgroepen: de schijven kan je later toepassen.");
            else
            {
                var bezet = groepen.Count(g => !Leeg(g));
                if (bezet > 0) res.Opmerkingen.Add($"{bezet} {(bezet == 1 ? "betalingsgroep heeft" : "betalingsgroepen hebben")} al schijven en blijven ongemoeid.");

                // Per lege groep het voorstel dat erbij hoort
                var toe = new List<(InvoicingPaymentGroup Groep, SchijfVoorbeeld Voorstel, bool Bijgebouw)>();
                foreach (var g in groepen.Where(Leeg))
                {
                    var isBij = bijgebouwGroepen.Contains(g.Id) && bij != null;
                    var v = isBij ? bij : hoofd;
                    if (v == null) { res.Opmerkingen.Add($"Voor “{g.Name}” is er geen schijvensjabloon gekozen: er komen geen schijven."); continue; }
                    if (Math.Abs(v.Totaal - 100m) > 0.005m) { res.Opmerkingen.Add($"De schijven voor “{g.Name}” komen samen op {v.Totaal:0.##} % uit in plaats van 100 %: ze zijn niet toegepast. Pas het schijvensjabloon aan."); continue; }
                    toe.Add((g, v, isBij));
                }
                if (toe.Count == 0) { if (groepen.Any(Leeg)) { /* opmerkingen staan er al */ } else res.Opmerkingen.Add("Geen betalingsgroep zonder schijven: er zijn geen schijven toegepast."); }
                else
                {
                    foreach (var deel in toe.GroupBy(x => x.Bijgebouw))
                    {
                        var namen = Lijstje(deel.Select(x => x.Groep.Name ?? ""));
                        var aantal = deel.First().Voorstel.Schijven.Count;
                        res.Stappen.Add(uitvoeren
                            ? $"Schijven: {aantal} schijven gezet in {namen}{(deel.Key ? " (parkings en bergingen)" : "")}."
                            : $"Schijven: {aantal} schijven in {namen}{(deel.Key ? " (parkings en bergingen)" : "")}.");
                    }
                    if (uitvoeren)
                    {
                        foreach (var (g, v, _) in toe)
                        {
                            _db.InvoicingPaymentStages.RemoveRange(g.InvoicingPaymentStages.ToList());
                            foreach (var s in v.Schijven)
                                g.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Name = s.Naam, Percentage = s.Percentage, Invoicable = false, PlanningCode = s.PlanningCode, PlanningVolgnr = s.PlanningVolgnr, BouwstaatId = s.BouwstaatId, BouwstaatRegel = s.BouwstaatRegel });
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
        }

        // Codes uit de sjablonen: schijven en mijlpalen hangen nu aan de taken en fases van de planning
        if (uitvoeren)
        {
            var schijfKoppelingen = await PlanningSchijfAutoKoppeling.KoppelOpCode(_db, projectId);
            if (schijfKoppelingen > 0) res.Stappen.Add($"{schijfKoppelingen} {(schijfKoppelingen == 1 ? "schijf hangt" : "schijven hangen")} nu aan een taak of fase in de planning.");
            await PlanningTrajectKoppeling.KoppelOpCode(_db, projectId);
        }

        if (res.Stappen.Count == 0 && res.Opmerkingen.Count == 0) res.Opmerkingen.Add("Er valt niets toe te passen.");
        res.Ok = true;
        return res;
    }
}
