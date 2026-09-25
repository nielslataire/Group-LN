namespace CPMCore.Models.GlV2;

/// <summary>Kleurvariant voor <see cref="GlV2KpiItemVm"/> — bepaalt de linker-accentrand en de
/// icoonkleur (icoon zelf blijft altijd wit), zie design-handoff 7a ("KPI-kaarten").</summary>
public enum GlV2KpiTone { Primary, Warning, Danger }

/// <summary>Herbruikbare gl-v2 KPI-kaart (design-handoff 7a) — icoon, label, cijfer. Render een lijst
/// ervan via <c>@await Html.PartialAsync("GlV2/_KpiStrip", kpiList)</c>; de partial kiest zelf de
/// "ruime" (≤4, desktop) of "compacte" (5+, of tablet/mobiel ongeacht aantal) kaartvorm op basis van
/// het aantal items — geen aparte parameter nodig.</summary>
public class GlV2KpiItemVm
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";

    /// <summary>Optionele derde regel onder het cijfer — de "2 woningen · 3 bergingen en parkings"-
    /// voetjes uit design-handoff 16a/16d. Leeg = de kaart blijft exact zoals ze was (label + cijfer),
    /// dus geen enkele bestaande pagina verandert hierdoor van hoogte of opbouw.</summary>
    public string? Hint { get; set; }

    /// <summary>Phosphor-klasse zonder "ph "-prefix, bv. "ph-buildings".</summary>
    public string IconClass { get; set; } = "ph-chart-bar";
    public GlV2KpiTone Tone { get; set; } = GlV2KpiTone.Primary;
}
