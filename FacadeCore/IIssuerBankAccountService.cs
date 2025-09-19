using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IIssuerBankAccountService
    {
        Task<IReadOnlyList<IssuerBankAccountBO>> ListByIssuerAsync(int issuerCompanyId, CancellationToken ct = default);
        Task<IssuerBankAccountBO?> GetAsync(int id, CancellationToken ct = default);
        Task<int> CreateAsync(IssuerBankAccountBO bo, CancellationToken ct = default);
        Task UpdateAsync(IssuerBankAccountBO bo, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task SetDefaultAsync(int id, CancellationToken ct = default);
    }
}
