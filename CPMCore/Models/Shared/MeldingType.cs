using System.Security.Cryptography;
using System.Text;

namespace CPMCore.Models;

/// <summary>
/// Categorieën van dashboard-meldingen. Naam en icoon per type worden
/// via MeldingTypeHelper opgehaald zodat de view geen hardcoded strings bevat.
/// </summary>
public enum MeldingType
{
    Onbekend             = 0,
    Oplevering           = 1,
    Documenten           = 2,
    Verzekering          = 3,
    AannemerCommentaar   = 4,
}

public static class MeldingTypeHelper
{
    private static readonly Dictionary<MeldingType, (string Label, string Icon, string PhosphorIcon)> _info = new()
    {
        [MeldingType.Onbekend]           = ("Overig",     "bx bx-info-circle",        "ph-info"),
        [MeldingType.Oplevering]         = ("Oplevering", "bx bx-checklist",          "ph-chart-bar"),
        [MeldingType.Documenten]         = ("Documenten", "bx bx-file",               "ph-file-text"),
        [MeldingType.Verzekering]        = ("Verzekering","bx bx-shield",             "ph-shield"),
        [MeldingType.AannemerCommentaar] = ("Aannemer",   "bx bx-message-bubble",     "ph-chat-circle-text"),
    };

    public static string GetLabel(MeldingType type)
        => _info.TryGetValue(type, out var v) ? v.Label : type.ToString();

    public static string GetIcon(MeldingType type)
        => _info.TryGetValue(type, out var v) ? v.Icon : "bx bx-info-circle";

    /// <summary>gl-v2 (design-handoff 7b) — Phosphor-equivalent van GetIcon, per de bestaande
    /// Boxicons→Phosphor-regel (zie DESIGN.md-draft "Icons").</summary>
    public static string GetPhosphorIcon(MeldingType type)
        => _info.TryGetValue(type, out var v) ? v.PhosphorIcon : "ph-info";

    public static MeldingType FromString(string? category) => category?.ToLowerInvariant() switch
    {
        "oplevering"          => MeldingType.Oplevering,
        "documenten"          => MeldingType.Documenten,
        "verzekering"         => MeldingType.Verzekering,
        "aannemercommentaar"  => MeldingType.AannemerCommentaar,
        _                     => MeldingType.Onbekend,
    };
}

/// <summary>
/// gl-v2 dashboard-meldingenscherm (design-handoff 7b) — de drie melding-bronnen
/// (verzekeringswaarschuwingen, projectinfo, aannemer-comments) hebben geen stabiele, uniforme Id
/// over hun bronnen heen (zie WarningBO), dus wordt een melding voor snooze-doeleinden geïdentificeerd
/// door een hash van projectId+category+tekst — dezelfde de-facto samengestelde sleutel die de
/// eerdere client-side (localStorage) snooze-implementatie in _DashboardProjectleider.cshtml al
/// gebruikte, nu server-side herbruikt zodat oude en nieuwe laag dezelfde melding identiek herkennen.
/// </summary>
public static class MeldingKeyHelper
{
    /// <summary>Neemt bewust het <see cref="MeldingType"/>-enum, niet een losse string: elke
    /// aanroeper (bell-badge in HomeController, groepsopbouw in _DashboardProjectleider.cshtml, de
    /// Snooze/Unsnooze-AJAX-endpoints) moet dezelfde categorie-representatie hashen, en een enum
    /// dwingt dat af — een raw string zou per bron een andere casing/spelling kunnen dragen
    /// (WarningBO.Category komt bv. lowercase binnen) en zo stilzwijgend een andere sleutel opleveren
    /// voor exact dezelfde melding. Roep <see cref="MeldingTypeHelper.FromString"/> eerst aan als je
    /// enkel de raw string hebt.</summary>
    public static string ComputeKey(int projectId, MeldingType category, string tekst)
    {
        var raw = $"{projectId}|{(int)category}|{tekst}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
