using BOCore;
using ServiceCore.Helpers;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/ChangeOrderDetailV2 — de wijzigingsopdracht als scherm per stap
/// (design-handoff punt 28, 28a–28i; de opvolger van 20d): verloop bovenaan, regels met interne
/// commissie, facturatieplan met voorschot/termijnen, ondertekening en facturen opvolgen. Eén WO = één
/// ChangeOrder-rij, ook tijdens de offertefase (IsQuote, dan toont QuoteIntakeV2 ze) — zie
/// ChangeOrderStatusHelper voor hoe de status berekend wordt.</summary>
public class ChangeOrderDetailV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int ChangeOrderId { get; set; }
    public bool IsNew { get; set; }
    public bool CanWrite { get; set; }
    /// <summary>"05/10/2026 14:32" — laatste keer opgeslagen (leeg als nog nooit via dit scherm).</summary>
    public string? SavedAtText { get; set; }
    /// <summary>Offerte: verwijderrecht volstaat; wijzigingsopdracht: enkel admin.</summary>
    public bool CanDelete { get; set; }
    public bool IsLocked { get; set; }
    public string? LockedReason { get; set; }

    public ChangeOrderStatus Status { get; set; }
    public string StatusLabel => ChangeOrderStatusHelper.DisplayName(Status);
    public bool IsQuote { get; set; }

    // Opdracht
    public int ClientAccountId { get; set; }
    public string ClientName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public int ContractActivityId { get; set; }
    public List<IdNameBO> ContractActivities { get; set; } = new();
    /// <summary>Enkel gevuld voor een nieuwe rij (ClientAccountId == 0): dan toont de pagina een
    /// klantkeuze i.p.v. de vaste "Klant · eenheid"-uitlezing.</summary>
    public List<IdNameBO> ClientAccounts { get; set; } = new();
    /// <summary>Zelfde keuzelijst, met per klant de eenheid en de btw uit haar betalingsgroep (28b "leeg
    /// beginnen": de btw van de regels volgt de gekozen eenheid).</summary>
    public List<ConvertClientOptionV2> ClientOptions { get; set; } = new();
    public decimal VatKlantPercentage { get; set; }
    /// <summary>Btw-code (Vattype) van de betalingsgroep van de klant — de standaard voor nieuwe regels.</summary>
    public int? VatKlantTypeId { get; set; }
    /// <summary>Btw-codes van het facturatiebedrijf (Vattype) waaruit per regel gekozen wordt.</summary>
    public List<VatTypeOptionV2> VatTypes { get; set; } = new();
    public string VatKlantLabel { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Subject { get; set; }
    public bool InvoiceableByBouwheer { get; set; }

    // Offerte-metadata (enkel getoond zolang/als IsQuote, of als "Bron" na Omzetten)
    public string? QuoteSupplierReference { get; set; }
    public decimal? QuoteVatPercentage { get; set; }
    public DateOnly QuoteDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public DateTime? QuoteConvertedAt { get; set; }

    // Regels
    public List<ChangeOrderDetailRowV2> Rows { get; set; } = new();
    public bool HasRowsNeedingReview => Rows.Any(r => r.NeedsReview);

    // Facturatieplan
    public string FacturatieplanPreset { get; set; } = "laatste-schijf"; // laatste-schijf | voorschot-saldo | na-ondertekening | eigen
    public List<ChangeOrderTermV2> Terms { get; set; } = new();
    public List<IdNameBO> Stages { get; set; } = new(); // voor de "bij schijf"-trigger-dropdown
    public string ConditionsText { get; set; } = "";

    // Ondertekening (rechterkolom) — rechtstreeks de signing-status, geen eigen kopie
    public bool SigningEnabled { get; set; }
    public bool CanStartSigning { get; set; }
    public FacadeCore.Signing.CaseStatusView? ActiveSigningCase { get; set; }
    public FacadeCore.Signing.CaseStatusView? CompletedSigningCase { get; set; }

    public List<ChangeOrderHistoryItemV2> History { get; set; } = new();

    // ── Scherm per stap (design-handoff punt 28) ─────────────────────────────────────────────────────
    // Eén scherm, de fase bepaalt wat het toont: melding, vergrendeling, facturatieplan, ondertekening,
    // facturen en knoppen. Alles hieronder wordt in de controller afgeleid (BuildScreenState) — de view
    // rekent zelf niets uit.

    /// <summary>"WO-006" / "OF-006".</summary>
    public string Number { get; set; } = "";
    /// <summary>Offerte: offerte | offerte-verzonden | offerte-omgezet. Wijzigingsopdracht: concept |
    /// ingetrokken | verlopen | verzonden | wacht | ondertekend | factureerbaar | gefactureerd | betaald |
    /// geweigerd | telaat.</summary>
    public string Phase { get; set; } = "concept";
    /// <summary>Regels, opdracht en facturatieplan zijn bewerkbaar (concept/ingetrokken + schrijfrecht).</summary>
    public bool IsEditable { get; set; }
    /// <summary>"Vergrendeld" / "Afgesloten" / "Archief" — het slotje op de kaartkoppen.</summary>
    public string? LockText { get; set; }
    public string StatusPillLabel { get; set; } = "";
    /// <summary>De .gl-v2-badge-toon: is-neutral | is-positive | is-attention | is-blocked | is-solid.</summary>
    public string StatusPillTone { get; set; } = "is-neutral";
    public bool IsMinwerk { get; set; }
    public string StepLabel { get; set; } = "";
    public CPMCore.Models.GlV2.GlV2StepsVm Stappenplan { get; set; } = new();
    /// <summary>info | success | warning | danger (de .gl-v2-notice-types).</summary>
    public string NoticeType { get; set; } = "info";
    public string NoticeTitle { get; set; } = "";
    public string NoticeText { get; set; } = "";
    public string RegelsSub { get; set; } = "";
    public string PlanSub { get; set; } = "";
    public string FooterText { get; set; } = "";
    public decimal DefaultCommission { get; set; }

    // Offerte van de leverancier (28a) / Bron
    public bool HasQuoteSource { get; set; }
    public string? SupplierName { get; set; }
    public decimal CostTotal { get; set; }
    public string? QuoteSourceFileName { get; set; }
    public string? QuoteSourceUrl { get; set; }
    public bool QuoteSourceIsImage { get; set; }
    public int? SourceChangeOrderId { get; set; }
    public string? SourceChangeOrderLabel { get; set; }
    /// <summary>1 = kopie, 2 = nieuwe versie.</summary>
    public byte? SourceKind { get; set; }
    /// <summary>Gevuld als er al een nieuwere versie van deze WO bestaat (deze is dan vervangen).</summary>
    public int? ReplacedByChangeOrderId { get; set; }

    // Ondertekening (zijkolom)
    public int? SigningCaseId { get; set; }
    public string SigningHeadText { get; set; } = "";
    /// <summary>is-grijs | is-goud | is-groen | is-rood.</summary>
    public string SigningHeadTone { get; set; } = "is-grijs";
    public List<ChangeOrderSignerV2> Signers { get; set; } = new();
    public List<KeyValuePair<string, string>> SigningMeta { get; set; } = new();
    public DateOnly? DateSendToClient { get; set; }
    public DateOnly? DateAgreement { get; set; }

    // Facturen (zijkolom)
    public List<ChangeOrderInvoiceV2> Invoices { get; set; } = new();
    /// <summary>De eerstvolgende termijn die nu gefactureerd mag worden (28e "Voorschot factureren").</summary>
    public ChangeOrderTermV2? NextInvoiceableTerm { get; set; }
    /// <summary>De oudste nog niet betaalde factuur (28f "Factuur openen", 28i).</summary>
    public ChangeOrderInvoiceV2? OpenInvoice { get; set; }

    /// <summary>De verzendmodal meteen openen (na "Verzenden naar klant"/"Verzenden per mail" of 21c
    /// "Aanmaken en verzenden"): 21d voor een WO, de offertemail voor een offerte.</summary>
    public bool OpenSendModal { get; set; }
    /// <summary>De 21c-omzetmodal meteen openen (na "Omzetten naar wijzigingsopdracht" op een offerte).</summary>
    public bool OpenConvertModal { get; set; }

    // Offerte aan de klant (IsQuote): de tussenstap vóór de wijzigingsopdracht.
    /// <summary>De WO die uit deze offerte gemaakt is (SourceKind=3) — de offerte is dan "Omgezet".</summary>
    public int? ConvertedToChangeOrderId { get; set; }
    /// <summary>Wie de offertemail krijgt (voorstel: klantenaccount + mede-eigenaars) — kaart "Verzending".</summary>
    public List<ChangeOrderSignerV2> Recipients { get; set; } = new();
}

