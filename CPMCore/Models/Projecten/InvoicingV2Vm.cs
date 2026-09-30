namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/InvoicingV2 (design-handoff 20a, "Facturatie — alles wat nu
/// factureerbaar is, per klantaccount"). Combineert wat <c>ProjectenController.Invoicing</c> al ophaalt
/// (schijven via <c>GetProjectInvoicableUnits</c>, WO-restbedragen via
/// <c>GetProjectInvoicableChangeOrders</c>) in één aanvinkbare tabel per klantenaccount. Een WO die nog
/// op een handtekening wacht staat, net als in 20a, gewoon TUSSEN de andere posten van datzelfde account
/// (<see cref="InvoicingPostRowV2.IsBlocked"/> — niet aanvinkbaar, rode achtergrond, link naar scherm
/// 21b) i.p.v. in een apart tabblad. De post-bedragen staan hier altijd voor 100 % van het account —
/// <see cref="InvoicingOwnerShareV2"/> is enkel informatief; de eigenaar-splitsing zelf gebeurt bij het
/// aanmaken van de facturen (<c>BuildStageInvoiceDraft</c>/<c>BuildChangeOrderInvoiceDraft</c>), niet hier.
///
/// Bewust NIET meegenomen (buiten scope van deze ronde — zie financieel_gl_v2_redesign-memo):
/// de "NOG NIET BEREIKT"-tab (geen "schijf nog niet bereikt"-concept in het schema), de segmentbalk per
/// account (vereist alle schijven incl. al gefactureerde, niet enkel de open) en het facturatieplan
/// "bij laatste schijf" (voorschot/tussentijds/saldo, eigen KPI in het wireframe).</summary>
public class InvoicingV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }

    public List<InvoicingAccountCardV2> Accounts { get; set; } = new();
    public List<InvoicingInvoiceRowV2> InvoicedRows { get; set; } = new();

    public IEnumerable<InvoicingPostRowV2> OpenRows => Accounts.SelectMany(a => a.Rows).Where(r => !r.IsBlocked);
    public IEnumerable<InvoicingPostRowV2> BlockedRows => Accounts.SelectMany(a => a.Rows).Where(r => r.IsBlocked);

    public int OpenPostCount => OpenRows.Count();
    public decimal OpenAmount => OpenRows.Sum(r => r.Amount);
    public int OpenAccountCount => Accounts.Count(a => a.Rows.Any(r => !r.IsBlocked));
    public decimal InvoicedAmount => InvoicedRows.Sum(r => r.AmountExVat);
    public int BlockedCount => BlockedRows.Count();
    public decimal BlockedAmount => BlockedRows.Sum(r => r.Amount);
}

/// <summary>Eén klantenaccount met alles wat er nu voor te factureren staat (open én geblokkeerd, door
/// elkaar zoals in 20a). <see cref="Owners"/> toont het hoofdaccount (100 % min de mede-eigenaars) en
/// elke mede-eigenaar met zijn <c>ClientContacts.CoOwnerPercentage</c> — zelfde aandeel dat
/// <c>MakeInvoices</c>/<c>MakeInvoicesCO</c> gebruiken om de conceptfacturen te splitsen.</summary>
public class InvoicingAccountCardV2
{
    public int ClientAccountId { get; set; }
    public string DisplayName { get; set; } = "";
    /// <summary>"Lot 2 · Woning 6 % btw · betalingsgroep Woning 6 % btw" (20a-kaartkop).</summary>
    public string? Subtitle { get; set; }
    public List<InvoicingOwnerShareV2> Owners { get; set; } = new();
    public List<InvoicingPostRowV2> Rows { get; set; } = new();
    public IEnumerable<InvoicingPostRowV2> OpenRows => Rows.Where(r => !r.IsBlocked);
    public IEnumerable<InvoicingPostRowV2> BlockedRows => Rows.Where(r => r.IsBlocked);
    public decimal OpenTotal => OpenRows.Sum(r => r.Amount);
    public bool HasCoOwners => Owners.Count > 1;
    public bool HasBlocked => Rows.Any(r => r.IsBlocked);
    /// <summary>Omschrijving van de eerste geblokkeerde post, voor de waarschuwingsbalk onderaan de kaart
    /// ("Factuur kan pas weg als WO-006 beslist is").</summary>
    public string? FirstBlockedDescription => BlockedRows.FirstOrDefault()?.Description;
    public bool HasNothingOpen => Rows.Count == 0;

    /// <summary>Verloopbalk (20a): % van de totale schijvenwaarde van dit account dat al gefactureerd,
    /// nog te factureren (Invoicable) resp. nog niet vrijgegeven is (InvoicingPaymentStages.Invoicable =
    /// false — de enige "nog niet bereikt"-indicator die het schema echt heeft, geen mijlpaal-datum).
    /// Blijft op 0/0/0 als het account geen enkele schijf heeft (bv. enkel WO's).</summary>
    public decimal ProgressInvoicedPct { get; set; }
    public decimal ProgressOpenPct { get; set; }
    public decimal ProgressNotReachedPct { get; set; }
    public bool HasProgress => ProgressInvoicedPct + ProgressOpenPct + ProgressNotReachedPct > 0m;

