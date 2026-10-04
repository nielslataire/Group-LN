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

        /// <summary>Optioneel: id van het &lt;form&gt; dat de Opslaan-knop moet posten via het HTML5
        /// form="..."-attribuut, voor pagina's waar deze balk niet als afstammeling van dat form
        /// staat (bv. TrajectSjabloonAdmin/Edit, waar enkel het echte &lt;form&gt; een verborgen
        /// payload-veld bevat). Leeg = ongewijzigd bestaand gedrag (submit via het omvattende form).</summary>
        public string SubmitFormId { get; set; }
    }
}
