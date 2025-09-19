using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    }
}
