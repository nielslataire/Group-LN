using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.Verkoop;

/// <summary>Verkoop (design-handoff 45): de pagina Te koop, de verkoopinstellingen en de berekening van wat een koper betaalt.
/// Eén plaats voor de cijfers, zodat de lijst (45a), het voorbeeld in de instellingen (45b), de berekenpagina (45c) en de documenten (45e/45h)
/// dezelfde bedragen tonen.
///
/// Berekening: grond + registratierechten + bouw + btw + kosten koper + btw op de kosten koper (zoals de oude Ramingsprijs). Welke belasting op grond en bouw valt, volgt het bestaande stelsel:
/// "verkoop onder btw-stelsel" of type registratie "grond" = btw op de bouw en registratierechten op de grond; type "bestaand gebouw" = enkel
/// registratierechten op alles; zonder keuze enkel btw op alles.</summary>
public class VerkoopService
{
    private readonly cpmRunningContext _db;

    public VerkoopService(cpmRunningContext db) { _db = db; }

    public const int TypeAppartement = 1;

    // ── Instellingen ──────────────────────────────────────────────────────────────────────────────

    public async Task<VerkoopInstellingen> GetInstellingenAsync(int projectId)
    {
        var s = await _db.ProjectSalesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Projectid == projectId);
        var extra = await _db.ProjectSalesExtraCost.AsNoTracking().Where(x => x.ProjectId == projectId).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
        return new VerkoopInstellingen
        {
            VatPercentage = s?.Vatpercentage ?? 0m,
            RegistrationPercentage = s?.RegistrationPercentage ?? 0m,
            MixedVat = s?.MixedVatregistration ?? false,
            RegistrationType = s?.RegistrationType ?? 0,
            FixedCertificateCost = s?.FixedCertificateCost ?? 0m,
            MortgageRegistrationCost = s?.MortageRegistrationCost ?? 0m,
            BaseCertificateCost = s?.BaseCertificateCost ?? 0m,
            ParcelCost = s?.ParcelCost ?? 0m,
            ConnectionFees = s?.ConnectionFees ?? 0m,
            SurveyorCost = s?.SurveyorCost ?? 0m,
            Extra = extra.Select(e => new VerkoopExtraKost { Id = e.Id, Naam = e.Name, Bedrag = e.Amount, Toelichting = e.Note }).ToList(),
        };
    }

    // ── Berekening ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Wat een koper betaalt voor één eenheid. <paramref name="grond"/> en <paramref name="bouw"/> zijn de waarden excl. btw.</summary>
    public static KoperBerekening Bereken(decimal grond, decimal bouw, bool appartement, VerkoopInstellingen i, bool enigeWoning = true)
    {
        var regime = i.Regime;
        decimal reg = 0m, vat = 0m;
        switch (regime)
        {
            case VerkoopRegime.Gemengd: reg = Pct(grond, i.RegistrationPercentage); vat = Pct(bouw, i.VatPercentage); break;
            case VerkoopRegime.Registratie: reg = Pct(grond + bouw, i.RegistrationPercentage); break;
            default: vat = Pct(grond + bouw, i.VatPercentage); break;
        }
        // Zoals de oude Ramingsprijs: 21 % btw op de akte-, hypotheek-, basisakte-, verkavelings- en landmetingskosten, aansluitkosten aan het btw-percentage van het project.
        const decimal btwKosten = 21m;
        var posten = new List<KostPost>
        {
        };
        if (appartement) posten.Add(new("Aandeel basisakte", i.BaseCertificateCost, "enkel bij appartementen", btwKosten));
        posten.Add(new("Aandeel verkavelingsakte", i.ParcelCost, null, btwKosten));
        posten.Add(new("Aansluitkosten", i.ConnectionFees, "water, elektriciteit, riolering", i.VatPercentage));
        posten.Add(new("Landmetingskosten", i.SurveyorCost, null, btwKosten));
        foreach (var e in i.Extra) posten.Add(new(e.Naam, e.Bedrag, e.Toelichting, btwKosten));
        posten = posten.Where(p => p.Bedrag != 0m).ToList();
        var kosten = posten.Sum(p => p.Bedrag);
        var kostenBtw = posten.Sum(p => Pct(p.Bedrag, p.BtwPct));
        var notaris = NotarisTarief.Bereken(grond, bouw, regime, enigeWoning, i.MortgageRegistrationCost);
        return new KoperBerekening
        {
            Grond = grond, Bouw = bouw, Registratierechten = reg, Btw = vat, KostenKoper = kosten, KostenBtw = kostenBtw, Notaris = notaris, EnigeWoning = enigeWoning, Posten = posten, Regime = regime,
            RegistratiePercentage = i.RegistrationPercentage, BtwPercentage = i.VatPercentage,
        };
    }

    private static decimal Pct(decimal basis, decimal pct) => Math.Round(basis * pct / 100m, 2);

    /// <summary>Website- en zichtbaarheidsinstellingen van het project (kaart Website op de instellingenpagina).</summary>
    public async Task<VerkoopWebsite> GetWebsiteAsync(int projectId)
    {
        var s = await _db.ProjectSalesSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Projectid == projectId);
        var p = await _db.Project.AsNoTracking().Where(x => x.ProjectId == projectId).Select(x => new { x.CommercialTitleNl, x.CommercialTextNl, x.IssuerCompanyIdBuilder }).FirstOrDefaultAsync();
        return new VerkoopWebsite
        {
            SaleVisible = s?.SaleVisible ?? false, IsFeatured = s?.IsFeatured ?? false, DisplayStatus = s?.SalesDisplayStatus,
            Title = p?.CommercialTitleNl ?? "", Text = p?.CommercialTextNl ?? "", BankAccountId = s?.BankAccountId, BuilderCompanyId = p?.IssuerCompanyIdBuilder,
        };
    }

    /// <summary>Bewaart de verkoopinstellingen, de eigen kosten koper en de websitevelden in één keer. Schrijft rechtstreeks via de context (zoals BorgService),
    /// zodat het oude scherm geen kolommen leegmaakt die hier niet staan.</summary>
    public async Task SaveAsync(int projectId, VerkoopInstellingen i, VerkoopWebsite w, string? bankIban)
    {
        var s = await _db.ProjectSalesSettings.FirstOrDefaultAsync(x => x.Projectid == projectId);
        if (s == null) { s = new ProjectSalesSettings { Projectid = projectId }; _db.ProjectSalesSettings.Add(s); }
        s.Vatpercentage = i.VatPercentage; s.RegistrationPercentage = i.RegistrationPercentage;
        s.RegistrationType = i.RegistrationType == 0 ? null : i.RegistrationType;
        s.MixedVatregistration = i.RegistrationType == 2;
        // FixedCertificateCost (Vaste aktekost) blijft ongemoeid: de notariskosten worden nu berekend (NotarisTarief)
        s.MortageRegistrationCost = i.MortgageRegistrationCost; s.BaseCertificateCost = i.BaseCertificateCost;
        s.ParcelCost = i.ParcelCost; s.ConnectionFees = i.ConnectionFees; s.SurveyorCost = i.SurveyorCost;
        s.SaleVisible = w.SaleVisible; s.IsFeatured = w.IsFeatured; s.SalesDisplayStatus = w.DisplayStatus;
        if (w.BankAccountId.HasValue) { s.BankAccountId = w.BankAccountId; if (!string.IsNullOrWhiteSpace(bankIban)) s.BankAccountNumber = bankIban; }

        var p = await _db.Project.FirstOrDefaultAsync(x => x.ProjectId == projectId);
        if (p != null) { p.CommercialTitleNl = string.IsNullOrWhiteSpace(w.Title) ? null : w.Title.Trim(); p.CommercialTextNl = string.IsNullOrWhiteSpace(w.Text) ? null : w.Text; }

        var bestaand = await _db.ProjectSalesExtraCost.Where(x => x.ProjectId == projectId).ToListAsync();
        _db.ProjectSalesExtraCost.RemoveRange(bestaand);
        var volgorde = 0;
        foreach (var e in i.Extra.Where(e => !string.IsNullOrWhiteSpace(e.Naam)))
            _db.ProjectSalesExtraCost.Add(new ProjectSalesExtraCost { ProjectId = projectId, Name = e.Naam.Trim(), Amount = e.Bedrag, Note = string.IsNullOrWhiteSpace(e.Toelichting) ? null : e.Toelichting.Trim(), SortOrder = volgorde++ });
        await _db.SaveChangesAsync();
    }

    /// <summary>Getal in Belgische notatie ("1.000,50", "12,5", "1000.5") naar decimal; leeg of onleesbaar = 0.</summary>
    public static decimal ParseBedrag(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        s = s.Replace("€", "").Replace(" ", "").Replace(" ", "");
        if (s.Contains(',')) s = s.Replace(".", "").Replace(',', '.');
        else if (s.Count(c => c == '.') > 1) s = s.Replace(".", "");
        return decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    // ── Te koop (45a) ─────────────────────────────────────────────────────────────────────────────

    public async Task<VerkoopIndexVm> GetIndexAsync(int projectId)
    {
        var project = await _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => new { p.ProjectName }).FirstOrDefaultAsync();
        var inst = await GetInstellingenAsync(projectId);

        var units = await _db.Units.AsNoTracking()
            .Where(u => u.ProjectId == projectId && u.AttachedUnitId == null && u.LinkedUnitId == null)
            .Include(u => u.Type).Include(u => u.ClientAccount).Include(u => u.UnitConstructionValue).Include(u => u.UnitFinishingOption)
            .OrderBy(u => u.Name).ToListAsync();

        var unitIds = units.Select(u => u.Id).ToList();
        var interesse = await _db.CrmContactUnit.AsNoTracking()
            .Where(cu => unitIds.Contains(cu.UnitId) && cu.Contact.ProjectId == projectId && cu.Contact.Status < 4)
            .GroupBy(cu => cu.UnitId).Select(g => new { UnitId = g.Key, N = g.Select(x => x.ContactId).Distinct().Count() })
            .ToDictionaryAsync(g => g.UnitId, g => g.N);

        var rijen = new List<VerkoopRijVm>();
        foreach (var u in units)
        {
            var verkocht = u.ClientAccountId != null;
            var optie = !verkocht && u.IsOption;
            // Een verkochte eenheid toont de prijs waarvoor ze verkocht werd, als die vastligt.
            // Verkocht: exact wat Klanten/Detail toont (UnitBO.TotalValueSold = verkochte grondwaarde + som van de verkochte bouwwaardes).
            // Niet verkocht: elke afwerking is een volledig alternatief, dus niet optellen (ServiceCore.Helpers.UnitPricing): we nemen de standaardafwerking.
            decimal grond, bouw;
            if (verkocht)
            {
                grond = u.LandValueSold ?? 0m;
                bouw = u.UnitConstructionValue.Sum(c => c.ValueSold ?? 0m);
            }
            else
            {
                grond = u.LandValue ?? 0m;
                var standaard = u.UnitFinishingOption.FirstOrDefault(o => o.IsDefault)?.Id;
                bouw = ServiceCore.Helpers.UnitPricing.Compute(u.UnitConstructionValue.Select(c => (c.FinishingOptionId, c.Value ?? 0m)), standaard).Standard;
            }
            var appartement = u.TypeId == TypeAppartement;
            var totaal = grond + bouw;
            var rij = new VerkoopRijVm
            {
                UnitId = u.Id, Naam = u.Name ?? "", TypeNaam = u.Type?.Name ?? "", TypeId = u.TypeId,
                Verdieping = (u.Level ?? 0) == 0 ? "gelijkvloers" : "verdieping " + u.Level,
                IsAppartement = appartement, Status = verkocht ? "Verkocht" : optie ? "Optie" : "Te koop",
                Grond = grond, Bouw = bouw, Totaal = totaal,
                PreKad = u.PreKad ?? "", Perceel = u.GroundSurface, Wonen = u.Surface, Tienduizendsten = u.Landshare,
                KoperNaam = u.ClientAccount?.Name, CompromisDatum = u.ClientAccount?.DateSalesAgreement, AkteDatum = u.ClientAccount?.DateDeedOfSale,
                OptionUntil = u.OptionUntil, OptionHolder = u.OptionHolder, SaleVia = u.SaleVia, OnlineSince = u.OnlineSince,
                Interessenten = interesse.GetValueOrDefault(u.Id),
                Koper = Bereken(grond, bouw, appartement, inst),
            };
            rijen.Add(rij);
        }

        // Vergelijking met stap 8 van het definitieve budget: wat de verkooplijnen voor deze eenheden voorzagen.
        var def = await _db.BudgetVersie.AsNoTracking().Where(v => v.ProjectId == projectId && v.Status == BudgetVersie.StatusDefinitief)
            .Select(v => new { v.Id }).FirstOrDefaultAsync();
        decimal? tovBudget = null;
        if (def != null)
        {
            var lijnen = await _db.BudgetVerkoopLijn.AsNoTracking().Where(l => l.BudgetVersieId == def.Id && l.UnitId != null).ToListAsync();
            if (lijnen.Count > 0)
            {
                var koppelIds = lijnen.Select(l => l.UnitId!.Value).ToHashSet();
                var budgetSom = lijnen.Sum(l => (l.Grondwaarde ?? 0m) + (l.Bouwwaarde ?? 0m));
                var prijsSom = rijen.Where(r => koppelIds.Contains(r.UnitId)).Sum(r => r.Totaal);
                tovBudget = prijsSom - budgetSom;
            }
        }

        return new VerkoopIndexVm
        {
            ProjectId = projectId, ProjectName = project?.ProjectName ?? "", Rijen = rijen,
            HeeftAppartementen = rijen.Any(r => r.IsAppartement),
            DefinitiefVersieId = def?.Id, TovBudget = tovBudget, Vandaag = DateOnly.FromDateTime(DateTime.Today),
        };
    }

    // ── Wijzigingen ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Eenheden op "Te koop" zetten (optie weg). Verkochte eenheden blijven ongemoeid: die wijzig je via de koper.</summary>
    public async Task<int> ZetTeKoopAsync(int projectId, IEnumerable<int> unitIds)
    {
        var ids = unitIds.Distinct().ToList();
        var units = await _db.Units.Where(u => u.ProjectId == projectId && ids.Contains(u.Id) && u.ClientAccountId == null).ToListAsync();
        foreach (var u in units) { u.IsOption = false; u.OptionUntil = null; u.OptionHolder = null; }
        await _db.SaveChangesAsync();
        return units.Count;
    }

    /// <summary>Eenheden in optie zetten met einddatum, optant en makelaar of herkomst.</summary>
    public async Task<int> ZetOptieAsync(int projectId, IEnumerable<int> unitIds, DateOnly? tot, string? optant, string? via)
    {
        var ids = unitIds.Distinct().ToList();
        var units = await _db.Units.Where(u => u.ProjectId == projectId && ids.Contains(u.Id) && u.ClientAccountId == null).ToListAsync();
        foreach (var u in units)
        {
            u.IsOption = true; u.OptionUntil = tot;
            u.OptionHolder = string.IsNullOrWhiteSpace(optant) ? null : optant.Trim();
            u.SaleVia = string.IsNullOrWhiteSpace(via) ? null : via.Trim();
        }
        await _db.SaveChangesAsync();
        return units.Count;
    }

    /// <summary>Eenheden op de website zetten (<c>OnlineSince</c> = vandaag, enkel als ze nog niet online staan) of er weer afhalen.</summary>
    public async Task<int> ZetOnlineAsync(int projectId, IEnumerable<int> unitIds, bool online)
    {
        var ids = unitIds.Distinct().ToList();
        var units = await _db.Units.Where(u => u.ProjectId == projectId && ids.Contains(u.Id) && u.ClientAccountId == null).ToListAsync();
        var vandaag = DateOnly.FromDateTime(DateTime.Today);
        foreach (var u in units) u.OnlineSince = online ? (u.OnlineSince ?? vandaag) : null;
        await _db.SaveChangesAsync();
        return units.Count;
    }
}

