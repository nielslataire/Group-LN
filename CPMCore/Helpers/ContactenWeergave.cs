using System.Globalization;

namespace CPMCore.Helpers;

/// <summary>Weergave-afleidingen voor de gl-v2 contacten (design-handoff punt 44): statussen, bronnen, namen en datums.</summary>
public static class ContactenWeergave
{
    public const int Nieuw = 0, Contact = 1, Bezichtiging = 2, Optie = 3, Verkocht = 4, Afgevallen = 5;
    public static readonly string[] Bronnen = { "Website", "Telefoon", "E-mail", "Makelaar", "Werfbord" };
    private static readonly CultureInfo Be = CultureInfo.GetCultureInfo("nl-BE");

    public static string Label(int status) => status switch
    {
        Nieuw => "Nieuw", Contact => "Contact", Bezichtiging => "Bezichtiging", Optie => "Optie", Verkocht => "Verkocht", Afgevallen => "Afgevallen", _ => "Nieuw"
    };

    /// <summary>Badge-variant (gl-v2/page.css): Nieuw vol groen, Optie goud, Verkocht groen, Afgevallen rood, de rest neutraal (zoals 44a).</summary>
    public static string Tone(int status) => status switch
    {
        Nieuw => "is-solid", Optie => "is-attention", Verkocht => "is-positive", Afgevallen => "is-blocked", _ => "is-neutral"
    };

    /// <summary>Open = nog niet verkocht of afgevallen.</summary>
    public static bool IsOpen(int status) => status < Verkocht;

    public static string Bron(string? s)
    {
        var t = (s ?? "").Trim().ToLowerInvariant();
        if (t.Contains("mail")) return "E-mail";
        if (t.Contains("tel") || t.Contains("gsm") || t.Contains("bel") || t.Contains("phone")) return "Telefoon";
        if (t.Contains("makelaar") || t.Contains("broker") || t.Contains("immo")) return "Makelaar";
        if (t.Contains("werfbord") || t.Contains("bord")) return "Werfbord";
        return "Website";
    }

    public static string SourceIcon(string source) => source switch
    {
        "Telefoon" => "ph-phone", "E-mail" => "ph-envelope-simple", "Makelaar" => "ph-handshake", "Werfbord" => "ph-signpost", _ => "ph-globe"
    };

    public static string SourceClass(string source) => source switch { "Website" => "is-website", "Makelaar" => "is-broker", _ => "" };

    public static string KindIcon(string kind) => kind switch
    {
        "form" => "ph-globe", "call" => "ph-phone", "mail" => "ph-envelope-simple", "visit" => "ph-calendar-check", "option" => "ph-handshake", "status" => "ph-arrows-clockwise", _ => "ph-note-pencil"
    };

    public static string KindLabel(string kind) => kind switch
    {
        "form" => "Formulier", "call" => "Gesprek", "mail" => "E-mail", "visit" => "Bezichtiging", "option" => "Optie", "status" => "Status", _ => "Notitie"
    };

    /// <summary>"Jeroen &amp; Lien Martens": voornaam, eventueel de partner, dan de familienaam.</summary>
    public static string Name(string? first, string? partner, string? last)
    {
        var f = (first ?? "").Trim(); var p = (partner ?? "").Trim(); var l = (last ?? "").Trim();
        var voor = p == "" ? f : (f == "" ? p : f + " & " + p);
        var naam = (voor + " " + l).Trim();
        return naam == "" ? "Naamloos contact" : naam;
    }

    public static string Initials(string? name)
    {
        var parts = (name ?? "").Split(new[] { ' ', '&' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0][0].ToString() + parts[^1][0]).ToUpperInvariant();
    }

    /// <summary>De opgeslagen tijd (UTC) als lokale tijd.</summary>
    public static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

    /// <summary>"vandaag 08:14", "gisteren 14:02" of "18/10 · 14:02".</summary>
    public static string When(DateTime utc, bool withTime = true)
    {
        var d = Local(utc); var today = DateTime.Today;
        var dag = d.Date == today ? "vandaag" : d.Date == today.AddDays(-1) ? "gisteren" : d.ToString("dd/MM", Be);
        return withTime ? dag + (d.Date == today || d.Date == today.AddDays(-1) ? " " : " · ") + d.ToString("HH:mm", Be) : dag;
    }

    /// <summary>"vandaag", "morgen" of "24/10" (volgende actie).</summary>
    public static string Day(DateOnly date, DateOnly today) => date == today ? "vandaag" : date == today.AddDays(1) ? "morgen" : date.ToString("dd/MM", Be);
}
