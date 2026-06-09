using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Infrastructure.Deduplication;

internal static class UnitFingerprintBuilder
{
    /// <summary>
    /// Bouwt een fingerprint-string voor een unit.
    /// Oppervlakte afgerond op 5 m², prijs afgerond op €5 000.
    /// </summary>
    public static string Build(
        MarketAsset unit,
        string normalizedProjectName,
        decimal? askingPrice)
    {
        var area = unit.LivingArea.HasValue
            ? (int)(Math.Round(unit.LivingArea.Value / 5.0m) * 5)
            : (int?)null;

        var price = askingPrice.HasValue
            ? (long)(Math.Round(askingPrice.Value / 5_000m) * 5_000)
            : (long?)null;

        return string.Join("|",
            normalizedProjectName,
            unit.PostalCode   ?? "",
            (int)unit.PropertyType,
            (int)unit.PropertySubType,
            area?.ToString()  ?? "",
            unit.Bedrooms?.ToString() ?? "",
            price?.ToString() ?? "",
            unit.Floor?.ToString() ?? "");
    }

    /// <summary>
    /// Berekent het overlap-percentage tussen twee fingerprint-sets.
    /// </summary>
    public static double OverlapRatio(
        IReadOnlyCollection<string> fingerprintsA,
        IReadOnlyCollection<string> fingerprintsB)
    {
        if (fingerprintsA.Count == 0 || fingerprintsB.Count == 0) return 0.0;

        var setB = new HashSet<string>(fingerprintsB, StringComparer.Ordinal);
        var overlap = fingerprintsA.Count(fp => setB.Contains(fp));
        return (double)overlap / Math.Max(fingerprintsA.Count, fingerprintsB.Count);
    }
}
