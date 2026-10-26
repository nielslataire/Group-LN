namespace CPMCore.Models.GlV2;

/// <summary>Contract van <c>GlV2/_Dropzone</c> (bestanden slepen of kiezen). Zie DESIGN.md "Dropzone (bestand slepen of kiezen)".</summary>
public class GlV2DropzoneVm
{
    /// <summary>Naam van het bestandsveld (model binding, bv. een IFormFile-eigenschap). Leeg = geen name (de pagina leest het veld zelf).</summary>
    public string Name { get; set; } = "";
    public string? Id { get; set; }
    /// <summary>Waarde van het accept-attribuut, bv. "application/pdf,.pdf" of "image/*". Ook de controle op het type in de browser.</summary>
    public string? Accept { get; set; }
    /// <summary>Maximale grootte per bestand in MB (leeg = geen controle in de browser; de server controleert zelf).</summary>
    public int? MaxMb { get; set; }
    public bool Multiple { get; set; }
    /// <summary>Het bestandsveld is verplicht (required): de browser blokkeert het formulier zonder bestand.</summary>
    public bool Required { get; set; }
    /// <summary>Alleen slepen: de zone is geen klikvlak (het bestandsveld is verborgen en de pagina opent de keuze zelf, bv. via een knop of label met for=…).
    /// Voor een zone met inhoud waarop de gebruiker moet kunnen klikken of tekenen, zoals een canvas.</summary>
    public bool DropOnly { get; set; }
    /// <summary>Vorm: "box" (gestapeld, standaard), "bar" (één regel: pictogram, tekst, link en status) of "compact" (laag vak, bv. een logo).</summary>
    public string Layout { get; set; } = "box";
    /// <summary>
    /// Wat er na het kiezen gebeurt. "row" (standaard): de zone toont een bestandsrij met Verwijderen. "inline": titel en ondertitel worden de bestandsnaam en grootte
    /// (met <see cref="ChangeText"/>), de zone blijft klikbaar. "select": de zone blijft ongewijzigd en de pagina toont zelf wat gekozen is. "list": voor meerdere
    /// bestanden; de gekozen bestanden komen onder de zone in een lijst met een kruisje per bestand (en een naamveld per bestand met <see cref="ItemNameField"/>);
    /// nieuwe bestanden komen erbij.
    /// </summary>
    public string Mode { get; set; } = "row";
    public string Icon { get; set; } = "ph-upload-simple";
    /// <summary>Eerste tekst. Met <see cref="LinkText"/> komt daar een onderstreepte tekst achter ("… of kies een bestand").</summary>
    public string Title { get; set; } = "Sleep een bestand hierheen of";
    public string? LinkText { get; set; } = "kies een bestand";
    public string? Sub { get; set; }
    /// <summary>Andere tekst op tablet en gsm (&lt; 1024px), waar slepen niet bestaat, bv. "Klik om te uploaden". Leeg = dezelfde tekst overal.</summary>
    public string? TitleTouch { get; set; }
    public string? SubTouch { get; set; }
    /// <summary>Eigen inhoud in de zone in plaats van pictogram, titel en ondertitel (bv. een rij met de naam van een sectie). De zone krijgt dan geen eigen
    /// rand of achtergrond (die komt van <see cref="CssClass"/>); slepen, kiezen en controleren blijven van het component.</summary>
    public Microsoft.AspNetCore.Html.IHtmlContent? Content { get; set; }
    /// <summary>Alleen bij Mode="inline": de ondertitel na het kiezen komt achter de grootte, bv. "klik om een ander bestand te kiezen".</summary>
    public string? ChangeText { get; set; }
    /// <summary>Een bestaand bestand (bv. de huidige bijlage van een concept): wordt getoond met een knop Verwijderen (Mode="row").</summary>
    public string? CurrentName { get; set; }
    public string? CurrentSub { get; set; }
    /// <summary>Een statusregel in de zone, bv. voor "bezig met inlezen" (zie GlV2Dropzone.setStatus).</summary>
    public bool StatusSlot { get; set; }
    /// <summary>Een extra link rechts in de zone (bv. "huidig document openen"); klikbaar los van het bestandsveld.</summary>
    public string? AsideText { get; set; }
    public string? AsideHref { get; set; }
    /// <summary>Een miniatuur van de gekozen afbeelding (Mode="row" en "list").</summary>
    public bool Preview { get; set; }
    /// <summary>Bij Mode="list": naam van een tekstveld per bestand dat samen met de bestanden wordt verstuurd (bv. "executionPlanNames"), in dezelfde volgorde.</summary>
    public string? ItemNameField { get; set; }
    public string? ItemNamePlaceholder { get; set; }
    /// <summary>Een bestand of schermafdruk uit het klembord (Ctrl+V) wordt als gekozen bestand genomen zolang de zone zichtbaar is.</summary>
    public bool AcceptPaste { get; set; }
    /// <summary>Een knop "Foto nemen" naast het kiezen (enkel op tablet en gsm; opent de camera).</summary>
    public bool Camera { get; set; }
    public string CameraText { get; set; } = "Foto nemen";
    /// <summary>Een label boven de zone (.gl-v2-field-label); samen met <see cref="RequiredMessage"/> staat de zone in een .gl-v2-field.</summary>
    public string? Label { get; set; }
    /// <summary>Verplicht in het Foutoverzicht (punt 24): de melding als er geen bestand gekozen is (data-gl-v2-required op het omhullende .gl-v2-field).</summary>
    public string? RequiredMessage { get; set; }
    /// <summary>De zone is verborgen bij het laden (de pagina toont ze later zelf, bv. nadat een bestand gekozen is).</summary>
    public bool Hidden { get; set; }
    /// <summary>Extra data-attributen op de zone, zonder "data-" ervoor (bv. "plan-drop" wordt data-plan-drop), zodat pagina-JS de zone kan vinden.</summary>
    public Dictionary<string, string> Data { get; set; } = new();
    /// <summary>Extra klassen op de zone, voor kleine pagina-eigen aanpassingen.</summary>
    public string? CssClass { get; set; }
    public string? AriaLabel { get; set; }
}
