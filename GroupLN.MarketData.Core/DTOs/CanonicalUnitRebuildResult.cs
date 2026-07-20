namespace GroupLN.MarketData.Core.DTOs;

public class CanonicalUnitRebuildResult
{
    public int ProjectsProcessed { get; set; }
    public int SourceUnitsScanned { get; set; }
    public int CanonicalUnitsCreated { get; set; }
    public int UnitsMerged { get; set; }
    public int ConflictsDetected { get; set; }
}
