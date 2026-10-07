using BOCore;
using DALCore.Models;

namespace CPMCore.Models.Traject;

/// <summary>Viewmodel voor de per-project trajectpagina (ProjectTraject/Index).</summary>
public class TrajectIndexVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";

    /// <summary>Null wanneer het project nog geen traject heeft.</summary>
    public Projecttraject? Traject { get; set; }

    public List<ProjecttrajectFase> Fases { get; set; } = new();

    /// <summary>Alle mijlpalen (project + per eenheid) die aan het filter voldoen.</summary>
    public List<Mijlpaal> Mijlpalen { get; set; } = new();

    /// <summary>Mijlpalen op projectniveau (UnitId == null).</summary>
    public List<Mijlpaal> ProjectMijlpalen => Mijlpalen.Where(m => m.UnitId == null).ToList();

    /// <summary>Per-eenheid mijlpalen (UnitId != null).</summary>
    public List<Mijlpaal> EenheidMijlpalen => Mijlpalen.Where(m => m.UnitId != null).ToList();

    /// <summary>Eenheden van het project (voor de matrix + de keuzelijst in de modal).</summary>
    public List<Units> ProjectUnits { get; set; } = new();

    public MijlpaalFilterBO Filter { get; set; } = new();

    /// <summary>Sjablonen die gekozen kunnen worden om een traject aan te maken.</summary>
    public List<TrajectSjabloon> Sjablonen { get; set; } = new();
    public int? VoorgesteldSjabloonId { get; set; }

    // KPI's
    public int AantalMijlpalen => Mijlpalen.Count;
    public int AantalBereikt { get; set; }
    public int AantalAchterstallig { get; set; }
    public int AantalBinnen14Dagen { get; set; }
    public ProjecttrajectFase? HuidigeFase { get; set; }

    /// <summary>Aantal gekoppelde "Punten" (ConstructionIssue, via ProjectTaak.ConstructionIssueId)
    /// per MijlpaalId — maakt zichtbaar dat een mijlpaal aan een werfpunt-opvolging hangt.</summary>
    public Dictionary<int, int> PuntenPerMijlpaal { get; set; } = new();

    /// <summary>Actieve triggers per MijlpaalId — voor de "Acties bij bereiken"-sectie in de
    /// kalender (_Kalender.cshtml). MijlpaalService.Search() include't Triggers niet (andere
    /// callers hebben dat niet nodig), dus apart geladen in de controller.</summary>
    public Dictionary<int, List<MijlpaalTrigger>> TriggersPerMijlpaal { get; set; } = new();

    // ── gl-v2 (design-handoff 30) ───────────────────────────────────────────────────────────────
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }

    /// <summary>Aantal toepasbare wijzigingen uit het sjabloon (nieuw + gewijzigd) — "Sync met sjabloon" toont dit.</summary>
    public int SyncOpenstaand { get; set; }

    /// <summary>Aantal mijlpalen die het sjabloon nog heeft voor een fase maar die nog niet in het traject staan, per faseNaam.</summary>
    public Dictionary<string, int> SyncNieuwPerFaseNaam { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Open dossier(s) dat aan een mijlpaal hangt, per MijlpaalId — voor "Naar dossier" in de melding.</summary>
    public Dictionary<int, ProjectDossier> DossierPerMijlpaal { get; set; } = new();

    /// <summary>Tab die bij het laden open moet staan (tijdlijn|mijlpalen|eenheden|kalender) en de mijlpaal die kort oplicht na opslaan.</summary>
    public string? StartTab { get; set; }
    public int? HighlightMijlpaalId { get; set; }

    /// <summary>Verantwoordelijke gebruiker per UserId → (volledige naam, initialen), voor de avatar naast een mijlpaal.</summary>
    public Dictionary<string, (string Naam, string Initialen)> Gebruikers { get; set; } = new();
}
