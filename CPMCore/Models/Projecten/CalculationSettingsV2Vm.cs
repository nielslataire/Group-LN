namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 — Nacalculatie, Budget instellen (design-handoff punt 42d). Per categorie een lijst
/// activiteiten met het budget (bewerkbaar) en, als er een definitief budget bestaat, de definitieve
/// waarde ernaast + het verschil. Bewaard wordt enkel wat afwijkt van het definitief budget (zie
/// <c>ProjectenController.CalculationSettingsV2Post</c>).</summary>
public class CalculationSettingsV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    /// <summary>"Naam vN" van het definitief budget, null = er is geen (enkel handmatig budget).</summary>
    public string? DefinitiefLabel { get; set; }
    public bool HeeftDefinitief => DefinitiefLabel != null;
    public List<CalculationSettingsGroupV2> Groepen { get; set; } = new();
    /// <summary>Activiteiten die nog niet in het budget staan (voor "Activiteit toevoegen…").</summary>
    public List<CalculationSettingsOptionV2> Opties { get; set; } = new();

    public decimal TotaalBudget => Groepen.Sum(g => g.Rijen.Sum(r => r.Budget));
}

public class CalculationSettingsGroupV2
{
    public int GroupId { get; set; }
    public string Naam { get; set; } = "";
    public List<CalculationSettingsRowV2> Rijen { get; set; } = new();
}

public class CalculationSettingsRowV2
{
    public int ActivityId { get; set; }
    public int GroupId { get; set; }
    public string Naam { get; set; } = "";
    /// <summary>Id van de bestaande handmatige regel (ProjectBudget), 0 = er is er nog geen.</summary>
    public int ManueelId { get; set; }
    public decimal Budget { get; set; }
    /// <summary>Waarde uit het definitief budget, null = deze activiteit staat er niet in.</summary>
    public decimal? Definitief { get; set; }
}

public class CalculationSettingsOptionV2
{
    public int ActivityId { get; set; }
    public int GroupId { get; set; }
    public string Naam { get; set; } = "";
    public string GroepNaam { get; set; } = "";
}

/// <summary>Eén gepost veld per rij (<c>rows[i].*</c>) van het Budget-instellen-formulier.</summary>
public class CalculationSettingsPostRowV2
{
    public int ActivityId { get; set; }
    public int GroupId { get; set; }
    public int ManueelId { get; set; }
    /// <summary>Invariante tekst ("1234.56") — de JS zet de Nederlandse invoer om bij het versturen.</summary>
    public string? Price { get; set; }
}