public enum VerkoopRegime { Btw, Registratie, Gemengd }

public class VerkoopInstellingen
{
    public decimal VatPercentage { get; set; }
    public decimal RegistrationPercentage { get; set; }
    public bool MixedVat { get; set; }
    /// <summary>1 = bestaand gebouw, 2 = grond (<c>BOCore.RegistrationType</c>), 0 = niet gekozen.</summary>
    public int RegistrationType { get; set; }
    public decimal FixedCertificateCost { get; set; }
    public decimal MortgageRegistrationCost { get; set; }
    public decimal BaseCertificateCost { get; set; }
    public decimal ParcelCost { get; set; }
    public decimal ConnectionFees { get; set; }
    public decimal SurveyorCost { get; set; }
    public List<VerkoopExtraKost> Extra { get; set; } = new();

    /// <summary>Zelfde regel als het oudere scherm: gemengd bij "verkoop onder btw-stelsel" of type grond; registratie bij bestaand gebouw; anders btw.</summary>
    public VerkoopRegime Regime => MixedVat || RegistrationType == 2 ? VerkoopRegime.Gemengd : RegistrationType == 1 ? VerkoopRegime.Registratie : VerkoopRegime.Btw;
}

public class VerkoopWebsite
{
    public bool SaleVisible { get; set; }
    public bool IsFeatured { get; set; }
    /// <summary>BOCore.SalesDisplayStatus; null = automatisch bepalen.</summary>
    public int? DisplayStatus { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public int? BankAccountId { get; set; }
    public int? BuilderCompanyId { get; set; }
}

public class VerkoopExtraKost
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public decimal Bedrag { get; set; }
    public string? Toelichting { get; set; }
}

