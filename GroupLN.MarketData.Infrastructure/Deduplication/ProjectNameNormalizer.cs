using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GroupLN.MarketData.Infrastructure.Deduplication;

internal static class ProjectNameNormalizer
{
    private static readonly Regex WhitespaceRegex   = new(@"\s+",            RegexOptions.Compiled);
    private static readonly Regex NonWordRegex       = new(@"[^\w\s]",        RegexOptions.Compiled);
    private static readonly Regex ResAbbrevRegex     = new(@"\bres\.\s*",     RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Strip "biedt ..." / "bieden ..." en alles erna (marketing-suffix)
    private static readonly Regex BiedtSuffixRegex   = new(@"\s+bied[et]\b.*$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Fase-suffix: Romeinse cijfers i–xii, arabische getallen 1–99, letters a–d
    private static readonly Regex PhaseTokenRegex    = new(@"^(i{1,3}|iv|v|vi{0,3}|ix|x{1,2}|xi{0,3}|\d{1,2}|[a-d])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] StripWords =
    [
        "nieuwbouwproject", "woonproject", "verkaveling",
        "appartementen", "woningen", "huizen",
        "nieuwbouw", "project",
    ];

    /// <summary>
    /// Normaliseert een projectnaam voor matching: lowercase, accenten weg, punctuatie weg,
    /// "biedt …"-suffix weg, "Res." → "residentie", generieke woorden weg.
    /// Fase-suffixen (I, II, 1, 2, a, b …) blijven behouden zodat fases niet samenvallen.
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var s = name.Trim().ToLowerInvariant();
        s = RemoveAccents(s);

        // Marketing-suffix verwijderen voor normalizing punctuatie
        s = BiedtSuffixRegex.Replace(s, string.Empty);

        s = ResAbbrevRegex.Replace(s, "residentie ");
        s = NonWordRegex.Replace(s, " ");

        foreach (var word in StripWords)
            s = Regex.Replace(s, $@"\b{Regex.Escape(word)}\b", " ", RegexOptions.IgnoreCase);

        return WhitespaceRegex.Replace(s, " ").Trim();
    }

    /// <summary>
    /// Jaccard word-level similarity tussen twee genormaliseerde namen [0,1].
    /// </summary>
    public static double Similarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;
        if (a == b) return 1.0;
        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal))
            return 0.90;

        var setA = new HashSet<string>(a.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var setB = new HashSet<string>(b.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (setA.Count == 0 || setB.Count == 0) return 0.0;

        var intersection = setA.Count(w => setB.Contains(w));
        var union = setA.Count + setB.Count - intersection;
        return (double)intersection / union;
    }

    /// <summary>
    /// Geeft true als één naam een fase-variant is van de andere
    /// ("Green Yard" vs "Green Yard I", "Green Yard I" vs "Green Yard II").
    /// Fases mogen nooit als Exact worden geclassificeerd.
    /// </summary>
    public static bool IsPhaseVariant(string normA, string normB)
    {
        if (string.IsNullOrEmpty(normA) || string.IsNullOrEmpty(normB)) return false;
        if (normA == normB) return false;

        var (shorter, longer) = normA.Length < normB.Length ? (normA, normB) : (normB, normA);
        if (!longer.StartsWith(shorter, StringComparison.Ordinal)) return false;

        var suffix = longer[shorter.Length..].Trim();
        return suffix.Length > 0 && PhaseTokenRegex.IsMatch(suffix);
    }

    private static string RemoveAccents(string s)
    {
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
