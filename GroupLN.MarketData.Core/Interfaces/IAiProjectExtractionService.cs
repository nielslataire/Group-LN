using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IAiProjectExtractionService
{
    /// <summary>
    /// Extraheert projectinformatie via de Anthropic API.
    /// Controleert de cache eerst (op InputHash). Fail-safe: null bij fout.
    /// </summary>
    Task<AiProjectExtractionResult?> ExtractAsync(
        AiProjectExtractionInput input,
        CancellationToken ct);
}
