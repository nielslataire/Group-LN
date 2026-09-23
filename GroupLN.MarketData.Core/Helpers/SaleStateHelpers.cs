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
}
