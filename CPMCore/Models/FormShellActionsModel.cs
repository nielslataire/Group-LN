namespace CPMCore.Models
{
    /// <summary>
    /// Voor de herbruikbare Opslaan/Annuleren-balk van .gl-form-shell
    /// (Views/Shared/_FormShellActions.cshtml) — dezelfde grote-knoppenstijl
    /// als de rest van de app (bv. Projecten/AddContract), altijd zichtbaar
    /// onderaan de schil.
    /// </summary>
    public class FormShellActionsModel
    {
        public string SubmitLabel { get; set; }
        public string SubmitIcon { get; set; } = "bx-save";
        public string CancelUrl { get; set; }
        public string CancelLabel { get; set; } = "Annuleren";
    }
}
