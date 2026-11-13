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
    private static readonly Regex VoorgangerRegex = new(@"^\s*(\d+(?:\.\d+)*)\s*(FS|SS|FF|SF)?\s*([+-]\s*\d+)?\s*d?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly cpmRunningContext _db;

    public PlanningService(cpmRunningContext db) => _db = db;

    // ── Interne gegevens ──

    private sealed class Data
    {
        public required Project Project { get; init; }
        public ProjectPlanning? Kop { get; set; }
        public List<PlanningSectie> Secties { get; set; } = new();
        public List<PlanningTaak> Taken { get; set; } = new();
        public List<PlanningAfhankelijkheid> Links { get; set; } = new();
        public required WerkKalender Kalender { get; init; }
        public bool HeeftWeerstation { get; init; }
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

    private static List<Knoop> BouwBoom(Data d)
    {
        int Soort(Knoop k) => k.IsFase ? 1 : 0;

        List<Knoop> Kinderen(PlanningSectie? ouder, Knoop? ouderKnoop)
        {
            var lijst = new List<Knoop>();
            if (ouder != null)
                lijst.AddRange(d.Taken.Where(t => t.PlanningSectieId == ouder.Id).Select(t => new Knoop { Taak = t, Ouder = ouderKnoop }));
            lijst.AddRange(d.Secties.Where(s => s.ParentSectieId == ouder?.Id).Select(s => new Knoop { IsFase = true, Sectie = s, Ouder = ouderKnoop }));
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

    private static IEnumerable<Knoop> Taken(Knoop fase) => Plat(fase.Kinderen).Where(k => !k.IsFase);

    // ── Doorrekenen ──

    private sealed record Doorgerekend(PlanResultaat Resultaat, List<Knoop> Boom, bool MetVerlet, DateOnly ProjectStart);

    private static Doorgerekend Rekenen(Data d)
    {
        var boom = BouwBoom(d);
        var metVerlet = d.Kop?.VerletMeetellen ?? true;
        var start = d.Project.StartDateConstruction ?? DateOnly.FromDateTime(DateTime.Today);
        var planTaken = Plat(boom).Where(k => !k.IsFase).Select(k => new PlanTaak(
            k.Taak!.Id,
            Math.Max(0, k.Taak.Duur),
            d.Links.Where(l => l.OpvolgerTaakId == k.Taak.Id)
                   .Select(l => new PlanLink(l.VoorgangerTaakId, (PlanLinkType)l.Type, l.WachtDagen)).ToList(),
            (PlanBeperking)k.Taak.BeperkingType,
            k.Taak.BeperkingDatum,
            k.Taak.Deadline,
            k.Taak.WerkelijkStart,
            k.Taak.IsAfgerond ? k.Taak.AfgerondOp : null,
            k.Taak.IsAfgerond)).ToList();

        var res = PlanningCalculator.Bereken(planTaken, start, d.Kalender, metVerlet);

        // Berekende datums bewaren (cache voor traject, dashboards en voortgang).
        foreach (var k in Plat(boom).Where(k => !k.IsFase))
        {
            if (!res.Taken.TryGetValue(k.Taak!.Id, out var r)) continue;
            if (k.Taak.Startdatum != r.Start) k.Taak.Startdatum = r.Start;
            if (k.Taak.Einddatum != r.Einde) k.Taak.Einddatum = r.Einde;
        }
        return new Doorgerekend(res, boom, metVerlet, start);
    }

    // ── Staat opbouwen ──

    public async Task<PlanningStaat> Laad(int projectId)
    {
        var d = await LaadData(projectId);
        if (d == null) return new PlanningStaat { ProjectId = projectId };
        return await Bouw(d, Rekenen(d));
    }

    private static string? Iso(DateOnly? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Dd(DateOnly d) => d.ToString("dd/MM/yyyy", Nl);

    private async Task<PlanningStaat> Bouw(Data d, Doorgerekend dr)
    {
        var res = dr.Resultaat;
        var kal = d.Kalender;
        var metVerlet = dr.MetVerlet;
        var staat = new PlanningStaat
        {
            ProjectId = d.Project.ProjectId,
            Versie = d.Kop?.Versie ?? 0,
            HeeftPlanning = d.Taken.Count > 0 || d.Secties.Count > 0,
        };

        // WBS per taak-id, voor de weergave van voorgangers.
        var wbsPerTaak = Plat(dr.Boom).Where(k => !k.IsFase).ToDictionary(k => k.Taak!.Id, k => k.Wbs);

        // Gekoppelde trajectmijlpalen (de koppeling staat aan de trajectkant: BronBinding = PlanningTaak/PlanningSectie).
        var koppelingen = await _db.Mijlpaal.AsNoTracking()
            .Where(m => m.Projecttraject.ProjectId == d.Project.ProjectId && (m.BronBinding == 4 || m.BronBinding == 5))
            .Select(m => new { m.Id, m.Naam, m.BronBinding, m.BronRefId, m.BronParam })
            .ToListAsync();
        List<PlanningMijlpaalLink> LinksVoor(int binding, int id) => koppelingen
            .Where(m => m.BronBinding == binding && (m.BronRefId ?? (int.TryParse((m.BronParam ?? "").Trim(), out var x) ? x : (int?)null)) == id)
            .Select(m => new PlanningMijlpaalLink { Id = m.Id, Naam = m.Naam }).ToList();

        var lots = await _db.ActivityGroup.AsNoTracking().ToDictionaryAsync(g => g.GroupId, g => g.Name);

        foreach (var k in Plat(dr.Boom))
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

        // Kop
        var kop = staat.Kop;
        kop.VerletMeetellen = metVerlet;
        kop.GeenWeerstation = !d.HeeftWeerstation;
        kop.GeenStartdatum = d.Project.StartDateConstruction == null;
        kop.ProjectStart = Iso(d.Project.StartDateConstruction);
        kop.Uitvoeringstermijn = d.Project.ExecutionDays;
        if (res.Start is DateOnly ps && res.Einde is DateOnly pe)
        {
            kop.Start = Iso(ps);
            kop.Einde = Iso(pe);
            kop.DuurWerkdagen = kal.Tel(ps.AddDays(-1), pe, metVerlet);
            kop.VerletDagen = kal.VerletDagen(ps, pe).Count();
        }
        var basisEinden = d.Taken.Where(t => t.BasisEinde != null).Select(t => t.BasisEinde!.Value).ToList();
        if (basisEinden.Count > 0 && res.Einde is DateOnly e2)
        {
            kop.BasisOp = d.Kop?.BasisVastgelegdOp?.ToString("dd/MM/yyyy", Nl);
            kop.BasisAfwijking = Verschil(kal, basisEinden.Max(), e2, metVerlet);
        }

        DateOnly? deadline = null;
        if (d.Project.StartDateConstruction is DateOnly sd && (d.Project.ExecutionDays ?? 0) > 0)
        {
            deadline = kal.Voeg(sd, d.Project.ExecutionDays!.Value);
            kop.Deadline = Iso(deadline);
            if (res.Einde is DateOnly e3) kop.Marge = Verschil(kal, e3, deadline.Value, metVerlet);
        }

        // Nietwerkdagen rond de planning (voor de arcering in de Gantt)
        if (res.Start is DateOnly s0 && res.Einde is DateOnly e0)
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
        if (deadline is DateOnly dl && res.Einde is DateOnly pe2 && pe2 > dl)
            staat.Meldingen.Add(new PlanningMelding
            {
                Ernst = "danger",
                Titel = $"Einde {Dd(pe2)} valt na de voorziene oplevering {Dd(dl)}",
                Tekst = $"{Dagen(kal.Tel(dl, pe2, metVerlet))} te laat.",
            });

        // Klanten met een afwijkende uitvoeringstermijn of aanvangsdatum
        if (d.Project.StartDateConstruction is DateOnly pStart && res.Einde is DateOnly pEinde)
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
                Staat = await Bouw(d, Rekenen(d)),
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
                _ => "Onbekende actie.",
            };
            if (fout != null) { await tx.RollbackAsync(); _db.ChangeTracker.Clear(); return new PlanningUitkomst { Fout = fout }; }

            // Opnieuw laden zodat de boom klopt met wat er net is bewaard, dan doorrekenen.
            await _db.SaveChangesAsync();
            d = (await LaadData(projectId))!;
            var dr = Rekenen(d);
            if (dr.Resultaat.Cyclus.Count > 0 && c.Type != "herstel")
            {
                var wbs = Plat(dr.Boom).Where(k => !k.IsFase && dr.Resultaat.Cyclus.Contains(k.Taak!.Id)).Select(k => k.Wbs).ToList();
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

            var uitkomst = new PlanningUitkomst { Ok = true, Undo = voorSnap, Staat = await Bouw(d, dr) };

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

    private static string? BasisVastleggen(Data d)
    {
        var dr = Rekenen(d);
        foreach (var k in Plat(dr.Boom).Where(k => !k.IsFase))
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

    private static List<Knoop> Broers(Data d, PlanningSectie? ouder)
    {
        var tmp = BouwBoom(d);
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
    private static void Plaats(Data d, PlanningSectie? ouder, bool isFase, int id, int? naId, string? naSoort)
    {
        var lijst = Broers(d, ouder);
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
    private static string? ParseVoorgangers(string? tekst, Data d, int? eigenTaakId, out List<(int Taak, byte Type, int Wacht)> uit)
    {
        uit = new List<(int, byte, int)>();
        if (string.IsNullOrWhiteSpace(tekst)) return null;
        var boom = BouwBoom(d);
        var perWbs = Plat(boom).ToDictionary(k => k.Wbs, k => k);

        foreach (var deel in tekst.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var m = VoorgangerRegex.Match(deel);
            if (!m.Success) return $"“{deel.Trim()}” is geen geldige voorganger. Typ bv. 5.1, 5.1SS of 5.1FS+3d.";
            if (!perWbs.TryGetValue(m.Groups[1].Value, out var knoop)) return $"Taak {m.Groups[1].Value} bestaat niet.";
            var type = m.Groups[2].Success ? (byte)Enum.Parse<PlanLinkType>(m.Groups[2].Value.ToUpperInvariant()) : (byte)PlanLinkType.FS;
            var wacht = m.Groups[3].Success ? int.Parse(m.Groups[3].Value.Replace(" ", "")) : 0;
            var bronnen = knoop.IsFase ? Taken(knoop).Select(k => k.Taak!.Id) : new[] { knoop.Taak!.Id };
            foreach (var id in bronnen)
            {
                if (id == eigenTaakId) return "Een taak kan niet op zichzelf wachten.";
                if (!uit.Any(x => x.Taak == id)) uit.Add((id, type, wacht));
            }
        }
        return null;
    }

    private void ZetVoorgangers(int opvolgerId, List<(int Taak, byte Type, int Wacht)> nieuw, Data d)
    {
        foreach (var l in d.Links.Where(l => l.OpvolgerTaakId == opvolgerId).ToList())
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
        if (sectie == null)
        {
            // Zonder fase: de laatste fase van het hoogste niveau, of een nieuwe "Fase 1".
            sectie = d.Secties.Where(s => s.ParentSectieId == null).OrderByDescending(s => s.Volgorde).FirstOrDefault();
            if (sectie == null)
            {
                sectie = new PlanningSectie { ProjectId = d.Project.ProjectId, Naam = "Fase 1", Volgorde = 10, IsActief = true };
                _db.PlanningSectie.Add(sectie);
                await _db.SaveChangesAsync();
                d.Secties.Add(sectie);
            }
        }

        string? voorFout = null;
        List<(int Taak, byte Type, int Wacht)> voorgangers = new();
        if (v.TryGetValue("voorgangers", out var vg) && vg.ValueKind == JsonValueKind.String)
            voorFout = ParseVoorgangers(vg.GetString(), d, null, out voorgangers);
        if (voorFout != null) return voorFout;

        var taak = new PlanningTaak { PlanningSectieId = sectie.Id, Naam = "", Duur = 1, Volgorde = int.MaxValue / 2 };
        var fout = ZetTaakVelden(d, taak, v, true);
        if (fout != null) return fout;
        if (string.IsNullOrWhiteSpace(taak.Naam)) return "Geef de taak een naam.";

        _db.PlanningTaak.Add(taak);
        await _db.SaveChangesAsync();
        d.Taken.Add(taak);
        ZetVoorgangers(taak.Id, voorgangers, d);
        await _db.SaveChangesAsync();
        Plaats(d, sectie, false, taak.Id, c.NaId, c.NaSoort);
        bewerkt.Add("t" + taak.Id);
        return null;
    }

    private async Task<string?> FaseToevoegen(Data d, PlanningCommando c, HashSet<string> bewerkt)
    {
        var v = c.Velden ?? new Dictionary<string, JsonElement>();
        var ouder = c.SectieId is int pid ? d.Secties.FirstOrDefault(s => s.Id == pid) : null;
        if (c.SectieId != null && ouder == null) return "De bovenliggende fase bestaat niet.";

        List<(int Taak, byte Type, int Wacht)> startNa = new();
        var fout = ParseVoorgangers(c.StartNa, d, null, out startNa);
        if (fout != null) return fout;

        var sectie = new PlanningSectie { ProjectId = d.Project.ProjectId, Naam = "", ParentSectieId = ouder?.Id, Volgorde = int.MaxValue / 2, IsActief = true };
        fout = ZetFaseVelden(sectie, v);
        if (fout != null) return fout;
        if (string.IsNullOrWhiteSpace(sectie.Naam)) return "Geef de fase een naam.";

        _db.PlanningSectie.Add(sectie);
        await _db.SaveChangesAsync();
        d.Secties.Add(sectie);
        Plaats(d, ouder, true, sectie.Id, c.NaId, c.NaSoort);
        bewerkt.Add("f" + sectie.Id);

        // Een fase is een groep: ze bestaat uit taken. De eerste taak (optioneel) krijgt de "start na"-voorgangers.
        var eerste = (c.EersteTaakNaam ?? "").Trim();
        if (eerste.Length > 0 || startNa.Count > 0)
        {
            var taak = new PlanningTaak { PlanningSectieId = sectie.Id, Naam = eerste.Length > 0 ? eerste : "Eerste taak", Duur = Math.Clamp(c.EersteTaakDuur ?? 1, 0, 2000), Volgorde = 10 };
            _db.PlanningTaak.Add(taak);
            await _db.SaveChangesAsync();
            d.Taken.Add(taak);
            ZetVoorgangers(taak.Id, startNa, d);
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
            var fout = ParseVoorgangers(vg.ValueKind == JsonValueKind.String ? vg.GetString() : null, d, id, out var lijst);
            if (fout != null) return fout;
            voorgangers = lijst;
        }
        var f = ZetTaakVelden(d, t, v, false);
        if (f != null) return f;
        if (voorgangers != null) ZetVoorgangers(id, voorgangers, d);
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
        var boom = BouwBoom(d);
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

    // ── Momentopname (ongedaan maken) ──

    private sealed record SnapSectie(int Id, string Naam, int? Ouder, int Volgorde, int? Lot, bool TeltMee);
    private sealed record SnapTaak(int Id, int Sectie, string Naam, string? Besch, int Duur, int Volgorde, byte Beperking, DateOnly? BD, DateOnly? Deadline,
        DateOnly? WStart, DateOnly? AfgerondOp, bool IsAfgerond, int Voortgang, string? Wie, DateOnly? BasisStart, DateOnly? BasisEinde);
    private sealed record SnapLink(int Voor, int Op, byte Type, int Wacht);
    private sealed record Snap(List<SnapSectie> S, List<SnapTaak> T, List<SnapLink> L, bool Verlet, DateTime? BasisOp);

    private static string Momentopname(Data d)
    {
        var snap = new Snap(
            d.Secties.Select(s => new SnapSectie(s.Id, s.Naam, s.ParentSectieId, s.Volgorde, s.ActivityGroupId, s.TeltMeeVoorVoortgang)).ToList(),
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
