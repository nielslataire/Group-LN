using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Helpers;

/// <summary>
/// Eén plek waar "verkocht", "beschikbaar" en "gereserveerd" bepaald worden uit de
/// combinatie van de bronstatus (SaleStatus) en de afgeleide lifecycle (LifecycleStatus).
///
/// Waarom beide: een projectunit die uit de inventaris verdwijnt houdt zijn laatste
/// SaleStatus (meestal Available) maar krijgt LifecycleStatus=LikelySold. Zonder deze
/// helper telt zo'n unit als "beschikbaar" en valt de verkoopgraad te laag uit.
/// </summary>
public static class SaleStateHelpers
{
    public static bool IsSoldLifecycle(AssetLifecycleStatus? lifecycle) =>
        lifecycle is AssetLifecycleStatus.SoldConfirmed or AssetLifecycleStatus.LikelySold;

    /// <summary>Verkocht volgens bron óf afgeleid (verdwenen uit aanbod).</summary>
    public static bool IsSold(SaleStatus? saleStatus, AssetLifecycleStatus? lifecycle) =>
        saleStatus == SaleStatus.Sold || IsSoldLifecycle(lifecycle);

    /// <summary>Gereserveerd of in optie, en niet intussen verkocht.</summary>
    public static bool IsReserved(SaleStatus? saleStatus, AssetLifecycleStatus? lifecycle) =>
        !IsSold(saleStatus, lifecycle)
        && saleStatus is SaleStatus.Reserved or SaleStatus.Option;

    /// <summary>
    /// Nog te koop: niet verkocht, niet gereserveerd. Een ontbrekende bronstatus
    /// (losse listings hebben er geen) telt als beschikbaar zolang de lifecycle
    /// niet het tegendeel zegt.
    /// </summary>
    public static bool IsAvailable(SaleStatus? saleStatus, AssetLifecycleStatus? lifecycle) =>
        !IsSold(saleStatus, lifecycle)
        && saleStatus is null or SaleStatus.Unknown or SaleStatus.Available;

    public static bool IsSold(MarketAsset asset) => IsSold(asset.SaleStatus, asset.LifecycleStatus);
    public static bool IsReserved(MarketAsset asset) => IsReserved(asset.SaleStatus, asset.LifecycleStatus);
    public static bool IsAvailable(MarketAsset asset) => IsAvailable(asset.SaleStatus, asset.LifecycleStatus);

    // ── Verkoopdatum en doorlooptijd ──────────────────────────────────────────

    /// <summary>
    /// Beste schatting van het verkoopmoment: bronbevestigd (FirstSoldAt), anders het
    /// moment waarop de lifecycle op (vermoedelijk) verkocht ging, anders de laatste
    /// statuswissel. Null als het pand niet verkocht is.
    /// </summary>
    public static DateTime? VerkoopDatum(
        SaleStatus? saleStatus,
        AssetLifecycleStatus? lifecycle,
        DateTime? firstSoldAt,
        DateTime? lifecycleStatusUpdatedAt,
        DateTime? statusChangedAt)
    {
        if (!IsSold(saleStatus, lifecycle)) return null;
        return firstSoldAt
            ?? (IsSoldLifecycle(lifecycle) ? lifecycleStatusUpdatedAt : null)
            ?? (saleStatus == SaleStatus.Sold ? statusChangedAt : null);
    }

    public static DateTime? VerkoopDatum(MarketAsset asset) =>
        VerkoopDatum(asset.SaleStatus, asset.LifecycleStatus, asset.FirstSoldAt, asset.LifecycleStatusUpdatedAt, asset.StatusChangedAt);

    /// <summary>
    /// Dagen tussen eerste waarneming en verkoop. Null als niet verkocht, of als het pand
    /// al verkocht was bij de eerste crawl (minder dan één dag): dan is de echte
    /// doorlooptijd onbekend en zou 0 de mediaan vertekenen.
    /// </summary>
    public static int? DoorlooptijdDagen(DateTime firstSeenAt, DateTime? verkoopDatum)
    {
        if (!verkoopDatum.HasValue) return null;
        var dagen = (verkoopDatum.Value - firstSeenAt).TotalDays;
        return dagen >= 1 ? (int)Math.Floor(dagen) : null;
    }

    public static int? DoorlooptijdDagen(MarketAsset asset) =>
        DoorlooptijdDagen(asset.FirstSeenAt, VerkoopDatum(asset));

    /// <summary>Mediaan van een reeks doorlooptijden; null bij een lege reeks.</summary>
    public static int? Mediaan(IEnumerable<int> waarden)
    {
        var lijst = waarden.OrderBy(v => v).ToList();
        if (lijst.Count == 0) return null;
        var mid = lijst.Count / 2;
        return lijst.Count % 2 == 1 ? lijst[mid] : (lijst[mid - 1] + lijst[mid]) / 2;
    }
}
