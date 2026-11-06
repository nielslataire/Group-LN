namespace CPMCore.Models.GlV2;

/// <summary>Invoer voor <c>Views/Shared/GlV2/_PersonPicker.cshtml</c> — de kiezer voor meerdere personen (collega's, contactpersonen, een getypt e-mailadres).</summary>
public class GlV2PersonPickerVm
{
    /// <summary>Naam van de verborgen input met de gekozen e-mailadressen (komma-gescheiden), voor modelbinding.</summary>
    public string Name { get; set; } = "";
    /// <summary>Element-id van de verborgen input; het tekstveld krijgt "&lt;id&gt;_input" (voor het label).</summary>
    public string Id { get; set; } = "";
    /// <summary>POST-adres dat { term, context } krijgt en [{ id (= e-mailadres), text, sub, group }] teruggeeft.</summary>
    public string LookupUrl { get; set; } = "";
    /// <summary>CSS-selector van een veld waarvan de waarde als `context` meegaat (bv. het gekozen project), zodat de lijst daar rekening mee houdt.</summary>
    public string? ContextSelector { get; set; }
    /// <summary>Placeholder zolang er niemand gekozen is.</summary>
    public string Placeholder { get; set; } = "Naam of e-mailadres";
    /// <summary>Placeholder als er al iemand gekozen is.</summary>
    public string PlaceholderMore { get; set; } = "Nog iemand toevoegen";
    /// <summary>Tekst in de lijst als niets past en de zoekterm geen e-mailadres is.</summary>
    public string EmptyText { get; set; } = "Niemand gevonden. Typ een volledig e-mailadres om iemand van buiten het CRM toe te voegen.";
}
