namespace CPMCore.Models.GlV2;

/// <summary>Herbruikbare gl-v2 zoekmodal (design-handoff 7e, "Klant zoeken") — projectbreed, niet
/// dashboard-specifiek. Render via <c>@await Html.PartialAsync("GlV2/_SearchModal", vm)</c>. Draagt
/// zelf geen zoeklogica: de partial haalt resultaten op bij <see cref="LookupUrl"/> (een bestaand
/// <c>GET ?term=&amp;take=</c>-endpoint dat <c>{ results: [{ id, text }] }</c> teruggeeft, zoals
/// Leveranciers/Klanten se Lookup-actions), en bouwt elke rij se link via <see cref="DetailUrlTemplate"/>
/// (met een <c>{id}</c>-plaatshouder).</summary>
public class GlV2SearchModalVm
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public string LookupUrl { get; set; } = "";
    public string DetailUrlTemplate { get; set; } = "";
    public string? CreateUrl { get; set; }
    public string CreateLabel { get; set; } = "";
}