public class ChangeOrderSignerV2
{
    public int PartyId { get; set; }
    public string Name { get; set; } = "";
    public string Initials { get; set; } = "";
    public string StatusText { get; set; } = "";
    /// <summary>is-grijs | is-goud | is-groen | is-rood.</summary>
    public string Tone { get; set; } = "is-grijs";
    public bool CanRemind { get; set; }
}

public class ChangeOrderInvoiceV2
{
    public int InvoiceId { get; set; }
    public string Number { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string StatusLabel { get; set; } = "";
    /// <summary>De .gl-v2-badge-toon van de statuschip (is-neutral/is-positive/is-solid/is-blocked).</summary>
    public string StatusTone { get; set; } = "is-positive";
    public bool IsPaid { get; set; }
    public bool IsOverdue { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class ChangeOrderDetailRowV2
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public int MeasurementType { get; set; } = (int)BOCore.MeasurementType.Vermoedelijk;
    public int MeasurementUnit { get; set; }
    public int Number { get; set; } = 1;
    public decimal Price { get; set; }
    public decimal Commission { get; set; }
    public decimal VatPercentage { get; set; }
    /// <summary>Gekozen btw-code (Vattype.Id, migratie 073); VatPercentage is het daaruit afgeleide percentage.</summary>
    public int? VatTypeId { get; set; }
    public bool NeedsReview { get; set; }
    public string? SourceImagePath { get; set; }
    /// <summary>Enkel weergave (nooit vertrouwd bij het posten): een uit de offerte overgenomen WO-regel —
    /// prijs, commissie en btw liggen vast, aantal en omschrijving niet (ChangeOrderDetail.SourceDetailId).</summary>
    public bool PriceLocked { get; set; }
}

public class ChangeOrderTermV2
{
    public int Id { get; set; }
    /// <summary>1=Voorschot, 2=Tussentijds, 3=Saldo.</summary>
    public byte Kind { get; set; }
    public string KindLabel => Kind switch { 1 => "Voorschot", 3 => "Saldo", _ => "Tussentijds" };
    public decimal? Percentage { get; set; }
    public decimal? FixedAmount { get; set; }
    /// <summary>1=NaOndertekening, 2=BijSchijf, 3=Manueel.</summary>
    public byte TriggerType { get; set; }
    public int? TriggerStageId { get; set; }
    public string TriggerLabel { get; set; } = "";
    public bool IsDeletable => Kind != 3;
    public decimal AmountExVat { get; set; }
    public bool IsInvoiced { get; set; }

    // Enkel-lezen-weergave (design-handoff 28c–28i: facturatieplan met STATUS-kolom)
    public bool IsTriggered { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string StatusLabel { get; set; } = "";
    /// <summary>De .gl-v2-badge-toon van de statuschip.</summary>
    public string StatusTone { get; set; } = "is-neutral";
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
}

public class ChangeOrderHistoryItemV2
{
    public DateTime? When { get; set; }
    public string Label { get; set; } = "";
    public string? Actor { get; set; }
}

/// <summary>Post-body van ChangeOrderDetailV2Save — model binding matcht de name="rows[i]."/"terms[i]."
/// prefixen uit _ChangeOrderDetailRowV2.cshtml/_ChangeOrderTermRowV2.cshtml.</summary>
public class ChangeOrderDetailV2SaveModel
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    public int ClientAccountId { get; set; }
    public int ContractActivityId { get; set; }
    public string Description { get; set; } = "";
    public string? Subject { get; set; }
    public bool InvoiceableByBouwheer { get; set; }
    public string? QuoteSupplierReference { get; set; }
    public decimal? QuoteVatPercentage { get; set; }
    public string? ConditionsText { get; set; }
    public List<ChangeOrderDetailRowV2> Rows { get; set; } = new();
    public List<ChangeOrderTermV2> Terms { get; set; } = new();
    /// <summary>Enkel voor een NIEUWE rij: offerte aan de klant (true) of rechtstreeks een
    /// wijzigingsopdracht (false). Een bestaande rij verandert nooit van soort via Opslaan.</summary>
    public bool IsQuote { get; set; }
    /// <summary>"Geldig tot" — op de offerte én op de wijzigingsopdracht (staat op het document).</summary>
    public DateOnly? ExpirationDate { get; set; }
    /// <summary>"quote" als het formulier van QuoteIntakeV2 (20c) komt — bij een validatiefout gaat de
    /// gebruiker dan terug naar dát scherm i.p.v. naar een leeg 20d.</summary>
    public string? ReturnTo { get; set; }
    /// <summary>Origineel offertebestand uit 20c (migratie 066) — enkel overschreven als er een nieuw
    /// bestand geladen werd, een lege waarde laat het bestaande staan.</summary>
    public string? QuoteSourcePath { get; set; }
    public string? QuoteSourceFileName { get; set; }
    /// <summary>"send" = na opslaan de verzendmodal openen (21d voor een WO, de offertemail voor een
    /// offerte); "convert" = na opslaan de 21c-omzetmodal openen; leeg = terug naar het scherm.</summary>
    public string? AfterSave { get; set; }
}

/// <summary>Post-body van ChangeOrderConvertV2 — 21c "Omzetten naar wijzigingsopdracht": maakt uit een
/// offerte aan de klant een nieuwe WO. Klant, regels en prijzen komen uit de offerte en liggen vast; hier
/// kies je enkel wat bij de WO hoort.</summary>
public class ChangeOrderConvertV2Model
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    /// <summary>laatste-schijf | voorschot-saldo | eigen.</summary>
    public string? ConvertPlan { get; set; }
    public string? ConvertDescription { get; set; }
    public string? ConvertConditions { get; set; }
    /// <summary>"open" (Aanmaken en openen) of "send" (Aanmaken en verzenden → 21d).</summary>
    public string? AfterSave { get; set; }
}

/// <summary>Inhoud van de 21c-modal (Modals/_ModalConvertQuoteV2) — AJAX-geladen vanuit de lijst (20b)
/// en vanuit het offertescherm. Alle bedragen komen kant-en-klaar van de server: de offerte is op dat
/// moment bewaard en haar prijzen veranderen bij het omzetten niet meer.</summary>
public class ConvertQuoteModalV2Vm
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    public string Subtitle { get; set; } = "";
    public string ClientLabel { get; set; } = "";
    public string VatLabel { get; set; } = "";
    public decimal VatPercentage { get; set; }
    public decimal CostTotal { get; set; }
    public decimal CommissionTotal { get; set; }
    public decimal ExclTotal { get; set; }
    public decimal InclTotal { get; set; }
    public string Description { get; set; } = "";
    public string Conditions { get; set; } = "";
    /// <summary>Waarom er nog niet omgezet kan worden (geen regels, al omgezet, …).</summary>
    public string? Problem { get; set; }
}

