using DALCore.Models;

namespace CPMCore.Models.Traject;

public class TrajectSjabloonListVm
{
    public List<TrajectSjabloon> Sjablonen { get; set; } = new();
    public Dictionary<int, int> AantalTrajectenPerSjabloon { get; set; } = new();
    /// <summary>Extra kolommen van de gl-v2-lijst (67a), per sjabloon-id.</summary>
    public Dictionary<int, TrajectSjabloonInfo> Info { get; set; } = new();
}

/// <summary>Wat de lijst naast fases en mijlpalen toont: automatische mijlpalen, de geschatte duur, waar het gebruikt wordt en wie het laatst wijzigde.</summary>
public class TrajectSjabloonInfo
{
    public int AantalAutomatisch { get; set; }
    /// <summary>Langste streefdatum-keten in dagen (anker + offset), 0 als niets een datum heeft.</summary>
    public int DuurDagen { get; set; }
    public List<string> GebruiktIn { get; set; } = new();
    public DateTime GewijzigdOp { get; set; }
    public string? GewijzigdDoor { get; set; }
}

public class TrajectSjabloonEditVm
{
    public TrajectSjabloon? Sjabloon { get; set; }
    public bool IsNieuw => Sjabloon == null || Sjabloon.Id == 0;
}