/// <summary>Kosten van de notaris voor de verkoopakte van een te bouwen woning, nagerekend met de rekenmodule van de notarissen (voorbeeld Lot 3: grond 121.000 +
/// bouw 446.000 gaf ereloon 3.055,74, btw op diensten 907,15 en 5.611,89 voor alles behalve btw en registratierechten op grond en bouw: komt exact overeen).
/// Ereloon: Tarief van de honoraria der notarissen (KB 16 december 1950, consolidatie 9 februari 2026), art. 17 nr. 81 2°: schaal J, of Jbis als de koper de woning als
/// eigen en enige woning betrekt, op de volledige prijs (grond + bouw), afgerond tot hele euro's. Daarbovenop: administratieve kosten 855 (art. 2 §2), uitgaven aan derden 309,
/// kosten overschrijving (= hypotheekkantoor uit de Verkoopinstellingen), recht op geschriften 100 en registratierecht op bijlagen 100. De btw (21 %) rekent op ereloon,
/// administratieve kosten, uitgaven aan derden en het recht op geschriften (zo komt de website uit); niet op de overschrijving en het registratierecht op bijlagen.
/// Het vaste gedeelte van de schaal en de forfaits worden om de twee jaar op 1 januari geïndexeerd (art. 19): volgende keer 1 januari 2028. De percentages niet.</summary>
public static class NotarisTarief
{
    public const int GeldigVanaf = 2026;
    public const decimal Vacaties = 855m;
    public const decimal UitgavenDerden = 309m;
    public const decimal RechtOpGeschriften = 100m;
    public const decimal RegistratieBijlagen = 100m;
    public const decimal Btw = 21m;
    private const decimal VastJ = 285m, VastJbis = 257m;
    // (bedrag van de schijf, percentage); null = de rest
    private static readonly (decimal? Bedrag, decimal Pct)[] SchaalJ =
        { (7500m, 2.5m), (10000m, 2.5m), (12500m, 1.75m), (15495m, 1.71m), (18595m, 1.14m), (186005m, 0.57m), (249905m, 0.20m), (null, 0.20m) };
    private static readonly (decimal? Bedrag, decimal Pct)[] SchaalJbis =
        { (7500m, 2.5m), (10000m, 2.5m), (12500m, 2.0m), (15495m, 1.5m), (18595m, 0.5m), (186005m, 0.485m), (249905m, 0.3m), (null, 0.2m) };

