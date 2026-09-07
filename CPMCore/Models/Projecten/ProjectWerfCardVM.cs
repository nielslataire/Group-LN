using BOCore;

namespace CPMCore.Models.Projecten;

/// <summary>
/// Eén "werf-kaart" (gl-werf-*). Gedeeld tussen de rol-dashboards
/// (Home/_DashboardProjectleider, _DashboardCeoCfo) en de projectenlijst
/// (Projecten/Index) zodat er maar één projectkaart-uiterlijk bestaat.
/// De partial (Views/Shared/_ProjectWerfCard.cshtml) rendert de volledige
/// col-wrapper; caller-specifieke chrome (rangschik-balk, pin, bedrijfsbadge)
/// wordt via de vlaggen hieronder in- of uitgeschakeld.
/// </summary>
public class ProjectWerfCardVM
{
    public ProjectBO Project { get; set; } = default!;

    /// <summary>Voortgang voor dit project, of null als er nog geen berekening is.</summary>
    public ProjectVoortgangBO? Voortgang { get; set; }

    /// <summary>Basis-URL voor afbeeldingen (Configuration["URL:ImageWebUrl"]).</summary>
    public string ImageBaseUrl { get; set; } = string.Empty;

    /// <summary>Bootstrap-kolomklassen voor de wrapper. Standaard = de
    /// dashboard-verdeling; Projecten/Index geeft hier zijn eigen grid-cel mee.</summary>
    public string ColumnClasses { get; set; } = "col-12 col-sm-6 col-lg-4 col-xxl-3 mb-4";

    /// <summary>Toon de rangschik-balk (enkel Projectleider "Mijn Werven").</summary>
    public bool ShowArrangeBar { get; set; }

    /// <summary>Toon het "vastgezet"-pin-icoon (enkel Projectleider).</summary>
    public bool IsPinned { get; set; }

    /// <summary>Bedrijfsbadge linksboven op de foto (enkel CeoCfo multi-company grid).</summary>
    public string? CompanyLabel { get; set; }
}
