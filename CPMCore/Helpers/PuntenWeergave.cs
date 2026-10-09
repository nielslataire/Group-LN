using System.Globalization;
using BOCore;

namespace CPMCore.Helpers;

/// <summary>Weergave-afleidingen voor de gl-v2 punten (design-handoff 40): statustoon, "afgesloten", prioriteitswoord.</summary>
public static class PuntenWeergave
{
    public static bool IsClosed(int status) => status == (int)ConstructionIssueStatus.Closed || status == (int)ConstructionIssueStatus.Resolved;

    /// <summary>Badge-variant (gl-v2/page.css) per status — Concept grijs, Ter goedkeuring aandacht, Doorgestuurd info, Gemeld positief, Afgesloten vol.</summary>
    public static string Tone(int status) => (ConstructionIssueStatus)status switch
    {
        ConstructionIssueStatus.Concept => "is-neutral",
        ConstructionIssueStatus.PendingApproval => "is-attention",
        ConstructionIssueStatus.Approved => "is-positive",
        ConstructionIssueStatus.Forwarded or ConstructionIssueStatus.Open or ConstructionIssueStatus.Assigned or ConstructionIssueStatus.InProgress or ConstructionIssueStatus.Reopened => "is-info",
        ConstructionIssueStatus.Reported or ConstructionIssueStatus.WaitingInspection => "is-positive",
        ConstructionIssueStatus.Closed or ConstructionIssueStatus.Resolved => "is-solid",
        ConstructionIssueStatus.Rejected => "is-blocked",
        ConstructionIssueStatus.OnHold => "is-inactive",
        _ => "is-neutral"
    };

    public static string Label(int status) => (ConstructionIssueStatus)status switch
    {
        ConstructionIssueStatus.Open or ConstructionIssueStatus.Assigned or ConstructionIssueStatus.InProgress or ConstructionIssueStatus.Reopened => "Doorgestuurd",
        ConstructionIssueStatus.WaitingInspection => "Gemeld uitgevoerd",
        ConstructionIssueStatus.Resolved => "Afgesloten",
        ConstructionIssueStatus.Concept => "Concept",
        ConstructionIssueStatus.PendingApproval => "Ter goedkeuring",
        ConstructionIssueStatus.Approved => "Goedgekeurd",
        ConstructionIssueStatus.Forwarded => "Doorgestuurd",
        ConstructionIssueStatus.Reported => "Gemeld uitgevoerd",
        ConstructionIssueStatus.Closed => "Afgesloten",
        ConstructionIssueStatus.Rejected => "Afgewezen",
        ConstructionIssueStatus.OnHold => "In de wacht",
        _ => status.ToString()
    };

    /// <summary>Wat de werfleider vanuit de huidige status mag doen (statusverloop 40j).</summary>
    public static IEnumerable<ConstructionIssueStatus> Next(int status) => (ConstructionIssueStatus)status switch
    {
        ConstructionIssueStatus.Concept => new[] { ConstructionIssueStatus.PendingApproval, ConstructionIssueStatus.Approved },
        ConstructionIssueStatus.PendingApproval => new[] { ConstructionIssueStatus.Approved, ConstructionIssueStatus.Concept },
        ConstructionIssueStatus.Approved => new[] { ConstructionIssueStatus.Concept },
        ConstructionIssueStatus.Reported or ConstructionIssueStatus.WaitingInspection => new[] { ConstructionIssueStatus.Closed, ConstructionIssueStatus.Rejected },
        ConstructionIssueStatus.Closed or ConstructionIssueStatus.Resolved => new[] { ConstructionIssueStatus.Forwarded },
        ConstructionIssueStatus.OnHold => new[] { ConstructionIssueStatus.Forwarded },
        _ => new[] { ConstructionIssueStatus.Reported, ConstructionIssueStatus.Closed, ConstructionIssueStatus.OnHold }
    };

    /// <summary>De nieuwe status waarop een legacy-waarde valt (voor filters/tellers, ook als de migratie nog niet draaide).</summary>
    public static int Canon(int status) => (ConstructionIssueStatus)status switch
    {
        ConstructionIssueStatus.Open or ConstructionIssueStatus.Assigned or ConstructionIssueStatus.InProgress or ConstructionIssueStatus.Reopened => (int)ConstructionIssueStatus.Forwarded,
        ConstructionIssueStatus.WaitingInspection => (int)ConstructionIssueStatus.Reported,
        ConstructionIssueStatus.Resolved => (int)ConstructionIssueStatus.Closed,
        _ => status
    };

    private static readonly CultureInfo Be = CultureInfo.GetCultureInfo("nl-BE");

    /// <summary>Een punt begint altijd met een hoofdletter, de rest is kleine letters. Uitzondering: afkortingen (2 tot 5 hoofdletters,
    /// bv. PVC, HWA, EPS) blijven staan — maar enkel als de titel zelf gemengd geschreven is; een titel volledig in hoofdletters
    /// wordt gewoon omgezet.</summary>
    public static string Titel(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var trimmed = s.Trim();
        var mixed = trimmed.Any(char.IsLower);
        var t = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\p{L}+", m =>
        {
            var w = m.Value;
            var isAcronym = mixed && w.Length is >= 2 and <= 5 && w.All(char.IsUpper);
            return isAcronym ? w : w.ToLower(Be);
        });
        return char.ToUpper(t[0], Be) + t.Substring(1);
    }

    /// <summary>Een kamer / zone is altijd in kleine letters.</summary>
    public static string Zone(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLower(Be);

    /// <summary>Een aannemersnaam: enkel de eerste letter van elk woord een hoofdletter.</summary>
    public static string Aannemer(string? s) => string.IsNullOrWhiteSpace(s) ? "" : Be.TextInfo.ToTitleCase(s.Trim().ToLower(Be));

    public static string PriorityWord(int p) => p switch { 3 => "kritiek", 2 => "hoog", 0 => "laag", _ => "" };
}