    /// <summary>Eén blokje per schijf (20a: losse segmentjes met witruimte ertussen, niet drie
    /// samengevoegde staven) — in schijf-volgorde, per eenheid na elkaar geplakt.</summary>
    public List<InvoicingProgressSegmentV2> ProgressSegments { get; set; } = new();
}

public class InvoicingProgressSegmentV2
{
    /// <summary>Relatief aandeel binnen de balk (CSS flex-grow) — de %-waarde van de schijf zelf.</summary>
    public decimal Percentage { get; set; }
    /// <summary>"invoiced" (groen) / "open" (goud) / "not-reached" (grijs, Invoicable = false).</summary>
    public string State { get; set; } = "not-reached";
}

public class InvoicingOwnerShareV2
{
    public string DisplayName { get; set; } = "";
    public decimal Percentage { get; set; }
    public bool IsMainOwner { get; set; }
}

/// <summary>Eén post in de kaart: een schijf (<see cref="StageId"/> gevuld), een WO-restbedrag
/// (<see cref="ChangeOrderDetailId"/> gevuld, aanvinkbaar) of diezelfde WO nog geblokkeerd
/// (<see cref="IsBlocked"/>, niet aanvinkbaar — scherm 21b via <see cref="SigningCaseId"/>).</summary>
public class InvoicingPostRowV2
{
    /// <summary>"Schijf", "Meerwerk" of "Minwerk" — badge op de rij.</summary>
    public string Kind { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>"schijf 4 van 11" / "ondertekend 02/06/2026" — kleine tweede lijn onder de omschrijving.</summary>
    public string? SubText { get; set; }
    /// <summary>Kolom MOMENT (20a): wanneer deze post factureerbaar wordt/werd — "na ondertekening" voor
    /// een klare WO, "blokkeert" voor een geblokkeerde. Blijft leeg voor schijven: er is geen
    /// "bereikt op"-datum in het schema (enkel Invoicable, geen mijlpaal-tracking).</summary>
    public string? Moment { get; set; }
    public string? UnitName { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public decimal VatPercentage { get; set; }

    public int? UnitId { get; set; }
    public int? StageId { get; set; }
    public int? ChangeOrderId { get; set; }
    public int? ChangeOrderDetailId { get; set; }
    public bool IsStage => StageId.HasValue;
    /// <summary>De structureel laatste schijf van haar groep (hoogste positie, "schijf N van N") — bij
    /// het aanvinken hiervan trekt de pagina alle nog niet gefactureerde meerwerken/minwerken van
    /// hetzelfde account verplicht mee (eindafrekening, Niels 2026-09-30). Enkel relevant voor schijven.</summary>
    public bool IsLastStage { get; set; }

    /// <summary>WO die nog op een handtekening wacht (ChangeOrder.DateAgreement = null) — niet
    /// aanvinkbaar, scherm 21b geeft hier de 4 opties.</summary>
    public bool IsBlocked { get; set; }
    public int? SigningCaseId { get; set; }
    public string? SigningStatusLabel { get; set; }
    /// <summary>Er loopt al een dossier: scherm 21b (herinneren/opladen/weigeren/annuleren). Anders enkel
    /// een link naar "Ondertekening starten" (SigningAdmin/Start).</summary>
    public bool HasActiveCase => SigningCaseId.HasValue;
}

public class InvoicingInvoiceRowV2
{
    public int InvoiceId { get; set; }
    public string PublicId { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public string ClientName { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    public decimal AmountExVat { get; set; }
    /// <summary>"schijven", "wijzigingsopdrachten" of "gemengd" — uit InvoicesDetails.LineType.</summary>
    public string Kind { get; set; } = "";
}

/// <summary>View-model van <c>Modals/_ModalMakeInvoicesV2</c> (21a/22i): puur berekend voorstel, vóór er
/// iets in de database komt — één rij per partij (hoofdaccount of mede-eigenaar) die effectief een
/// conceptfactuur zou krijgen.</summary>
public class InvoicePreviewV2Vm
{
    public List<InvoicePreviewOwnerV2> Owners { get; set; } = new();
    public List<string> Notices { get; set; } = new();
    public decimal GrandTotal => Owners.Sum(o => o.Total);
}

public class InvoicePreviewOwnerV2
{
    public string OwnerName { get; set; } = "";
    public decimal Percentage { get; set; }
    public List<InvoicePreviewLineV2> Lines { get; set; } = new();
    public decimal Total { get; set; }
}

public class InvoicePreviewLineV2
{
    public string Text { get; set; } = "";
    public decimal Amount { get; set; }
}
