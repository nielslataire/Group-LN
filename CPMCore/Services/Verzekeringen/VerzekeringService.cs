using BOCore;
using CPMCore.Models.GlV2;
using CPMCore.Models.Projecten;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Budget;
using System.Globalization;

namespace CPMCore.Services.Verzekeringen;

/// <summary>Verzekeringen van een project (design-handoff punt 43): polissen, attesten van aannemers en de aandachtspunten die er
/// automatisch uit volgen. Eén plaats voor alle berekeningen, zodat overzicht, attestentab en formulier dezelfde cijfers tonen.
///
/// Een polis is een rij in <c>Insurances</c>. Vóór migratie 110 hing elke polis aan een contractactiviteit van een makelaar; nu mag dat ontbreken
/// (polis rechtstreeks bij de maatschappij) en bewaart de rij zelf het project. Attesten van aannemers (tienjarige per woning, BA) staan in
/// <c>InsuranceAttest</c>, per contract. De aandachtspunten worden telkens opnieuw afgeleid en nergens bewaard: ze verdwijnen vanzelf als de oorzaak weg is.</summary>
public class VerzekeringService
{
    private readonly cpmRunningContext _db;
    private readonly IProjectService _projects;
    private readonly BudgetBerekeningService _budget;
    private readonly IDocumentService _docs;
    private static readonly CultureInfo Be = CultureInfo.GetCultureInfo("nl-BE");

    public const int AttestTienjarige = 0;
    public const int AttestBa = 1;
    public const int AttestOntbreekt = 0;
    public const int AttestOntvangen = 1;
    public const int AttestNietVereist = 2;
    /// <summary>Activiteit "Verzekeringen" (lot in de activiteitencatalogus): contracten die enkel hieruit bestaan zijn contracten met een makelaar.</summary>
    public const int VerzekeringActiviteitId = 142;
    private const int TienjarigeStandaardJaren = 10;
    private const int WaarschuwDagen = 30;

    /// <summary>Werken die onder de tienjarige aansprakelijkheid vallen (wet Peeters-Borsus): stabiliteit, waterdichtheid, dak.
    /// De keuze wordt per contract afgeleid uit de naam van de activiteitengroep en de activiteit; de gebruiker kan ze per contract wijzigen.</summary>
    private static readonly string[] TienjarigeSleutels = { "ruwbouw", "onderbouw", "bovenbouw", "dak", "stabiliteit", "fundering", "structuur", "gevel", "architect", "ingenieur", "studiebureau" };

    public VerzekeringService(cpmRunningContext db, IProjectService projects, BudgetBerekeningService budget, IDocumentService docs)
    {
        _db = db;
        _projects = projects;
        _budget = budget;
        _docs = docs;
    }

    // ── Hulpfuncties ────────────────────────────────────────────────────────────────────────────

    private static string Geld(decimal v) => "€ " + v.ToString("N0", Be);
    private static string Dat(DateOnly d) => d.ToString("dd/MM/yyyy", Be);
    private static DateOnly Vandaag => DateOnly.FromDateTime(DateTime.Today);

    public static string TypeNaam(int type) => type switch
    {
        1 => "ABR", 2 => "Brand", 3 => "Tienjarige", 4 => "BA-uitbating", _ => "Andere"
    };

    private static string TypeKey(int type) => type switch { 1 => "abr", 2 => "brand", 3 => "tienjarige", 4 => "ba", _ => "andere" };

    // ── Polissen ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Berekende looptijd van één polis.</summary>
    private sealed class Looptijd
    {
        public DateOnly? Start { get; set; }
        /// <summary>Einde van de werken/dekking (ABR: termijn + verlenging).</summary>
        public DateOnly? Einde { get; set; }
        /// <summary>Einde van de oorspronkelijke termijn, zonder verlenging (enkel ABR).</summary>
        public DateOnly? TermijnEinde { get; set; }
        public DateOnly? WaarborgEinde { get; set; }
        /// <summary>Einde komt uit een ingevulde einddatum (de gebruiker beëindigde de polis of vulde ze in), niet uit de termijn.</summary>
        public bool EindeIsIngevuld { get; set; }
    }

    private static Looptijd Bereken(Insurances p, DateOnly? vo)
    {
        var l = new Looptijd { Start = p.StartAtDelivery && vo.HasValue ? vo : p.Startdate };
        if (p.Type == 1 && l.Start.HasValue && (p.Period ?? 0) > 0)
        {
            l.TermijnEinde = l.Start.Value.AddMonths(p.Period!.Value);
            l.Einde = l.Start.Value.AddMonths(p.Period.Value + (p.ExtensionPeriod ?? 0));
            if ((p.GuaranteePeriod ?? 0) > 0) l.WaarborgEinde = l.Einde.Value.AddMonths(p.GuaranteePeriod!.Value);
            if (p.Enddate.HasValue)
            {
                l.EindeIsIngevuld = true;
                // Een einddatum vóór het berekende einde (de polis werd vroeger beëindigd) wint. Een latere (de oude pagina vult termijn,
                // verlenging én waarborgperiode in als einddatum) verandert niets aan het einde van de werken.
                if (p.Enddate.Value < l.Einde.Value) { l.Einde = p.Enddate; l.WaarborgEinde = null; }
            }
            return l;
        }
        if (p.Type == 3 && l.Start.HasValue && !p.Enddate.HasValue)
        {
            l.Einde = l.Start.Value.AddYears(p.DurationYears ?? TienjarigeStandaardJaren);
            return l;
        }
        l.Einde = p.Enddate;
        l.EindeIsIngevuld = p.Enddate.HasValue;
        return l;
    }

    private sealed class PolisStatus
    {
        public string Label { get; set; } = "";
        public string Tone { get; set; } = "is-neutral";
        public string Key { get; set; } = "lopend";
        public bool Beeindigd { get; set; }
        public bool Lopend { get; set; }
        public int? DagenTotEinde { get; set; }
    }

    private static PolisStatus Status(Insurances p, Looptijd l, DateOnly? vo)
    {
        var v = Vandaag;
        if (p.IsRequested) return new PolisStatus { Label = "Aangevraagd", Tone = "is-attention", Key = "aandacht" };
        if (l.Start.HasValue && l.Start.Value > v) return new PolisStatus { Label = "Start " + Dat(l.Start.Value), Tone = "is-info", Key = "lopend" };

        if (p.Type == 1 && l.Einde.HasValue)
        {
            var verlengd = l.TermijnEinde.HasValue && v > l.TermijnEinde.Value && (p.ExtensionPeriod ?? 0) > 0;
            if (v <= l.Einde.Value)
            {
                var dagen = l.Einde.Value.DayNumber - v.DayNumber;
                if (dagen <= WaarschuwDagen) return new PolisStatus { Label = $"Vervalt over {dagen} d.", Tone = "is-attention", Key = "aandacht", Lopend = true, DagenTotEinde = dagen };
                return new PolisStatus { Label = verlengd ? "Lopend · verlengd" : "Lopend", Tone = "is-positive", Lopend = true, DagenTotEinde = dagen };
            }
            if (l.WaarborgEinde.HasValue && v <= l.WaarborgEinde.Value) return new PolisStatus { Label = "Waarborgperiode", Tone = "is-info", Key = "lopend", Lopend = true };
            if (p.Enddate.HasValue || (vo.HasValue && vo.Value <= v)) return new PolisStatus { Label = "Beëindigd", Tone = "is-neutral", Key = "beeindigd", Beeindigd = true };
            return new PolisStatus { Label = "Vervallen", Tone = "is-blocked", Key = "aandacht" };
        }

        if (l.Einde.HasValue)
        {
            if (l.Einde.Value < v) return new PolisStatus { Label = l.EindeIsIngevuld ? "Beëindigd" : "Verlopen", Tone = "is-neutral", Key = "beeindigd", Beeindigd = true };
            var dagen = l.Einde.Value.DayNumber - v.DayNumber;
            if (dagen <= WaarschuwDagen) return new PolisStatus { Label = $"Vervalt over {dagen} d.", Tone = "is-attention", Key = "aandacht", Lopend = true, DagenTotEinde = dagen };
            return new PolisStatus { Label = dagen <= 120 ? $"Vervalt over {dagen} d." : "Lopend", Tone = dagen <= 120 ? "is-attention" : "is-positive", Key = dagen <= 120 ? "aandacht" : "lopend", Lopend = true, DagenTotEinde = dagen };
        }
        return new PolisStatus { Label = "Lopend", Tone = "is-positive", Lopend = true };
    }

    private async Task<List<Insurances>> LaadPolissenAsync(int projectId)
    {
        return await _db.Insurances.AsNoTracking()
            .Include(i => i.InsuranceCompany)
            .Include(i => i.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(c => c.Company)
            .Where(i => i.ProjectId == projectId || (i.ContractActivity != null && i.ContractActivity.Contract.ProjectId == projectId))
            .OrderBy(i => i.Type).ThenBy(i => i.Id)
            .ToListAsync();
    }

    // ── Overzicht (43a + 43b) ───────────────────────────────────────────────────────────────────

    public async Task<VerzekeringenV2Vm> GetOverzichtAsync(int projectId, string tab, bool canWrite, bool canDelete, bool metBudget = true)
    {
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId)
            .Select(p => new { p.ProjectName, p.DeliveryDate, p.DeliveryDateDef, p.StartDateConstruction, p.TienjarigeBron, p.TienjarigeOpmerking }).FirstOrDefaultAsync();
        var vo = project?.DeliveryDate;
        var doDatum = project?.DeliveryDateDef;
        var projectNaam = project?.ProjectName ?? "";

        var polissen = await LaadPolissenAsync(projectId);
        var units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId && !u.IsLink)
            .OrderBy(u => u.Name)
            .Select(u => new EenheidInfo
            {
                Id = u.Id, Naam = u.Name ?? "", Verkocht = u.ClientAccountId != null, KoperId = u.ClientAccountId,
                Koper = u.ClientAccount != null ? u.ClientAccount.Name ?? "" : "",
                Groep = u.Type != null && u.Type.Group != null ? u.Type.Group.Name : ""
            })
            .ToListAsync();
        var contracten = await _db.Contract.AsNoTracking()
            .Include(c => c.Company)
            .Include(c => c.ContractActivity).ThenInclude(a => a.Activity).ThenInclude(a => a.Group)
            .Where(c => c.ProjectId == projectId).OrderBy(c => c.Id).ToListAsync();
        var attesten = await _db.InsuranceAttest.AsNoTracking().Where(a => a.ProjectId == projectId).ToListAsync();
        var attestIds = attesten.Select(a => a.Id).ToList();
        var attestUnits = await _db.InsuranceAttestUnit.AsNoTracking().Where(x => attestIds.Contains(x.AttestId)).ToListAsync();

