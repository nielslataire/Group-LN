using System.Text.RegularExpressions;

namespace GroupLN.MarketData.Infrastructure.TitleResolution;

/// <summary>
/// Probeert een echte projectnaam te vinden in tekst via gedefinieerde regex-patronen.
/// Wordt toegepast vóór AI-extractie. Uitbreidbaar via <see cref="ExtraPatterns"/>.
/// </summary>
public static class ProjectNameRegexExtractor
{
    public record ExtractionResult(string ProjectName, string MatchedPattern, int PatternIndex);

    // Basispatronen — elk pattern moet groep 1 bevatten voor de eigenlijke naam
    private static readonly (string Pattern, RegexOptions Options)[] BuiltInPatterns =
    [
        // "het project Weylerhof" / "Het project Karmel"
        (@"[Hh]et\s+project\s+([A-Z][a-zA-ZÀ-ÿ'\s\-]{2,30}?)(?:\s*[\.,;–\-]|\s*$|\s+(?:te|in|van|met)\b)",
            RegexOptions.None),

        // "woonproject Karmel" / "Woonproject De Berk"
        (@"[Ww]oonproject\s+([A-Z][a-zA-ZÀ-ÿ'\s\-]{2,30}?)(?:\s*[\.,;–\-]|\s*$|\s+(?:te|in|van|met)\b)",
            RegexOptions.None),

        // "residentie De Berk" / "Residentie Zuidlaan"
        (@"[Rr]esidentie\s+([A-Z][a-zA-ZÀ-ÿ'\s\-]{2,30}?)(?:\s*[\.,;–\-]|\s*$|\s+(?:te|in|van|met)\b)",
            RegexOptions.None),

        // "nieuwbouwproject Weylerhof"
        (@"[Nn]ieuwbouwproject\s+([A-Z][a-zA-ZÀ-ÿ'\s\-]{2,30}?)(?:\s*[\.,;–\-]|\s*$|\s+(?:te|in|van|met)\b)",
            RegexOptions.None),

        // "'t Molenhof" / "'t Kapelhof"
        (@"'t\s+([A-Z][a-zA-ZÀ-ÿ]{2,25})",
            RegexOptions.None),

        // "project De Nieuwe Sluis" / "Project Keiberg"
        (@"\b[Pp]roject\s+([A-Z][a-zA-ZÀ-ÿ'\s\-]{2,30}?)(?:\s*[\.,;–\-]|\s*$|\s+(?:te|in|van|met)\b)",
            RegexOptions.None),

        // "Appartementsproject Vossenslag"
        (@"[Aa]ppartementsproject\s+([A-Z][a-zA-ZÀ-ÿ'\s\-]{2,30}?)(?:\s*[\.,;–\-]|\s*$|\s+(?:te|in|van|met)\b)",
            RegexOptions.None),

        // "ontwikkelingsproject Villa Rustica"
        (@"[Oo]ntwikkelingsproject\s+([A-Z][a-zA-ZÀ-ÿ'\s\-]{2,30}?)(?:\s*[\.,;–\-]|\s*$|\s+(?:te|in|van|met)\b)",
            RegexOptions.None),
    ];

    /// <summary>
    /// Extra patronen die vanuit config/code kunnen worden toegevoegd.
    /// </summary>
    public static readonly List<(string Pattern, RegexOptions Options)> ExtraPatterns = [];

    public static ExtractionResult? TryExtract(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var allPatterns = BuiltInPatterns.Concat(ExtraPatterns).ToArray();
        for (int i = 0; i < allPatterns.Length; i++)
        {
            var (pattern, opts) = allPatterns[i];
            var match = Regex.Match(text, pattern, opts | RegexOptions.Singleline);
            if (!match.Success || match.Groups.Count < 2) continue;

            var name = match.Groups[1].Value.Trim().TrimEnd(',', '.', ';', '-', '–');
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3) continue;

            return new ExtractionResult(name, pattern, i);
        }

        return null;
    }

    /// <summary>
    /// Zoekt in meerdere tekstvelden (h1, og:title, description, body) in volgorde van vertrouwen.
    /// Geeft het eerste betrouwbare resultaat terug.
    /// </summary>
    public static ExtractionResult? TryExtractFromMultiple(params string?[] texts)
    {
        foreach (var text in texts)
        {
            var result = TryExtract(text);
            if (result is not null) return result;
        }
        return null;
    }
}
