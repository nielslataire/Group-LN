namespace CPMCore.Models.Traject;

/// <summary>Parameters voor de herbruikbare "Maak taak"-quickadd-modal (dossier-/mijlpaal-/punt-detail).</summary>
public class TaakQuickAddVm
{
    public string ModalId { get; set; } = "modalTaakQuickAdd";
    public int? ProjectId { get; set; }
    public int? UnitId { get; set; }
    public int? MijlpaalId { get; set; }
    public int? ProjectDossierId { get; set; }
    public int? ConstructionIssueId { get; set; }
    public string? DefaultTitel { get; set; }
}
