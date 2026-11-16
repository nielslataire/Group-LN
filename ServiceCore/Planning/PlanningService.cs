using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>
/// Bouwplanning van een project: laden, wijzigen en doorrekenen. De pagina stuurt een <see cref="PlanningCommando"/> en krijgt de
/// volledig herberekende staat terug. De rekenregels staan in <see cref="PlanningCalculator"/>, de werkdagen in <see cref="WerkKalender"/>.
/// Een fase is een <see cref="PlanningSectie"/>, een taak een <see cref="PlanningTaak"/> (duur 0 = mijlpaal). Start en einde van een taak
/// worden na elke wijziging herberekend en in <c>Startdatum</c>/<c>Einddatum</c> bewaard, zodat traject en dashboards ze kunnen lezen.
/// </summary>
public class PlanningService : IPlanningService
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");
    private static readonly Regex VoorgangerRegex = new(@"^\s*(H:)?(\d+(?:\.\d+)*)\s*(FS|SS|FF|SF)?\s*([+-]\s*\d+)?\s*d?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly cpmRunningContext _db;
    private readonly ITrajectRecalculationService? _traject;

    public PlanningService(cpmRunningContext db, ITrajectRecalculationService? traject = null)
    {
        _db = db;
        _traject = traject;
    }

    // ── Interne gegevens ──

    private sealed class Data
    {
        public required Project Project { get; init; }
        public ProjectPlanning? Kop { get; set; }
        public List<BouwPlanning> Planningen { get; set; } = new();
        public List<PlanningSectie> Secties { get; set; } = new();
        public List<PlanningTaak> Taken { get; set; } = new();
        public List<PlanningAfhankelijkheid> Links { get; set; } = new();
        public required WerkKalender Kalender { get; init; }
        public bool HeeftWeerstation { get; init; }

        /// <summary>Id van de hoofdplanning; 0 zolang die nog niet in de database staat (dan zit elke fase er virtueel in).</summary>
        public int HoofdId => Planningen.FirstOrDefault(p => p.IsHoofd)?.Id ?? 0;

        public int PlanningVan(PlanningSectie s) => s.BouwPlanningId ?? HoofdId;

        public int PlanningVanTaak(PlanningTaak t)
        {
            var s = Secties.FirstOrDefault(x => x.Id == t.PlanningSectieId);
            return s == null ? HoofdId : PlanningVan(s);
        }

        /// <summary>Een geldige planning: de gevraagde als die bestaat, anders de hoofdplanning.</summary>
        public int Doel(int? planningId)
            => planningId is int p && p != 0 && Planningen.Any(x => x.Id == p) ? p : HoofdId;
    }

    private sealed class Knoop
    {
        public bool IsFase;
        public PlanningSectie? Sectie;
        public PlanningTaak? Taak;
        public Knoop? Ouder;
        public List<Knoop> Kinderen = new();
        public string Wbs = "";
        public int Niveau;
        public int Id => IsFase ? Sectie!.Id : Taak!.Id;
        public int Volgorde => IsFase ? Sectie!.Volgorde : Taak!.Volgorde;
        public string Sleutel => (IsFase ? "f" : "t") + Id;
    }

    private async Task<Data?> LaadData(int projectId)
    {
        var project = await _db.Project.FirstOrDefaultAsync(p => p.ProjectId == projectId);
        if (project == null) return null;

        var planningen = await _db.BouwPlanning.Where(b => b.ProjectId == projectId && b.IsActief)
            .OrderByDescending(b => b.IsHoofd).ThenBy(b => b.Volgorde).ThenBy(b => b.Id).ToListAsync();
        var secties = await _db.PlanningSectie.Where(s => s.ProjectId == projectId && s.IsActief).ToListAsync();
        var sectieIds = secties.Select(s => s.Id).ToList();
        var taken = await _db.PlanningTaak.Where(t => sectieIds.Contains(t.PlanningSectieId)).ToListAsync();
        var taakIds = taken.Select(t => t.Id).ToList();
        var links = await _db.PlanningAfhankelijkheid.Where(l => taakIds.Contains(l.OpvolgerTaakId)).ToListAsync();
        var kop = await _db.ProjectPlanning.FirstOrDefaultAsync(k => k.ProjectId == projectId);

        return new Data
        {
            Project = project,
            Kop = kop,
            Planningen = planningen,
            Secties = secties,
            Taken = taken,
            Links = links,
            Kalender = await LaadKalender(projectId, project.WheaterStationId),
            HeeftWeerstation = (project.WheaterStationId ?? 0) != 0,
        };
    }

    /// <summary>Zelfde bronnen als <c>ProjectService.LaadWerkKalender</c>: feestdagen/bouwverlof van bedrijf + project en het verlet van het weerstation.</summary>
    private async Task<WerkKalender> LaadKalender(int projectId, int? weerstationId)
    {
        var verlof = await _db.VacationDays.AsNoTracking()
            .Where(v => v.ProjectId == null || v.ProjectId == projectId)
            .Select(v => v.VacationDay).ToListAsync();
        var verlet = (weerstationId ?? 0) == 0
            ? new List<DateOnly>()
            : await _db.BadWeatherDays.AsNoTracking()
                .Where(b => b.WeatherstationId == weerstationId)
                .Select(b => b.Date).Distinct().ToListAsync();
        return new WerkKalender(verlof, verlet);
    }

    // ── Boom ──

    private static List<Knoop> BouwBoom(Data d, int planningId)
    {
        int Soort(Knoop k) => k.IsFase ? 1 : 0;

        List<Knoop> Kinderen(PlanningSectie? ouder, Knoop? ouderKnoop)
        {
            var lijst = new List<Knoop>();
            if (ouder != null)
                lijst.AddRange(d.Taken.Where(t => t.PlanningSectieId == ouder.Id).Select(t => new Knoop { Taak = t, Ouder = ouderKnoop }));
            lijst.AddRange(d.Secties.Where(s => s.ParentSectieId == ouder?.Id && (ouder != null || d.PlanningVan(s) == planningId)).Select(s => new Knoop { IsFase = true, Sectie = s, Ouder = ouderKnoop }));
            lijst = lijst.OrderBy(k => k.Volgorde).ThenBy(Soort).ThenBy(k => k.Id).ToList();
            foreach (var k in lijst.Where(k => k.IsFase)) k.Kinderen = Kinderen(k.Sectie, k);
            return lijst;
        }

        var wortels = Kinderen(null, null);

        void Nummer(List<Knoop> lijst, string voorvoegsel, int niveau)
        {
            for (var i = 0; i < lijst.Count; i++)
            {
                lijst[i].Wbs = voorvoegsel + (i + 1);
                lijst[i].Niveau = niveau;
                if (lijst[i].IsFase) Nummer(lijst[i].Kinderen, lijst[i].Wbs + ".", niveau + 1);
            }
        }
        Nummer(wortels, "", 0);
        return wortels;
    }

    private static IEnumerable<Knoop> Plat(IEnumerable<Knoop> lijst)
    {
        foreach (var k in lijst)
        {
            yield return k;
            if (k.IsFase)
                foreach (var kind in Plat(k.Kinderen)) yield return kind;
        }
    }

    /// <summary>De bomen van alle plannings van het project (de hoofdplanning, ook de virtuele met Id 0, staat er altijd in).</summary>
    private static Dictionary<int, List<Knoop>> BouwBomen(Data d)
    {
        var ids = new List<int> { d.HoofdId };
        ids.AddRange(d.Planningen.Where(p => !p.IsHoofd).Select(p => p.Id));
        return ids.Distinct().ToDictionary(id => id, id => BouwBoom(d, id));
    }

    private static IEnumerable<Knoop> AllePlat(Dictionary<int, List<Knoop>> bomen) => bomen.Values.SelectMany(Plat);

    private static IEnumerable<Knoop> Taken(Knoop fase) => Plat(fase.Kinderen).Where(k => !k.IsFase);

    // ── Doorrekenen ──

    private sealed record Doorgerekend(PlanResultaat Resultaat, Dictionary<int, List<Knoop>> Bomen, bool MetVerlet, DateOnly ProjectStart);

    private static Doorgerekend Rekenen(Data d)
    {
        var bomen = BouwBomen(d);
        var metVerlet = d.Kop?.VerletMeetellen ?? true;
        var start = d.Project.StartDateConstruction ?? DateOnly.FromDateTime(DateTime.Today);
        var planTaken = new List<PlanTaak>();
        foreach (var (planningId, boom) in bomen.OrderBy(b => b.Key == d.HoofdId ? 0 : 1).ThenBy(b => b.Key))
        {
            var startVanaf = d.Planningen.FirstOrDefault(p => p.Id == planningId)?.StartDatum;
            var eerste = true;
            foreach (var k in Plat(boom).Where(k => !k.IsFase))
            {
                planTaken.Add(new PlanTaak(
                    k.Taak!.Id,
                    Math.Max(0, k.Taak.Duur),
                    d.Links.Where(l => l.OpvolgerTaakId == k.Taak.Id)
                           .Select(l => new PlanLink(l.VoorgangerTaakId, (PlanLinkType)l.Type, l.WachtDagen)).ToList(),
                    (PlanBeperking)k.Taak.BeperkingType,
                    k.Taak.BeperkingDatum,
                    k.Taak.Deadline,
                    k.Taak.WerkelijkStart,
                    k.Taak.IsAfgerond ? k.Taak.AfgerondOp : null,
                    k.Taak.IsAfgerond,
                    startVanaf,
                    eerste));
                eerste = false;
            }
        }

        var res = PlanningCalculator.Bereken(planTaken, start, d.Kalender, metVerlet);

        // Berekende datums bewaren (cache voor traject, dashboards en voortgang).
        foreach (var k in AllePlat(bomen).Where(k => !k.IsFase))
        {
            if (!res.Taken.TryGetValue(k.Taak!.Id, out var r)) continue;
            if (k.Taak.Startdatum != r.Start) k.Taak.Startdatum = r.Start;
            if (k.Taak.Einddatum != r.Einde) k.Taak.Einddatum = r.Einde;
        }
        return new Doorgerekend(res, bomen, metVerlet, start);
    }

    // ── Staat opbouwen ──

    public async Task<PlanningStaat> Laad(int projectId, int? planningId = null)
    {
        var d = await LaadData(projectId);
        if (d == null) return new PlanningStaat { ProjectId = projectId };
        return await Bouw(d, Rekenen(d), d.Doel(planningId));
    }

    private static string? Iso(DateOnly? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Dd(DateOnly d) => d.ToString("dd/MM/yyyy", Nl);

    private async Task<PlanningStaat> Bouw(Data d, Doorgerekend dr, int actief)
    {
        var res = dr.Resultaat;
        var kal = d.Kalender;
        var metVerlet = dr.MetVerlet;
        var boom = dr.Bomen[actief];
        var actieveTaken = Plat(boom).Where(k => !k.IsFase).Select(k => k.Taak!).ToList();
        var actieveRes = actieveTaken.Where(t => res.Taken.ContainsKey(t.Id)).Select(t => res.Taken[t.Id]).ToList();
        DateOnly? planStart = actieveRes.Count > 0 ? actieveRes.Min(r => r.Start) : null;
        DateOnly? planEinde = actieveRes.Count > 0 ? actieveRes.Max(r => r.Einde) : null;

        var staat = new PlanningStaat
        {
            ProjectId = d.Project.ProjectId,
            Versie = d.Kop?.Versie ?? 0,
            HeeftPlanning = boom.Count > 0,
            ActiefPlanningId = actief,
        };

        // WBS per taak-id, voor de weergave van voorgangers: die van de getoonde planning, en die van de hoofdplanning met "H:" ervoor.
        var wbsPerTaak = Plat(boom).Where(k => !k.IsFase).ToDictionary(k => k.Taak!.Id, k => k.Wbs);
        if (actief != d.HoofdId && dr.Bomen.TryGetValue(d.HoofdId, out var hoofdBoom))
            foreach (var k in Plat(hoofdBoom).Where(k => !k.IsFase))
                wbsPerTaak[k.Taak!.Id] = "H:" + k.Wbs;

        // Overzicht van alle plannings
        var bouwdelen = await _db.InvoicingPaymentGroup.AsNoTracking()
            .Where(g => g.ProjectId == d.Project.ProjectId).OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name }).ToListAsync();
        var eenhedenPerGroep = await _db.Units.AsNoTracking()
            .Where(u => u.ProjectId == d.Project.ProjectId && u.PaymentGroupId != null)
            .GroupBy(u => u.PaymentGroupId!.Value).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.N);
        var lijstPlannings = d.Planningen.Count > 0 ? d.Planningen : new List<BouwPlanning> { new() { Id = 0, Naam = "Hoofdplanning", IsHoofd = true } };
        foreach (var bp in lijstPlannings)
        {
            var info = new PlanningInfo
            {
                Id = bp.Id,
                Naam = bp.Naam,
                IsHoofd = bp.IsHoofd,
                BouwdeelId = bp.PaymentGroupId,
                BouwdeelNaam = bp.PaymentGroupId is int gid ? bouwdelen.FirstOrDefault(b => b.Id == gid)?.Name : null,
                StartDatum = Iso(bp.StartDatum),
            };
            if (dr.Bomen.TryGetValue(bp.Id, out var pb))
            {
                var ts = Plat(pb).Where(k => !k.IsFase && res.Taken.ContainsKey(k.Taak!.Id)).Select(k => res.Taken[k.Taak!.Id]).ToList();
                info.AantalTaken = ts.Count;
                if (ts.Count > 0) { info.Start = Iso(ts.Min(r => r.Start)); info.Einde = Iso(ts.Max(r => r.Einde)); }
            }
            staat.Planningen.Add(info);
        }
        foreach (var g in bouwdelen)
            staat.Bouwdelen.Add(new PlanningBouwdeelOptie
            {
                Id = g.Id,
                Naam = g.Name ?? "",
                AantalEenheden = eenhedenPerGroep.TryGetValue(g.Id, out var n) ? n : 0,
                PlanningId = d.Planningen.FirstOrDefault(p => p.PaymentGroupId == g.Id)?.Id,
            });

        // Gekoppelde trajectmijlpalen (de koppeling staat aan de trajectkant: BronBinding = PlanningTaak/PlanningSectie).
        var koppelingen = await _db.Mijlpaal.AsNoTracking()
            .Where(m => m.Projecttraject.ProjectId == d.Project.ProjectId && (m.BronBinding == 4 || m.BronBinding == 5))
            .Select(m => new { m.Id, m.Naam, m.BronBinding, m.BronRefId, m.BronParam })
            .ToListAsync();
        List<PlanningMijlpaalLink> LinksVoor(int binding, int id) => koppelingen
            .Where(m => m.BronBinding == binding && (m.BronRefId ?? (int.TryParse((m.BronParam ?? "").Trim(), out var x) ? x : (int?)null)) == id)
            .Select(m => new PlanningMijlpaalLink { Id = m.Id, Naam = m.Naam }).ToList();

        var lots = await _db.ActivityGroup.AsNoTracking().ToDictionaryAsync(g => g.GroupId, g => g.Name);

        foreach (var k in Plat(boom))
        {
            var rij = new PlanningRij { Sleutel = k.Sleutel, Id = k.Id, Wbs = k.Wbs, Niveau = k.Niveau, OuderFaseId = k.Ouder?.Sectie?.Id };
            if (k.IsFase)
            {
                var s = k.Sectie!;
                var kinderen = Taken(k).Where(t => res.Taken.ContainsKey(t.Taak!.Id)).ToList();
                rij.Soort = "fase";
                rij.Naam = s.Naam;
                rij.TeltMee = s.TeltMeeVoorVoortgang;
                rij.LotId = s.ActivityGroupId;
                rij.LotNaam = s.ActivityGroupId is int lid && lots.TryGetValue(lid, out var ln) ? ln : null;
                rij.Mijlpalen = LinksVoor(5, s.Id);
                if (kinderen.Count > 0)
                {
                    var start = kinderen.Min(t => res.Taken[t.Taak!.Id].Start);
                    var einde = kinderen.Max(t => res.Taken[t.Taak!.Id].Einde);
                    rij.Start = Iso(start);
                    rij.Einde = Iso(einde);
                    rij.Duur = kal.Tel(start.AddDays(-1), einde, metVerlet);
                    var gewogen = kinderen.Where(t => t.Taak!.Duur > 0).ToList();
                    var totaal = gewogen.Sum(t => (decimal)t.Taak!.Duur);
                    rij.Voortgang = totaal > 0 ? (int)Math.Round(gewogen.Sum(t => t.Taak!.Duur * (decimal)t.Taak.VoortgangPct) / totaal, MidpointRounding.AwayFromZero) : 0;
                    rij.Afgerond = kinderen.All(t => t.Taak!.IsAfgerond);
                    rij.Kritiek = kinderen.Any(t => res.Taken[t.Taak!.Id].IsKritiek);
                    var bs = kinderen.Where(t => t.Taak!.BasisStart != null).Select(t => t.Taak!.BasisStart!.Value).ToList();
                    var be = kinderen.Where(t => t.Taak!.BasisEinde != null).Select(t => t.Taak!.BasisEinde!.Value).ToList();
                    if (bs.Count > 0 && be.Count > 0)
                    {
                        rij.BasisStart = Iso(bs.Min());
                        rij.BasisEinde = Iso(be.Max());
                        rij.Afwijking = Verschil(kal, be.Max(), einde, metVerlet);
                    }
                }
            }
            else
            {
                var t = k.Taak!;
                var r = res.Taken[t.Id];
                rij.Naam = t.Naam;
                rij.Duur = t.Duur;
                rij.IsMijlpaal = t.Duur == 0;
                rij.Start = Iso(r.Start);
                rij.Einde = Iso(r.Einde);
                rij.Voortgang = t.VoortgangPct;
                rij.Afgerond = t.IsAfgerond;
                rij.Kritiek = r.IsKritiek;
                rij.Speling = r.InCyclus ? null : r.Speling;
                rij.Voorgangers = VoorgangersTekst(d.Links.Where(l => l.OpvolgerTaakId == t.Id), wbsPerTaak);
                rij.VoorgangerLinks = d.Links.Where(l => l.OpvolgerTaakId == t.Id && wbsPerTaak.ContainsKey(l.VoorgangerTaakId))
                    .Select(l => new PlanningLinkInfo { TaakId = l.VoorgangerTaakId, Type = ((PlanLinkType)l.Type).ToString(), Wacht = l.WachtDagen }).ToList();
                rij.BepalendeVoorganger = r.BepalendeVoorgangerId is int bv && wbsPerTaak.TryGetValue(bv, out var bw) ? bw : null;
                rij.Beperking = t.BeperkingType;
                rij.BeperkingDatum = Iso(t.BeperkingDatum);
                rij.Deadline = Iso(t.Deadline);
                rij.WerkelijkStart = Iso(t.WerkelijkStart);
                rij.WerkelijkEinde = Iso(t.AfgerondOp);
                rij.Notitie = t.Beschrijving;
                rij.VerantwoordelijkeId = t.VerantwoordelijkeUserId;
                rij.BasisStart = Iso(t.BasisStart);
                rij.BasisEinde = Iso(t.BasisEinde);
                if (t.BasisEinde is DateOnly bse) rij.Afwijking = Verschil(kal, bse, r.Einde, metVerlet);
                rij.Mijlpalen = LinksVoor(4, t.Id);
                foreach (var w in r.Waarschuwingen) rij.Waarschuwingen.Add(WaarschuwingTekst(w));
            }
            staat.Rijen.Add(rij);
        }

        // Mijlpalen uit het Traject die aan de planning gekoppeld kunnen worden: vrije (handmatige) en die al aan de planning hangen.
        // Een mijlpaal die automatisch uit iets anders volgt (document, dossier, factuur …) blijft buiten de lijst: koppelen zou die bron overschrijven.
        var sectieTekst = Plat(boom).Where(k => k.IsFase).ToDictionary(k => k.Sectie!.Id, k => k.Wbs + " " + k.Sectie!.Naam);
        var taakTekst = Plat(boom).Where(k => !k.IsFase).ToDictionary(k => k.Taak!.Id, k => k.Wbs + " " + k.Taak!.Naam);
        var trajectMijlpalen = await _db.Mijlpaal.AsNoTracking()
            .Where(m => m.Projecttraject.ProjectId == d.Project.ProjectId && m.UnitId == null && m.Status != 3
                        && (m.BronBinding == null || m.BronBinding == 0 || m.BronBinding == 4 || m.BronBinding == 5))
            .OrderBy(m => m.Volgorde)
            .Select(m => new { m.Id, m.Naam, m.BronBinding, m.BronRefId, m.BronParam, FaseNaam = m.ProjecttrajectFase != null ? m.ProjecttrajectFase.Naam : null })
            .ToListAsync();
        foreach (var m in trajectMijlpalen)
        {
            var optie = new PlanningMijlpaalOptie { Id = m.Id, Naam = m.Naam ?? "", FaseNaam = m.FaseNaam };
            var verwijst = m.BronRefId ?? (int.TryParse((m.BronParam ?? "").Trim(), out var px) ? px : (int?)null);
            if (m.BronBinding == 4 && verwijst is int tid && taakTekst.TryGetValue(tid, out var tt)) { optie.GekoppeldAanSleutel = "t" + tid; optie.GekoppeldAanTekst = tt; }
            else if (m.BronBinding == 5 && verwijst is int sid && sectieTekst.TryGetValue(sid, out var st)) { optie.GekoppeldAanSleutel = "f" + sid; optie.GekoppeldAanTekst = st; }
            staat.TrajectMijlpalen.Add(optie);
        }

        // Kop
        var kop = staat.Kop;
        kop.VerletMeetellen = metVerlet;
        kop.GeenWeerstation = !d.HeeftWeerstation;
        kop.GeenStartdatum = d.Project.StartDateConstruction == null;
        kop.ProjectStart = Iso(d.Project.StartDateConstruction);
        kop.Uitvoeringstermijn = d.Project.ExecutionDays;
        if (planStart is DateOnly ps && planEinde is DateOnly pe)
        {
            kop.Start = Iso(ps);
            kop.Einde = Iso(pe);
            kop.DuurWerkdagen = kal.Tel(ps.AddDays(-1), pe, metVerlet);
            kop.VerletDagen = kal.VerletDagen(ps, pe).Count();
        }
        var basisEinden = actieveTaken.Where(t => t.BasisEinde != null).Select(t => t.BasisEinde!.Value).ToList();
        if (basisEinden.Count > 0 && planEinde is DateOnly e2)
        {
            kop.BasisOp = d.Kop?.BasisVastgelegdOp?.ToString("dd/MM/yyyy", Nl);
            kop.BasisAfwijking = Verschil(kal, basisEinden.Max(), e2, metVerlet);
        }

        DateOnly? deadline = null;
        if (d.Project.StartDateConstruction is DateOnly sd && (d.Project.ExecutionDays ?? 0) > 0)
        {
            deadline = kal.Voeg(sd, d.Project.ExecutionDays!.Value);
            kop.Deadline = Iso(deadline);
            if (planEinde is DateOnly e3) kop.Marge = Verschil(kal, e3, deadline.Value, metVerlet);
        }

        // Nietwerkdagen rond de planning (voor de arcering in de Gantt)
        if (planStart is DateOnly s0 && planEinde is DateOnly e0)
        {
            var van = s0.AddDays(-14);
            var tot = e0.AddDays(60);
            staat.NietWerkdagen.AddRange(kal.VerlofDagen(van, tot).Select(x => new PlanningNietWerkdag { Datum = Iso(x)!, Soort = "verlof" }));
            if (metVerlet)
                staat.NietWerkdagen.AddRange(kal.VerletDagen(van, tot).Select(x => new PlanningNietWerkdag { Datum = Iso(x)!, Soort = "verlet" }));
        }

        // Meldingen
        if (kop.GeenStartdatum && staat.HeeftPlanning)
            staat.Meldingen.Add(new PlanningMelding { Ernst = "warning", Titel = "Geen aanvangsdatum", Tekst = "Vul de aanvangsdatum van het project in. Tot dan rekent de planning vanaf vandaag." });
        if (kop.GeenWeerstation && (d.Project.ExecutionDays ?? 0) > 0)
            staat.Meldingen.Add(new PlanningMelding { Ernst = "warning", Titel = "Geen weerstation", Tekst = "De werkdagen en de deadline rekenen zonder weerverlet. Kies een weerstation bij het project." });
        if (deadline is DateOnly dl && planEinde is DateOnly pe2 && pe2 > dl)
            staat.Meldingen.Add(new PlanningMelding
            {
                Ernst = "danger",
                Titel = $"Einde {Dd(pe2)} valt na de voorziene oplevering {Dd(dl)}",
                Tekst = $"{Dagen(kal.Tel(dl, pe2, metVerlet))} te laat.",
            });

        // Klanten met een afwijkende uitvoeringstermijn of aanvangsdatum
        if (d.Project.StartDateConstruction is DateOnly pStart && planEinde is DateOnly pEinde)
        {
            var klantIds = await _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == d.Project.ProjectId && u.ClientAccountId != null)
                .Select(u => u.ClientAccountId!.Value).Distinct().ToListAsync();
            var klanten = await _db.ClientAccount.AsNoTracking().Where(k => klantIds.Contains(k.Id)).ToListAsync();
            foreach (var kl in klanten.Where(x => x.DeliveryDate == null))
            {
                var kStart = kl.StartDateConstruction ?? pStart;
                var kDagen = (kl.ExecutionDays ?? 0) > 0 ? kl.ExecutionDays!.Value : (d.Project.ExecutionDays ?? 0);
                var wijktAf = (kl.StartDateConstruction != null && kl.StartDateConstruction != pStart)
                              || ((kl.ExecutionDays ?? 0) > 0 && kl.ExecutionDays != d.Project.ExecutionDays);
                if (!wijktAf || kDagen <= 0) continue;
                var kDeadline = kal.Voeg(kStart, kDagen);
                if (pEinde > kDeadline)
                    staat.Meldingen.Add(new PlanningMelding
                    {
                        Ernst = "warning",
                        Titel = $"Einde {Dd(pEinde)} valt na de voorziene oplevering van {kl.Name} ({Dd(kDeadline)})",
                        Tekst = $"{Dagen(kal.Tel(kDeadline, pEinde, metVerlet))} te laat volgens de afwijkende termijn van deze klant.",
                    });
            }
        }

        foreach (var rij in staat.Rijen.Where(r => r.Soort == "taak"))
            foreach (var w in res.Taken[rij.Id].Waarschuwingen)
                staat.Meldingen.Add(new PlanningMelding
                {
                    Ernst = w.Type == PlanWaarschuwingType.GeenVoorganger ? "info" : "warning",
                    Titel = $"{rij.Wbs} {rij.Naam}",
                    Tekst = WaarschuwingTekst(w),
                    RijSleutel = rij.Sleutel,
                });

        staat.Lots = lots.OrderBy(l => l.Value).Select(l => new PlanningGebruiker { Id = l.Key.ToString(), Naam = l.Value }).ToList();
        staat.Gebruikers = await _db.Users.AsNoTracking().Where(u => u.IsActive)
            .OrderBy(u => u.Voornaam).ThenBy(u => u.Familienaam)
            .Select(u => new PlanningGebruiker { Id = u.UserId, Naam = (u.Voornaam + " " + u.Familienaam).Trim() })
            .ToListAsync();

        return staat;
    }

    private static int Verschil(WerkKalender kal, DateOnly basis, DateOnly nu, bool metVerlet)
        => nu >= basis ? kal.Tel(basis, nu, metVerlet) : -kal.Tel(nu, basis, metVerlet);

    private static string Dagen(int n) => n == 1 ? "1 werkdag" : n + " werkdagen";

    private static string WaarschuwingTekst(PlanWaarschuwing w) => w.Type switch
    {
        PlanWaarschuwingType.DeadlineOverschreden => $"Einde na de deadline ({Dagen(w.Dagen)} te laat).",
        PlanWaarschuwingType.BeperkingOverschreden => $"Valt na de beperking ({Dagen(w.Dagen)} te laat).",
        PlanWaarschuwingType.GeenVoorganger => "Heeft geen voorganger en start op de eerste projectdag.",
        PlanWaarschuwingType.Cyclus => "Wacht op zichzelf (cyclus in de voorgangers).",
        _ => "",
    };

    private static string VoorgangersTekst(IEnumerable<PlanningAfhankelijkheid> links, Dictionary<int, string> wbs)
    {
        var delen = new List<string>();
        foreach (var l in links.OrderBy(l => wbs.TryGetValue(l.VoorgangerTaakId, out var w) ? w : ""))
        {
            if (!wbs.TryGetValue(l.VoorgangerTaakId, out var nummer)) continue;
            var type = (PlanLinkType)l.Type == PlanLinkType.FS ? "" : ((PlanLinkType)l.Type).ToString();
            var wacht = l.WachtDagen == 0 ? "" : (l.WachtDagen > 0 ? "+" : "-") + Math.Abs(l.WachtDagen) + "d";
            delen.Add(nummer + type + wacht);
        }
        return string.Join("; ", delen);
    }

    // ── Commando's ──

    public async Task<PlanningUitkomst> Voer(int projectId, PlanningCommando c, string? userId)
    {
        var d = await LaadData(projectId);
        if (d == null) return new PlanningUitkomst { Fout = "Project niet gevonden." };

        var huidigeVersie = d.Kop?.Versie ?? 0;
        if (c.Versie != huidigeVersie)
        {
            return new PlanningUitkomst
            {
                Conflict = true,
                Fout = "Iemand anders wijzigde de planning intussen. De pagina wordt herladen.",
                Staat = await Bouw(d, Rekenen(d), d.Doel(c.PlanningId)),
            };
        }

        var voorSnap = Momentopname(d);
        var voor = d.Taken.ToDictionary(t => t.Id, t => (t.Startdatum, t.Einddatum));
        var voorEinde = d.Taken.Count > 0 ? d.Taken.Max(t => t.Einddatum) : null;
        var bewerkt = new HashSet<string>();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (d.Kop == null)
            {
                d.Kop = new ProjectPlanning { ProjectId = projectId, Versie = 0, CreatedDate = DateTime.UtcNow, VerletMeetellen = true };
                _db.ProjectPlanning.Add(d.Kop);
            }
            await ZorgVoorHoofdplanning(d);
            string? fout = c.Type switch
            {
                "taakToevoegen" => await TaakToevoegen(d, c, bewerkt),
                "faseToevoegen" => await FaseToevoegen(d, c, bewerkt),
                "wijzig" => await Wijzig(d, c, bewerkt),
                "verwijder" => await Verwijder(d, c),
                "verplaats" => await Verplaats(d, c, bewerkt),
                "basisVastleggen" => BasisVastleggen(d),
                "verletMeetellen" => VerletMeetellen(d, c),
                "herstel" => await Herstel(d, c),
                "planningToevoegen" => await PlanningToevoegen(d, c, bewerkt),
                "planningWijzig" => await PlanningWijzig(d, c),
                "planningVerwijder" => await PlanningVerwijder(d, c),
                _ => "Onbekende actie.",
            };
            if (fout != null) { await tx.RollbackAsync(); _db.ChangeTracker.Clear(); return new PlanningUitkomst { Fout = fout }; }

            // Opnieuw laden zodat de boom klopt met wat er net is bewaard, dan doorrekenen.
            await _db.SaveChangesAsync();
            d = (await LaadData(projectId))!;
            var dr = Rekenen(d);
            if (dr.Resultaat.Cyclus.Count > 0 && c.Type != "herstel")
            {
                var wbs = AllePlat(dr.Bomen).Where(k => !k.IsFase && dr.Resultaat.Cyclus.Contains(k.Taak!.Id)).Select(k => k.Wbs).ToList();
                await tx.RollbackAsync(); _db.ChangeTracker.Clear();
                return new PlanningUitkomst { Fout = $"Dat kan niet: de taken {string.Join(", ", wbs)} zouden op elkaar wachten (cyclus)." };
            }

            d.Kop ??= new ProjectPlanning { ProjectId = projectId, CreatedDate = DateTime.UtcNow };
            if (_db.Entry(d.Kop).State == EntityState.Detached) _db.ProjectPlanning.Add(d.Kop);
            d.Kop.Versie = huidigeVersie + 1;
            d.Kop.ModifiedDate = DateTime.UtcNow;
            d.Kop.ModifiedByUserId = userId;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            await HerberekenTrajectAsync(projectId);

            // Plannings aanmaken, wijzigen of verwijderen is niet ongedaan te maken met een momentopname van de fases.
            var planningCommando = c.Type.StartsWith("planning", StringComparison.Ordinal);
            var nieuw = bewerkt.FirstOrDefault(b => b.StartsWith("p", StringComparison.Ordinal));
            int? nieuwId = nieuw != null && int.TryParse(nieuw[1..], out var np) ? np : null;
            var toon = c.Type == "planningVerwijder" ? d.HoofdId : d.Doel(nieuwId ?? c.PlanningId);
            var uitkomst = new PlanningUitkomst
            {
                Ok = true,
                Undo = planningCommando ? null : voorSnap,
                NieuwePlanningId = nieuwId,
                Staat = await Bouw(d, dr, toon),
            };

            // Wat is er door deze wijziging verschoven?
            var verschoven = new List<string>();
            foreach (var t in d.Taken)
                if (voor.TryGetValue(t.Id, out var oud) && (oud.Startdatum != t.Startdatum || oud.Einddatum != t.Einddatum) && !bewerkt.Contains("t" + t.Id))
                    verschoven.Add("t" + t.Id);
            uitkomst.Verschoven = verschoven;
            var nuEinde = d.Taken.Count > 0 ? d.Taken.Max(t => t.Einddatum) : null;
            var delta = voorEinde is DateOnly ve && nuEinde is DateOnly ne ? Verschil(d.Kalender, ve, ne, dr.MetVerlet) : 0;
            if (verschoven.Count > 0 || delta != 0)
            {
                var delen = new List<string>();
                if (verschoven.Count > 0) delen.Add(verschoven.Count == 1 ? "1 andere taak verschoven" : $"{verschoven.Count} andere taken verschoven");
                if (delta != 0 && nuEinde is DateOnly ne2) delen.Add($"einde {(delta > 0 ? "+" : "−")}{Math.Abs(delta)} werkdag{(Math.Abs(delta) == 1 ? "" : "en")} ({Dd(ne2)})");
                uitkomst.Samenvatting = string.Join(" · ", delen);
            }
            return uitkomst;
        }
        catch
        {
            await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    /// <summary>
    /// Gekoppelde trajectmijlpalen volgen de planning: na een wijziging herrekent het Traject zijn streefdata (en zet een mijlpaal op bereikt
    /// zodra haar taak afgerond is). Faalt dat, dan blijft de planning gewoon bewaard; het Traject herrekent ook zelf op de achtergrond.
    /// </summary>
    private async Task HerberekenTrajectAsync(int projectId)
    {
        if (_traject == null) return;
        try
        {
            var gekoppeld = await _db.Mijlpaal.AsNoTracking()
                .AnyAsync(m => m.Projecttraject.ProjectId == projectId && (m.BronBinding == 4 || m.BronBinding == 5));
            if (gekoppeld) await _traject.RecalculateProject(projectId, null);
        }
        catch
        {
            // bewust genegeerd: zie samenvatting
        }
    }

    private static int? VerwijstNaar(Mijlpaal m) => m.BronRefId ?? (int.TryParse((m.BronParam ?? "").Trim(), out var x) ? x : (int?)null);

    public async Task<PlanningUitkomst> Koppel(int projectId, PlanningKoppelCommando c, string? userId)
    {
        if (c == null) return new PlanningUitkomst { Fout = "Geen koppeling opgegeven." };
        var isFase = c.Soort == "fase";
        var binding = isFase ? 5 : 4;

        var doelOk = isFase
            ? await _db.PlanningSectie.AnyAsync(s => s.Id == c.Id && s.ProjectId == projectId && s.IsActief)
            : await _db.PlanningTaak.AnyAsync(t => t.Id == c.Id && t.PlanningSectie.ProjectId == projectId && t.PlanningSectie.IsActief);
        if (!doelOk) return new PlanningUitkomst { Fout = isFase ? "De fase bestaat niet meer." : "De taak bestaat niet meer." };

        var ids = new[] { c.MijlpaalId, c.VorigeMijlpaalId }.Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var mijlpalen = await _db.Mijlpaal.Where(m => ids.Contains(m.Id) && m.Projecttraject.ProjectId == projectId).ToListAsync();
        var nu = DateTime.UtcNow;

        // Eerst losmaken wat in de keuzelijst stond, maar enkel als het echt aan dit doel hing.
        if (c.VorigeMijlpaalId is int vid && vid != c.MijlpaalId)
        {
            var oud = mijlpalen.FirstOrDefault(m => m.Id == vid);
            if (oud != null && oud.BronBinding == binding && VerwijstNaar(oud) == c.Id)
            {
                oud.BronBinding = null; oud.BronRefId = null; oud.BronParam = null;
                oud.ModifiedByUserId = userId; oud.ModifiedDate = nu;
                _db.MijlpaalHistoriek.Add(new MijlpaalHistoriek
                {
                    MijlpaalId = oud.Id, Actie = (int)BOCore.MijlpaalHistoriekActie.Bijgewerkt, UserId = userId, Timestamp = nu,
                    Opmerking = "Koppeling met de planning losgemaakt",
                });
            }
        }

        if (c.MijlpaalId is int nid)
        {
            var m = mijlpalen.FirstOrDefault(x => x.Id == nid);
            if (m == null) return new PlanningUitkomst { Fout = "Deze mijlpaal hoort niet bij het Traject van dit project." };
            if (m.UnitId != null) return new PlanningUitkomst { Fout = "Enkel een mijlpaal van het hele project kan aan de planning gekoppeld worden, niet een van één eenheid." };
            var vrij = m.BronBinding == null || m.BronBinding == 0 || m.BronBinding == 4 || m.BronBinding == 5;
            if (!vrij)
                return new PlanningUitkomst { Fout = "Deze mijlpaal volgt al automatisch iets anders, bijvoorbeeld een document of een dossier. Haal die koppeling eerst weg in het Traject." };
            if (!(m.BronBinding == binding && VerwijstNaar(m) == c.Id))
            {
                m.BronBinding = binding; m.BronRefId = c.Id; m.BronParam = null;
                m.ModifiedByUserId = userId; m.ModifiedDate = nu;
                _db.MijlpaalHistoriek.Add(new MijlpaalHistoriek
                {
                    MijlpaalId = m.Id, Actie = (int)BOCore.MijlpaalHistoriekActie.Bijgewerkt, UserId = userId, Timestamp = nu,
                    Opmerking = isFase ? "Gekoppeld aan een fase van de planning" : "Gekoppeld aan een taak van de planning",
                });
            }
        }

        await _db.SaveChangesAsync();
        await HerberekenTrajectAsync(projectId);

        var d = await LaadData(projectId);
        if (d == null) return new PlanningUitkomst { Fout = "Project niet gevonden." };
        return new PlanningUitkomst { Ok = true, Staat = await Bouw(d, Rekenen(d), d.Doel(c.PlanningId)) };
    }

    private static string? BasisVastleggen(Data d)
    {
        var dr = Rekenen(d);
        foreach (var k in AllePlat(dr.Bomen).Where(k => !k.IsFase))
        {
            k.Taak!.BasisStart = k.Taak.Startdatum;
            k.Taak.BasisEinde = k.Taak.Einddatum;
        }
        if (d.Kop != null) d.Kop.BasisVastgelegdOp = DateTime.UtcNow;
        return null;
    }

    private static string? VerletMeetellen(Data d, PlanningCommando c)
    {
        var aan = c.Velden != null && c.Velden.TryGetValue("aan", out var e) && e.ValueKind == JsonValueKind.True;
        if (d.Kop != null) d.Kop.VerletMeetellen = aan;
        return null;
    }

    // ── Plaatsen en nummeren ──

    private static List<Knoop> Broers(Data d, PlanningSectie? ouder, int planningId)
    {
        var tmp = BouwBoom(d, planningId);
        if (ouder == null) return tmp;
        return Plat(tmp).FirstOrDefault(k => k.IsFase && k.Sectie!.Id == ouder.Id)?.Kinderen ?? new List<Knoop>();
    }

    private static void ZetVolgorde(List<Knoop> lijst)
    {
        for (var i = 0; i < lijst.Count; i++)
        {
            if (lijst[i].IsFase) lijst[i].Sectie!.Volgorde = (i + 1) * 10;
            else lijst[i].Taak!.Volgorde = (i + 1) * 10;
        }
    }

    /// <summary>Zet een item (al toegevoegd aan <paramref name="d"/>) op de gevraagde plaats binnen zijn bovenliggende fase.</summary>
    private static void Plaats(Data d, PlanningSectie? ouder, int planningId, bool isFase, int id, int? naId, string? naSoort)
    {
        var lijst = Broers(d, ouder, planningId);
        var item = lijst.FirstOrDefault(k => k.IsFase == isFase && k.Id == id);
        if (item == null) return;
        lijst.Remove(item);
        var index = lijst.Count;
        if (naId is int n)
        {
            var na = lijst.FindIndex(k => k.Id == n && k.IsFase == (naSoort == "fase"));
            if (na >= 0) index = na + 1;
        }
        lijst.Insert(index, item);
        ZetVolgorde(lijst);
    }

    // ── Velden toepassen ──

    private static bool TryDatum(JsonElement e, out DateOnly? datum, out string? fout)
    {
        datum = null; fout = null;
        if (e.ValueKind == JsonValueKind.Null) return true;
        if (e.ValueKind == JsonValueKind.String)
        {
            var s = e.GetString();
            if (string.IsNullOrWhiteSpace(s)) return true;
            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) { datum = dt; return true; }
        }
        fout = "Ongeldige datum.";
        return false;
    }

    private static bool TryInt(JsonElement e, out int? waarde)
    {
        waarde = null;
        if (e.ValueKind == JsonValueKind.Null) return true;
        if (e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out var i)) { waarde = i; return true; }
        if (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var j)) { waarde = j; return true; }
        return false;
    }

    private string? ZetTaakVelden(Data d, PlanningTaak t, Dictionary<string, JsonElement> v, bool nieuw)
    {
        if (v.TryGetValue("naam", out var naam))
        {
            var s = (naam.ValueKind == JsonValueKind.String ? naam.GetString() : null)?.Trim();
            if (string.IsNullOrEmpty(s)) return "Geef de taak een naam.";
            t.Naam = s.Length > 300 ? s[..300] : s;
        }
        if (v.TryGetValue("duur", out var duur))
        {
            if (!TryInt(duur, out var n) || n is null || n < 0 || n > 2000) return "De duur moet een aantal werkdagen zijn tussen 0 en 2000.";
            t.Duur = n.Value;
        }
        if (v.TryGetValue("notitie", out var notitie))
        {
            var s = notitie.ValueKind == JsonValueKind.String ? notitie.GetString() : null;
            t.Beschrijving = string.IsNullOrWhiteSpace(s) ? null : (s!.Length > 1000 ? s[..1000] : s);
        }
        if (v.TryGetValue("verantwoordelijke", out var wie))
            t.VerantwoordelijkeUserId = wie.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(wie.GetString()) ? wie.GetString() : null;

        if (v.TryGetValue("beperking", out var bep))
        {
            if (!TryInt(bep, out var b) || b is null || b < 0 || b > 6) return "Ongeldige beperking.";
            t.BeperkingType = (byte)b.Value;
        }
        if (v.TryGetValue("beperkingDatum", out var bd))
        {
            if (!TryDatum(bd, out var dat, out var f)) return f;
            t.BeperkingDatum = dat;
        }
        if (t.BeperkingType != 0 && t.BeperkingDatum == null) return "Kies een datum bij de beperking.";
        if (t.BeperkingType == 0) t.BeperkingDatum = null;

        if (v.TryGetValue("deadline", out var dl))
        {
            if (!TryDatum(dl, out var dat, out var f)) return f;
            t.Deadline = dat;
        }
        if (v.TryGetValue("werkelijkStart", out var ws))
        {
            if (!TryDatum(ws, out var dat, out var f)) return f;
            t.WerkelijkStart = dat;
        }
        if (v.TryGetValue("werkelijkEinde", out var we))
        {
            if (!TryDatum(we, out var dat, out var f)) return f;
            t.AfgerondOp = dat;
            t.IsAfgerond = dat != null;
            if (dat != null) t.VoortgangPct = 100;
            else if (t.VoortgangPct == 100) t.VoortgangPct = 99;
        }
        if (v.TryGetValue("voortgang", out var vg))
        {
            if (!TryInt(vg, out var p) || p is null || p < 0 || p > 100) return "De voortgang moet tussen 0 en 100 liggen.";
            t.VoortgangPct = p.Value;
            var vandaag = DateOnly.FromDateTime(DateTime.Today);
            if (p == 100)
            {
                t.IsAfgerond = true;
                t.AfgerondOp ??= vandaag;
                t.WerkelijkStart ??= t.Startdatum ?? vandaag;
            }
            else
            {
                t.IsAfgerond = false;
                t.AfgerondOp = null;
                if (p > 0) t.WerkelijkStart ??= t.Startdatum ?? vandaag;
            }
        }
        return null;
    }

    private static string? ZetFaseVelden(PlanningSectie s, Dictionary<string, JsonElement> v)
    {
        if (v.TryGetValue("naam", out var naam))
        {
            var n = (naam.ValueKind == JsonValueKind.String ? naam.GetString() : null)?.Trim();
            if (string.IsNullOrEmpty(n)) return "Geef de fase een naam.";
            s.Naam = n.Length > 200 ? n[..200] : n;
        }
        if (v.TryGetValue("lotId", out var lot))
        {
            if (!TryInt(lot, out var id)) return "Ongeldig budgetlot.";
            s.ActivityGroupId = id;
            if (id == null) s.TeltMeeVoorVoortgang = false;
        }
        if (v.TryGetValue("teltMee", out var tm))
        {
            var aan = tm.ValueKind == JsonValueKind.True;
            if (aan && s.ActivityGroupId == null) return "Kies eerst een budgetlot om de voortgang te laten meetellen.";
            s.TeltMeeVoorVoortgang = aan;
        }
        return null;
    }

    // ── Voorgangers ──

    /// <summary>Leest "5.1FS+3d; 4.2SS; 2.8" en geeft per voorganger de taak, het type en de wachttijd. Een verwijzing naar een fase telt voor al haar taken.</summary>
    private static string? ParseVoorgangers(string? tekst, Data d, int? eigenTaakId, int planningId, out List<(int Taak, byte Type, int Wacht)> uit)
    {
        uit = new List<(int, byte, int)>();
        if (string.IsNullOrWhiteSpace(tekst)) return null;
        var perWbs = Plat(BouwBoom(d, planningId)).ToDictionary(k => k.Wbs, k => k);
        // "H:3.1" verwijst naar de hoofdplanning (enkel vanuit een bouwdeelplanning).
        var perHoofdWbs = planningId == d.HoofdId ? null : Plat(BouwBoom(d, d.HoofdId)).ToDictionary(k => k.Wbs, k => k);

        foreach (var deel in tekst.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var m = VoorgangerRegex.Match(deel);
            if (!m.Success) return $"“{deel.Trim()}” is geen geldige voorganger. Typ bv. 5.1, 5.1SS of 5.1FS+3d.";
            var nummer = m.Groups[2].Value;
            Knoop? knoop;
            if (m.Groups[1].Success)
            {
                if (perHoofdWbs == null) return "“H:” verwijst naar de hoofdplanning en kan enkel in een bouwdeelplanning.";
                if (!perHoofdWbs.TryGetValue(nummer, out knoop)) return $"Taak H:{nummer} bestaat niet in de hoofdplanning.";
            }
            else if (!perWbs.TryGetValue(nummer, out knoop)) return $"Taak {nummer} bestaat niet.";
            var type = m.Groups[3].Success ? (byte)Enum.Parse<PlanLinkType>(m.Groups[3].Value.ToUpperInvariant()) : (byte)PlanLinkType.FS;
            var wacht = m.Groups[4].Success ? int.Parse(m.Groups[4].Value.Replace(" ", "")) : 0;
            var bronnen = knoop.IsFase ? Taken(knoop).Select(k => k.Taak!.Id) : new[] { knoop.Taak!.Id };
            foreach (var id in bronnen)
            {
                if (id == eigenTaakId) return "Een taak kan niet op zichzelf wachten.";
                if (!uit.Any(x => x.Taak == id)) uit.Add((id, type, wacht));
            }
        }
        return null;
    }

    /// <summary>
    /// Vervangt de voorgangers die je in de pagina ziet: die uit dezelfde planning en uit de hoofdplanning. Voorgangers uit een andere
    /// bouwdeelplanning (estafette) staan niet in dat veld en blijven dus behouden.
    /// </summary>
    private void ZetVoorgangers(int opvolgerId, List<(int Taak, byte Type, int Wacht)> nieuw, Data d, int planningId)
    {
        bool Zichtbaar(int voorgangerId)
        {
            var t = d.Taken.FirstOrDefault(x => x.Id == voorgangerId);
            if (t == null) return true;
            var pid = d.PlanningVanTaak(t);
            return pid == planningId || pid == d.HoofdId;
        }
        foreach (var l in d.Links.Where(l => l.OpvolgerTaakId == opvolgerId && Zichtbaar(l.VoorgangerTaakId)).ToList())
        {
            _db.PlanningAfhankelijkheid.Remove(l);
            d.Links.Remove(l);
        }
        foreach (var (taak, type, wacht) in nieuw)
        {
            var l = new PlanningAfhankelijkheid { VoorgangerTaakId = taak, OpvolgerTaakId = opvolgerId, Type = type, WachtDagen = wacht };
            _db.PlanningAfhankelijkheid.Add(l);
            d.Links.Add(l);
        }
    }

    // ── Commando-uitvoering ──

    private async Task<string?> TaakToevoegen(Data d, PlanningCommando c, HashSet<string> bewerkt)
    {
        var v = c.Velden ?? new Dictionary<string, JsonElement>();
        var sectie = c.SectieId is int sid ? d.Secties.FirstOrDefault(s => s.Id == sid) : null;
        var planningId = sectie != null ? d.PlanningVan(sectie) : d.Doel(c.PlanningId);
        if (sectie == null)
        {
            // Zonder fase: de laatste fase van het hoogste niveau van deze planning, of een nieuwe "Fase 1".
            sectie = d.Secties.Where(s => s.ParentSectieId == null && d.PlanningVan(s) == planningId).OrderByDescending(s => s.Volgorde).FirstOrDefault();
            if (sectie == null)
            {
                sectie = new PlanningSectie { ProjectId = d.Project.ProjectId, Naam = "Fase 1", Volgorde = 10, IsActief = true, BouwPlanningId = planningId };
                _db.PlanningSectie.Add(sectie);
                await _db.SaveChangesAsync();
                d.Secties.Add(sectie);
            }
        }

        string? voorFout = null;
        List<(int Taak, byte Type, int Wacht)> voorgangers = new();
        if (v.TryGetValue("voorgangers", out var vg) && vg.ValueKind == JsonValueKind.String)
            voorFout = ParseVoorgangers(vg.GetString(), d, null, planningId, out voorgangers);
        if (voorFout != null) return voorFout;

        var taak = new PlanningTaak { PlanningSectieId = sectie.Id, Naam = "", Duur = 1, Volgorde = int.MaxValue / 2 };
        var fout = ZetTaakVelden(d, taak, v, true);
        if (fout != null) return fout;
        if (string.IsNullOrWhiteSpace(taak.Naam)) return "Geef de taak een naam.";

        _db.PlanningTaak.Add(taak);
        await _db.SaveChangesAsync();
        d.Taken.Add(taak);
        ZetVoorgangers(taak.Id, voorgangers, d, planningId);
        await _db.SaveChangesAsync();
        Plaats(d, sectie, planningId, false, taak.Id, c.NaId, c.NaSoort);
        bewerkt.Add("t" + taak.Id);
        return null;
    }

    private async Task<string?> FaseToevoegen(Data d, PlanningCommando c, HashSet<string> bewerkt)
    {
        var v = c.Velden ?? new Dictionary<string, JsonElement>();
        var ouder = c.SectieId is int pid ? d.Secties.FirstOrDefault(s => s.Id == pid) : null;
        if (c.SectieId != null && ouder == null) return "De bovenliggende fase bestaat niet.";
        var planningId = ouder != null ? d.PlanningVan(ouder) : d.Doel(c.PlanningId);

        List<(int Taak, byte Type, int Wacht)> startNa = new();
        var fout = ParseVoorgangers(c.StartNa, d, null, planningId, out startNa);
        if (fout != null) return fout;

        var sectie = new PlanningSectie { ProjectId = d.Project.ProjectId, Naam = "", ParentSectieId = ouder?.Id, Volgorde = int.MaxValue / 2, IsActief = true, BouwPlanningId = planningId };
        fout = ZetFaseVelden(sectie, v);
        if (fout != null) return fout;
        if (string.IsNullOrWhiteSpace(sectie.Naam)) return "Geef de fase een naam.";

        _db.PlanningSectie.Add(sectie);
        await _db.SaveChangesAsync();
        d.Secties.Add(sectie);
        Plaats(d, ouder, planningId, true, sectie.Id, c.NaId, c.NaSoort);
        bewerkt.Add("f" + sectie.Id);

        // Een fase is een groep: ze bestaat uit taken. De eerste taak (optioneel) krijgt de "start na"-voorgangers.
        var eerste = (c.EersteTaakNaam ?? "").Trim();
        if (eerste.Length > 0 || startNa.Count > 0)
        {
            var taak = new PlanningTaak { PlanningSectieId = sectie.Id, Naam = eerste.Length > 0 ? eerste : "Eerste taak", Duur = Math.Clamp(c.EersteTaakDuur ?? 1, 0, 2000), Volgorde = 10 };
            _db.PlanningTaak.Add(taak);
            await _db.SaveChangesAsync();
            d.Taken.Add(taak);
            ZetVoorgangers(taak.Id, startNa, d, planningId);
            bewerkt.Add("t" + taak.Id);
        }
        return null;
    }

    private async Task<string?> Wijzig(Data d, PlanningCommando c, HashSet<string> bewerkt)
    {
        var v = c.Velden ?? new Dictionary<string, JsonElement>();
        if (c.Id is not int id) return "Geen rij opgegeven.";

        if (c.Soort == "fase")
        {
            var s = d.Secties.FirstOrDefault(x => x.Id == id);
            if (s == null) return "De fase bestaat niet meer.";
            bewerkt.Add("f" + id);
            return ZetFaseVelden(s, v);
        }

        var t = d.Taken.FirstOrDefault(x => x.Id == id);
        if (t == null) return "De taak bestaat niet meer.";
        bewerkt.Add("t" + id);

        List<(int Taak, byte Type, int Wacht)>? voorgangers = null;
        if (v.TryGetValue("voorgangers", out var vg))
        {
            var fout = ParseVoorgangers(vg.ValueKind == JsonValueKind.String ? vg.GetString() : null, d, id, d.PlanningVanTaak(t), out var lijst);
            if (fout != null) return fout;
            voorgangers = lijst;
        }
        var f = ZetTaakVelden(d, t, v, false);
        if (f != null) return f;
        if (voorgangers != null) ZetVoorgangers(id, voorgangers, d, d.PlanningVanTaak(t));
        await Task.CompletedTask;
        return null;
    }

    private async Task<string?> Verwijder(Data d, PlanningCommando c)
    {
        if (c.Items == null || c.Items.Count == 0) return "Niets om te verwijderen.";

        var teVerwijderenSecties = new HashSet<int>();
        var teVerwijderenTaken = new HashSet<int>();

        void VoegFaseToe(int id)
        {
            if (!teVerwijderenSecties.Add(id)) return;
            foreach (var t in d.Taken.Where(t => t.PlanningSectieId == id)) teVerwijderenTaken.Add(t.Id);
            foreach (var kind in d.Secties.Where(s => s.ParentSectieId == id)) VoegFaseToe(kind.Id);
        }
        foreach (var item in c.Items)
        {
            if (item.Soort == "fase") VoegFaseToe(item.Id);
            else teVerwijderenTaken.Add(item.Id);
        }

        var links = d.Links.Where(l => teVerwijderenTaken.Contains(l.VoorgangerTaakId) || teVerwijderenTaken.Contains(l.OpvolgerTaakId)).ToList();
        // Links waarvan de opvolger buiten de selectie valt, staan niet in d.Links: aanvullen uit de database.
        var extra = await _db.PlanningAfhankelijkheid.Where(l => teVerwijderenTaken.Contains(l.VoorgangerTaakId) || teVerwijderenTaken.Contains(l.OpvolgerTaakId)).ToListAsync();
        _db.PlanningAfhankelijkheid.RemoveRange(extra.Union(links));
        await _db.SaveChangesAsync();

        _db.PlanningTaak.RemoveRange(d.Taken.Where(t => teVerwijderenTaken.Contains(t.Id)));
        await _db.SaveChangesAsync();

        // Fases: diepste eerst (de FK naar de bovenliggende fase kent geen cascade).
        int Diepte(PlanningSectie s) { var n = 0; var p = s.ParentSectieId; while (p != null) { n++; p = d.Secties.FirstOrDefault(x => x.Id == p)?.ParentSectieId; } return n; }
        foreach (var s in d.Secties.Where(s => teVerwijderenSecties.Contains(s.Id)).OrderByDescending(Diepte).ToList())
        {
            _db.PlanningSectie.Remove(s);
            await _db.SaveChangesAsync();
        }
        return null;
    }

    private async Task<string?> Verplaats(Data d, PlanningCommando c, HashSet<string> bewerkt)
    {
        if (c.Id is not int id) return "Geen rij opgegeven.";
        var isFase = c.Soort == "fase";
        var bomen = BouwBomen(d);
        var boom = bomen.Values.FirstOrDefault(b => Plat(b).Any(k => k.IsFase == isFase && k.Id == id)) ?? new List<Knoop>();
        var knoop = Plat(boom).FirstOrDefault(k => k.IsFase == isFase && k.Id == id);
        if (knoop == null) return "De rij bestaat niet meer.";
        bewerkt.Add(knoop.Sleutel);

        var lijst = knoop.Ouder == null ? boom : knoop.Ouder.Kinderen;
        var index = lijst.IndexOf(knoop);

        switch (c.Actie)
        {
            case "omhoog":
                if (index <= 0) return null;
                lijst.RemoveAt(index); lijst.Insert(index - 1, knoop);
                ZetVolgorde(lijst);
                break;
            case "omlaag":
                if (index >= lijst.Count - 1) return null;
                lijst.RemoveAt(index); lijst.Insert(index + 1, knoop);
                ZetVolgorde(lijst);
                break;
            case "inspringen":
            {
                // De rij erboven moet een fase zijn: de rij komt achteraan in die fase.
                var boven = index > 0 ? lijst[index - 1] : null;
                if (boven == null || !boven.IsFase) return "Inspringen kan enkel onder een fase. Zet eerst een fase boven deze rij.";
                if (isFase) knoop.Sectie!.ParentSectieId = boven.Sectie!.Id;
                else knoop.Taak!.PlanningSectieId = boven.Sectie!.Id;
                lijst.RemoveAt(index);
                ZetVolgorde(lijst);
                boven.Kinderen.Add(knoop);
                ZetVolgorde(boven.Kinderen);
                break;
            }
            case "uitspringen":
            {
                var ouder = knoop.Ouder;
                if (ouder == null) return null;
                var grootouder = ouder.Ouder;
                if (!isFase && grootouder == null)
                    return "Een taak hoort altijd in een fase. Gebruik Uitspringen op een deelfase of verplaats de taak naar een andere fase.";
                // Naar het niveau van de bovenliggende fase, direct erna. Een taak kan enkel in een fase landen.
                if (isFase) knoop.Sectie!.ParentSectieId = grootouder?.Sectie?.Id;
                else knoop.Taak!.PlanningSectieId = grootouder!.Sectie!.Id;
                ouder.Kinderen.Remove(knoop);
                ZetVolgorde(ouder.Kinderen);
                var doel = grootouder == null ? boom : grootouder.Kinderen;
                doel.Insert(doel.IndexOf(ouder) + 1, knoop);
                ZetVolgorde(doel);
                break;
            }
            default:
                return "Onbekende verplaatsing.";
        }
        await Task.CompletedTask;
        return null;
    }

    // ── Plannings beheren ──

    /// <summary>Schrijfacties werken altijd op een echte hoofdplanning: bestaat die nog niet, dan wordt ze nu aangemaakt en nemen de bestaande fases ze over.</summary>
    private async Task ZorgVoorHoofdplanning(Data d)
    {
        if (d.Planningen.Any(p => p.IsHoofd)) return;
        var hoofd = new BouwPlanning { ProjectId = d.Project.ProjectId, Naam = "Hoofdplanning", IsHoofd = true, Volgorde = 0, IsActief = true, CreatedDate = DateTime.UtcNow };
        _db.BouwPlanning.Add(hoofd);
        await _db.SaveChangesAsync();
        d.Planningen.Insert(0, hoofd);
        foreach (var s in d.Secties.Where(s => s.BouwPlanningId == null)) s.BouwPlanningId = hoofd.Id;
        await _db.SaveChangesAsync();
    }

    private static string? PlanningVelden(BouwPlanning bp, Dictionary<string, JsonElement> v)
    {
        if (v.TryGetValue("naam", out var naam))
        {
            var n = (naam.ValueKind == JsonValueKind.String ? naam.GetString() : null)?.Trim();
            if (string.IsNullOrEmpty(n)) return "Geef de planning een naam.";
            bp.Naam = n.Length > 200 ? n[..200] : n;
        }
        if (v.TryGetValue("startDatum", out var sd))
        {
            if (!TryDatum(sd, out var dat, out var f)) return f;
            bp.StartDatum = dat;
        }
        if (v.TryGetValue("bouwdeelId", out var bd))
        {
            if (!TryInt(bd, out var gid)) return "Ongeldig bouwdeel.";
            if (bp.IsHoofd && gid != null) return "De hoofdplanning hoort bij het hele project en kan niet aan een bouwdeel hangen.";
            bp.PaymentGroupId = gid;
        }
        return null;
    }

    private async Task<string?> PlanningToevoegen(Data d, PlanningCommando c, HashSet<string> bewerkt)
    {
        var bp = new BouwPlanning
        {
            ProjectId = d.Project.ProjectId, Naam = "", IsHoofd = false, IsActief = true, CreatedDate = DateTime.UtcNow,
            Volgorde = (d.Planningen.Count == 0 ? 0 : d.Planningen.Max(p => p.Volgorde)) + 10,
        };
        var fout = PlanningVelden(bp, c.Velden ?? new Dictionary<string, JsonElement>());
        if (fout != null) return fout;
        if (string.IsNullOrWhiteSpace(bp.Naam)) return "Geef de planning een naam.";
        fout = await ControleerBouwdeel(d, bp);
        if (fout != null) return fout;

        _db.BouwPlanning.Add(bp);
        await _db.SaveChangesAsync();
        d.Planningen.Add(bp);
        bewerkt.Add("p" + bp.Id);
        return null;
    }

    private async Task<string?> PlanningWijzig(Data d, PlanningCommando c)
    {
        var bp = d.Planningen.FirstOrDefault(p => p.Id == (c.PlanningId is null or 0 ? d.HoofdId : c.PlanningId));
        if (bp == null) return "De planning bestaat niet meer.";
        var fout = PlanningVelden(bp, c.Velden ?? new Dictionary<string, JsonElement>());
        if (fout != null) return fout;
        return await ControleerBouwdeel(d, bp);
    }

    /// <summary>Het bouwdeel moet bij dit project horen en nog geen andere planning hebben.</summary>
    private async Task<string?> ControleerBouwdeel(Data d, BouwPlanning bp)
    {
        if (bp.PaymentGroupId is not int gid) return null;
        var bestaat = await _db.InvoicingPaymentGroup.AnyAsync(g => g.Id == gid && g.ProjectId == d.Project.ProjectId);
        if (!bestaat) return "Dit bouwdeel hoort niet bij dit project.";
        if (d.Planningen.Any(p => p.Id != bp.Id && p.PaymentGroupId == gid)) return "Dit bouwdeel heeft al een planning.";
        return null;
    }

    private async Task<string?> PlanningVerwijder(Data d, PlanningCommando c)
    {
        var bp = d.Planningen.FirstOrDefault(p => p.Id == c.PlanningId);
        if (bp == null) return "De planning bestaat niet meer.";
        if (bp.IsHoofd) return "De hoofdplanning kan niet verwijderd worden. Verwijder eventueel de fases en taken erin.";

        var wortels = d.Secties.Where(s => s.ParentSectieId == null && d.PlanningVan(s) == bp.Id).ToList();
        if (wortels.Count > 0)
        {
            var fout = await Verwijder(d, new PlanningCommando { Items = wortels.Select(w => new PlanningItemRef { Soort = "fase", Id = w.Id }).ToList() });
            if (fout != null) return fout;
        }
        _db.BouwPlanning.Remove(bp);
        await _db.SaveChangesAsync();
        d.Planningen.Remove(bp);
        return null;
    }

    // ── Momentopname (ongedaan maken) ──

    private sealed record SnapSectie(int Id, string Naam, int? Ouder, int Volgorde, int? Lot, bool TeltMee, int Planning = 0);
    private sealed record SnapTaak(int Id, int Sectie, string Naam, string? Besch, int Duur, int Volgorde, byte Beperking, DateOnly? BD, DateOnly? Deadline,
        DateOnly? WStart, DateOnly? AfgerondOp, bool IsAfgerond, int Voortgang, string? Wie, DateOnly? BasisStart, DateOnly? BasisEinde);
    private sealed record SnapLink(int Voor, int Op, byte Type, int Wacht);
    private sealed record Snap(List<SnapSectie> S, List<SnapTaak> T, List<SnapLink> L, bool Verlet, DateTime? BasisOp);

    private static string Momentopname(Data d)
    {
        var snap = new Snap(
            d.Secties.Select(s => new SnapSectie(s.Id, s.Naam, s.ParentSectieId, s.Volgorde, s.ActivityGroupId, s.TeltMeeVoorVoortgang, d.PlanningVan(s))).ToList(),
            d.Taken.Select(t => new SnapTaak(t.Id, t.PlanningSectieId, t.Naam, t.Beschrijving, t.Duur, t.Volgorde, t.BeperkingType, t.BeperkingDatum, t.Deadline,
                t.WerkelijkStart, t.AfgerondOp, t.IsAfgerond, t.VoortgangPct, t.VerantwoordelijkeUserId, t.BasisStart, t.BasisEinde)).ToList(),
            d.Links.Select(l => new SnapLink(l.VoorgangerTaakId, l.OpvolgerTaakId, l.Type, l.WachtDagen)).ToList(),
            d.Kop?.VerletMeetellen ?? true,
            d.Kop?.BasisVastgelegdOp);
        return JsonSerializer.Serialize(snap, Json);
    }

    private async Task<string?> Herstel(Data d, PlanningCommando c)
    {
        if (string.IsNullOrWhiteSpace(c.Momentopname)) return "Er is niets om ongedaan te maken.";
        Snap? snap;
        try { snap = JsonSerializer.Deserialize<Snap>(c.Momentopname, Json); }
        catch (JsonException) { return "De momentopname is ongeldig."; }
        if (snap == null) return "De momentopname is ongeldig.";

        // 1. alle afhankelijkheden weg (worden op het einde opnieuw aangemaakt)
        var taakIds = d.Taken.Select(t => t.Id).ToList();
        var bestaandeLinks = await _db.PlanningAfhankelijkheid.Where(l => taakIds.Contains(l.OpvolgerTaakId) || taakIds.Contains(l.VoorgangerTaakId)).ToListAsync();
        _db.PlanningAfhankelijkheid.RemoveRange(bestaandeLinks);
        await _db.SaveChangesAsync();
        d.Links.Clear();

        // 2. fases: ouders vóór kinderen
        int Diepte(SnapSectie s) { var n = 0; var p = s.Ouder; while (p != null) { n++; p = snap.S.FirstOrDefault(x => x.Id == p)?.Ouder; if (n > 50) break; } return n; }
        var sectieMap = new Dictionary<int, int>();
        var bestaandS = d.Secties.ToDictionary(s => s.Id);
        foreach (var ss in snap.S.OrderBy(Diepte))
        {
            if (!bestaandS.TryGetValue(ss.Id, out var s))
            {
                s = new PlanningSectie { ProjectId = d.Project.ProjectId, IsActief = true };
                _db.PlanningSectie.Add(s);
                d.Secties.Add(s);
            }
            s.Naam = ss.Naam;
            s.Volgorde = ss.Volgorde;
            s.ActivityGroupId = ss.Lot;
            s.TeltMeeVoorVoortgang = ss.TeltMee;
            // Een planning die intussen verwijderd is, valt terug op de hoofdplanning.
            s.BouwPlanningId = d.Planningen.Any(x => x.Id == ss.Planning) ? ss.Planning : (d.HoofdId != 0 ? d.HoofdId : null);
            s.ParentSectieId = ss.Ouder is int o && sectieMap.TryGetValue(o, out var nieuwOuder) ? nieuwOuder : null;
            await _db.SaveChangesAsync();
            sectieMap[ss.Id] = s.Id;
        }

        // 3. taken
        var taakMap = new Dictionary<int, int>();
        var bestaandT = d.Taken.ToDictionary(t => t.Id);
        foreach (var st in snap.T)
        {
            if (!sectieMap.TryGetValue(st.Sectie, out var sid)) continue;
            if (!bestaandT.TryGetValue(st.Id, out var t))
            {
                t = new PlanningTaak { PlanningSectieId = sid, Naam = st.Naam };
                _db.PlanningTaak.Add(t);
                d.Taken.Add(t);
            }
            t.PlanningSectieId = sid;
            t.Naam = st.Naam; t.Beschrijving = st.Besch; t.Duur = st.Duur; t.Volgorde = st.Volgorde;
            t.BeperkingType = st.Beperking; t.BeperkingDatum = st.BD; t.Deadline = st.Deadline;
            t.WerkelijkStart = st.WStart; t.AfgerondOp = st.AfgerondOp; t.IsAfgerond = st.IsAfgerond; t.VoortgangPct = st.Voortgang;
            t.VerantwoordelijkeUserId = st.Wie; t.BasisStart = st.BasisStart; t.BasisEinde = st.BasisEinde;
            await _db.SaveChangesAsync();
            taakMap[st.Id] = t.Id;
        }

        // 4. wat niet in de momentopname staat, verdwijnt (taken eerst, dan fases van diep naar ondiep)
        var teBehoudenT = taakMap.Values.ToHashSet();
        _db.PlanningTaak.RemoveRange(d.Taken.Where(t => !teBehoudenT.Contains(t.Id)));
        await _db.SaveChangesAsync();
        var teBehoudenS = sectieMap.Values.ToHashSet();
        int DiepteDb(PlanningSectie s) { var n = 0; var p = s.ParentSectieId; while (p != null) { n++; p = d.Secties.FirstOrDefault(x => x.Id == p)?.ParentSectieId; if (n > 50) break; } return n; }
        foreach (var s in d.Secties.Where(s => !teBehoudenS.Contains(s.Id)).OrderByDescending(DiepteDb).ToList())
        {
            _db.PlanningSectie.Remove(s);
            await _db.SaveChangesAsync();
        }

        // 5. afhankelijkheden terug
        foreach (var sl in snap.L)
        {
            if (!taakMap.TryGetValue(sl.Voor, out var voor) || !taakMap.TryGetValue(sl.Op, out var op)) continue;
            var l = new PlanningAfhankelijkheid { VoorgangerTaakId = voor, OpvolgerTaakId = op, Type = sl.Type, WachtDagen = sl.Wacht };
            _db.PlanningAfhankelijkheid.Add(l);
        }

        // 6. mijlpalen die naar een heraangemaakte taak/fase wezen, volgen het nieuwe id
        foreach (var (oud, nieuw) in taakMap.Where(kv => kv.Key != kv.Value))
            foreach (var m in await _db.Mijlpaal.Where(m => m.BronBinding == 4 && m.BronRefId == oud && m.Projecttraject.ProjectId == d.Project.ProjectId).ToListAsync())
                m.BronRefId = nieuw;
        foreach (var (oud, nieuw) in sectieMap.Where(kv => kv.Key != kv.Value))
            foreach (var m in await _db.Mijlpaal.Where(m => m.BronBinding == 5 && m.BronRefId == oud && m.Projecttraject.ProjectId == d.Project.ProjectId).ToListAsync())
                m.BronRefId = nieuw;

        if (d.Kop != null) { d.Kop.VerletMeetellen = snap.Verlet; d.Kop.BasisVastgelegdOp = snap.BasisOp; }
        await _db.SaveChangesAsync();
        return null;
    }
}
