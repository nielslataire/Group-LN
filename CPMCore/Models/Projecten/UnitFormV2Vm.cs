using BOCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 eenheidsformulier (design-handoff 16b "Eenheid bewerken" + 16c "Nieuwe eenheid").
/// 16c's eigen regel: "Nieuw gebruikt exact het formulier van 16b" — daarom één model + één view
/// (Projecten/UnitFormV2.cshtml) voor AddUnit én EditUnit; enkel <see cref="IsNew"/> verschilt.
/// Alles wat het formulier toont wordt in één POST (<c>SaveUnitV2</c>) bewaard — anders dan de legacy
/// pagina, waar afwerkingsopties al bij het klikken via AJAX in de database belandden (onmogelijk voor
/// een nog niet bestaande eenheid, en strijdig met de "niet-opgeslagen wijzigingen"-badge).</summary>
public class UnitFormV2Vm
{
    // ── Gebonden (komt uit het formulier terug) ─────────────────────────────────────────────────
    public UnitBO Unit { get; set; } = new UnitBO();
    public int ProjectId { get; set; }
    public int SelectedGroupType { get; set; }
    public int SelectedType { get; set; }
    /// <summary>Constructieprijzen van een eenheid ZONDER afwerkingen (grondwaarde + deze = verkoopprijs).
    /// Zodra er afwerkingen zijn, horen de regels bij een afwerking en blijft dit leeg — zelfde regel als de
    /// publieke site (ServiceCore.Helpers.UnitPricing).</summary>
    public List<UnitConstructionValueBO> BaseConstructionValues { get; set; } = new();
    public List<UnitFinishingOptionBO> FinishingOptions { get; set; } = new();
    public List<RoomBO> Rooms { get; set; } = new();
    /// <summary>"save" (standaard) of "next" — Opslaan en naar de volgende eenheid / Opslaan en nog een.</summary>
    public string? SaveMode { get; set; }
    public string? ReturnUrl { get; set; }
    /// <summary>Verkoopplan verwijderen zonder een nieuw bestand te kiezen.</summary>
    public bool RemovePlan { get; set; }

    // ── Niet gebonden: bij een ongeldige POST opnieuw opgebouwd (RefillUnitFormV2) ─────────────
    [BindNever]     public string ProjectName { get; set; } = "";
    [BindNever]     public int ProjectLandShare { get; set; }
    [BindNever]     public int LandShareOthers { get; set; }
    [BindNever]     public int ProjectUnitCount { get; set; }
    [BindNever]     public List<UnitGroupTypeBO> GroupTypes { get; set; } = new();
    [BindNever]     public List<UnitTypeBO> Types { get; set; } = new();
    [BindNever]     public List<IdNameBO> AttachableUnits { get; set; } = new();
    [BindNever]     public List<IdNameBO> PaymentGroups { get; set; } = new();
    [BindNever]     public List<UnitExecutionPlanVm> ExecutionPlans { get; set; } = new();
    [BindNever]     public List<UnitFormV2LinkedUnit> LinkedUnits { get; set; } = new();
    [BindNever]     public List<IdNameBO> CopySources { get; set; } = new();
    [BindNever]     public int CopyFromUnitId { get; set; }
    [BindNever]     public string CopyFromName { get; set; } = "";
    [BindNever]     public string StatusLabel { get; set; } = "";
    [BindNever]     public string StatusTone { get; set; } = "is-neutral";
    [BindNever]     public int? NextUnitId { get; set; }
    [BindNever]     public string? NextUnitName { get; set; }
    /// <summary>Som van bouwwaarderegels zonder afwerkingsoptie BIJ een eenheid die wél afwerkingen heeft
    /// (komt in de praktijk niet voor) — het formulier toont die niet, maar verwijdert ze ook nooit stilletjes.</summary>
    [BindNever]     public decimal UnlistedBaseValue { get; set; }
    public bool IsSecondaryUnit => (Unit?.Type?.GroupId ?? 0) is 2 or 3;

    public bool IsNew => Unit == null || Unit.Id == 0;
}

public class UnitFormV2LinkedUnit
{
    public int UnitId { get; set; }
    public string Name { get; set; } = "";
    public string TypeLine { get; set; } = "";
    public decimal Price { get; set; }
}

/// <summary>Eén afwerkingsblok in de view: sleutel voor de gegenereerde veldnamen
/// (<c>FinishingOptions[key].…</c>) + het blok zelf.</summary>
public class UnitFormV2OptionRow
{
    public string Key { get; set; } = "";
    public UnitFinishingOptionBO Option { get; set; } = new UnitFinishingOptionBO();
    public List<IdNameBO> PaymentGroups { get; set; } = new();
}

public class UnitFormV2CvRow
{
    /// <summary>Leeg = een constructieprijs van een eenheid zonder afwerkingen (BaseConstructionValues[key]).</summary>
    public string OptionKey { get; set; } = "";
    public string Key { get; set; } = "";
    public UnitConstructionValueBO Value { get; set; } = new UnitConstructionValueBO();
    public List<IdNameBO> PaymentGroups { get; set; } = new();
}

public class UnitFormV2RoomRow
{
    public string Key { get; set; } = "";
    public RoomBO Room { get; set; } = new RoomBO();
}
