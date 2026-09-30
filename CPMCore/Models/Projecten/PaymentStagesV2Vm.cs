namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 view-model voor Projecten/PaymentStagesV2 (design-handoff 21g, "Betalingsschijven —
/// schijven × eenheden, btw van de groep, eindafrekening gemarkeerd"). Eén kaart per betalingsgroep,
/// een matrix-tabel schijf × eenheid — zie <see cref="PaymentStageCellV2"/> voor de status per cel.</summary>
public class PaymentStagesV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public List<PaymentGroupCardV2> Groups { get; set; } = new();
}

public class PaymentGroupCardV2
{
    public int GroupId { get; set; }
    public string Name { get; set; } = "";
    public decimal VatPercentage { get; set; }
    public List<PaymentGroupUnitV2> Units { get; set; } = new();
    public List<PaymentStageRowV2> Stages { get; set; } = new();
    public decimal TotalPercentage => Stages.Sum(s => s.Percentage);
    /// <summary>Structurele positie van de laatste schijf ("schijf 11") — de eindafrekening.</summary>
    public int LastStageNumber => Stages.Count;
}

public class PaymentGroupUnitV2
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public bool IsSold { get; set; }
}

public class PaymentStageRowV2
{
    public int StageId { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public decimal Percentage { get; set; }
    public bool IsLast { get; set; }
    /// <summary>Sleutel = UnitId.</summary>
    public Dictionary<int, PaymentStageCellV2> CellsByUnitId { get; set; } = new();
    /// <summary>Minstens één cel staat op "reached" (klaar om aan te duiden of net aangeduid) —
    /// stuurt of de rij een "Bereikt aanduiden"-link toont.</summary>
    public bool HasActionableUnits { get; set; }
    /// <summary>De groep-brede InvoicingPaymentStages.Invoicable-vlag staat aan — elke eenheid van deze
    /// schijf toont dan "reached", ook zonder eigen UnitPaymentStageReached-rij. Zo'n cel kan niet per
    /// eenheid teruggezet worden (dat zou meteen weer "reached" tonen door de groep-vlag) — enkel een
    /// individuele UnitPaymentStageReached-rij (stage.Invoicable == false) mag je terug uitzetten.</summary>
    public bool IsGroupForced { get; set; }
}

public class PaymentStageCellV2
{
    /// <summary>"invoiced" (groen) / "reached" (goud, nog te factureren) / "not-reached" (grijs) /
    /// "not-sold" (streepje).</summary>
    public string State { get; set; } = "not-reached";
}

/// <summary>Modals/_ModalMarkStageReachedV2 — "Schijf bereikt aanduiden" (21h).</summary>
public class MarkStageReachedV2Vm
{
    public int ProjectId { get; set; }
    public int StageId { get; set; }
    public int StageNumber { get; set; }
    public string StageName { get; set; } = "";
    public decimal StagePercentage { get; set; }
    public List<MarkStageReachedUnitV2> Units { get; set; } = new();
}

public class MarkStageReachedUnitV2
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public string? OwnerName { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Al aangevinkt (aanvinkbaar) versus al gefactureerd/niet verkocht (getoond, niet aanvinkbaar).</summary>
    public bool IsActionable { get; set; }
    public string? DisabledReason { get; set; }
}

/// <summary>Modals/_ModalPickProofPhotoV2 — 21h's "BEWIJS": "Werffoto's kiezen uit Media", read-only
/// selectie uit de project-Media (foto's, geen video's).</summary>
public class PickProofPhotoV2Vm
{
    public int ProjectId { get; set; }
    public List<PickProofPhotoItemV2> Photos { get; set; } = new();
}

public class PickProofPhotoItemV2
{
    public int MediaId { get; set; }
    public string ThumbUrl { get; set; } = "";
    public string Title { get; set; } = "";
}
