namespace CPMCore.Models.Planning;

/// <summary>Bouwplanning van een project (Projecten › Planning). De pagina tekent lijst en Gantt client-side uit <see cref="StaatJson"/>.</summary>
public class PlanningIndexVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }

    /// <summary>De volledig doorgerekende planning (<c>FacadeCore.PlanningStaat</c>) als JSON.</summary>
    public string StaatJson { get; set; } = "{}";
}
