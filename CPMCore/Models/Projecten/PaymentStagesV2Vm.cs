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

    // ── 68a/68b: tabs, kop en "vraagt actie" ──
    /// <summary>Eenheden die verkocht zijn (een klantenrekening hebben).</summary>
    public int SoldUnits => Units.Count(u => u.IsSold);
    /// <summary>Schijven die volgens de planning bereikt zijn en waarvoor nog eenheden te markeren zijn.</summary>
    public int DueStageCount => Stages.Count(s => s.IsDue && s.MarkableCount > 0);
    /// <summary>Verschillende eenheden die in die schijven nog te markeren zijn.</summary>
    public int DueUnitCount => Stages.Where(s => s.IsDue).SelectMany(s => s.CellsByUnitId.Where(c => c.Value.State == "not-reached").Select(c => c.Key)).Distinct().Count();
    /// <summary>De schijven in kolom-volgorde van die schijven (voor de knop "Alle n markeren").</summary>
    public IEnumerable<int> DueStageIds => Stages.Where(s => s.IsDue && s.MarkableCount > 0).Select(s => s.StageId);
    /// <summary>Kolomkoppen boven de eenheden: per bouwlaag of, zonder bouwlagen, per soort.</summary>
    public List<PaymentUnitHeaderV2> UnitHeaders { get; set; } = new();
}

public class PaymentUnitHeaderV2
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
}

public class PaymentGroupUnitV2
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = "";
    public bool IsSold { get; set; }
    /// <summary>Verkocht en met een verkocht bedrag: enkel dan kan een schijf voor de eenheid gemarkeerd of gefactureerd worden.</summary>
    public bool Eligible { get; set; }
    /// <summary>De groep waaronder de kolom staat (bouwlaag of soort); een scheidingslijn komt waar ze wisselt.</summary>
    public string GroupLabel { get; set; } = "";
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

    // ── 68a: de stand uit de planning, per schijf ──
    /// <summary>De taak of fase in de planning waaraan de schijf hangt ("3.1.2 Opstort gelijkvloers"); leeg = niet gekoppeld.</summary>
    public string? PlanningTask { get; set; }
    public DateOnly? PlanningDate { get; set; }
    /// <summary>De taak is afgevinkt of haar einddatum is voorbij: de schijf is volgens de planning bereikt.</summary>
    public bool IsDue { get; set; }
    /// <summary>Eenheden waarvoor de schijf nog gemarkeerd kan worden.</summary>
    public int MarkableCount => CellsByUnitId.Values.Count(c => c.State == "not-reached");
    public int InvoicedCount => CellsByUnitId.Values.Count(c => c.State == "invoiced");
    public int ReachedCount => CellsByUnitId.Values.Count(c => c.State == "reached");
    /// <summary>Eenheden waarvoor de schijf ooit kan gelden (verkocht en met verkocht bedrag).</summary>
    public int EligibleCount => CellsByUnitId.Values.Count(c => c.State != "not-sold" && c.State != "not-invoicable");
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
