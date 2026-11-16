using System.Collections.Generic;
using System.Linq;

namespace CPMCore.Models.GlV2;

/// <summary>Eén regel in de kaart "Vraagt actie" (partial GlV2/_VraagtActie).</summary>
public class GlV2VraagtActieItem
{
    /// <summary>"u" = dringend, "a" = aandacht.</summary>
    public string Severity { get; set; } = "a";
    public string Type { get; set; } = "";
    /// <summary>Verwijzing naast de titel (eenheid, contractnaam, factuurnummer), optioneel.</summary>
    public string? Entiteit { get; set; }
    public string? Extra { get; set; }
    /// <summary>Waar de regel naartoe gaat; null = gewone tekst zonder link.</summary>
    public string? BekijkUrl { get; set; }
    public string Icon { get; set; } = "ph-warning-circle";
    /// <summary>Categorie voor het filter, bv. Contracten of Werf.</summary>
    public string Categorie { get; set; } = "";
    /// <summary>Ouderdom of termijn rechts in de regel, bv. "56 dagen te laat".</summary>
    public string? Meta { get; set; }
}

/// <summary>Model van de gedeelde kaart "Vraagt actie" (design-handoff punt 60): Projecten/Detail, het contract en de facturen van een leverancier.</summary>
public class GlV2VraagtActieVm
{
    public List<GlV2VraagtActieItem> Items { get; set; } = new();

    /// <summary>De filterknoppen naast Alles, in volgorde. Leeg = geen filterknoppen.</summary>
    public List<string> Categories { get; set; } = new();

    public string Subtitle { get; set; } = "Wat dit project tegenhoudt, dringend eerst.";
    public string EmptyTitle { get; set; } = "Niets dat dit project tegenhoudt";
    public string EmptyText { get; set; } = "Geen openstaande contracten, werfpunten, verzekeringen of waarschuwingen.";

    public IEnumerable<GlV2VraagtActieItem> Urgent => Items.Where(i => i.Severity == "u");
    public IEnumerable<GlV2VraagtActieItem> Attention => Items.Where(i => i.Severity != "u");
}
