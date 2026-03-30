namespace CPMCore.Models;

/// <summary>
/// Categorieën van dashboard-meldingen. Naam en icoon per type worden
/// via MeldingTypeHelper opgehaald zodat de view geen hardcoded strings bevat.
/// </summary>
public enum MeldingType
{
    Onbekend    = 0,
    Oplevering  = 1,
    Documenten  = 2,
    Verzekering = 3,
}

public static class MeldingTypeHelper
{
    private static readonly Dictionary<MeldingType, (string Label, string Icon)> _info = new()
    {
        [MeldingType.Onbekend]    = ("Overig",       "bx bx-info-circle"),
        [MeldingType.Oplevering]  = ("Oplevering",   "bx bx-checklist"),
        [MeldingType.Documenten]  = ("Documenten",   "bx bx-file"),
        [MeldingType.Verzekering] = ("Verzekering",  "bx bx-shield"),
    };

    public static string GetLabel(MeldingType type)
        => _info.TryGetValue(type, out var v) ? v.Label : type.ToString();

    public static string GetIcon(MeldingType type)
        => _info.TryGetValue(type, out var v) ? v.Icon : "bx bx-info-circle";

    public static MeldingType FromString(string? category) => category?.ToLowerInvariant() switch
    {
        "oplevering"  => MeldingType.Oplevering,
        "documenten"  => MeldingType.Documenten,
        "verzekering" => MeldingType.Verzekering,
        _             => MeldingType.Onbekend,
    };
}
