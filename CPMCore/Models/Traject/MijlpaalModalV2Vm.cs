using DALCore.Models;

namespace CPMCore.Models.Traject;

/// <summary>Viewmodel voor het gl-v2 mijlpaalformulier (design-handoff 30e) — één formulier voor toevoegen én bewerken.</summary>
public class MijlpaalModalV2Vm
{
    public int ProjectId { get; set; }

    /// <summary>Null = nieuwe mijlpaal.</summary>
    public Mijlpaal? Bestaand { get; set; }
    public bool IsNieuw => Bestaand == null;

    /// <summary>Voorgeselecteerde fase (bij "+ Mijlpaal in deze fase") of de fase van de bestaande mijlpaal.</summary>
    public int? FaseId { get; set; }
    public int? UnitId { get; set; }

    public List<ProjecttrajectFase> Fases { get; set; } = new();
    public List<Units> Units { get; set; } = new();

    /// <summary>Alle mijlpalen van het traject (projectniveau): voor "Plaats in de fase" en het relatieve anker.</summary>
    public List<Mijlpaal> Mijlpalen { get; set; } = new();

    public int? ActieveFaseId { get; set; }
}
