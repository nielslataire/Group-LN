using BOCore;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/DetailCoordinatieV2 (design-handoff punt 18, "Coördinatie —
/// combinatie schijven + regie", 18a en 18b). Hangt als <c>ProjectCoordinatieModel.GlV2</c> naast het
/// legacy model, zodat Views/Projecten/DetailCoordinatie.cshtml ongemoeid blijft. Alle cijfers zijn
/// EXCL. btw, zoals de mockup ("€ 25.812,50 excl. btw").</summary>
public class DetailCoordinatieV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool IsCoordinationProject { get; set; }
    public bool CanWrite { get; set; }

    public CoordinationContractType? ContractType { get; set; }
    public bool ShowSchijven { get; set; }
    public bool ShowRegie { get; set; }
    /// <summary>Beide types: de pagina krijgt tabs (Overzicht/Schijven/Regie/Facturen). Eén type: geen tabs (18b).</summary>
    public bool IsCombined => ShowSchijven && ShowRegie;

    /// <summary>Kan er vanaf deze pagina gefactureerd worden? Zo niet, dan staat hier waarom (geen
    /// coördinatiebedrijf, geen bouwheer, …) — de selectiebalk toont die reden i.p.v. stil niets te doen.</summary>
    public string? InvoiceBlockedReason { get; set; }
    public bool CanInvoice => CanWrite && InvoiceBlockedReason is null;

    // ── Schijven ─────────────────────────────────────────────────────────────────────────────────
    public decimal ContractPrice { get; set; }
    public List<CoordSliceRowV2> Slices { get; set; } = new();
    public decimal SlicesTotalPct => Slices.Sum(s => s.Percentage);
    public decimal SlicesInvoicedAmount => Slices.Where(s => s.IsInvoiced).Sum(s => s.Amount);
    public decimal SlicesOpenAmount => Slices.Where(s => !s.IsInvoiced).Sum(s => s.Amount);
    public int SlicesInvoicedCount => Slices.Count(s => s.IsInvoiced);

    // ── Regie ────────────────────────────────────────────────────────────────────────────────────
    /// <summary>Alle prestaties, nieuwste eerst.</summary>
    public List<CoordRegieRowV2> Regie { get; set; } = new();
    public List<CoordRateRowV2> Rates { get; set; } = new();
    /// <summary>Standaard verplaatsing (heen en terug) in km uit ProjectDistanceKm × 2.</summary>
    public decimal DefaultKm { get; set; }
    public decimal? DistanceOneWayKm { get; set; }
    public decimal KmAllowance { get; set; }
    public IEnumerable<CoordRegieRowV2> RegieOpen => Regie.Where(r => !r.IsInvoiced);
    public IEnumerable<CoordRegieRowV2> RegieInvoiced => Regie.Where(r => r.IsInvoiced);
    public decimal RegieOpenHours => RegieOpen.Sum(r => r.Hours);
    public decimal RegieOpenAmount => RegieOpen.Sum(r => r.Amount);
    public decimal RegieInvoicedAmount => RegieInvoiced.Sum(r => r.Amount);
    public decimal RegieTotalHours => Regie.Sum(r => r.Hours);
    public DateOnly? RegieOldestOpen => RegieOpen.Select(r => (DateOnly?)r.Date).DefaultIfEmpty(null).Min();
    public DateOnly? RegieFirstEver => Regie.Select(r => (DateOnly?)r.Date).DefaultIfEmpty(null).Min();

    // ── Facturen ─────────────────────────────────────────────────────────────────────────────────
    public List<CoordInvoiceRowV2> Invoices { get; set; } = new();
    /// <summary>Facturen van het coördinatiebedrijf op dit project die aan geen enkele schijf of prestatie hangen.</summary>
    public List<CoordInvoiceRowV2> UnlinkedInvoices => Invoices.Where(i => !i.IsLinked).ToList();

    public decimal InvoicedTotal => (ShowSchijven ? SlicesInvoicedAmount : 0m) + (ShowRegie ? RegieInvoicedAmount : 0m);
    public decimal ToInvoiceTotal => (ShowSchijven ? SlicesOpenAmount : 0m) + (ShowRegie ? RegieOpenAmount : 0m);

    // ── Instellingen-zijpaneel (18b) ─────────────────────────────────────────────────────────────
    public int? CoordinationIssuerCompanyId { get; set; }
    public string? IssuerCompanyName { get; set; }
    public string? CoordinationReference { get; set; }
    public string? ProjectAddress { get; set; }
    public List<ProjectIssuerCompanyOptionVM> IssuerCompanies { get; set; } = new();
    public List<IdNameBO> AvailableUsers { get; set; } = new();
}

public class CoordSliceRowV2
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Description { get; set; } = "";
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoicePublicId { get; set; }
    public bool IsInvoiced => InvoiceId.HasValue;
    /// <summary>Wat er in de factuurlijn stond — enkel voor het voorstel in de koppelmodal.</summary>
    public string InvoiceLabel => InvoicePublicId ?? (InvoiceId.HasValue ? "Concept" : "");
}

public class CoordRegieRowV2
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Initials { get; set; } = "?";
    public string? Description { get; set; }
    public decimal Hours { get; set; }
    /// <summary>Effectieve verplaatsing van deze prestatie (0 = geen).</summary>
    public decimal Km { get; set; }
    /// <summary>Afwijkend van de standaardafstand — de gouden stip (18a §4).</summary>
    public bool KmIsCustom { get; set; }
    /// <summary>Open prestatie: het actuele projecttarief. Gefactureerd: het tarief waarmee gefactureerd werd.</summary>
    public decimal Rate { get; set; }
    public bool HasRate { get; set; }
    public decimal HoursAmount => Hours * Rate;
    public decimal KmAmount { get; set; }
    public decimal Amount => HoursAmount + KmAmount;
    public int? InvoiceId { get; set; }
    public string? InvoicePublicId { get; set; }
    public bool IsInvoiced => InvoiceId.HasValue;
}

public class CoordRateRowV2
{
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Initials { get; set; } = "?";
    public decimal Rate { get; set; }
}

public class CoordInvoiceRowV2
{
    public int Id { get; set; }
    public string PublicId { get; set; } = "";
    public DateOnly Date { get; set; }
    public string StatusLabel { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    public decimal AmountExVat { get; set; }
    public int SliceCount { get; set; }
    public int RegieCount { get; set; }
    public decimal RegieHours { get; set; }
    public bool IsLinked => SliceCount > 0 || RegieCount > 0;
    public string Kind => SliceCount > 0 && RegieCount > 0 ? "schijven + regie"
        : SliceCount > 0 ? "schijven"
        : RegieCount > 0 ? "regie"
        : "niet gekoppeld";
}
