using BOCore;
using ServiceCore.Helpers;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/ChangeOrderDetailV2 (design-handoff 20d, "Wijzigingsopdracht
/// — verloop bovenaan, regels met interne commissie, facturatieplan met voorschot/termijnen,
/// ondertekening opvolgen"). Eén WO = één ChangeOrder-rij, ook tijdens de offertefase (IsQuote) — zie
/// ChangeOrderStatusHelper voor hoe de status berekend wordt.</summary>
public class ChangeOrderDetailV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int ChangeOrderId { get; set; }
    public bool IsNew { get; set; }
    public bool CanWrite { get; set; }
    public bool IsLocked { get; set; }
    public string? LockedReason { get; set; }

    public ChangeOrderStatus Status { get; set; }
    public string StatusLabel => ChangeOrderStatusHelper.DisplayName(Status);
    public bool IsQuote { get; set; }
    public List<ChangeOrderStepV2> Steps { get; set; } = new();

    // Opdracht
    public int ClientAccountId { get; set; }
    public string ClientName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public int ContractActivityId { get; set; }
    public List<IdNameBO> ContractActivities { get; set; } = new();
    /// <summary>Enkel gevuld voor een nieuwe rij (ClientAccountId == 0): dan toont de pagina een
    /// klantkeuze i.p.v. de vaste "Klant · eenheid"-uitlezing.</summary>
    public List<IdNameBO> ClientAccounts { get; set; } = new();
    public decimal VatKlantPercentage { get; set; }
    public string VatKlantLabel { get; set; } = "";
    public string Description { get; set; } = "";
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
}

public class ChangeOrderStepV2
{
    public string Label { get; set; } = "";
    public string? Hint { get; set; }
    /// <summary>"done" / "current" / "future".</summary>
    public string State { get; set; } = "future";
}

public class ChangeOrderDetailRowV2
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public int MeasurementType { get; set; }
    public int MeasurementUnit { get; set; }
    public int Number { get; set; } = 1;
    public decimal Price { get; set; }
    public decimal Commission { get; set; }
    public decimal VatPercentage { get; set; }
    public bool NeedsReview { get; set; }
    public string? SourceImagePath { get; set; }
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
    public bool InvoiceableByBouwheer { get; set; }
    public string? QuoteSupplierReference { get; set; }
    public decimal? QuoteVatPercentage { get; set; }
    public string? ConditionsText { get; set; }
    public List<ChangeOrderDetailRowV2> Rows { get; set; } = new();
    public List<ChangeOrderTermV2> Terms { get; set; } = new();
    /// <summary>QuoteIntakeV2's "Omzetten naar wijzigingsopdracht" op een nog niet opgeslagen offerte —
    /// bespaart een aparte round-trip (opslaan, dan pas omzetten): de Save-actie doet de in-place
    /// IsQuote-overgang er meteen bij als deze vlag aanstaat.</summary>
    public bool ConvertAfterSave { get; set; }
    /// <summary>"quote" als het formulier van QuoteIntakeV2 (20c) komt — bij een validatiefout gaat de
    /// gebruiker dan terug naar dát scherm i.p.v. naar een leeg 20d.</summary>
    public string? ReturnTo { get; set; }
}
