using CPMCore.Models.Instellingen;

namespace CPMCore.Services;

public interface IMarketDataStatusService
{
    Task<MarketDataStatusModel> GetStatusAsync(int recenteRunsAantal = 50, CancellationToken ct = default);
}
