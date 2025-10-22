using BOCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacadeCore
{
    public interface IConnectionCostService
    {
        /// <summary>
        /// Haalt alle open inkomende lijnen die als Aansluitkost gemarkeerd zijn
        /// en nog niet (volledig) verrekend werden in een settlement.
        /// </summary>
        Task<IReadOnlyList<ConnectionPoolRow>> GetOpenConnectionCostPoolAsync(
            int projectId,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken ct = default);
    }
    public sealed record ConnectionPoolRow(
    int DetailId,
    int IncomingInvoiceId,
    DateOnly InvoiceDate,
    string SupplierName,
    string LineText,
    decimal RemainingExcl
);
}
