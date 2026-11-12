namespace FacadeCore;

/// <summary>Keuzes uit de afdrukmodal van de puntenlijst (design-handoff 40u).</summary>
public class ConstructionIssuePrintOptions
{
    /// <summary>De punten die de gebruiker koos (huidige selectie, alle open of aangevinkt); de server vertrouwt enkel op deze ids binnen het project.</summary>
    public List<int> IssueIds { get; set; } = new();
    /// <summary>"lijst" (A4 liggend, tabel), "aannemer" (A4 staand, een aannemer per pagina) of "rondgang" (A4 staand, met afvinkvakjes).</summary>
    public string Type { get; set; } = "lijst";
    /// <summary>"geen", "aannemer", "eenheid" of "fase". Bij Type "aannemer" staat het vast op aannemer.</summary>
    public string Group { get; set; } = "geen";
    /// <summary>"deadline" (oudste eerst), "nr" (hoogste nummer eerst), "eenheid" of "status".</summary>
    public string Sort { get; set; } = "deadline";
    public bool Photos { get; set; } = true;
    public bool Plans { get; set; } = true;
    public bool History { get; set; }
    public bool Closed { get; set; }
    /// <summary>Interne notities (historiek die als intern is gemarkeerd) mee op papier.</summary>
    public bool Internal { get; set; }
}
