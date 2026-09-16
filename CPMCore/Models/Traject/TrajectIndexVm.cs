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
}