/// <summary>Inhoud van de offertemail-modal (Modals/_ModalSendQuoteV2): de offerte als PDF per mail naar
/// de eigenaars, met een vrij bericht. Geen handtekening — het akkoord volgt op de wijzigingsopdracht.</summary>
public class SendQuoteModalV2Vm
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    public string Number { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string? Problem { get; set; }
    public List<CPMCore.Models.Signing.SigningStartPartyVm> Parties { get; set; } = new();
    public string Subject { get; set; } = "";
    /// <summary>Wie de offerte verstuurt: "me" (de ingelogde gebruiker, standaard) of "lead" (de projectleider).</summary>
    public List<QuoteSenderV2> Senders { get; set; } = new();
    public string? PdfUrl { get; set; }
    public string? PdfFileName { get; set; }
    public string? TestRecipient { get; set; }
}

/// <summary>Post-body van ChangeOrderQuoteSendV2 (offertemail "Verzenden").</summary>
public class ChangeOrderQuoteSendV2Model
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    public List<CPMCore.Models.Signing.SigningStartPartyVm> Parties { get; set; } = new();
    public string? Subject { get; set; }
    public string? Message { get; set; }
    /// <summary>"me" of "lead" — zie SendQuoteModalV2Vm.Senders.</summary>
    public string? Sender { get; set; }
}