    /// <summary>Grondslag van het ereloon: de volledige prijs (grond + bouw), in hele euro's.</summary>
    public static decimal Grondslag(decimal grond, decimal bouw, VerkoopRegime regime)
        => Math.Round(grond + bouw, 0, MidpointRounding.AwayFromZero);

    public static NotarisKosten Bereken(decimal grond, decimal bouw, VerkoopRegime regime, bool enigeWoning, decimal overschrijving = 0m)
    {
        var basis = Grondslag(grond, bouw, regime);
        if (basis <= 0m) return new NotarisKosten { Overschrijving = overschrijving, Schaal = enigeWoning ? "Jbis" : "J" };
        var schaal = enigeWoning ? SchaalJbis : SchaalJ;
        var rest = basis; var fee = enigeWoning ? VastJbis : VastJ;
        foreach (var (bedrag, pct) in schaal)
        {
            if (rest <= 0m) break;
            var deel = bedrag.HasValue ? Math.Min(rest, bedrag.Value) : rest;
            fee += deel * pct / 100m; rest -= deel;
        }
        fee = Math.Round(fee, 2, MidpointRounding.AwayFromZero);
        var btw = Math.Round((fee + Vacaties + UitgavenDerden + RechtOpGeschriften) * Btw / 100m, 2, MidpointRounding.AwayFromZero);
        return new NotarisKosten
        {
            Grondslag = basis, Ereloon = fee, Vacaties = Vacaties, Uitgaven = UitgavenDerden, Overschrijving = overschrijving,
            RechtOpGeschriften = RechtOpGeschriften, RegistratieBijlagen = RegistratieBijlagen, Btw = btw, Schaal = enigeWoning ? "Jbis" : "J"
        };
    }
}

