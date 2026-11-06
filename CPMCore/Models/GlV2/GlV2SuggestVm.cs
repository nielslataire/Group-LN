namespace CPMCore.Models.GlV2;

/// <summary>Invoer voor <c>Views/Shared/GlV2/_SuggestField.cshtml</c> — een tekstveld met suggesties uit een server-lijst. De tekst blijft vrij: een suggestie vult het veld, ze dwingt niets af.</summary>
public class GlV2SuggestVm
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string? Value { get; set; }
    public string? Placeholder { get; set; }
    /// <summary>POST-adres dat { term } krijgt en [{ text, sub, tag }] teruggeeft.</summary>
    public string LookupUrl { get; set; } = "";
    public string Icon { get; set; } = "ph-map-pin";
    public int MaxLength { get; set; } = 200;
}