public class QuoteSenderV2
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Email { get; set; } = "";
}

public class VatTypeOptionV2
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public decimal Percentage { get; set; }
    /// <summary>Gesloten veld: "6 % · CODE" (compact).</summary>
    public string ShortDisplay => $"{Percentage:0.##} % · {Code}";
    /// <summary>Open lijst: "6 % - omschrijving", zonder code.</summary>
    public string LongDisplay => string.IsNullOrWhiteSpace(Description) ? $"{Percentage:0.##} %" : $"{Percentage:0.##} % - {Description}";
}

public class ConvertClientOptionV2
{
    public int Id { get; set; }
    public string Display { get; set; } = "";
    public string? UnitName { get; set; }
    public decimal VatPercentage { get; set; }
    public int? VatTypeId { get; set; }
    public string VatLabel { get; set; } = "";
    public string? OwnersHint { get; set; }
}

/// <summary>Inhoud van de kopie-modal (Modals/_ModalCopyChangeOrderV2, design-handoff 29b "Kopie"):
/// welke WO, voor welke eenheid.</summary>
public class CopySourceV2
{
    public int Id { get; set; }
    public string Display { get; set; } = "";
    public bool IsQuote { get; set; }
}

public class CopyChangeOrderModalV2Vm
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    /// <summary>Offertes én wijzigingsopdrachten die gekopieerd kunnen worden.</summary>
    public List<CopySourceV2> Sources { get; set; } = new();
    public List<ConvertClientOptionV2> Clients { get; set; } = new();
}

