namespace CPMCore.Models.Issues;

/// <summary>Een rij in de gl-v2 puntenlijst (design-handoff 40a). Alles wat de client-side filters/sortering nodig hebben staat erin.</summary>
public class PuntRowV2
{
    public int Id { get; set; }
    public int Nr { get; set; }
    public string Title { get; set; } = "";
    public int Priority { get; set; }
    public bool HasPlan { get; set; }
    public int? UnitId { get; set; }
    public string UnitName { get; set; } = "Algemeen";
    public string Zone { get; set; } = "";
    public int Phase { get; set; }
    public string PhaseLabel { get; set; } = "";
    public int? ContractorId { get; set; }
    public string Contractor { get; set; } = "";
    public int Status { get; set; }
    public string StatusLabel { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    /// <summary>open | afgesloten — voor het standaardfilter "Open (alle behalve afgesloten)".</summary>
    public bool IsClosed { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool Overdue { get; set; }
    public int Sent { get; set; }
    public int PhotoCount { get; set; }
    public string? FirstPhotoUrl { get; set; }
    public string NrLabel => "P-" + Nr.ToString("000");
    /// <summary>Id's van de verslagen waar dit punt in zit, komma-gescheiden (filter "Verslag").</summary>
    public string VerslagIds { get; set; } = "";
}

public class PuntenIndexV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public List<PuntRowV2> Rows { get; set; } = new();
    public List<KeyValuePair<int, string>> Units { get; set; } = new();
    public List<KeyValuePair<int, string>> Contractors { get; set; } = new();
    /// <summary>Alle aannemers van het project (voor "Aannemer toewijzen" in de selectiebalk), ook zonder punten.</summary>
    public List<KeyValuePair<int, string>> ProjectContractors { get; set; } = new();
    public Dictionary<int, int> OpenPerContractor { get; set; } = new();
    public List<KeyValuePair<int, string>> Phases { get; set; } = new();
    public DateOnly Today { get; set; }
    /// <summary>Voor het snel-ingeven-paneel (40c): categorieën, types, recent gebruikte zones.</summary>
    public List<KeyValuePair<int, string>> Categories { get; set; } = new();
    public List<KeyValuePair<int, string>> Types { get; set; } = new();
    public List<string> RecentZones { get; set; } = new();
    /// <summary>Aantal plannen van het project (tab "Op plan").</summary>
    public int PlanCount { get; set; }
    public List<(int Id, string Naam, bool Open)> Verslagen { get; set; } = new();
}

/// <summary>De tabs boven Punten (lijst / op plan) — gewone links tussen de twee pagina's.</summary>
public class PuntTabsV2Vm
{
    public int ProjectId { get; set; }
    /// <summary>lijst | plan</summary>
    public string Active { get; set; } = "lijst";
    public int ListCount { get; set; }
    public int PlanCount { get; set; }
}

/// <summary>Een bericht/gebeurtenis in "Historiek &amp; communicatie" (40o).</summary>
public class PuntHistoriekItemV2
{
    public DateTime Timestamp { get; set; }
    public string Author { get; set; } = "";
    public string Initials { get; set; } = "";
    /// <summary>message | status | system</summary>
    public string Kind { get; set; } = "system";
    public bool IsInternal { get; set; }
    public bool FromContractor { get; set; }
    public string Text { get; set; } = "";
    public string? StatusLabel { get; set; }
    public string? StatusTone { get; set; }
}

public class PuntDetailV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public int Id { get; set; }
    public string NrLabel { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string StatusLabel { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    public int Status { get; set; }
    public int Priority { get; set; }
    public string PriorityLabel { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string UnitName { get; set; } = "Algemeen";
    public string Zone { get; set; } = "";
    public string PhaseLabel { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public bool DuePaused { get; set; }
    public DateTime? OnHoldSince { get; set; }
    public string? OnHoldReason { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string ContractorName { get; set; } = "";
    public string? ContractorContact { get; set; }
    public string? ContractorEmail { get; set; }
    public string? ContractorPhone { get; set; }
    public DateTime? LastPortalOpen { get; set; }
    public List<(int Id, string Url)> Photos { get; set; } = new();
    public bool HasPlan { get; set; }
    public string? PlanName { get; set; }
    public string? PlanUrl { get; set; }
    public int PlanPage { get; set; } = 1;
    public double PlanX { get; set; }
    public double PlanY { get; set; }
    public List<PuntHistoriekItemV2> History { get; set; } = new();
    public int Reminders { get; set; }
    /// <summary>Statussen waar de werfleider het punt vanuit de huidige status naartoe kan zetten.</summary>
    public List<(int Status, string Label)> NextStatuses { get; set; } = new();
}

/// <summary>Goedkeuren &amp; doorsturen (40d): per aannemer de punten die meegestuurd kunnen worden.</summary>
public class PuntSendIssueV2
{
    public int Id { get; set; }
    public string Nr { get; set; } = "";
    public string Title { get; set; } = "";
    public string Where { get; set; } = "";
    public string? Due { get; set; }
    /// <summary>Concept of Ter goedkeuring: wordt bij het versturen goedgekeurd.</summary>
    public bool IsNew { get; set; }
}

public class PuntSendGroupV2
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public List<PuntSendIssueV2> Issues { get; set; } = new();
}

public class PuntenSendV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public int PendingApproval { get; set; }
    public int OpenAtContractors { get; set; }
    public int WithoutContractor { get; set; }
    /// <summary>Gevuld als de pagina vanuit de selectiebalk geopend werd: enkel deze punten.</summary>
    public List<int> OnlyIds { get; set; } = new();
    public List<PuntSendGroupV2> Groups { get; set; } = new();
}

/// <summary>Een plan (PDF) van een eenheid of een algemeen plan van het project. PlanId 0 = het hoofdplan van de eenheid.</summary>
public class PuntPlanV2
{
    public string Key { get; set; } = "";
    public int? UnitId { get; set; }
    public string UnitName { get; set; } = "Algemeen";
    public int PlanId { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
}

public class PuntPinV2
{
    public int Id { get; set; }
    public string Nr { get; set; } = "";
    public string Title { get; set; } = "";
    public string PlanKey { get; set; } = "";
    public int Page { get; set; } = 1;
    public double X { get; set; }
    public double Y { get; set; }
    /// <summary>Canonieke status (8 concept … 12 in de wacht), voor de pinkleur.</summary>
    public int Status { get; set; }
    public string StatusLabel { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    public bool Closed { get; set; }
    public string Unit { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Contractor { get; set; } = "";
    public string? Due { get; set; }
    public List<string> Photos { get; set; } = new();
    public string Url { get; set; } = "";
}

public class PuntenPlanV2Vm
{
    public PuntenIndexV2Vm Index { get; set; } = new();
    public List<PuntPlanV2> Plans { get; set; } = new();
    public List<PuntPinV2> Pins { get; set; } = new();
    public string? SelectedKey { get; set; }
    public int? SelectedIssueId { get; set; }
}
