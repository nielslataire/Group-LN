namespace CPMCore.Models.GlV2;

/// <summary>Invoer voor <c>Views/Shared/GlV2/_SelectList.cshtml</c> — de Basis-variant van het gl-v2
/// keuzelijst-component (design-handoff 4h) voor een lijst die NIET uit een enum komt (types, hoofd-
/// eenheden, betalingsgroepen, verdiepingen …). <c>GlV2Select</c> (EditorTemplate) dekt enkel enums; deze
/// partial neemt een expliciete naam, waarde en itemlijst, zodat hij ook binnen gegenereerde rijen
/// (<c>FinishingOptions[key].…</c>) kan staan.</summary>
public class GlV2SelectListVm
{
    /// <summary>Veldnaam zoals de modelbinder hem verwacht (bv. "SelectedType", "Rooms[k].Type").</summary>
    public string Name { get; set; } = "";
    /// <summary>Element-id; leeg = afgeleid van Name.</summary>
    public string? Id { get; set; }
    public string? Label { get; set; }
    public bool Required { get; set; }
    public string? Value { get; set; }
    public List<GlV2SelectItem> Items { get; set; } = new();
    /// <summary>Tekst van de gesloten trigger zolang er niets gekozen is.</summary>
    public string Placeholder { get; set; } = "Kies …";
    /// <summary>Indien gezet: een eerste kiesbare optie met waarde "" (bv. "Geen — dit is een hoofdeenheid").</summary>
    public string? EmptyText { get; set; }
    public string? Help { get; set; }
    public string? Error { get; set; }
    public bool Disabled { get; set; }
    /// <summary>Zonder label/wrapper — voor een cel in een tabelrij.</summary>
    public bool Bare { get; set; }
    public string? Icon { get; set; }
    /// <summary>Extra data-attributen op de .gl-v2-select-wrapper (bv. "data-role" → "type").</summary>
    public Dictionary<string, string> Data { get; set; } = new();
}

public class GlV2SelectItem
{
    public string Value { get; set; } = "";
    public string Text { get; set; } = "";
    /// <summary>Groepskop (opeenvolgende items met dezelfde Group vallen onder één kop).</summary>
    public string? Group { get; set; }
    public bool Disabled { get; set; }
    /// <summary>Tweede, gedimde regel onder de tekst (bv. "staanplaats · buiten").</summary>
    public string? Sub { get; set; }
}