/// <summary>Inhoud van de 21d-modal "Verzenden ter ondertekening" (Modals/_ModalSendChangeOrderV2) —
/// vanuit het opmaakscherm (28a/28b) en vanuit 21c "Aanmaken en verzenden". Ontvangers, regel en
/// vervaldatum zijn het voorstel van de ondertekenmodule (zelfde bron als SigningAdmin/Start).</summary>
public class SendChangeOrderModalV2Vm
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    public string Number { get; set; } = "";
    public string Subtitle { get; set; } = "";
    /// <summary>Elektronisch ondertekenen staat aan én de gebruiker mag het starten; anders blijft enkel
    /// het kanaal "Alleen PDF" over.</summary>
    public bool CanSign { get; set; }
    public string? CannotSignReason { get; set; }
    /// <summary>Waarom er nog niet verzonden kan worden (geen regels, "controleer"-vlag, plan ≠ 100 %).</summary>
    public string? Problem { get; set; }
    public List<CPMCore.Models.Signing.SigningStartPartyVm> Parties { get; set; } = new();
    public int SigningRule { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public int? ReminderAfterDays { get; set; }
    public string? PreviewUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? TestRecipient { get; set; }
}

/// <summary>Post-body van ChangeOrderSendV2 (21d "Verzenden").</summary>
public class ChangeOrderSendV2Model
{
    public int ProjectId { get; set; }
    public int ChangeOrderId { get; set; }
    /// <summary>"sign" = online ondertekenen (persoonlijke link per e-mail); "pdf" = enkel de PDF: de
    /// wijzigingsopdracht wordt als verzonden gemarkeerd, het akkoord registreer je later zelf.</summary>
    public string? Channel { get; set; }
    public int SigningRule { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public List<CPMCore.Models.Signing.SigningStartPartyVm> Parties { get; set; } = new();
    /// <summary>Vrij bericht voor de uitnodigingsmail (enkel bij "sign"; SigningCase.InvitationMessage, migratie 068).</summary>
    public string? InvitationMessage { get; set; }
}
