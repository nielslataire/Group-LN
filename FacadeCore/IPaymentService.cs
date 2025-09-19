using BOCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacadeCore
{
    public interface IPaymentService
    {
        Task<PaymentCreateResult> CreatePaymentAsync(DateTime receivedDate, decimal amount, string method, string structuredComm = null, string notes = null, CancellationToken ct = default);
        Task AllocateAsync(int paymentId, int invoiceId, decimal amount, CancellationToken ct = default); // boekt deel van betaling op factuur
        Task DeallocateAsync(int paymentAllocationId, CancellationToken ct = default);                    // haalt allocatie weg
        Task AutoMatchByOGMAsync(string structuredComm, CancellationToken ct = default);                  // optioneel: match betaling op OGM
        Task RecomputeStatusAsync(int invoiceId, CancellationToken ct = default);
    }
}
