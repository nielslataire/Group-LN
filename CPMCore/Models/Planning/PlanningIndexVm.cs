namespace CPMCore.Models.Planning;

/// <summary>Bouwplanning van een project (Projecten › Planning). De pagina tekent lijst en Gantt client-side uit <see cref="StaatJson"/>.</summary>
public class PlanningIndexVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string? ProjectCode { get; set; }
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }

    /// <summary>Schrijfrecht op het Traject: nodig om een taak of fase aan een trajectmijlpaal te koppelen.</summary>
    public bool CanLinkTraject { get; set; }

    /// <summary>Recht om planningssjablonen te maken (Instellingen) en te verwijderen; toepassen kan met het gewone planningsrecht.</summary>
    public bool CanSaveTemplate { get; set; }
    public bool CanDeleteTemplate { get; set; }

    /// <summary>De volledig doorgerekende planning (<c>FacadeCore.PlanningStaat</c>) als JSON.</summary>
    public string StaatJson { get; set; } = "{}";
}
