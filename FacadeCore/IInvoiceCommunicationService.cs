using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IInvoiceCommunicationService
    {
        Task<IReadOnlyList<InvoiceEmailLogBO>> GetEmailLogsAsync(int invoiceId, CancellationToken ct = default);
        Task SaveEmailLogAsync(InvoiceEmailLogBO log, CancellationToken ct = default);
        Task<InvoiceUblBO?> GetInvoiceUblAsync(int invoiceId, CancellationToken ct = default);
        Task SaveInvoiceUblAsync(InvoiceUblBO ubl, CancellationToken ct = default);
    }
}