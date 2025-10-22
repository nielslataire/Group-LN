using BOCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacadeCore
{
    public interface IConnectionSettlementService
    {
        /// <summary>
        /// Verdeel inkomende aansluitkosten over units (via ProjectConnectionKey.Weight),
        /// trek klant-voorschotten af en maak klantfacturen (mode Utilities).
        /// </summary>
        Task<ConnectionSettlementResult> CreateSettlementAsync(
            int projectId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? periodLabel,
            int issuerCompanyId,          // wie reikt de klantfacturen uit
            bool issueInvoicesNow,
            CancellationToken ct = default);

        /// <summary>
        /// Zelfde berekening als Create, maar zonder schrijven (geen settlement/facturen).
        /// </summary>
        Task<ConnectionSettlementResult> PreviewSettlementAsync(
            int projectId,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken ct = default);
    }

    /// <summary>Resultaat van een preview of run.</summary>
    public sealed record ConnectionSettlementResult(
        int SettlementId,
        decimal PoolAmount,
        IReadOnlyDictionary<int, decimal> AllocPerClient,   // ClientAccountId → aandeel vóór voorschotten
        IReadOnlyDictionary<int, decimal> NetPerClient      // ClientAccountId → na aftrek voorschotten
    );

}
