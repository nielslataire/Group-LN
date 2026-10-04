namespace CPMCore.Models;

/// <summary>
/// Herbruikbare paginakop: terug-knop + titel + subtitel (het cluster dat vrijwel
/// identiek terugkomt op tientallen views). De acties rechts van de titel horen
/// hier bewust niet bij — die verschillen te veel in inhoud (knop, btn-group,
/// dropdown...) om zinvol te generaliseren; zie DESIGN.md voor de bijhorende
/// ".gl-page-header__actions"-plaatsingsklasse die de aanroepende pagina zelf vult.
/// </summary>
public class PageHeaderModel
{
    public required string Title { get; set; }
    public string? Subtitle { get; set; }

    /// <summary>Null of leeg = geen terug-knop.</summary>
    public string? BackUrl { get; set; }
    public string? BackAriaLabel { get; set; }
}
