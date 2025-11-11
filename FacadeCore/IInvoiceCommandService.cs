using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IInvoiceCommandService
    {
        Task<(int invoiceId, string? publicId)> CreateWithLinesAsync(
            InvoiceDraftBO bo,
            bool issueNow,
            CancellationToken ct = default);
        Task<int> CreateDraftAsync(InvoiceDraftBO bo, CancellationToken ct = default);
        Task<string> IssueDraftAsync(int invoiceId, int? seriesId = null, DateOnly? issueDate = null, CancellationToken ct = default);
        Task DeleteAsync(int invoiceId, CancellationToken ct = default);
        Task MarkAsSentAsync(int invoiceId, DateTime sentAtUtc, CancellationToken ct = default);
        Task UpdateAsync(InvoiceUpdateBO bo, CancellationToken ct = default);
        Task UpdateDraftAsync(int invoiceId, InvoiceDraftBO bo, CancellationToken ct = default);

    }
}
