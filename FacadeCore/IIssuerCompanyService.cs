using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IIssuerCompanyService
    {
        Task<IReadOnlyList<IssuerCompanyBO>> GetAllAsync(CancellationToken ct = default);
        Task<IssuerCompanyBO> GetAsync(int id, CancellationToken ct = default);
        Task<int> CreateAsync(IssuerCompanyBO bo, CancellationToken ct = default);
        Task UpdateAsync(IssuerCompanyBO bo, CancellationToken ct = default);
        Task DisableAsync(int id, CancellationToken ct = default); // soft delete: IsActive = false

        Task<IReadOnlyList<PaymentTermOptionBO>> GetPaymentTermOptionsAsync(CancellationToken ct = default);
    }
}
