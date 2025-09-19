using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IIssuerSeriesService
    {
        Task<IReadOnlyList<InvoiceSeriesBO>> ListByIssuerAsync(int issuerCompanyId, CancellationToken ct = default);
        Task<InvoiceSeriesBO?> GetAsync(int id, CancellationToken ct = default);
        Task<int> CreateAsync(InvoiceSeriesBO bo, CancellationToken ct = default);
        Task UpdateAsync(InvoiceSeriesBO bo, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);   // hard delete
        Task DisableAsync(int id, CancellationToken ct = default);  // soft delete (IsActive = false)

        Task<IReadOnlyList<InvoiceSequenceBO>> ListSequencesAsync(int seriesId, CancellationToken ct = default);
        Task<int> CreateSequenceAsync(int seriesId, int fiscalYear, int startAt, CancellationToken ct = default);
        Task UpdateSequenceAsync(int id, int currentNumber, CancellationToken ct = default);
        Task DeleteSequenceAsync(int id, CancellationToken ct = default);
    }
}