public class NotarisKosten
{
    public decimal Grondslag { get; set; }
    public decimal Ereloon { get; set; }
    public decimal Vacaties { get; set; }
    public decimal Uitgaven { get; set; }
    /// <summary>Kosten overschrijving = het hypotheekkantoor uit de Verkoopinstellingen.</summary>
    public decimal Overschrijving { get; set; }
    public decimal RechtOpGeschriften { get; set; }
    public decimal RegistratieBijlagen { get; set; }
    public decimal Btw { get; set; }
    public string Schaal { get; set; } = "J";
    public decimal Totaal => Ereloon + Vacaties + Uitgaven + Overschrijving + RechtOpGeschriften + RegistratieBijlagen + Btw;
}

public record KostPost(string Naam, decimal Bedrag, string? Toelichting, decimal BtwPct = 0m);

public class KoperBerekening
{
    public decimal Grond { get; set; }
    public decimal Bouw { get; set; }
    public decimal Registratierechten { get; set; }
    public decimal Btw { get; set; }
    public decimal KostenKoper { get; set; }
    /// <summary>Btw op de kosten koper (zie <see cref="VerkoopService.Bereken"/>).</summary>
    public decimal KostenBtw { get; set; }
    /// <summary>Ereloon, administratieve kosten en btw van de notaris voor de verkoopakte (<see cref="NotarisTarief"/>).</summary>
    public NotarisKosten Notaris { get; set; } = new();
    public bool EnigeWoning { get; set; }
    public decimal RegistratiePercentage { get; set; }
    public decimal BtwPercentage { get; set; }
    public VerkoopRegime Regime { get; set; }
    public List<KostPost> Posten { get; set; } = new();
    public decimal Vraagprijs => Grond + Bouw;
    public decimal Totaal => Grond + Registratierechten + Bouw + Btw + KostenKoper + KostenBtw + Notaris.Totaal;
}

