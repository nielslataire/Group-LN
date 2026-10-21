namespace CPMCore.Models.GlV2;

/// <summary>Schakelaar / segmented control (projectbreed, zie DESIGN.md "Segmented control (schakelaar)").
/// Render via <c>@await Html.PartialAsync("GlV2/_Segmented", vm)</c>. Opties zijn knoppen: de pagina-JS zet <c>.is-active</c>
/// bij een klik en leest de <c>data-*</c>-attributen. Voor een keuze die met het formulier meegepost wordt, schrijf je liever
/// radio's (<c>&lt;label&gt;&lt;input type="radio"&gt;&lt;span&gt;…</c>) in een <c>.gl-v2-segmented</c>-container; dezelfde css doet de rest.</summary>
public class GlV2SegmentedVm
{
    public string? Id { get; set; }
    /// <summary>true = omlijnd met groen gevulde keuze (formulierkeuzes, 42c); anders de grijze baan.</summary>
    public bool Outline { get; set; }
    /// <summary>"" (34px), "sm" (28px, filterbalken) of "compact" (kleinere tekst, smalle modals).</summary>
    public string Size { get; set; } = "";
    /// <summary>Opties even breed over de volle breedte.</summary>
    public bool Fill { get; set; } = true;
    public string? AriaLabel { get; set; }
    public string? CssClass { get; set; }
    public List<GlV2SegmentedItem> Items { get; set; } = new();
}

public class GlV2SegmentedItem
{
    public string Text { get; set; } = "";
    public bool Active { get; set; }
    public bool Disabled { get; set; }
    /// <summary>Phosphor-klasse zonder "ph "-prefix, bv. "ph-globe".</summary>
    public string? Icon { get; set; }
    /// <summary>Wordt <c>data-{sleutel}="{waarde}"</c> op de knop (bv. ["kind"]="1").</summary>
    public Dictionary<string, string> Data { get; set; } = new();
}
