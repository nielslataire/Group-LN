using System.Globalization;
using BOCore;
using BOCore.Budget;
using CPMCore.Configuration;
using CPMCore.Documents.GlV2;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using ServiceCore.Budget;
using static CPMCore.Documents.GlV2.GlV2PdfComponents;

namespace CPMCore.Services;

/// <summary>
/// Bouwt het budget-PDF-model uit de berekeningen van de wizard en rendert het in de gl-v2-layout (design-handoff 35q intern, 35r bank).
/// Alle getallen komen uit dezelfde diensten als de resultaatpagina (berekening, verkoopvoorstel, herberekende verkooplijnen, verkoopresultaat),
/// zodat PDF en scherm nooit uiteenlopen. De bankversie laat financiering, onvoorzien en publiciteit weg uit de kostprijs en toont geen verkoop per eenheid.
/// </summary>
public sealed class BudgetPdfBuilder
{
    private static readonly CultureInfo NlBe = CultureInfo.GetCultureInfo("nl-BE");
    private readonly cpmRunningContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BudgetPdfBuilder> _logger;
    private readonly IIssuerCompanyService _issuerCompanyService;
    private readonly GlV2PdfCompanyOptions _fallback;
    private readonly BudgetBerekeningService _berekening;
    private readonly IVerkoopVoorstelService _voorstel;
    private readonly BudgetActivityService _activiteiten;
    private readonly IBudgetService _budgetService;
    private readonly BouwIndexService _bouwIndex;

    public BudgetPdfBuilder(cpmRunningContext db, IWebHostEnvironment env, ILogger<BudgetPdfBuilder> logger, IIssuerCompanyService issuerCompanyService,
        IOptions<GlV2PdfCompanyOptions> fallback, BudgetBerekeningService berekening, IVerkoopVoorstelService voorstel, BudgetActivityService activiteiten, IBudgetService budgetService, BouwIndexService bouwIndex)
    {
        _db = db; _env = env; _logger = logger; _issuerCompanyService = issuerCompanyService; _fallback = fallback.Value;
        _berekening = berekening; _voorstel = voorstel; _activiteiten = activiteiten; _budgetService = budgetService; _bouwIndex = bouwIndex;
    }

    private static string E(decimal v) => (v < 0 ? "− € " : "€ ") + Math.Abs(v).ToString("N0", NlBe);
    private static string Pct(decimal fractie) => (fractie * 100m).ToString("0.00", NlBe) + " %";

    public async Task<(byte[] Pdf, string Bestandsnaam)?> RenderAsync(int versieId, bool intern, string opgemaaktDoor, CancellationToken ct = default)
    {
        var versie = await _db.BudgetVersie.AsNoTracking()
            .Include(v => v.BudgetMaster).Include(v => v.BudgetGegevens)
            .Include(v => v.Project).ThenInclude(p => p.PostalCode)
            .FirstOrDefaultAsync(v => v.Id == versieId, ct);
        if (versie is null) return null;

        var model = await BouwModelAsync(versie, intern, opgemaaktDoor, ct);
        var issuer = await LaadIssuerAsync(versie.Project, ct);
        var company = BouwBedrijf(issuer);
        var fontsAvailable = Documents.GlV2.GlV2PdfFonts.EnsureRegistered(_env, _logger);
        var pdf = new BudgetDocumentV2(model, company, fontsAvailable).GeneratePdf();

        var naam = $"Budget_{(intern ? "intern" : "bank")}_{versie.Project?.ProjectName}_v{versie.Versienummer}_{DateTime.Now:yyyyMMdd}.pdf";
        foreach (var c in Path.GetInvalidFileNameChars()) naam = naam.Replace(c, '_');
        return (pdf, naam.Replace(' ', '_'));
    }

    private async Task<IssuerCompanyBO?> LaadIssuerAsync(Project? project, CancellationToken ct)
    {
        if (project is null) return null;
        var id = await Services.Signing.ChangeOrderIssuerResolver.ResolveIssuerCompanyIdAsync(_db, project, ct);
        return id is int i ? await _issuerCompanyService.GetAsync(i, ct) : null;
    }