        var vm = new VerzekeringenV2Vm
        {
            ProjectId = projectId, ProjectName = projectNaam, CanWrite = canWrite, CanDelete = canDelete,
            Tab = tab == "attesten" ? "attesten" : "polissen",
        };

        var woningen = units.Where(u => u.Groep.Contains("woon", StringComparison.OrdinalIgnoreCase)).ToList();
        vm.Woningen = woningen.Select(u => new VzWoning { Id = u.Id, Naam = u.Naam, Koper = u.Koper }).ToList();
        var rijen = BouwPolisRijen(projectId, polissen, vo, attesten, contracten, out var statussen);
        var tjPolissen = polissen.Where(p => p.Type == 3 && statussen.TryGetValue(p.Id, out var st) && !st.Beeindigd).ToList();
        var tjHeel = tjPolissen.Any(p => p.UnitId == null);
        var tjDekking = new TjDekking
        {
            Heeft = tjPolissen.Count > 0, HeelProject = tjHeel, Totaal = woningen.Count,
            Ongedekt = tjHeel ? new List<EenheidInfo>() : woningen.Where(w => tjPolissen.All(p => p.UnitId != w.Id)).ToList(),
        };
        tjDekking.Gedekt = woningen.Count - tjDekking.Ongedekt.Count;
        vm.TjProjectBron = project?.TienjarigeBron;
        vm.TjProjectOpmerking = project?.TienjarigeOpmerking;
        vm.Attesten = BouwAttesten(contracten, attesten, attestUnits, woningen.Count, tjDekking, project?.TienjarigeBron, project?.TienjarigeOpmerking);
        var kopers = await KopersStatusAsync(attesten, attestUnits, units, polissen);
        var (puntenOpen, ontbrekend) = await BouwAandachtspuntenAsync(projectId, projectNaam, polissen, statussen, units, vm.Attesten, contracten, kopers, tjDekking, woningen, project?.TienjarigeBron, project?.TienjarigeOpmerking, vo, doDatum, metBudget);
        vm.Aandachtspunten = puntenOpen;
        vm.Polissen = rijen;
        foreach (var ont in ontbrekend) vm.Polissen.Insert(Math.Min(vm.Polissen.Count, ont.Plaats), ont.Rij);
        vm.Tijdlijn = BouwTijdlijn(polissen, statussen, vo, doDatum, project?.StartDateConstruction, vm.Attesten, ontbrekend.Any(o => o.Rij.TypeKey == "brand"));
        vm.Kpis = BouwKpis(vm, polissen, statussen, vo, ontbrekend.Count);
        return vm;
    }

    private sealed class EenheidInfo
    {
        public int Id { get; set; }
        public string Naam { get; set; } = "";
        public bool Verkocht { get; set; }
        public int? KoperId { get; set; }
        public string Koper { get; set; } = "";
        public string Groep { get; set; } = "";
    }

    /// <summary>Wat de lopende tienjarige polissen van het project dekken: één polis voor het hele project (UnitId leeg), of per woning of appartement een eigen polis.</summary>
    private sealed class TjDekking
    {
        public bool Heeft { get; set; }
        public bool HeelProject { get; set; }
        public int Gedekt { get; set; }
        public int Totaal { get; set; }
        public List<EenheidInfo> Ongedekt { get; set; } = new();
        public bool Volledig => Heeft && (HeelProject || Ongedekt.Count == 0);
    }

    private sealed class OntbrekendePolis
    {
        public VzPolisRij Rij { get; set; } = new();
        public int Plaats { get; set; }
    }

    private List<VzPolisRij> BouwPolisRijen(int projectId, List<Insurances> polissen, DateOnly? vo, List<InsuranceAttest> attesten, List<Contract> contracten,
        out Dictionary<int, PolisStatus> statussen)
    {
        statussen = new Dictionary<int, PolisStatus>();
        var rijen = new List<VzPolisRij>();
        foreach (var p in polissen)
        {
            var l = Bereken(p, vo);
            var st = Status(p, l, vo);
            statussen[p.Id] = st;
            var makelaar = p.ContractActivity?.Contract?.Company?.BedrijfsNaam;
            var soort = TypeNaam(p.Type);
            if (p.Type == 2) soort = p.Building == 1 ? "Brand nieuwbouw" : p.Building == 0 ? "Brand bestaand gebouw" : "Brand";
            var rij = new VzPolisRij
            {
                Id = p.Id, TypeKey = TypeKey(p.Type), Soort = soort,
                Maatschappij = p.InsuranceCompany?.Name ?? "—",
                MaatschappijSub = string.IsNullOrWhiteSpace(makelaar) ? "rechtstreeks" : makelaar!,
                Polisnummer = string.IsNullOrWhiteSpace(p.Polisnummer) ? "—" : p.Polisnummer!,
                Waarde = p.InsuredValue.HasValue ? Geld(p.InsuredValue.Value) : "—",
                Status = st.Label, StatusTone = st.Tone, StatusKey = st.Key, IsBeeindigd = st.Beeindigd, DocumentId = p.DocumentId,
            };
            // Dekking (ingevulde start en einde, of de afgeleide termijn) met subregels per type
            if (p.StartAtDelivery && vo.HasValue && p.Type is 2 or 3) { rij.Dekking = "vanaf " + Dat(vo.Value); }
            else if (l.Start.HasValue && l.Einde.HasValue) rij.Dekking = Dat(l.Start.Value) + " – " + Dat(l.Einde.Value);
            else if (l.Start.HasValue) rij.Dekking = "vanaf " + Dat(l.Start.Value);
            else if (p.StartAtDelivery) rij.Dekking = "start bij voorlopige oplevering";
            if (p.Type == 1)
            {
                if ((p.ExtensionPeriod ?? 0) > 0 && l.TermijnEinde.HasValue && l.Einde.HasValue) { rij.Dekking = Dat(l.Start!.Value) + " – " + Dat(l.TermijnEinde.Value); rij.DekkingSub = "verlengd tot " + Dat(l.Einde.Value); }
                if (l.Einde.HasValue && l.WaarborgEinde.HasValue) rij.Waarborg = Dat(l.Einde.Value) + " – " + Dat(l.WaarborgEinde.Value);
            }
            else if (p.Type == 3)
            {
                if (p.StartAtDelivery) rij.DekkingSub = $"start bij VO · {p.DurationYears ?? TienjarigeStandaardJaren} jaar";
                else rij.DekkingSub = $"{p.DurationYears ?? TienjarigeStandaardJaren} jaar";
            }
            else if (p.Type == 2 && !string.IsNullOrWhiteSpace(p.AppliesTo)) rij.DekkingSub = p.AppliesTo!;
            if (!string.IsNullOrWhiteSpace(p.Note)) rij.WaarborgSub = p.Note!;
            if (p.Type == 1 && !string.IsNullOrWhiteSpace(p.AppliesTo)) rij.DekkingSub2 = p.AppliesTo!;
            if (!string.IsNullOrWhiteSpace(p.EndReason)) rij.DekkingSub = (string.IsNullOrEmpty(rij.DekkingSub) ? "" : rij.DekkingSub + " · ") + p.EndReason;
            rij.Zoek = $"{rij.Soort} {rij.Maatschappij} {rij.MaatschappijSub} {p.Polisnummer}".ToLowerInvariant();
            rijen.Add(rij);
        }

        // Ontvangen tienjarige-attesten van aannemers staan ook in de lijst, met status "Attest aannemer".
        foreach (var a in attesten.Where(a => a.Kind == AttestTienjarige && a.Status == AttestOntvangen))
        {
            var contract = contracten.FirstOrDefault(c => c.Id == a.ContractId);
            var bedrijf = contract?.Company?.BedrijfsNaam ?? "aannemer";
            rijen.Add(new VzPolisRij
            {
                Id = a.Id, TypeKey = "tienjarige", Soort = "Tienjarige", IsAttest = true,
                Maatschappij = string.IsNullOrWhiteSpace(a.Insurer) ? "—" : a.Insurer!,
                MaatschappijSub = "attest " + bedrijf,
                Polisnummer = string.IsNullOrWhiteSpace(a.PolicyNumber) ? "—" : a.PolicyNumber!,
                Dekking = a.ValidUntil.HasValue ? "tot " + Dat(a.ValidUntil.Value) : "10 jaar na oplevering",
                DekkingSub = a.UnitsReceived.HasValue && a.UnitsRequired.HasValue ? $"{a.UnitsReceived} van {a.UnitsRequired} woningen" : "",
                DocumentId = a.DocumentId,
                Status = "Attest aannemer", StatusTone = "is-info", StatusKey = "attest",
                Zoek = $"tienjarige {a.Insurer} {bedrijf} {a.PolicyNumber}".ToLowerInvariant()
            });
        }
        return rijen;
    }

    // ── Aandachtspunten ─────────────────────────────────────────────────────────────────────────

    private async Task<(List<VzAandachtspunt> punten, List<OntbrekendePolis> ontbrekend)> BouwAandachtspuntenAsync(int projectId, string projectNaam,
        List<Insurances> polissen, Dictionary<int, PolisStatus> statussen, List<EenheidInfo> units, List<VzAttestRij> attesten, List<Contract> contracten,
        (int totaal, int gekoppeld) kopers, TjDekking tj, List<EenheidInfo> woningen, int? projectBron, string? tjOpmerking, DateOnly? vo, DateOnly? doDatum, bool metBudget)
    {
        var punten = new List<VzAandachtspunt>();
        var ontbrekend = new List<OntbrekendePolis>();
        var v = Vandaag;
        string Toevoegen(int type, int? building = null, int? unitId = null, string? from = null) =>
            $"/Projecten/AddInsurance?projectid={projectId}&type={type}" + (building.HasValue ? $"&building={building}" : "") + (unitId.HasValue ? $"&unit={unitId}" : "") + (from != null ? $"&from={from}" : "");
        string Bewerk(int id) => $"/Projecten/AddInsurance?projectid={projectId}&id={id}";

        bool Actief(Insurances p) => statussen.TryGetValue(p.Id, out var s) && !s.Beeindigd;
        var abr = polissen.Where(p => p.Type == 1 && Actief(p)).ToList();

        // 1. Brandverzekering voor onverkochte eenheden. Tot de voorlopige oplevering dekt de ABR het gebouw, en een verkochte eenheid verzekert de koper.
        //    Pas als de voorlopige oplevering bereikt is en een woning of appartement nog geen koper heeft, moet de ontwikkelaar zelf een brandpolis hebben.
        var brandPolissen = polissen.Where(p => p.Type == 2 && p.Building != 0 && Actief(p)).ToList();
        var brandHeel = brandPolissen.Any(p => p.UnitId == null);
        var voBereikt = vo.HasValue && vo.Value <= v;
        var brandOngedekt = woningen.Where(w => !w.Verkocht && !brandHeel && brandPolissen.All(p => p.UnitId != w.Id)).ToList();
        if (voBereikt && brandOngedekt.Count > 0)
        {
            var namen = string.Join(", ", brandOngedekt.Take(4).Select(w => w.Naam)) + (brandOngedekt.Count > 4 ? " …" : "");
            punten.Add(new VzAandachtspunt
            {
                Sleutel = "brand-nieuwbouw", Tone = "danger", Titel = "Brandverzekering nieuwbouw nog niet afgesloten",
                Kicker = "opgeleverd, nog geen koper · " + namen,
                Tekst = $"De voorlopige oplevering ({Dat(vo!.Value)}) is bereikt en {namen} {(brandOngedekt.Count == 1 ? "heeft" : "hebben")} nog geen koper. Zolang er geen koper is, moet jij het gebouw tegen brand verzekeren. Een verkochte eenheid verzekert de koper zelf.",
                Acties = { new VzActie { Label = "Polis toevoegen", Href = Toevoegen(2, 1, brandOngedekt.Count == 1 ? brandOngedekt[0].Id : null, "brand"), Icon = "ph-plus" } }
            });
            ontbrekend.Add(new OntbrekendePolis
            {
                Plaats = polissen.Count(p => p.Type <= 2),
                Rij = new VzPolisRij
                {
                    TypeKey = "brand", Soort = "Brand nieuwbouw", Maatschappij = "Nog af te sluiten", MaatschappijSub = "opgeleverd, nog geen koper",
                    Dekking = "vanaf " + Dat(vo!.Value), DekkingSub = "enkel onverkochte eenheden (" + string.Join(", ", brandOngedekt.Take(3).Select(w => w.Naam)) + ")",
                    Status = "Ontbreekt", StatusTone = "is-blocked", StatusKey = "ontbreekt", IsOntbrekend = true, ToevoegenHref = Toevoegen(2, 1, brandOngedekt.Count == 1 ? brandOngedekt[0].Id : null, "brand"),
                    Zoek = "brand nieuwbouw ontbreekt nog af te sluiten"
                }
            });
        }

        // 2. Tienjarige attesten ontbreken
        var zonderTj = attesten.Where(a => a.TjOntbreekt).ToList();
        if (zonderTj.Count > 0)
        {
            punten.Add(new VzAandachtspunt
            {
                Sleutel = "tienjarige-attesten", Tone = "warning",
                Titel = zonderTj.Count == 1 ? "1 aannemer zonder attest tienjarige aansprakelijkheid" : $"{zonderTj.Count} aannemers zonder attest tienjarige aansprakelijkheid",
                Kicker = "contracten · zie Attesten aannemers",
                Tekst = "Ruwbouw, dakwerken en stabiliteit vallen onder de wet Peeters-Borsus. Bij woningen geeft de aannemer per woning een attest af. Vraag ze op vóór de start van hun werken.",
                Acties = { new VzActie { Label = "Attesten opvragen", Href = $"/Projecten/DetailInsurances?projectid={projectId}&tab=attesten", Icon = "ph-paper-plane-tilt" } }
            });
        }

        // 2b. Contracten die via de projectpolis lopen: er moet een tienjarige polis zijn, voor het hele project of per woning of appartement
        var viaProject = attesten.Count(a => a.ViaProject);
        var opm = string.IsNullOrWhiteSpace(tjOpmerking) ? "" : " Opmerking: " + tjOpmerking!.Trim();
        if (viaProject > 0 && !tj.Heeft)
        {
            punten.Add(new VzAandachtspunt
            {
                // Met een opmerking (bv. "polis volgt van de notaris") is dit een gewone mededeling: je weet ervan en het telt niet mee op het dashboard.
                Sleutel = "projectpolis-ontbreekt", Tone = string.IsNullOrWhiteSpace(tjOpmerking) ? "warning" : "info", Titel = "Projectpolis tienjarige ontbreekt",
                Kicker = projectBron == 1 ? "project · via projectpolis" : "contracten · via projectpolis",
                Tekst = (projectBron == 1 ? "Voor dit project koos je Tienjarige via projectpolis" : viaProject == 1 ? "Bij 1 contract koos je Tienjarige via projectpolis" : $"Bij {viaProject} contracten koos je Tienjarige via projectpolis")
                        + ", maar er is geen lopende tienjarige polis. Voeg de polis toe: één voor het hele project, of per woning of appartement." + opm,
                Acties = { new VzActie { Label = "Polis toevoegen", Href = Toevoegen(3, null, null, "tienjarige"), Icon = "ph-plus" } }
            });
        }
        else if (viaProject > 0 && !tj.HeelProject && tj.Ongedekt.Count > 0)
        {
            var namen = string.Join(", ", tj.Ongedekt.Take(4).Select(w => w.Naam)) + (tj.Ongedekt.Count > 4 ? " …" : "");
            punten.Add(new VzAandachtspunt
            {
                Sleutel = "projectpolis-woningen", Tone = "warning", Titel = tj.Ongedekt.Count == 1 ? "1 eenheid zonder tienjarige polis" : $"{tj.Ongedekt.Count} eenheden zonder tienjarige polis",
                Kicker = $"contracten · via projectpolis · {tj.Gedekt} van {tj.Totaal} gedekt",
                Tekst = $"De aannemers die onder de projectpolis vallen, geven geen eigen attest af. Voor {namen} bestaat nog geen tienjarige polis. Voeg per eenheid een polis toe, of één polis voor het hele project." + opm,
                Acties = { new VzActie { Label = "Polis toevoegen", Href = Toevoegen(3, null, tj.Ongedekt.Count == 1 ? tj.Ongedekt[0].Id : null, "tienjarige"), Icon = "ph-plus" } }
            });
        }

        // 2c. Tienjarige attesten bezorgen aan de kopers (koppelen in Documenten en delen in het klantenportaal)
        if (kopers.totaal > 0 && kopers.gekoppeld < kopers.totaal)
        {
            var rest = kopers.totaal - kopers.gekoppeld;
            punten.Add(new VzAandachtspunt
            {
                Sleutel = "tienjarige-kopers", Tone = "warning", Titel = "Tienjarige attesten bezorgen aan de kopers",
                Kicker = "documenten · klanten",
                Tekst = $"{kopers.gekoppeld} van {kopers.totaal} kopers hebben het tienjarige attest van hun woning in Documenten. De attesten horen bij de verkoopovereenkomst en het opleveringsdossier. Koppel de {(rest == 1 ? "laatste" : rest + " andere")}: ze komen dan ook in het klantenportaal.",
                Acties = { new VzActie { Label = "Aan kopers koppelen", Href = "/Projecten/InsuranceAttestKoppelKopers", Icon = "ph-link", Post = true } }
            });
        }

        // 3. Verzekerde waarde ABR lager dan het definitieve budget
        var (bouwkost, budgetDatum) = metBudget ? await DefinitiefBudgetAsync(projectId) : ((decimal?)null, (DateOnly?)null);
        var abrMetWaarde = abr.Where(p => p.InsuredValue.HasValue).ToList();
        if (bouwkost.HasValue && abrMetWaarde.Count > 0)
        {
            var verzekerd = abrMetWaarde.Sum(p => p.InsuredValue!.Value);
            if (verzekerd + 0.5m < bouwkost.Value)
            {
                var polis = abrMetWaarde[0];
                var email = polis.ContractActivity?.Contract?.Company?.Email;
                var actie = new VzActie { Label = string.IsNullOrWhiteSpace(email) ? "Polis aanpassen" : "Makelaar mailen", Icon = string.IsNullOrWhiteSpace(email) ? "ph-pencil-simple" : "ph-envelope-simple", Href = Bewerk(polis.Id) };
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var onderwerp = Uri.EscapeDataString($"ABR-polis {projectNaam}: verzekerde waarde aanpassen");
                    var tekst = Uri.EscapeDataString($"Beste,\n\nHet definitieve budget voor {projectNaam} gaat uit van {Geld(bouwkost.Value)} aan bouwkost. De ABR-polis verzekert momenteel {Geld(verzekerd)}. Kunnen jullie de verzekerde waarde aanpassen?\n\nBedankt.");
                    actie.Href = $"mailto:{email}?subject={onderwerp}&body={tekst}";
                }
                punten.Add(new VzAandachtspunt
                {
                    Sleutel = "abr-waarde", Tone = "warning", Titel = "Verzekerde waarde ABR lager dan het definitieve budget",
                    Kicker = "budget definitief" + (budgetDatum.HasValue ? " · " + Dat(budgetDatum.Value) : ""),
                    Tekst = $"De ABR verzekert {Geld(verzekerd)} aan werken. Het definitieve budget bouwkosten is {Geld(bouwkost.Value)}. Vraag de makelaar om de waarde aan te passen (verschil {Geld(bouwkost.Value - verzekerd)}).",
                    Acties = { actie }
                });
            }
        }

        // 4. ABR-termijn en verlenging, vervaldata, aangevraagde en verkochte-eenheid-polissen
        foreach (var p in polissen)
        {
            if (!statussen.TryGetValue(p.Id, out var st)) continue;
            var l = Bereken(p, vo);
            var naam = TypeNaam(p.Type) + (string.IsNullOrWhiteSpace(p.Polisnummer) ? "" : " · polis " + p.Polisnummer);
            var maatschappij = p.InsuranceCompany?.Name ?? "de maatschappij";
            if (p.Type == 1 && l.TermijnEinde.HasValue && (p.ExtensionPeriod ?? 0) > 0 && v > l.TermijnEinde.Value && st.Lopend)
            {
                punten.Add(new VzAandachtspunt
                {
                    Sleutel = "abr-verlengd-" + p.Id, Tone = "info", Titel = $"ABR-termijn liep af op {Dat(l.TermijnEinde.Value)}: verlenging actief",
                    Kicker = string.IsNullOrWhiteSpace(p.Polisnummer) ? "ABR-polis" : "polis " + p.Polisnummer,
                    Tekst = $"De werken lopen langer dan de oorspronkelijke termijn. De verlenging tot {Dat(l.Einde!.Value)} staat in de polis. Controleer of {maatschappij} de verlenging bevestigd heeft.",
                    Acties = { new VzActie { Label = "Polis openen", Href = Bewerk(p.Id), Icon = "ph-pencil-simple" } }
                });
            }
            else if (st.Label == "Vervallen")
            {
                punten.Add(new VzAandachtspunt
                {
                    Sleutel = $"vervallen-{TypeKey(p.Type)}-{p.Id}", Tone = "danger", Titel = $"{TypeNaam(p.Type)}-polis is vervallen",
                    Kicker = naam, Tekst = $"De termijn van deze polis is voorbij en de werken zijn nog niet opgeleverd. Verleng de polis bij {maatschappij}, of beëindig ze als ze niet meer nodig is.",
                    Acties = { new VzActie { Label = "Polis bewerken", Href = Bewerk(p.Id), Icon = "ph-pencil-simple" } }
                });
            }
            else if (st.DagenTotEinde is int dagen && dagen <= WaarschuwDagen && p.RemindExpiry && st.Lopend)
            {
                punten.Add(new VzAandachtspunt
                {
                    Sleutel = $"vervalt-{TypeKey(p.Type)}-{p.Id}", Tone = "warning", Titel = $"{TypeNaam(p.Type)}-polis vervalt over {dagen} dagen",
                    Kicker = naam + (l.Einde.HasValue ? " · " + Dat(l.Einde.Value) : ""),
                    Tekst = $"Verleng de polis bij {maatschappij} of beëindig ze. Zonder verlenging valt de dekking weg.",
                    Acties = { new VzActie { Label = "Polis bewerken", Href = Bewerk(p.Id), Icon = "ph-pencil-simple" } }
                });
            }

            if (p.IsRequested)
            {
                var start = l.Start;
                var dringend = start.HasValue && start.Value.DayNumber - v.DayNumber <= WaarschuwDagen && p.RemindStart;
                punten.Add(new VzAandachtspunt
                {
                    Sleutel = "aangevraagd-" + p.Id, Tone = dringend ? "warning" : "info", Titel = $"{TypeNaam(p.Type)}-polis aangevraagd, nog niet ontvangen",
                    Kicker = start.HasValue ? "start " + Dat(start.Value) : "aangevraagd",
                    Tekst = "Je bewaarde deze polis als aangevraagd. Vul het polisnummer in zodra de maatschappij de polis bezorgt.",
                    Acties = { new VzActie { Label = "Polis aanvullen", Href = Bewerk(p.Id), Icon = "ph-pencil-simple" } }
                });
            }

            if (p.UnitId.HasValue && p.RemindSale && !st.Beeindigd)
            {
                var eenheid = units.FirstOrDefault(u => u.Id == p.UnitId.Value);
                if (eenheid != null && eenheid.Verkocht)
                {
                    punten.Add(new VzAandachtspunt
                    {
                        Sleutel = "verkocht-" + p.Id, Tone = "warning", Titel = $"{eenheid.Naam} is verkocht: polis {TypeNaam(p.Type).ToLowerInvariant()} opzeggen of overdragen",
                        Kicker = naam, Tekst = "Deze polis gold voor een onverkochte eenheid. Na de verkoop verzekert de koper de eenheid. Zeg de polis op of draag ze over.",
                        Acties = { new VzActie { Label = "Polis bewerken", Href = Bewerk(p.Id), Icon = "ph-pencil-simple" } }
                    });
                }
            }

            if (st.Beeindigd && !string.IsNullOrWhiteSpace(p.EndReason))
            {
                punten.Add(new VzAandachtspunt
                {
                    Sleutel = "beeindigd-" + p.Id, Tone = "success", Opgelost = true,
                    Titel = (p.Type == 2 ? (p.Building == 0 ? "Brandverzekering bestaand gebouw" : "Brandverzekering") : TypeNaam(p.Type) + "-polis") + " beëindigd",
                    Kicker = p.Enddate.HasValue ? "opgelost " + Dat(p.Enddate.Value) : "opgelost", Tekst = p.EndReason!
                });
            }
        }

        // Volgorde: rood, goud, grijs, daarna opgelost
        int Orde(VzAandachtspunt a) => a.Opgelost ? 9 : a.Tone == "danger" ? 0 : a.Tone == "warning" ? 1 : 2;
        punten = punten.OrderBy(Orde).ToList();
        return (punten, ontbrekend);
    }

    /// <summary>Totale bouwkost van het definitieve budget (stap 6) en de datum waarop die versie gemaakt werd; null zonder definitief budget.</summary>
    public async Task<(decimal? bouwkost, DateOnly? datum)> DefinitiefBudgetAsync(int projectId)
    {
        var versie = await _db.BudgetVersie.AsNoTracking()
            .Where(b => b.ProjectId == projectId && b.Status == BudgetVersie.StatusDefinitief)
            .OrderByDescending(b => b.CreatedAt).FirstOrDefaultAsync();
        if (versie == null) return (null, null);
        try
        {
            var res = await _budget.BerekenAsync(versie.Id);
            return (res.TotaalBouw, DateOnly.FromDateTime(versie.CreatedAt));
        }
        catch { return (null, null); }
    }

    // ── Kpi's ───────────────────────────────────────────────────────────────────────────────────

    private List<GlV2KpiItemVm> BouwKpis(VerzekeringenV2Vm vm, List<Insurances> polissen, Dictionary<int, PolisStatus> statussen, DateOnly? vo, int aantalOntbrekend)
    {
        var lopend = polissen.Where(p => statussen.TryGetValue(p.Id, out var s) && s.Lopend).ToList();
        var types = lopend.Select(p => p.Type).Distinct().OrderBy(t => t).Select(t => TypeNaam(t).ToLowerInvariant() == "abr" ? "ABR" : TypeNaam(t).ToLowerInvariant());
        var ontbrekendRij = vm.Polissen.FirstOrDefault(p => p.IsOntbrekend);

        var tj = vm.Attesten.Where(a => a.TjVereist).ToList();
        var tjOntvangen = tj.Count(a => !a.TjOntbreekt);
        var ba = vm.Attesten;
        var baOntvangen = ba.Count(a => !a.BaOntbreekt);
        var totaal = tj.Count + ba.Count;
        var ontvangen = tjOntvangen + baOntvangen;

        DateOnly? volgende = null; Insurances? volgendePolis = null;
        foreach (var p in lopend)
        {
            var l = Bereken(p, vo);
            var einde = l.Einde;
            if (einde.HasValue && einde.Value >= Vandaag && (!volgende.HasValue || einde.Value < volgende.Value)) { volgende = einde; volgendePolis = p; }
        }

        return new List<GlV2KpiItemVm>
        {
            new() { Label = "Lopende polissen", Value = lopend.Count.ToString(), IconClass = "ph-shield-check", Hint = lopend.Count == 0 ? "nog geen lopende polis" : string.Join(" · ", types) },
            new() { Label = "Ontbrekend", Value = aantalOntbrekend.ToString(), IconClass = "ph-warning-circle", Tone = aantalOntbrekend > 0 ? GlV2KpiTone.Danger : GlV2KpiTone.Primary,
                    Hint = ontbrekendRij != null ? "brand nieuwbouw voor onverkochte eenheden" : "geen ontbrekende polissen" },
            new() { Label = "Attesten aannemers", Value = totaal == 0 ? "—" : $"{ontvangen} / {totaal}", IconClass = "ph-seal-check", Tone = totaal > 0 && ontvangen < totaal ? GlV2KpiTone.Warning : GlV2KpiTone.Primary,
                    Hint = totaal == 0 ? "geen contracten" : $"tienjarige {tjOntvangen}/{tj.Count} · BA {baOntvangen}/{ba.Count}" },
            new() { Label = "Volgende vervaldag", Value = volgende.HasValue ? Dat(volgende.Value) : "—", IconClass = "ph-calendar-check",
                    Hint = volgendePolis != null ? TypeNaam(volgendePolis.Type) + (volgendePolis.InsuranceCompany != null ? " · " + volgendePolis.InsuranceCompany.Name : "") : "geen vervaldatum bekend" },
        };
    }

    // ── Tijdlijn "Dekking in de tijd" ───────────────────────────────────────────────────────────

    private VzTijdlijn BouwTijdlijn(List<Insurances> polissen, Dictionary<int, PolisStatus> statussen, DateOnly? vo, DateOnly? doDatum, DateOnly? werfStart,
        List<VzAttestRij> attesten, bool brandOntbreekt)
    {
        var v = Vandaag;
        var datums = new List<DateOnly> { v };
        if (vo.HasValue) datums.Add(vo.Value);
        if (doDatum.HasValue) datums.Add(doDatum.Value);
        if (werfStart.HasValue) datums.Add(werfStart.Value);
        foreach (var p in polissen)
        {
            var l = Bereken(p, vo);
            if (l.Start.HasValue) datums.Add(l.Start.Value);
            if (l.Einde.HasValue && p.Type != 3) datums.Add(l.Einde.Value);
            if (l.WaarborgEinde.HasValue) datums.Add(l.WaarborgEinde.Value);
        }
        var startJaar = datums.Min(d => d.Year);
        var eindJaar = Math.Max(datums.Where(d => d >= v.AddYears(-1)).DefaultIfEmpty(v).Max(d => d.Year) + (doDatum.HasValue || vo.HasValue ? 1 : 0), startJaar + 2);
        eindJaar = Math.Min(eindJaar, startJaar + 7);
        var rs = new DateOnly(startJaar, 1, 1);
        var re = new DateOnly(eindJaar, 12, 31);
        double span = Math.Max(1, re.DayNumber - rs.DayNumber);
        double Pct(DateOnly d) => Math.Clamp((d.DayNumber - rs.DayNumber) / span * 100.0, 0, 100);
        VzBalk Balk(DateOnly van, DateOnly tot, string soort, string tekst, bool pijl = false)
        {
            var l = Pct(van); var r = Pct(tot);
            return new VzBalk { LinksPct = l, BreedtePct = Math.Max(1.2, r - l), Soort = soort, Tekst = tekst, Pijl = pijl };
        }

        var t = new VzTijdlijn { Jaren = Enumerable.Range(startJaar, eindJaar - startJaar + 1).ToList(), VandaagPct = Pct(v) };

        // Werf
        var werfVan = werfStart ?? (datums.Count > 1 ? datums.Min() : v);
        var werfTot = doDatum ?? vo;
        if (werfTot.HasValue)
        {
            var rij = new VzTijdlijnRij { Label = "Werf", Sub = "start ▸ VO ▸ DO" };
            rij.Balken.Add(Balk(werfVan, werfTot.Value, "werf", ""));
            if (vo.HasValue) rij.Markers.Add(new VzMarker { Pct = Pct(vo.Value), Label = "VO " + Dat(vo.Value) });
            if (doDatum.HasValue) rij.Markers.Add(new VzMarker { Pct = Pct(doDatum.Value), Label = "DO " + Dat(doDatum.Value) });
            t.Rijen.Add(rij);
        }

        foreach (var p in polissen.Where(p => p.Type != 3))
        {
            var l = Bereken(p, vo);
            statussen.TryGetValue(p.Id, out var st);
            var naam = p.Type == 2 ? (p.Building == 1 ? "Brand nieuwbouw" : p.Building == 0 ? "Brand bestaand gebouw" : "Brand") : TypeNaam(p.Type);
            var sub = (p.InsuranceCompany?.Name ?? "") + (st?.Beeindigd == true && !string.IsNullOrWhiteSpace(p.EndReason) ? " · " + p.EndReason : "");
            var rij = new VzTijdlijnRij { Label = naam, Sub = sub };
            if (l.Start.HasValue)
            {
                var einde = l.Einde ?? re;
                var open = !l.Einde.HasValue;
                var soort = st?.Beeindigd == true ? "beeindigd" : "polis";
                if (p.Type == 1 && l.TermijnEinde.HasValue && l.Einde.HasValue && l.Einde.Value > l.TermijnEinde.Value)
                {
                    rij.Balken.Add(Balk(l.Start.Value, l.TermijnEinde.Value, "polis", ""));
                    rij.Balken.Add(Balk(l.TermijnEinde.Value, l.Einde.Value, "verlengd", "verlengd"));
                }
                else rij.Balken.Add(Balk(l.Start.Value, einde, soort, "", open));
            }
            t.Rijen.Add(rij);
            if (p.Type == 1 && l.Einde.HasValue && l.WaarborgEinde.HasValue)
                t.Rijen.Add(new VzTijdlijnRij { Label = "ABR waarborgperiode", Sub = "na VO · onderhoud", Balken = { Balk(l.Einde.Value, l.WaarborgEinde.Value, "waarborg", "tot " + Dat(l.WaarborgEinde.Value)) } });
        }

        if (brandOntbreekt && vo.HasValue)
            t.Rijen.Add(new VzTijdlijnRij { Label = "Brand nieuwbouw", Sub = "nog af te sluiten", Balken = { Balk(vo.Value, re, "ontbrekend", "ontbreekt vanaf VO", true) } });

        var tjPolis = polissen.FirstOrDefault(p => p.Type == 3);
        var tjAttesten = attesten.Where(a => a.TjVereist).ToList();
        if (vo.HasValue && (tjPolis != null || tjAttesten.Count > 0))
        {
            var jaren = tjPolis?.DurationYears ?? TienjarigeStandaardJaren;
            var einde = vo.Value.AddYears(jaren);
            var sub = tjAttesten.Count > 0 ? $"{tjAttesten.Count(a => !a.TjOntbreekt)} van {tjAttesten.Count} attesten · loopt door tot {einde.Year}" : $"{jaren} jaar na VO";
            t.Rijen.Add(new VzTijdlijnRij { Label = "Tienjarige", Sub = sub, Balken = { Balk(vo.Value, einde, "tienjarige", "", einde > re) } });
        }
        return t;
    }

    // ── Attesten van aannemers (43b) ────────────────────────────────────────────────────────────

    private static bool TienjarigeVereist(Contract c)
    {
        foreach (var a in c.ContractActivity)
        {
            var tekst = ((a.Activity?.Group?.Name ?? "") + " " + (a.Activity?.Omschrijving ?? "")).ToLowerInvariant();
            if (TienjarigeSleutels.Any(s => tekst.Contains(s))) return true;
        }
        return false;
    }

    /// <summary>Is voor dit contract een tienjarige-attest van de aannemer nodig? Het contract zelf (Tienjarige via de aannemer, via de projectpolis of niet vereist)
    /// gaat voor. Zonder keuze leidt CPM het af uit de werken, en een door de gebruiker gezette status Niet vereist blijft gelden.</summary>
    private static bool TjVereist(Contract c, InsuranceAttest? tj, int? projectBron)
    {
        var bron = c.TienjarigeBron ?? projectBron;   // een eigen keuze op het contract gaat voor op die van het project
        if (bron is 1 or 2) return false;
        if (tj != null && tj.Status == AttestNietVereist) return false;
        if (bron == 0) return true;
        return tj != null || TienjarigeVereist(c);
    }

    private List<VzAttestRij> BouwAttesten(List<Contract> contracten, List<InsuranceAttest> attesten, List<InsuranceAttestUnit> attestUnits, int woningen, TjDekking tjDekking, int? projectBron, string? projectOpmerking)
    {
        var rijen = new List<VzAttestRij>();
        var v = Vandaag;
        foreach (var c in contracten)
        {
            // Een contract met een makelaar (enkel de activiteit "Verzekeringen") geeft zelf geen attesten af.
            if (c.ContractActivity.Count > 0 && c.ContractActivity.All(a => a.ActivityId == VerzekeringActiviteitId)) continue;
            var tj = attesten.FirstOrDefault(a => a.ContractId == c.Id && a.Kind == AttestTienjarige);
            var ba = attesten.FirstOrDefault(a => a.ContractId == c.Id && a.Kind == AttestBa);
            var bedrijf = c.Company?.BedrijfsNaam ?? "—";
            var vereist = TjVereist(c, tj, projectBron);
            var viaProject = (c.TienjarigeBron ?? projectBron) == 1;
            var rij = new VzAttestRij
            {
                ContractId = c.Id, Bedrijf = bedrijf, BedrijfEmail = c.Company?.Email ?? "",
                ContractLabel = string.IsNullOrWhiteSpace(c.ContractName) ? "Contract #" + c.Id : c.ContractName!,
                Lot = string.Join(", ", c.ContractActivity.Select(a => a.Activity?.Group?.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(2)),
                TjVereist = vereist, ViaProject = viaProject, TjUnitsRequired = tj?.UnitsRequired ?? woningen, TjUnitsReceived = tj?.UnitsReceived ?? 0,
            };

            // Tienjarige
            if (viaProject)
            {
                rij.TjStatus = "Via projectpolis"; rij.TjTone = tjDekking.Volledig ? "is-info" : "is-attention";
                rij.TjSub = !tjDekking.Heeft ? "projectpolis ontbreekt" + (string.IsNullOrWhiteSpace(projectOpmerking) ? "" : " · " + projectOpmerking!.Trim()) : tjDekking.HeelProject ? "dekt de projectpolis" : $"{tjDekking.Gedekt} van {tjDekking.Totaal} eenheden gedekt";
            }
            else if (!vereist) { rij.TjStatus = "Niet vereist"; rij.TjTone = "is-neutral"; rij.TjSub = c.TienjarigeBron == 2 ? "volgens het contract" : tj != null ? "door jou aangeduid" : "werken vallen buiten de wet"; }
            else if (tj != null && tj.Status == AttestOntvangen)
            {
                rij.TjStatus = "Ontvangen"; rij.TjTone = "is-positive";
                rij.TjSub = (string.IsNullOrWhiteSpace(tj.Insurer) ? "" : tj.Insurer + " · ") + (tj.UnitsRequired is > 0 ? $"{tj.UnitsReceived ?? 0} / {tj.UnitsRequired} woningen" : "attest ontvangen");
            }
            else
            {
                rij.TjStatus = "Ontbreekt"; rij.TjTone = "is-blocked"; rij.TjOntbreekt = true;
                rij.TjSub = woningen > 0 || tj?.UnitsRequired is > 0 ? $"{tj?.UnitsReceived ?? 0} / {tj?.UnitsRequired ?? woningen} woningen" : "";
            }

            // BA (aanbevolen, niet verplicht)
            if (ba != null && ba.Status == AttestNietVereist) { rij.BaStatus = "Niet vereist"; rij.BaTone = "is-neutral"; }
            else if (ba != null && ba.Status == AttestOntvangen)
            {
                if (ba.ValidUntil.HasValue && ba.ValidUntil.Value < v) { rij.BaStatus = "Verlopen"; rij.BaTone = "is-blocked"; rij.BaSub = "sinds " + Dat(ba.ValidUntil.Value); rij.BaOntbreekt = true; }
                else if (ba.ValidUntil.HasValue && ba.ValidUntil.Value.DayNumber - v.DayNumber <= 60) { rij.BaStatus = "Vervalt"; rij.BaTone = "is-attention"; rij.BaSub = Dat(ba.ValidUntil.Value); }
                else { rij.BaStatus = "Geldig"; rij.BaTone = "is-positive"; rij.BaSub = ba.ValidUntil.HasValue ? "tot " + Dat(ba.ValidUntil.Value) : "doorlopend"; }
            }
            else { rij.BaStatus = "Niet ontvangen"; rij.BaTone = "is-attention"; rij.BaOntbreekt = true; rij.BaSub = "aanbevolen · contractueel"; }

            VzAttestPrefill Prefill(InsuranceAttest? a, int standaardStatus, int vereist) => new()
            {
                Status = a?.Status ?? standaardStatus, UnitsRequired = a?.UnitsRequired ?? vereist, UnitsReceived = a?.UnitsReceived ?? 0,
                ValidUntil = a?.ValidUntil?.ToString("yyyy-MM-dd") ?? "", Insurer = a?.Insurer ?? "", PolicyNumber = a?.PolicyNumber ?? "", Note = a?.Note ?? "", DocumentId = a?.DocumentId,
                UnitIds = a == null ? new List<int>() : attestUnits.Where(u => u.AttestId == a.Id).Select(u => u.UnitId).ToList()
            };
            rij.Tj = Prefill(tj, vereist ? AttestOntbreekt : AttestNietVereist, woningen);
            rij.Ba = Prefill(ba, AttestOntbreekt, 0);

            // Laatst opgevraagd
            var laatst = new[] { tj?.LastRequestedAt, ba?.LastRequestedAt }.Where(d => d.HasValue).Select(d => d!.Value).OrderByDescending(d => d).FirstOrDefault();
            if (laatst != default)
            {
                rij.IsOpgevraagd = true;
                rij.Laatst = laatst.ToLocalTime().ToString("dd/MM/yyyy", Be) + (rij.KanOpvragen ? " · geen antwoord" : "");
            }
            else rij.Laatst = rij.KanOpvragen ? "nog niet" : "—";
            rij.Zoek = $"{bedrijf} {rij.ContractLabel} {rij.Lot}".ToLowerInvariant();
            rijen.Add(rij);
        }
        return rijen;
    }

    // ── Opslaan: polis ──────────────────────────────────────────────────────────────────────────

    public async Task<(bool ok, string? fout, int id)> SavePolisAsync(VzPolisInput i, int? documentId)
    {
        // Een nieuwe tienjarige polis voor meerdere eenheden: één polis per eenheid met dezelfde gegevens (het polisnummer kan je daarna per eenheid aanpassen).
        if (i.Id == 0 && i.Type == 3 && i.UnitIds is { Count: > 0 })
        {
            var eerste = 0;
            foreach (var uid in i.UnitIds.Where(u => u > 0).Distinct())
            {
                var kopie = i.Kopie();
                kopie.UnitId = uid;
                kopie.UnitIds = null;
                var r = await SavePolisAsync(kopie, documentId);
                if (!r.ok) return r;
                if (eerste == 0) eerste = r.id;
            }
            if (eerste > 0) return (true, null, eerste);
        }
        if (i.ProjectId <= 0) return (false, "Project ontbreekt.", 0);
        if (i.Type < 1 || i.Type > 5) return (false, "Kies een type verzekering.", 0);
        if (i.InsuranceCompanyId is null or <= 0) return (false, "Kies een maatschappij.", 0);
        var vo = (await _db.Project.AsNoTracking().Where(p => p.ProjectId == i.ProjectId).Select(p => p.DeliveryDate).FirstOrDefaultAsync());
        if (i.StartAtDelivery && !vo.HasValue) return (false, "Vul eerst de datum van de voorlopige oplevering in bij het project, of kies een vaste startdatum.", 0);
        if (!i.StartAtDelivery && !i.Startdate.HasValue && !i.Requested) return (false, "Vul een startdatum in, of laat de polis starten bij de voorlopige oplevering.", 0);
        if (i.Enddate.HasValue && i.Startdate.HasValue && i.Enddate.Value < i.Startdate.Value) return (false, "De einddatum ligt vóór de startdatum.", 0);

        Insurances entity;
        if (i.Id > 0)
        {
            entity = await _db.Insurances.Include(x => x.ContractActivity).ThenInclude(a => a.Contract).FirstOrDefaultAsync(x => x.Id == i.Id) ?? new Insurances();
            if (entity.Id == 0) return (false, "De verzekering bestaat niet meer.", 0);
            var eigenProject = entity.ProjectId ?? entity.ContractActivity?.Contract?.ProjectId;
            if (eigenProject != i.ProjectId) return (false, "Deze verzekering hoort bij een ander project.", 0);
        }
        else
        {
            entity = new Insurances { ProjectId = i.ProjectId };
            if (i.BrokerCompanyId is > 0)
            {
                var caId = _projects.CreateInsuranceContractActivity(i.ProjectId, i.BrokerCompanyId.Value);
                if (caId <= 0) return (false, "De verzekeringsactiviteit voor de makelaar kon niet aangemaakt worden.", 0);
                entity.ContractActivityId = caId;
            }
            _db.Insurances.Add(entity);
        }

        entity.ProjectId = i.ProjectId;
        entity.Type = i.Type;
        entity.Building = i.Type == 2 ? (i.Building ?? 1) : null;
        entity.UnitId = i.UnitId is > 0 ? i.UnitId : null;
        entity.AppliesTo = string.IsNullOrWhiteSpace(i.AppliesTo) ? null : i.AppliesTo.Trim();
        entity.InsuredValue = i.InsuredValue;
        entity.InsuranceCompanyId = i.InsuranceCompanyId;
        entity.Polisnummer = string.IsNullOrWhiteSpace(i.Polisnummer) ? null : i.Polisnummer.Trim();
        entity.StartAtDelivery = i.StartAtDelivery;
        entity.Startdate = i.StartAtDelivery ? null : i.Startdate;
        entity.Enddate = i.Enddate;
        entity.Period = i.Type == 1 ? i.Period : null;
        entity.ExtensionPeriod = i.Type == 1 ? i.ExtensionPeriod : null;
        entity.GuaranteePeriod = i.Type == 1 ? i.GuaranteePeriod : null;
        entity.DurationYears = i.Type == 3 ? (i.DurationYears ?? TienjarigeStandaardJaren) : null;
        entity.Premium = i.Premium;
        entity.Note = string.IsNullOrWhiteSpace(i.Note) ? null : i.Note.Trim();
        entity.RemindStart = i.RemindStart;
        entity.RemindSale = i.RemindSale;
        entity.RemindExpiry = i.RemindExpiry;
        // "Aangevraagd" geldt zolang er geen polisnummer is, of zolang de gebruiker de polis zo bewaart.
        entity.IsRequested = i.Requested && string.IsNullOrWhiteSpace(entity.Polisnummer);
        if (documentId.HasValue) entity.DocumentId = documentId;
        await _db.SaveChangesAsync();
        // Een tienjarige polis voor één eenheid met een document: het document hoort ook bij die eenheid en haar koper (zoals een attest van een aannemer).
        if (entity.Type == 3 && entity.UnitId.HasValue && entity.DocumentId.HasValue)
            await KoppelDocumentAanEenheidAsync(i.ProjectId, entity.DocumentId.Value, entity.UnitId.Value);
        // Een tienjarige polis voor het hele project met een document: alle verkochte woningen en hun kopers krijgen het ook.
        else if (entity.Type == 3 && !entity.UnitId.HasValue && entity.DocumentId.HasValue)
            await KoppelProjectPolisAanKopersAsync(i.ProjectId, entity.DocumentId.Value);
        return (true, null, entity.Id);
    }

    public async Task<bool> BeeindigAsync(int projectId, int id, DateOnly datum, string? reden)
    {
        var e = await _db.Insurances.Include(x => x.ContractActivity).ThenInclude(a => a.Contract).FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return false;
        if ((e.ProjectId ?? e.ContractActivity?.Contract?.ProjectId) != projectId) return false;
        e.Enddate = datum;
        e.EndReason = string.IsNullOrWhiteSpace(reden) ? null : reden.Trim();
        e.IsRequested = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<VzPolisFormVm> GetFormAsync(int projectId, int? id, VzPolisInput? prefill, IEnumerable<IdNameBO> brokers, IEnumerable<IdNameBO> companies)
    {
        var vm = new VzPolisFormVm();
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => new { p.ProjectName, p.DeliveryDate }).FirstOrDefaultAsync();
        vm.ProjectName = project?.ProjectName ?? "";
        vm.VoDatum = project?.DeliveryDate;

        if (id is > 0)
        {
            var e = await _db.Insurances.AsNoTracking().Include(x => x.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(c => c.Company).FirstOrDefaultAsync(x => x.Id == id.Value);
            if (e != null)
            {
                vm.Input = new VzPolisInput
                {
                    Id = e.Id, ProjectId = projectId, Type = e.Type, Building = e.Building, UnitId = e.UnitId, AppliesTo = e.AppliesTo, InsuredValue = e.InsuredValue,
                    InsuranceCompanyId = e.InsuranceCompanyId, Polisnummer = e.Polisnummer, Startdate = e.Startdate, StartAtDelivery = e.StartAtDelivery, Enddate = e.Enddate,
                    Period = e.Period, ExtensionPeriod = e.ExtensionPeriod, GuaranteePeriod = e.GuaranteePeriod, DurationYears = e.DurationYears, Premium = e.Premium, Note = e.Note,
                    RemindStart = e.RemindStart, RemindSale = e.RemindSale, RemindExpiry = e.RemindExpiry, Requested = e.IsRequested,
                };
                vm.HasBroker = e.ContractActivity != null;
                vm.BrokerName = e.ContractActivity?.Contract?.Company?.BedrijfsNaam;
                vm.DocumentId = e.DocumentId;
            }
        }
        else if (prefill != null) vm.Input = prefill;
        vm.Input.ProjectId = projectId;
        if (vm.Input.Id == 0 && vm.Input.Type == 2 && vm.Input.Building == null) vm.Input.Building = 1;

        vm.Maatschappijen = companies.Select(c => new GlV2SelectItem { Value = c.ID.ToString(), Text = c.Display }).ToList();
        vm.Makelaars = brokers.Select(c => new GlV2SelectItem { Value = c.ID.ToString(), Text = c.Display }).ToList();
        var units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId && !u.IsLink).OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, Verkocht = u.ClientAccountId != null }).ToListAsync();
        vm.Eenheden = units.Select(u => new GlV2SelectItem { Value = u.Id.ToString(), Text = u.Name + (u.Verkocht ? " (verkocht)" : " (onverkocht)") }).ToList();

        var (bouwkost, datum) = await DefinitiefBudgetAsync(projectId);
        vm.BudgetBouwkost = bouwkost;
        vm.BudgetBron = datum.HasValue ? "definitief budget " + Dat(datum.Value) : null;
        return vm;
    }

    // ── Opslaan: attest ─────────────────────────────────────────────────────────────────────────

    public async Task<(bool ok, string? fout, int gekoppeld)> SaveAttestAsync(int projectId, int contractId, int kind, int status, List<int>? unitIds,
        DateOnly? geldigTot, string? maatschappij, string? polisnummer, string? opmerking, int? documentId)
    {
        var contract = await _db.Contract.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contractId && c.ProjectId == projectId);
        if (contract == null) return (false, "Contract niet gevonden.", 0);
        if (kind is not (AttestTienjarige or AttestBa)) return (false, "Onbekende soort attest.", 0);
        if (status is < 0 or > 2) return (false, "Onbekende status.", 0);
        var a = await _db.InsuranceAttest.FirstOrDefaultAsync(x => x.ContractId == contractId && x.Kind == kind);
        if (a == null)
        {
            a = new InsuranceAttest { ProjectId = projectId, ContractId = contractId, Kind = kind, CreatedAt = DateTime.UtcNow };
            _db.InsuranceAttest.Add(a);
        }
        a.Status = status;
        a.ValidUntil = status == AttestOntvangen ? geldigTot : null;
        a.Insurer = string.IsNullOrWhiteSpace(maatschappij) ? null : maatschappij.Trim();
        a.PolicyNumber = string.IsNullOrWhiteSpace(polisnummer) ? null : polisnummer.Trim();
        a.Note = string.IsNullOrWhiteSpace(opmerking) ? null : opmerking.Trim();
        if (status == AttestOntvangen && a.ReceivedAt == null) a.ReceivedAt = Vandaag;
        if (documentId.HasValue) a.DocumentId = documentId;
        a.UpdatedAt = DateTime.UtcNow;

        // Tienjarige: voor welke woningen het attest geldt (alleen bij Ontvangen). Het aantal volgt daaruit.
        var woningIds = await _db.Units.AsNoTracking()
            .Where(u => u.ProjectId == projectId && !u.IsLink && u.Type != null && u.Type.Group != null && u.Type.Group.Name.Contains("woon"))
            .Select(u => u.Id).ToListAsync();
        var gekozen = kind == AttestTienjarige && status == AttestOntvangen
            ? (unitIds ?? new List<int>()).Distinct().Where(id => woningIds.Contains(id)).ToList()
            : new List<int>();
        a.UnitsRequired = kind == AttestTienjarige ? woningIds.Count : null;
        a.UnitsReceived = kind == AttestTienjarige && status == AttestOntvangen ? gekozen.Count : null;
        await _db.SaveChangesAsync();

        if (kind == AttestTienjarige)
        {
            var oud = await _db.InsuranceAttestUnit.Where(x => x.AttestId == a.Id).ToListAsync();
            _db.InsuranceAttestUnit.RemoveRange(oud.Where(x => !gekozen.Contains(x.UnitId)));
            foreach (var id in gekozen.Where(id => oud.All(x => x.UnitId != id))) _db.InsuranceAttestUnit.Add(new InsuranceAttestUnit { AttestId = a.Id, UnitId = id });
            await _db.SaveChangesAsync();
        }
        var gekoppeld = await KoppelAttestAanKopersAsync(a, projectId);
        return (true, null, gekoppeld);
    }

    /// <summary>Koppelt een ontvangen tienjarige-attest (met bestand) aan de woningen waarvoor het geldt en aan hun kopers, en deelt het in het klantenportaal.
    /// Bestaande koppelingen blijven ongemoeid. Geeft het aantal nieuwe koppelingen.</summary>
    private async Task<int> KoppelAttestAanKopersAsync(InsuranceAttest a, int projectId)
    {
        if (a.Kind != AttestTienjarige || a.Status != AttestOntvangen || !a.DocumentId.HasValue) return 0;
        var unitIds = await _db.InsuranceAttestUnit.AsNoTracking().Where(x => x.AttestId == a.Id).Select(x => x.UnitId).ToListAsync();
        if (unitIds.Count == 0) return 0;
        var eenheden = await _db.Units.AsNoTracking().Where(u => unitIds.Contains(u.Id) && u.ProjectId == projectId).Select(u => new { u.Id, u.ClientAccountId }).ToListAsync();
        var docId = a.DocumentId.Value;
        var bestaand = await _db.DocumentLinks.AsNoTracking().Where(l => l.DocumentId == docId).Select(l => new { l.UnitId, l.ClientAccountId }).ToListAsync();
        var eenhedenGekoppeld = bestaand.Where(l => l.UnitId.HasValue).Select(l => l.UnitId!.Value).ToHashSet();
        var klantenGekoppeld = bestaand.Where(l => l.ClientAccountId.HasValue).Select(l => l.ClientAccountId!.Value).ToHashSet();
        var aantal = 0;
        foreach (var e in eenheden)
        {
            if (eenhedenGekoppeld.Add(e.Id))
            {
                var r = await _docs.AddLink(projectId, docId, "unit", e.Id, e.ClientAccountId.HasValue);
                if (r.Ok) aantal++;
            }
            if (e.ClientAccountId.HasValue && klantenGekoppeld.Add(e.ClientAccountId.Value))
            {
                var r = await _docs.AddLink(projectId, docId, "client", e.ClientAccountId.Value, true);
                if (r.Ok) aantal++;
            }
        }
        return aantal;
    }

    /// <summary>Koppelt alle ontvangen tienjarige-attesten van het project aan de kopers van de woningen (bv. nadat een woning verkocht werd).</summary>
    public async Task<int> KoppelKopersAsync(int projectId)
    {
        var attesten = await _db.InsuranceAttest.AsNoTracking()
            .Where(a => a.ProjectId == projectId && a.Kind == AttestTienjarige && a.Status == AttestOntvangen && a.DocumentId != null).ToListAsync();
        var totaal = 0;
        foreach (var a in attesten) totaal += await KoppelAttestAanKopersAsync(a, projectId);
        var polissen = await _db.Insurances.AsNoTracking().Where(p => p.ProjectId == projectId && p.Type == 3 && p.UnitId != null && p.DocumentId != null).ToListAsync();
        foreach (var p in polissen) totaal += await KoppelDocumentAanEenheidAsync(projectId, p.DocumentId!.Value, p.UnitId!.Value);
        var projectPolissen = await _db.Insurances.AsNoTracking().Where(p => p.ProjectId == projectId && p.Type == 3 && p.UnitId == null && p.DocumentId != null).ToListAsync();
        foreach (var p in projectPolissen) totaal += await KoppelProjectPolisAanKopersAsync(projectId, p.DocumentId!.Value);
        return totaal;
    }

    /// <summary>Koppelt het document van een tienjarige polis voor het hele project aan alle verkochte woningen en hun kopers (met delen in het klantenportaal).</summary>
    private async Task<int> KoppelProjectPolisAanKopersAsync(int projectId, int documentId)
    {
        var verkocht = await _db.Units.AsNoTracking()
            .Where(u => u.ProjectId == projectId && !u.IsLink && u.ClientAccountId != null && u.Type != null && u.Type.Group != null && u.Type.Group.Name.Contains("woon"))
            .Select(u => u.Id).ToListAsync();
        var aantal = 0;
        foreach (var id in verkocht) aantal += await KoppelDocumentAanEenheidAsync(projectId, documentId, id);
        return aantal;
    }

    /// <summary>Een eenheid kreeg een koper: koppel de tienjarige polissen (voor deze eenheid of voor het hele project) en de attesten van aannemers voor deze eenheid aan die koper
    /// en deel ze in het klantenportaal. Wordt aangeroepen door <see cref="BuyerLinkActionFilter"/> nadat een actie een koper zette.</summary>
    public async Task<int> KoppelEenheidAanKoperAsync(int unitId)
    {
        var u = await _db.Units.AsNoTracking().Where(x => x.Id == unitId).Select(x => new { x.Id, x.ProjectId, x.ClientAccountId }).FirstOrDefaultAsync();
        if (u == null || !u.ClientAccountId.HasValue) return 0;
        var docIds = await _db.Insurances.AsNoTracking()
            .Where(p => p.ProjectId == u.ProjectId && p.Type == 3 && p.DocumentId != null && (p.UnitId == null || p.UnitId == u.Id))
            .Select(p => p.DocumentId!.Value).ToListAsync();
        var attestDocs = await (from a in _db.InsuranceAttest.AsNoTracking()
                                join au in _db.InsuranceAttestUnit.AsNoTracking() on a.Id equals au.AttestId
                                where a.ProjectId == u.ProjectId && a.Kind == AttestTienjarige && a.Status == AttestOntvangen && a.DocumentId != null && au.UnitId == u.Id
                                select a.DocumentId!.Value).ToListAsync();
        var aantal = 0;
        foreach (var docId in docIds.Concat(attestDocs).Distinct()) aantal += await KoppelDocumentAanEenheidAsync(u.ProjectId, docId, u.Id);
        return aantal;
    }

    /// <summary>Koppelt een document aan één eenheid en aan de koper ervan (met delen in het klantenportaal als er een koper is). Bestaande koppelingen blijven ongemoeid.</summary>
    private async Task<int> KoppelDocumentAanEenheidAsync(int projectId, int documentId, int unitId)
    {
        var e = await _db.Units.AsNoTracking().Where(u => u.Id == unitId && u.ProjectId == projectId).Select(u => new { u.Id, u.ClientAccountId }).FirstOrDefaultAsync();
        if (e == null) return 0;
        var bestaand = await _db.DocumentLinks.AsNoTracking().Where(l => l.DocumentId == documentId).Select(l => new { l.UnitId, l.ClientAccountId }).ToListAsync();
        var aantal = 0;
        if (!bestaand.Any(l => l.UnitId == e.Id))
        {
            var r = await _docs.AddLink(projectId, documentId, "unit", e.Id, e.ClientAccountId.HasValue);
            if (r.Ok) aantal++;
        }
        if (e.ClientAccountId.HasValue && !bestaand.Any(l => l.ClientAccountId == e.ClientAccountId))
        {
            var r = await _docs.AddLink(projectId, documentId, "client", e.ClientAccountId.Value, true);
            if (r.Ok) aantal++;
        }
        return aantal;
    }

    private async Task<(int totaal, int gekoppeld)> KopersStatusAsync(List<InsuranceAttest> attesten, List<InsuranceAttestUnit> attestUnits, List<EenheidInfo> units, List<Insurances> polissen)
    {
        var met = attesten.Where(a => a.Kind == AttestTienjarige && a.Status == AttestOntvangen && a.DocumentId.HasValue).ToList();
        // Een tienjarige polis per eenheid met een document telt mee als attest voor die eenheid.
        var eenheidPolissen = polissen.Where(p => p.Type == 3 && p.UnitId.HasValue && p.DocumentId.HasValue).ToList();
        // Een tienjarige polis voor het hele project met een document: elke verkochte woning heeft er recht op.
        var projectPolissen = polissen.Where(p => p.Type == 3 && !p.UnitId.HasValue && p.DocumentId.HasValue).ToList();
        if (met.Count == 0 && eenheidPolissen.Count == 0 && projectPolissen.Count == 0) return (0, 0);
        var docIds = met.Select(a => a.DocumentId!.Value).Concat(eenheidPolissen.Select(p => p.DocumentId!.Value)).Concat(projectPolissen.Select(p => p.DocumentId!.Value)).Distinct().ToList();
        var links = await _db.DocumentLinks.AsNoTracking().Where(l => docIds.Contains(l.DocumentId) && l.ClientAccountId != null)
            .Select(l => new { l.DocumentId, ClientId = l.ClientAccountId!.Value }).ToListAsync();
        int totaal = 0, gekoppeld = 0;
        foreach (var a in met)
        {
            foreach (var u in attestUnits.Where(x => x.AttestId == a.Id))
            {
                var e = units.FirstOrDefault(x => x.Id == u.UnitId);
                if (e?.KoperId == null) continue;
                totaal++;
                if (links.Any(l => l.DocumentId == a.DocumentId && l.ClientId == e.KoperId)) gekoppeld++;
            }
        }
        foreach (var p in eenheidPolissen)
        {
            var e = units.FirstOrDefault(x => x.Id == p.UnitId);
            if (e?.KoperId == null) continue;
            totaal++;
            if (links.Any(l => l.DocumentId == p.DocumentId && l.ClientId == e.KoperId)) gekoppeld++;
        }
        foreach (var p in projectPolissen)
        {
            foreach (var e in units.Where(x => x.KoperId != null && x.Groep.Contains("woon", StringComparison.OrdinalIgnoreCase)))
            {
                totaal++;
                if (links.Any(l => l.DocumentId == p.DocumentId && l.ClientId == e.KoperId)) gekoppeld++;
            }
        }
        return (totaal, gekoppeld);
    }

    /// <summary>Rode en gouden aandachtspunten van de lopende projecten voor het dashboard van de projectleider (enkel de eigen projecten; null = alle projecten).
    /// Zonder de berekening van het definitieve budget, zodat het dashboard snel blijft. Een fout (bv. migratie nog niet gedraaid) breekt het dashboard nooit.</summary>
    public async Task<List<WarningBO>> GetDashboardWarningsAsync(string? userId)
    {
        var lijst = new List<WarningBO>();
        try
        {
            var q = _db.Project.AsNoTracking().Where(p => p.StatusId == (int)ProjectStatusType.Uitvoering);
            if (!string.IsNullOrEmpty(userId)) q = q.Where(p => p.AspNetUserId == userId);
            var projecten = await q.Select(p => new { p.ProjectId, p.ProjectName }).ToListAsync();
            foreach (var p in projecten)
            {
                var ov = await GetOverzichtAsync(p.ProjectId, "polissen", false, false, metBudget: false);
                // De oude controle op ABR-vervaldata (InsuranceService.CheckInsurances) blijft bestaan: die punten niet dubbel tonen.
                foreach (var a in ov.Aandachtspunten.Where(a => !a.Opgelost && (a.Tone == "danger" || a.Tone == "warning")
                             && !a.Sleutel.StartsWith("vervallen-abr") && !a.Sleutel.StartsWith("vervalt-abr") && !a.Sleutel.StartsWith("abr-verlengd")))
                    lijst.Add(new WarningBO { ID = 0, ProjectId = p.ProjectId, Type = a.Tone, Category = "verzekering", Display = $"{p.ProjectName}: {a.Titel}" });
            }
        }
        catch { /* het dashboard mag nooit stuk gaan door verzekeringen */ }
        return lijst;
    }

    /// <summary>Bewaart de keuze voor de tienjarige voor het hele project (null per aannemer afgeleid, 1 via projectpolis) en de opmerking erbij.</summary>
    public async Task<bool> SaveProjectTienjarigeAsync(int projectId, int? bron, string? opmerking)
    {
        var p = await _db.Project.FirstOrDefaultAsync(x => x.ProjectId == projectId);
        if (p == null) return false;
        p.TienjarigeBron = bron == 1 ? 1 : null;
        var tekst = string.IsNullOrWhiteSpace(opmerking) ? null : opmerking.Trim();
        p.TienjarigeOpmerking = tekst != null && tekst.Length > 300 ? tekst[..300] : tekst;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>Legt vast dat de ontbrekende attesten van deze contracten opgevraagd zijn (de mail zelf opent de gebruiker in zijn eigen e-mailprogramma).</summary>
    public async Task<int> MarkeerOpgevraagdAsync(int projectId, IEnumerable<int> contractIds, string? gebruiker)
    {
        var ids = contractIds.Distinct().ToList();
        var contracten = await _db.Contract.AsNoTracking().Include(c => c.ContractActivity).ThenInclude(a => a.Activity).ThenInclude(a => a.Group)
            .Where(c => c.ProjectId == projectId && ids.Contains(c.Id)).ToListAsync();
        var bestaand = await _db.InsuranceAttest.Where(a => a.ProjectId == projectId && ids.Contains(a.ContractId)).ToListAsync();
        var projectBron = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.TienjarigeBron).FirstOrDefaultAsync();
        var nu = DateTime.UtcNow;
        var aantal = 0;
        foreach (var c in contracten)
        {
            foreach (var kind in new[] { AttestTienjarige, AttestBa })
            {
                var a = bestaand.FirstOrDefault(x => x.ContractId == c.Id && x.Kind == kind);
                if (a != null && a.Status is AttestOntvangen or AttestNietVereist) continue;
                if (kind == AttestTienjarige && !TjVereist(c, a, projectBron)) continue;
                if (a == null)
                {
                    a = new InsuranceAttest { ProjectId = projectId, ContractId = c.Id, Kind = kind, Status = AttestOntbreekt, CreatedAt = nu };
                    _db.InsuranceAttest.Add(a);
                }
                a.LastRequestedAt = nu;
                a.LastRequestedBy = gebruiker;
                a.UpdatedAt = nu;
                aantal++;
            }
        }
        await _db.SaveChangesAsync();
        return aantal;
    }

    /// <summary>De mails voor "Attesten opvragen": per aannemer één bericht met de ontbrekende attesten.</summary>
    public async Task<List<VzMail>> BouwMailsAsync(int projectId, IEnumerable<int> contractIds, string gebruiker)
    {
        var overzicht = await GetOverzichtAsync(projectId, "attesten", false, false);
        var projectNaam = overzicht.ProjectName;
        var mails = new List<VzMail>();
        foreach (var r in overzicht.Attesten.Where(a => contractIds.Contains(a.ContractId) && a.KanOpvragen))
        {
            var lijst = new List<string>();
            if (r.TjOntbreekt) lijst.Add("- Attest tienjarige aansprakelijkheid" + (r.TjUnitsRequired > 0 ? $" (één per woning, {r.TjUnitsRequired} woningen)" : ""));
            if (r.BaOntbreekt) lijst.Add("- Attest BA-verzekering (aanbevolen, op te nemen in het contract)");
            var onderwerp = $"Verzekeringsattesten {projectNaam} · {r.ContractLabel}";
            var tekst = $"Beste,\n\nVoor project {projectNaam} (contract {r.ContractLabel}) hebben we nog de volgende verzekeringsattesten van jullie nodig:\n\n{string.Join("\n", lijst)}\n\nGelieve ze zo snel mogelijk te bezorgen, liefst vóór de start van jullie werken.\n\nMet vriendelijke groeten,\n{gebruiker}";
            mails.Add(new VzMail
            {
                ContractId = r.ContractId, Bedrijf = r.Bedrijf, Email = r.BedrijfEmail, Onderwerp = onderwerp, Tekst = tekst,
                Mailto = $"mailto:{r.BedrijfEmail}?subject={Uri.EscapeDataString(onderwerp)}&body={Uri.EscapeDataString(tekst)}"
            });
        }
        return mails;
    }

    /// <summary>Welke aandachtspunten dit type polis kan oplossen (lijst rechts onderaan het formulier).</summary>
    public static List<string> LostOp(int type, int? building) => type switch
    {
        1 => new() { "Verzekerde waarde ABR lager dan het definitieve budget", "ABR-termijn loopt af of is vervallen" },
        2 when building != 0 => new() { "Brandverzekering nieuwbouw nog niet afgesloten" },
        3 => new() { "Projectpolis tienjarige ontbreekt, of eenheden zonder tienjarige polis (contracten met Via projectpolis)" },
        _ => new()
    };
}

public class VzMail
{
    public int ContractId { get; set; }
    public string Bedrijf { get; set; } = "";
    public string Email { get; set; } = "";
    public string Onderwerp { get; set; } = "";
    public string Tekst { get; set; } = "";
    public string Mailto { get; set; } = "";
}
