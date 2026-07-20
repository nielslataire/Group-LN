namespace GroupLN.MarketData.Core.Settings;

/// <summary>
/// Hulpfuncties voor de "0 = onbeperkt" conventie op Max*-configuratievelden.
/// </summary>
public static class LimitExtensions
{
    /// <summary>true wanneer de limiet 0 of negatief is (= onbeperkt).</summary>
    public static bool IsUnlimited(this int max) => max <= 0;

    /// <summary>Geeft int.MaxValue terug wanneer onbeperkt, anders de waarde zelf.</summary>
    public static int ToEffectiveMax(this int max) => max > 0 ? max : int.MaxValue;

    /// <summary>Past Take(max) toe wanneer max > 0; anders: geeft de volledige reeks terug.</summary>
    public static IEnumerable<T> WithLimit<T>(this IEnumerable<T> source, int max) =>
        max > 0 ? source.Take(max) : source;

    /// <summary>Geeft "onbeperkt" terug wanneer max &lt;= 0, anders de waarde als string.</summary>
    public static string ToLimitLabel(this int max) => max > 0 ? max.ToString() : "onbeperkt";
}
