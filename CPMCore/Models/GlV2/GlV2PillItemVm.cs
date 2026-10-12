namespace CPMCore.Models.GlV2;

/// <summary>Eén item in een <c>GlV2/_PillSelector</c>-rij (design-handoff 42f, "activiteit-pills in de
/// inhoud, onder de categorie-tabbar") — een rij navigatie-pills met een telling-chip, bv. "Alle
/// activiteiten · 3" / "Elektrische installatie · 4". Elke pill is een link (geen formuliertoestand),
/// dus <see cref="Url"/> is verplicht.</summary>
public class GlV2PillItemVm
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsActive { get; set; }
    public string Url { get; set; } = "";
}