public class VerkoopRijVm
{
    public int UnitId { get; set; }
    public string Naam { get; set; } = "";
    public string TypeNaam { get; set; } = "";
    public int? TypeId { get; set; }
    public string Verdieping { get; set; } = "";
    public bool IsAppartement { get; set; }
    /// <summary>"Te koop", "Optie" of "Verkocht".</summary>
    public string Status { get; set; } = "Te koop";
    public decimal Grond { get; set; }
    public decimal Bouw { get; set; }
    public decimal Totaal { get; set; }
    public string PreKad { get; set; } = "";
    public decimal? Perceel { get; set; }
    public decimal? Wonen { get; set; }
    public decimal? Tienduizendsten { get; set; }
    public string? KoperNaam { get; set; }
    public DateOnly? CompromisDatum { get; set; }
    public DateOnly? AkteDatum { get; set; }
    public DateOnly? OptionUntil { get; set; }
    public string? OptionHolder { get; set; }
    public string? SaleVia { get; set; }
    public DateOnly? OnlineSince { get; set; }
    public int Interessenten { get; set; }
    public KoperBerekening Koper { get; set; } = new();

    public decimal? PrijsPerM2Perceel => Perceel.GetValueOrDefault() > 0m ? Math.Round(Grond / Perceel!.Value, 0) : null;
    public decimal? PrijsPerM2Wonen => Wonen.GetValueOrDefault() > 0m ? Math.Round(Totaal / Wonen!.Value, 0) : null;
    public int GrondPct => Totaal > 0m ? (int)Math.Round(Grond / Totaal * 100m) : 0;
}

public class VerkoopIndexVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public bool HeeftAppartementen { get; set; }
    public List<VerkoopRijVm> Rijen { get; set; } = new();
    public int? DefinitiefVersieId { get; set; }
    /// <summary>Huidige prijzen min wat de verkooplijnen van het definitieve budget (stap 8) voor dezelfde eenheden voorzagen; null = geen definitief budget of geen gekoppelde lijnen.</summary>
    public decimal? TovBudget { get; set; }
    public DateOnly Vandaag { get; set; }

    public int Aantal(string status) => Rijen.Count(r => r.Status == status);
    public decimal TotaalWaarde => Rijen.Sum(r => r.Totaal);
    public decimal VerkochtWaarde => Rijen.Where(r => r.Status == "Verkocht").Sum(r => r.Totaal);
    public decimal NogTeVerkopen => Rijen.Where(r => r.Status != "Verkocht").Sum(r => r.Totaal);
    public decimal? GemPrijsPerM2
    {
        get
        {
            var m2 = Rijen.Where(r => r.Wonen.GetValueOrDefault() > 0m).Sum(r => r.Wonen!.Value);
            var som = Rijen.Where(r => r.Wonen.GetValueOrDefault() > 0m).Sum(r => r.Totaal);
            return m2 > 0m ? Math.Round(som / m2, 0) : null;
        }
    }
}
