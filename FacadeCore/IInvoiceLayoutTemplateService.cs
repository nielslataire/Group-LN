using BOCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FacadeCore;

public interface IInvoiceLayoutTemplateService
{
    Task EnsureDefaultsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceLayoutTemplateBO>> ListAsync(CancellationToken ct = default);
    Task<InvoiceLayoutTemplateBO?> GetAsync(int id, CancellationToken ct = default);
    Task<InvoiceLayoutTemplateBO?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<int> CreateAsync(InvoiceLayoutTemplateBO bo, CancellationToken ct = default);
    Task UpdateAsync(InvoiceLayoutTemplateBO bo, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}