using FacadeCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.IncomingInvoices
{
    public interface IOctopusIncomingInvoiceSyncService
    {
        Task<IncomingInvoiceSyncResultVm> SyncAsync(int issuerCompanyId, DateTime? modifiedSinceOverride = null, CancellationToken ct = default);
    }
}
