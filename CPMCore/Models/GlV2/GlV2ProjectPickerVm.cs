namespace CPMCore.Models.GlV2;

/// <summary>gl-v2 projectkiezer-modal (design-handoff 7f, "PROJECTKIEZER (POPOVER)"). Draagt de
/// projectlijst plus welke daarvan al vastgezet zijn, zodat <c>GlV2/_ProjectPickerModal.cshtml</c> het
/// speldje kan tonen naast een al-gepind project in de RECENT-lijst (zelfde <c>PinnedProjectIds</c>
/// als de rest van het dashboard, geen los ophaalmechanisme).</summary>
public class GlV2ProjectPickerVm
{
    public List<BOCore.ProjectBO> Projects { get; set; } = new();
    public HashSet<int> PinnedProjectIds { get; set; } = new();
}
