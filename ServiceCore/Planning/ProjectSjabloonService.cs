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
        var planning = await _db.PlanningSjabloon.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Naam);
        var traject = await _db.TrajectSjabloon.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Naam);
        var schijven = await _db.SchijfSjabloon.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Naam);
        return lijst.Select(s => new ProjectSjabloonBeheerRij
        {
            Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, IsActief = s.IsActief, GewijzigdOp = s.ModifiedDate ?? s.CreatedDate,
            PlanningNaam = s.PlanningSjabloonId is int p && planning.TryGetValue(p, out var pn) ? pn : null,
            TrajectNaam = s.TrajectSjabloonId is int t && traject.TryGetValue(t, out var tn) ? tn : null,
            SchijvenNaam = s.SchijfSjabloonId is int c && schijven.TryGetValue(c, out var cn) ? cn : null,
        }).ToList();
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
                var rijen = a.EenPlanningVoorAlles
                    ? new List<PlanningToepassingPlanning> { new() { EenheidIds = eenheden.Select(u => u.Id).ToList(), Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak } }
                    : eenheden.Select(u => new PlanningToepassingPlanning { EenheidIds = new List<int> { u.Id }, Verdiepingen = a.Verdiepingen, Kelder = a.Kelder, Dak = a.Dak }).ToList();
                var tekst = a.EenPlanningVoorAlles ? $"één planning voor {eenheden.Count} {(eenheden.Count == 1 ? "eenheid" : "eenheden")} ({Lijstje(eenheden.Select(u => u.Name ?? ""))})"
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
