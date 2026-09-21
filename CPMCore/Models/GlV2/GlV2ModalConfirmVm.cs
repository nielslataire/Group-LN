namespace CPMCore.Models.GlV2;

/// <summary>Kleurvariant voor <see cref="GlV2ModalConfirmVm"/> — bepaalt icoon-tint, randaccent en
/// bevestigknop-kleur, zie design-handoff 4j ("Modals per type en per schermformaat").</summary>
public enum GlV2ModalTone { Success, Warning, Danger }

/// <summary>Herbruikbare gl-v2 TYPE 1 "Bevestiging"-modal (design-handoff 4j) — icoon, titel, tekst,
/// annuleer-/bevestigknop, gepost via een eigen &lt;form&gt;. Renderen via
/// <c>@await Html.PartialAsync("GlV2/_ModalConfirm", new GlV2ModalConfirmVm { ... })</c>; open 'm
/// vanaf een knop/link met <c>data-bs-toggle="modal" data-bs-target="#{Id}"</c>, standaard Bootstrap
/// modal-gedrag, geen eigen JS nodig. Voor een per-rij bevestiging met wisselende inhoud (zoals de
/// facturenlijst) blijft het AJAX-partial-patroon (zie <c>Invoices/Modals/_ModalDeleteInvoiceV2</c>)
/// de juiste keuze — dit component is voor het ene-vaste-doel-per-pagina geval.</summary>
public class GlV2ModalConfirmVm
{
    public string Id { get; set; } = "glV2ConfirmModal";
    public GlV2ModalTone Tone { get; set; } = GlV2ModalTone.Danger;

    /// <summary>Phosphor-klasse zonder "ph "-prefix, bv. "ph-trash".</summary>
    public string IconClass { get; set; } = "ph-question";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CancelLabel { get; set; } = "Annuleren";
    public string ConfirmLabel { get; set; } = "Bevestigen";

    public string FormAction { get; set; } = "";
    public string FormMethod { get; set; } = "post";
    public Dictionary<string, string> HiddenFields { get; set; } = new();
}