    private GlV2PdfCompanyInfo BouwBedrijf(IssuerCompanyBO? issuer)
    {
        var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
        byte[]? logo = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
        if (issuer is null)
            return new GlV2PdfCompanyInfo
            {
                Name = _fallback.Name, Tagline = _fallback.Tagline, Street = _fallback.Street, PostalCity = _fallback.PostalCity, Phone = _fallback.Phone,
                Email = _fallback.Email, Website = _fallback.Website, LegalForm = _fallback.LegalForm, VatNumber = _fallback.VatNumber, Iban = _fallback.Iban, LogoBytes = logo,
            };
        string Join(params string?[] s) => string.Join(" ", s.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new GlV2PdfCompanyInfo
        {
            Name = issuer.Name ?? _fallback.Name,
            Street = Join(issuer.AddressLine1, issuer.AddressLine2) is { Length: > 0 } st ? st : _fallback.Street,
            PostalCity = Join(issuer.PostalCode, issuer.City) is { Length: > 0 } pc ? pc : _fallback.PostalCity,
            Phone = issuer.Phone ?? _fallback.Phone,
            Email = issuer.Email ?? _fallback.Email,
            Website = issuer.Website ?? _fallback.Website,
            LegalForm = Join(issuer.LegalName ?? issuer.Name, issuer.CompanyLegalFormAbbreviation),
            VatNumber = issuer.VatNumber ?? _fallback.VatNumber,
            Iban = issuer.DefaultBankAccountIban ?? _fallback.Iban,
            LogoBytes = issuer.LogoBytes is { Length: > 0 } ? issuer.LogoBytes : logo,
        };
    }

    private async Task<BudgetPdfModel> BouwModelAsync(BudgetVersie versie, bool intern, string opgemaaktDoor, CancellationToken ct)
    {
        var id = versie.Id;
        var r = await _berekening.BerekenAsync(id);
        var p = await _berekening.GetOrCreateParamsAsync(id);
        var voorstel = await _voorstel.BerekenAsync(id);
        var sam = await _voorstel.SamenvattingAsync(id);
        var lijnen = await _voorstel.HerberekendeLijnenAsync(id);
        var lots = await _activiteiten.GetLotGroepenAsync(id);
        var oppResp = _budgetService.GetBudgetOppervlaktes(id);
        var opps = (oppResp.Success && oppResp.Values != null ? oppResp.Values : new List<BudgetOppervlaktesBO>()).OrderBy(o => o.SortOrder).ToList();
        var issuer = await LaadIssuerAsync(versie.Project, ct);

        var n = Math.Max(1, r.AantalEenheden);
        var gba = r.TotaalGBA > 0 ? r.TotaalGBA : opps.Sum(o => o.BewoonbareOpp);
        var fin = r.Financiering.Sum(f => f.Bedrag);
        var onv = r.Onvoorzien;
        var publ = r.Forfaits.Where(f => string.Equals(f.Omschrijving, "Publiciteit", StringComparison.OrdinalIgnoreCase)).Sum(f => f.Bedrag);
        var kostprijs = intern ? r.TotaalKosten : r.TotaalKosten - fin - onv - publ;
        var vr = VerkoopResultaat.Bereken(sam, voorstel, lijnen, p.MakelaarsCommissiePerc ?? 0m, p.OverigeVerkoopkosten ?? 0m, kostprijs);

        var datum = DateTime.Now;
        var status = (versie.Status ?? "").ToLowerInvariant();
        var versieKort = $"v{versie.Versienummer}";
        var m = new BudgetPdfModel
        {
            Intern = intern,
            ProjectNaam = versie.Project?.ProjectName ?? "",
            VersieKort = versieKort,
            DatumTekst = datum.ToString("dd/MM/yyyy", NlBe),
            AantalEenheden = r.AantalEenheden,
            Kostprijs = kostprijs,
            KostprijsToelichting = intern ? "alle kosten inbegrepen" : "",
            HeeftVerkoop = vr.HeeftVerkoop,
            Verkoopwaarde = vr.Verkoopwaarde,
            Verkoopkosten = vr.Verkoopkosten,
            Resultaat = vr.Resultaat,
            MargeOpKost = vr.MargeOpKostprijs,
            MargeOpVerkoop = vr.MargeOpVerkoop,
            ResultaatPerEenheid = vr.ResultaatPerLijn,
            BreakEven = vr.BreakEven,
            TotaalBouw = r.TotaalBouw,
            TotaalGba = gba,
        };
        m.Ondertitel = $"Budget {versieKort}{(string.IsNullOrWhiteSpace(status) ? "" : " · " + status)} {m.DatumTekst}" + (intern ? " · alle kosten, incl. financiering en reserves" : "");
        m.VoetMidden = $"Budget{(intern ? " intern" : "")} · {versieKort} · {m.DatumTekst}";

        // ── Projectfiche ─────────────────────────────────────────────────────────────────────────
        var pc = versie.Project?.PostalCode;
        var types = opps.Where(o => !string.IsNullOrWhiteSpace(o.TypeName)).GroupBy(o => o.TypeName!.Trim()).Select(g => $"{g.Count()} × {g.Key.ToLowerInvariant()}");
        var alleWoon = opps.Count > 0 && opps.All(o => (o.GroupTypeName ?? "").Contains("woon", StringComparison.OrdinalIgnoreCase));
        var perceel = opps.Sum(o => o.Grondopp + o.GrondoppAgrarisch);
        string Index(decimal start, decimal nu) => start > 0 ? (nu / start).ToString("0.000", NlBe) : "—";
        var g0 = versie.BudgetGegevens;
        // Zelfde terugval als de rest van de wizard: ontbreekt een bewaarde index, dan de actieve index (start = 100)
        var sStart = g0?.SIndexStart ?? 100m; var iStart = g0?.IIndexStart ?? 100m;
        var sNu = g0?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
        var iNu = g0?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I2021");
        var peil = (g0?.UpdatedAt ?? versie.CreatedAt).ToString("MM/yyyy", NlBe);
        m.IndexPeildatum = peil;
        // Zoals op stap 1 (Gegevens): de ingevulde indexwaarden zelf, met peildatum en bron van de gekozen S-index
        var sRij = BudgetControleService.ZoekIndexRij(await _db.BouwIndex.AsNoTracking().Where(x => x.IndexType == "S").ToListAsync(ct), sNu);
        string indexInfo = sRij is null ? "handmatig ingevuld"
            : string.Join(" · ", new[] { (sRij.GeldigVanaf?.ToString("dd/MM/yyyy") ?? (sRij.Jaar.HasValue ? (sRij.Maand.HasValue ? $"{sRij.Maand:00}/{sRij.Jaar}" : sRij.Jaar.ToString()) : null)) is { } pd ? "peildatum " + pd : null, string.IsNullOrWhiteSpace(sRij.Bron) ? null : "bron " + sRij.Bron }.Where(x => x != null));
        var start = versie.Project?.StartDateConstruction; var oplev = versie.Project?.DeliveryDate;
        m.Fiche = new[]
        {
            new FicheVeld("Werf / adres", versie.Project?.Street ?? "—", pc is null ? null : $"{pc.Postcode} {pc.Gemeente}"),
            new FicheVeld("Bouwheer", issuer?.Name ?? "—", issuer is null ? null : string.Join(", ", new[] { string.Join(" ", new[] { issuer.AddressLine1, issuer.AddressLine2 }.Where(s => !string.IsNullOrWhiteSpace(s))), string.Join(" ", new[] { issuer.PostalCode, issuer.City }.Where(s => !string.IsNullOrWhiteSpace(s))) }.Where(s => !string.IsNullOrWhiteSpace(s)))),
            new FicheVeld("Eenheden", $"{r.AantalEenheden} {(alleWoon ? "woningen" : "eenheden")}", string.Join(" · ", types)),
            new FicheVeld("Oppervlakte", $"{gba.ToString("N0", NlBe)} m² GBA", perceel > 0 ? $"perceel {perceel.ToString("N0", NlBe)} m²" : null),
            new FicheVeld("Versie", string.IsNullOrWhiteSpace(versie.BudgetMaster?.Naam) ? versieKort : $"{versie.BudgetMaster!.Naam} · {versieKort}", status == "definitief" ? "Definitief" : status == "afgerond" ? "Afgerond" : "Concept"),
            new FicheVeld("Indexen", $"S {sNu.ToString("F4", NlBe)} · I {iNu.ToString("F4", NlBe)}", indexInfo),
            new FicheVeld("Bouwstart", start?.ToString("MM/yyyy", NlBe) ?? "—", oplev is null ? null : $"oplevering {oplev.Value.ToString("MM/yyyy", NlBe)}"),
            new FicheVeld("Opgemaakt", string.IsNullOrWhiteSpace(opgemaaktDoor) ? "—" : opgemaaktDoor, m.DatumTekst),
        };

        // ── Kostenoverzicht ──────────────────────────────────────────────────────────────────────
        bool IsVerzekering(BudgetKostenPostBO k) => (k.Omschrijving ?? "").StartsWith("Decennale", StringComparison.OrdinalIgnoreCase) || (k.Omschrijving ?? "").StartsWith("ABR", StringComparison.OrdinalIgnoreCase);
        string PostBasis(BudgetKostenPostBO k) => k.IsPerc ? $"{Pct(k.Percentage)} × {E(k.BasisBedrag)}" : "forfait";
        var erelonen = r.KostenOpBouw.Where(k => !IsVerzekering(k) && k.Bedrag != 0m).ToList();
        var verz = r.KostenOpBouw.Where(k => IsVerzekering(k) && k.Bedrag != 0m).ToList();
        var forfaitsZonderPubl = r.Forfaits.Where(f => f.Bedrag != 0m && !string.Equals(f.Omschrijving, "Publiciteit", StringComparison.OrdinalIgnoreCase)).ToList();
        var grond = r.Grond.Where(k => k.Bedrag != 0m).ToList();

        var overzicht = new List<BudgetPost>
        {
            new("Bouwkost (activiteiten)", $"{gba.ToString("N0", NlBe)} m² GBA · {(gba > 0 ? E(r.TotaalBouw / gba) : "—")}/m² · p. {p:bouw}", r.TotaalBouw),
        };
        foreach (var f in forfaitsZonderPubl)
            overzicht.Add(new(f.Omschrijving == "Infrastructuur" ? "Aansluitingen nutsvoorzieningen" : f.Omschrijving, f.Omschrijving == "Infrastructuur" ? $"{r.AantalEenheden} × {E(f.Bedrag / n)}" : "forfait", f.Bedrag));
        if (erelonen.Count > 0) overzicht.Add(new("Erelonen & studies", "p. {p:erelonen}", erelonen.Sum(k => k.Bedrag)));
        if (verz.Count > 0) overzicht.Add(new("Verzekeringen", "decennale + ABR · p. {p:erelonen}", verz.Sum(k => k.Bedrag)));
        if (grond.Count > 0) overzicht.Add(new("Grondkost", "aankoop + rechten + notaris · p. {p:grond}", grond.Sum(k => k.Bedrag)));
        if (intern)
        {
            if (fin != 0m) overzicht.Add(new("Financieringskosten", "p. {p:financiering}", fin));
            if (onv != 0m) overzicht.Add(new("Onvoorzien", $"{Pct(p.OnvoorzienPerc)} van bouwkost", onv));
            if (publ != 0m) overzicht.Add(new("Publiciteit", p.PubliciteitPerc is > 0m ? $"{Pct(p.PubliciteitPerc.Value)} van de verkoopprijs" : "forfait", publ));
        }
        m.Kostenoverzicht = overzicht;

        // ── Bouwkosten per activiteit (lots uit stap 6) ─────────────────────────────────────────
        var delen = new List<BudgetDeel>();
        var deelNr = 0;
        foreach (var lot in lots.OrderBy(l => l.LotNummer))
        {
            var rijen = new List<BudgetActiviteitRij>();
            foreach (var l in lot.Lijnen)
            {
                var bedrag = l.TotaalAlternatief * (l.Correctiefactor <= 0m ? 1m : l.Correctiefactor);
                if (l.TotaalAlternatief <= 0m) continue;
                rijen.Add(new BudgetActiviteitRij(l.ActivityOmschrijving ?? "", l.AantalEenheden > 0 ? $"{l.AantalEenheden} × {E(bedrag / l.AantalEenheden)}" : "forfait", bedrag));
            }
            if (rijen.Count == 0) continue;
            deelNr++;
            delen.Add(new BudgetDeel($"Deel {deelNr} — {lot.LotNaam}", rijen, rijen.Sum(x => x.Bedrag)));
        }
        m.Delen = delen;
        m.Erelonen = erelonen.Select(k => new BudgetPost(k.Omschrijving ?? "", PostBasis(k), k.Bedrag)).ToList();
        m.Verzekeringen = verz.Select(k => new BudgetPost(k.Omschrijving ?? "", PostBasis(k), k.Bedrag)).ToList();
        m.Overige = forfaitsZonderPubl.Select(f => new BudgetPost(f.Omschrijving == "Infrastructuur" ? "Aansluitingen nutsvoorzieningen" : f.Omschrijving ?? "", f.Omschrijving == "Infrastructuur" ? $"{r.AantalEenheden} × {E(f.Bedrag / n)}" : "forfait", f.Bedrag)).ToList();

        // ── Grondkost ────────────────────────────────────────────────────────────────────────────
        var aankopen = NotarisTarief.Aankopen(p.GrondAankopenJson, p.AankoopprijsGrond ?? 0m);
        m.Aankopen = aankopen.Select((a, i) => new BudgetGrondAankoopRij(i + 1, a.O ?? "", DateTime.TryParse(a.D, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("dd/MM/yyyy", NlBe) : "", a.P)).ToList();
        m.Grondposten = grond.Select(k =>
        {
            var o = k.Omschrijving ?? "";
            var basis = o.StartsWith("Aankoopprijs", StringComparison.OrdinalIgnoreCase) ? $"{aankopen.Count} {(aankopen.Count == 1 ? "aankoop" : "aankopen")}"
                : o.StartsWith("Registratierechten", StringComparison.OrdinalIgnoreCase) ? $"{Pct(k.Percentage)} × {E(k.BasisBedrag)}"
                : o.StartsWith("Notariskosten", StringComparison.OrdinalIgnoreCase) ? "wettelijke schaal + 21 % btw"
                : o.StartsWith("Notaris: akte", StringComparison.OrdinalIgnoreCase) ? $"{aankopen.Count} {(aankopen.Count == 1 ? "akte" : "akten")} × {E(p.AktekostenPerAkte ?? NotarisTarief.AktekostenStandaard)} + 21 % btw"
                : "";
            return new BudgetPost(o, basis, k.Bedrag);
        }).ToList();
        m.TotaalGrond = grond.Sum(k => k.Bedrag);
        m.GrondPerEenheid = $"{E(m.TotaalGrond / n)} / eenheid";

        if (intern)
        {
            m.Financiering = r.Financiering.Where(f => f.Bedrag != 0m).Select(f => new BudgetPost(f.Omschrijving ?? "", f.IsPerc ? $"{Pct(f.Percentage)} × {E(f.BasisBedrag)}" : "forfait", f.Bedrag)).ToList();
            var res = new List<BudgetPost>();
            if (onv != 0m) res.Add(new("Onvoorzien", $"{Pct(p.OnvoorzienPerc)} × bouwkost", onv));
            if (publ != 0m) res.Add(new("Publiciteit", p.PubliciteitPerc is > 0m ? $"{Pct(p.PubliciteitPerc.Value)} van de verkoopprijs" : "forfait", publ));
            m.Reserves = res;
        }

        // ── Verkoopkosten, verkooptabel en resultaat per eenheid ─────────────────────────────────
        if (vr.HeeftVerkoop)
        {
            var com = Pct(vr.CommissiePerc);
            var vk = new List<BudgetPost>
            {
                new("Makelaar — grondaandeel", $"{com} × {E(vr.GrondDeel)} + 21 % btw (niet aftrekbaar)", vr.GrondKostInclBtw),
                new("Makelaar — bouwaandeel", $"{com} × {E(vr.BouwDeel)} · btw aftrekbaar, telt niet mee", vr.CommissieBouw),
            };
            if (vr.Overige > 0m) vk.Add(new("Overige verkoopkosten", "forfait per project", vr.Overige));
            m.VerkoopkostenPosten = vk;
            m.Aandachtstekst = intern ? $"Interne versie: bevat reserves (onvoorzien {E(onv)}) en financieringskosten. Niet bezorgen aan bank of klant — gebruik daarvoor de bankversie." : "";
        }
        else
        {
            m.Aandachtstekst = intern ? $"Interne versie: bevat reserves (onvoorzien {E(onv)}) en financieringskosten. Niet bezorgen aan bank of klant — gebruik daarvoor de bankversie." : "";
        }

        if (intern && vr.HeeftVerkoop)
        {
            var eenheden = voorstel.Eenheden.ToDictionary(e => (e.EenheidNaam ?? "").Trim(), e => e, StringComparer.OrdinalIgnoreCase);
            var metPrijs = lijnen.Where(l => l.Vraagprijs is > 0m).ToList();
            var tabel = new List<BudgetVerkoopRij>(); var resRijen = new List<BudgetResultaatRij>();
            foreach (var l in metPrijs)
            {
                eenheden.TryGetValue((l.EenheidNaam ?? "").Trim(), out var e);
                var vp = l.Vraagprijs!.Value;
                var g = (l.Grondwaarde ?? 0m) + (l.GrondwaardeAgrarisch ?? 0m);
                var gShare = (g + (l.Bouwwaarde ?? 0m)) > 0m ? g / (g + (l.Bouwwaarde ?? 0m)) : (vr.Verkoopwaarde > 0m ? vr.GrondDeel / vr.Verkoopwaarde : 0m);
                var gDeel = vp * gShare; var bDeel = vp - gDeel;
                var type = e is null ? "" : (string.IsNullOrWhiteSpace(e.TypeName) ? e.GroupTypeName : e.TypeName) ?? "";
                tabel.Add(new BudgetVerkoopRij(l.EenheidNaam ?? "", type, gDeel, bDeel, vp, e is { BewoonbareOpp: > 0m } ? vp / e.BewoonbareOpp : 0m));
                var kosten = vr.CommissiePerc * (gDeel * (1m + VerkoopResultaat.BtwCommissie) + bDeel) + vr.Overige / metPrijs.Count;
                var kp = e?.Kostprijs ?? 0m;
                var res = vp - kosten - kp;
                resRijen.Add(new BudgetResultaatRij(l.EenheidNaam ?? "", vp, kosten, kp, res, kp > 0m ? res / kp : 0m));
            }
            m.Verkooptabel = tabel; m.ResultaatPerLot = resRijen;
        }

        // ── Oppervlaktes ─────────────────────────────────────────────────────────────────────────
        m.Oppervlaktes = opps.Select((o, i) => new BudgetOppRij
        {
            Nr = i + 1, Naam = o.EenheidNaam ?? "", Hoofdtype = o.GroupTypeName ?? "", Subtype = o.TypeName ?? "",
            Bewoonbaar = o.BewoonbareOpp, Tuin = o.Tuin, TerrasPrefab = o.TerrasPrefab, TerrasGelijkvloers = o.TerrasGelijkvloers, Dakterras = o.Dakterras,
            GarageBovengronds = o.GaragesParkingsBovenGr, BergingOndergronds = o.GarBergOndergronds, BergingGelijkvloers = o.BergGelijkvloers, Carport = o.Carports, Poorten = o.Poorten,
            DoorritGvl = o.DoorritGVL, Zolder = o.Zolder, GemeenschappelijkeDelen = o.GemeenschappelijkeDelen, Wegenis = o.Wegenis,
            Grond = intern ? o.Grondopp : o.Grondopp + o.GrondoppAgrarisch, Grond2 = intern ? o.GrondoppAgrarisch : 0m,
            GrondTotaal = intern && o.GrondoppAgrarisch != 0m ? o.Grondopp + o.GrondoppAgrarisch : 0m, Gereduceerd = o.OppGereduceerd,
        }).ToList();
        bool Is(BudgetOppervlaktesBO o, string s) => (o.GroupTypeName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase);
        var woon = opps.Count(o => Is(o, "woon")); var park = opps.Count(o => Is(o, "park") || Is(o, "garage")); var comm = opps.Count(o => Is(o, "commerci"));
        var bew = opps.Sum(o => o.BewoonbareOpp); var ger = opps.Sum(o => o.OppGereduceerd); var grondTot = opps.Sum(o => o.Grondopp); var grond2 = opps.Sum(o => o.GrondoppAgrarisch);
        string M(decimal v) => v.ToString("N2", NlBe) + " m²";
        var tegels = new List<KpiTegel>
        {
            new("Wooneenheden", woon.ToString(NlBe)), new("Parkeerplaatsen", park.ToString(NlBe)), new("Commercieel", comm.ToString(NlBe)),
            new("Totaal bewoonbaar", M(bew)), new("Gereduceerd", M(ger), "VMSW-factoren"),
            new("Gem. / eenheid", M(opps.Count > 0 ? bew / opps.Count : 0m), "bewoonbaar"),
            new("Grondopp. totaal", M(grondTot + grond2), intern && grond2 > 0m ? $"grond {M(grondTot)} + grond 2 {M(grond2)}" : null, Donker: true),
        };
        m.OppTegels = tegels;
        m.OppNoot = "Gereduceerde oppervlakte volgens de VMSW-reductiefactoren van deze budgetversie." + (intern && grond2 > 0m ? " Grond 2 = bijkomende grondoppervlakte (tweede zone met eigen waarde)." : "");
        return m;
    }
}
