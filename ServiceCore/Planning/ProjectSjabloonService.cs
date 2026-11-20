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
        var planningen = await _db.PlanningSjabloon.AsNoTracking().Include(s => s.Blokken).ThenInclude(b => b.Taken).ToDictionaryAsync(s => s.Id);
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
            var pl = await _db.PlanningSjabloon.AsNoTracking().Include(x => x.Blokken).ThenInclude(b => b.Taken).FirstOrDefaultAsync(x => x.Id == pid);
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
                    PerVerdieping = pl.Blokken.Any(b => b.Herhaling == 1), Fases = blokken.Count, Verloop = verloop,
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
        => (await _db.ProjectSjabloon.AsNoTracking().Where(s => s.IsActief).OrderBy(s => s.Naam).ToListAsync())
            .Select(s => new ProjectSjabloonKeuze { Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, Verdiepingen = Math.Max(1, s.StandaardVerdiepingen), Kelder = s.StandaardKelder, Dak = s.StandaardDak })
            .ToList();

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
            SchijfSjabloonId = s.SchijfSjabloonId, StandaardVerdiepingen = s.StandaardVerdiepingen, StandaardKelder = s.StandaardKelder, StandaardDak = s.StandaardDak, IsActief = s.IsActief,
        };
    }

    public async Task<ProjectSjabloonBewerkUitkomst> Bewaar(ProjectSjabloonBewerk s, string? userId)
    {
        var fouten = new List<string>();
        var naam = (s.Naam ?? "").Trim();
        if (naam.Length == 0) fouten.Add("Geef het sjabloon een naam.");
        if (naam.Length > 200) fouten.Add("De naam is te lang.");
        if (s.PlanningSjabloonId == null && s.TrajectSjabloonId == null && s.SchijfSjabloonId == null) fouten.Add("Kies minstens één sjabloon: planning, traject of schijven.");
        if (s.StandaardVerdiepingen < 1 || s.StandaardVerdiepingen > 20) fouten.Add("Het standaard aantal verdiepingen ligt tussen 1 en 20.");
        var dak = string.IsNullOrWhiteSpace(s.StandaardDak) ? null : s.StandaardDak.Trim().ToLowerInvariant();
        if (dak != null && dak is not ("hellend" or "plat")) fouten.Add("Het dak is hellend of plat.");
        if (s.PlanningSjabloonId is int p && !await _db.PlanningSjabloon.AnyAsync(x => x.Id == p)) fouten.Add("Het gekozen planningssjabloon bestaat niet meer.");
        if (s.TrajectSjabloonId is int t && !await _db.TrajectSjabloon.AnyAsync(x => x.Id == t)) fouten.Add("Het gekozen trajectsjabloon bestaat niet meer.");
        if (s.SchijfSjabloonId is int c && !await _db.SchijfSjabloon.AnyAsync(x => x.Id == c)) fouten.Add("Het gekozen schijvensjabloon bestaat niet meer.");
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
        e.PlanningSjabloonId = s.PlanningSjabloonId; e.TrajectSjabloonId = s.TrajectSjabloonId; e.SchijfSjabloonId = s.SchijfSjabloonId;
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

    // ── toepassen ──

    private static ProjectSjabloonAntwoorden Schoon(ProjectSjabloonAntwoorden a)
    {
        var dak = string.IsNullOrWhiteSpace(a?.Dak) ? null : a.Dak.Trim().ToLowerInvariant();
        return new ProjectSjabloonAntwoorden
        {
            Verdiepingen = Math.Max(1, Math.Min(a?.Verdiepingen ?? 1, 20)), Kelder = a?.Kelder ?? false,
            Dak = dak is "hellend" or "plat" ? dak : null, EenPlanningVoorAlles = a?.EenPlanningVoorAlles ?? false,
            InHoofdplanning = a?.InHoofdplanning ?? false,
        };
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
                var hoofdLeeg = hoofd != null && !await _db.PlanningSectie.AnyAsync(s => s.ProjectId == projectId && (s.BouwPlanningId == hoofd.Id || s.BouwPlanningId == null));
                var inHoofd = hoofd != null && hoofdLeeg;
                if (a.EenPlanningVoorAlles && a.InHoofdplanning && !inHoofd)
                    res.Opmerkingen.Add("De hoofdplanning heeft al fasen of taken: de planning komt in een aparte planning.");
                var rijen = inHoofd
                    ? new List<PlanningToepassingPlanning> { new() { PlanningId = hoofd!.Id, Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak } }
                    : a.EenPlanningVoorAlles
                    ? new List<PlanningToepassingPlanning> { new() { EenheidIds = eenheden.Select(u => u.Id).ToList(), Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak } }
                    : eenheden.Select(u => new PlanningToepassingPlanning { EenheidIds = new List<int> { u.Id }, Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak }).ToList();
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

        // ── schijven: betalingsgroepen zonder (gebruikte) schijven
        if (sj.SchijfSjabloonId is int schijfSjabloonId)
        {
            var voorstel = await _schijven.Voorbeeld(schijfSjabloonId, a.Verdiepingen, a.Kelder, a.Dak);
            if (voorstel == null) res.Opmerkingen.Add("Het schijvensjabloon van dit projectsjabloon bestaat niet meer of is leeg: er komen geen schijven.");
            else
            {
                res.Schijven = voorstel.Schijven;
                foreach (var w in voorstel.Waarschuwingen) res.Opmerkingen.Add(w);
                var groepen = await _db.InvoicingPaymentGroup.Include(g => g.InvoicingPaymentStages).Where(g => g.ProjectId == projectId).OrderBy(g => g.Name).ToListAsync();
                var stageIds = groepen.SelectMany(g => g.InvoicingPaymentStages.Select(s => s.Id)).ToList();
                var bereikt = (await _db.UnitPaymentStageReached.AsNoTracking().Where(r => stageIds.Contains(r.PaymentStageId)).Select(r => r.PaymentStageId).ToListAsync()).ToHashSet();
                var gefactureerd = (await _db.InvoicesDetails.AsNoTracking().Where(x => x.PaymentStageId != null && stageIds.Contains(x.PaymentStageId.Value)).Select(x => x.PaymentStageId!.Value).ToListAsync()).ToHashSet();
                bool Leeg(InvoicingPaymentGroup g) => g.InvoicingPaymentStages.Count <= 1 && !g.InvoicingPaymentStages.Any(s => bereikt.Contains(s.Id) || gefactureerd.Contains(s.Id));
                var leeg = groepen.Where(Leeg).ToList();
                var bezet = groepen.Count - leeg.Count;
                if (groepen.Count == 0) res.Opmerkingen.Add("Er zijn nog geen betalingsgroepen: de schijven kan je later toepassen.");
                else
                {
                    if (bezet > 0) res.Opmerkingen.Add($"{bezet} {(bezet == 1 ? "betalingsgroep heeft" : "betalingsgroepen hebben")} al schijven en blijven ongemoeid.");
                    if (leeg.Count > 0 && Math.Abs(voorstel.Totaal - 100m) > 0.005m)
                        res.Opmerkingen.Add($"De schijven uit het sjabloon komen samen op {voorstel.Totaal:0.##} % uit in plaats van 100 %: ze zijn niet toegepast. Pas het schijvensjabloon aan.");
                    else if (leeg.Count > 0 && !uitvoeren)
                        res.Stappen.Add($"Schijven: {voorstel.Schijven.Count} schijven in {Lijstje(leeg.Select(g => g.Name ?? ""))}.");
                    else if (leeg.Count > 0)
                    {
                        foreach (var g in leeg)
                        {
                            _db.InvoicingPaymentStages.RemoveRange(g.InvoicingPaymentStages.ToList());
                            foreach (var s in voorstel.Schijven)
                                g.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Name = s.Naam, Percentage = s.Percentage, Invoicable = false, PlanningCode = s.PlanningCode, PlanningVolgnr = s.PlanningVolgnr });
                        }
                        await _db.SaveChangesAsync();
                        res.Stappen.Add($"Schijven: {voorstel.Schijven.Count} schijven gezet in {Lijstje(leeg.Select(g => g.Name ?? ""))}.");
                    }
                    else res.Opmerkingen.Add("Geen betalingsgroep zonder schijven: er zijn geen schijven toegepast.");
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
