namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 koppeldialoog (design-handoff optie 16c "Koppelen vanuit de lijst · nieuwe eenheid"):
/// de herziene versie van Projecten/Modals/_ModalAddLink.cshtml. 16c's eigen kritiek op die oude
/// dialoog — "begon met twee lege bedragvelden zonder te weten waarvoor" — bepaalt de volgorde hier:
/// eerst <b>wát</b> je koppelt (een keuzelijst gegroepeerd in "los te koop" en "al gekoppeld", die
/// tweede groep niet kiesbaar), dan de bedragen die uit die keuze volgen, dan onderaan de nieuwe prijs
/// van het lot.
/// Koppelen = de gekozen eenheid onder dit lot hangen (<c>Units.AttachedUnitId</c>, hetzelfde veld dat
/// de "Gekoppelde eenheid"-dropdown op AddUnit/EditUnit al zet en dat de boom van 16a opbouwt) — niet
/// het oudere KOPPELING-mechanisme (<c>Units.IsLink</c>/<c>LinkedUnitId</c>) van de legacy dialoog, dat
/// een extra samengestelde pseudo-eenheid aanmaakt en de leden uit de lijst laat verdwijnen.</summary>
public class AttachUnitV2Vm
{
    public int ProjectId { get; set; }

    /// <summary>De hoofdeenheid waaraan gekoppeld wordt.</summary>
    public int LotUnitId { get; set; }
    public string LotName { get; set; } = "";

    /// <summary>Waar de POST naar terugkeert (lokale url) — de lijst is de standaard, het eenheidsformulier
    /// (16b, Type & koppeling) geeft zijn eigen url mee zodat je niet op de lijst belandt.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Prijs van het lot zoals de lijst die nu toont (eigen prijs + wat er al aan hangt) —
    /// de "van"-waarde in de voetregel "Nieuwe prijs Lot 2, casco".</summary>
    public decimal LotPrice { get; set; }

    /// <summary>Nevenruimtes die nog aan geen enkel lot hangen — kiesbaar.</summary>
    public List<AttachUnitV2Candidate> Available { get; set; } = new();

    /// <summary>Nevenruimtes die al aan een ander lot hangen — zichtbaar maar niet kiesbaar, zodat je
    /// ziet dat ze bestaan én waarom je ze hier niet kan kiezen (16c: "al gekoppeld (niet kiesbaar)").</summary>
    public List<AttachUnitV2Candidate> AlreadyAttached { get; set; } = new();
}

public class AttachUnitV2Candidate
{
    public int UnitId { get; set; }
    public string Name { get; set; } = "";
    /// <summary>"staanplaats · buiten" — subtype en verdieping onder de naam.</summary>
    public string TypeLine { get; set; } = "";
    /// <summary>Grondwaarde van de eenheid zelf — vult het (bewerkbare) veld GRONDWAARDE voor.</summary>
    public decimal LandValue { get; set; }
    /// <summary>Bouwwaarde van de eenheid zelf (som van haar bouwwaarderegels) — vult het veld
    /// BOUWWAARDE, dat bewust alleen-lezen is: de bouwwaarde zit in losse
    /// <c>UnitConstructionValue</c>-regels per betalingsgroep/afwerkingsoptie, niet in één bedrag, dus
    /// hier één getal laten typen zou de onderliggende regels stil laten afwijken.</summary>
    public decimal ConstructionValue { get; set; }
    /// <summary>Totale prijs van de eenheid — wat er bij het lot bij komt.</summary>
    public decimal Price { get; set; }
    /// <summary>Enkel gevuld in <see cref="AttachUnitV2Vm.AlreadyAttached"/>: "bij Lot 1".</summary>
    public string? AttachedToName { get; set; }
}
