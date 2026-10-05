namespace CPMCore.Models.GlV2;

/// <summary>Invoer voor <c>Views/Shared/Partials/_GlV2Combo.cshtml</c> — de keuzelijst met zoekveld en knop
/// (design-handoff punt 34). Eén van <see cref="LookupUrl"/> (server-zoekopdracht) of <see cref="OptionsJson"/>
/// (statische lijst, JSON [{id,text,sub}]).</summary>
public class GlV2ComboVm
{
    /// <summary>Naam van de verborgen id-input (modelbinding), bv. "Contract.Company.ID".</summary>
    public string Name { get; set; } = "";
    /// <summary>Element-id van de verborgen input; standaard Name met . → _.</summary>
    public string? Id { get; set; }
    public string? Value { get; set; }
    /// <summary>Leesbare tekst van de huidige waarde (komt in het veld te staan).</summary>
    public string? DisplayText { get; set; }
    public string? Placeholder { get; set; }
    public string? LookupUrl { get; set; }
    public string? OptionsJson { get; set; }
    public int? MinChars { get; set; }
    public string? CountrySelector { get; set; }
    /// <summary>Toont de "Nieuw"-knop en de "… aanmaken"-rij (event gl-v2:combo-new).</summary>
    public bool AllowNew { get; set; }
    public string NewLabel { get; set; } = "Nieuw";
    /// <summary>Woord in '"x" aanmaken als nieuw …' (bv. "bedrijf").</summary>
    public string NewEntity { get; set; } = "item";
    public bool Avatar { get; set; }
    public string? ListLabel { get; set; }
    /// <summary>Waarde van de verborgen input als er niets gekozen is ("" of "0").</summary>
    public string EmptyValue { get; set; } = "";
    /// <summary>Phosphor-icoonklasse voor voorin het veld (bv. "ph-buildings"); wordt een vergrootglas tijdens het typen.</summary>
    public string Icon { get; set; } = "ph-magnifying-glass";
    /// <summary>Optionele tweede verborgen input voor de weergavenaam (bv. Project.Developer.Display).</summary>
    public string? TextName { get; set; }
    public string? TextValue { get; set; }
    public bool ReadOnly { get; set; }
    public bool Disabled { get; set; }
    public bool HasError { get; set; }
    /// <summary>Tweede regel onder het veld (bv. btw-nummer · gemeente van de gekozen rij).</summary>
    public string? Help { get; set; }
}
