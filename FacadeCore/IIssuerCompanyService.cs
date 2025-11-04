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
        Task<IReadOnlyList<IssuerListItemBO>> ListActiveIssuersAsync(CancellationToken ct = default);
        Task<int?> GetFirstActiveIssuerIdAsync(CancellationToken ct = default);
        Task<IReadOnlyList<PaymentTermBO>> ListPaymentTermsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<VatTypeBO>> ListVatTypeAsync(CancellationToken ct = default);
        Task<IReadOnlyList<CompanyLegalFormBO>> ListLegalFormsAsync(CancellationToken ct = default);
    }
    // Lichte BO’s voor de UI-lijsten
    public class IssuerListItemBO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? DefaultPaymentTermId { get; set; }
        public int? DefaultVatTypeId { get; set; }
    }

    public class PaymentTermBO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Days { get; set; }
    }
    public class VatTypeBO
    {
        public int Id { get; set; }
        public int VATPercentage { get; set; }
        public string VATText { get; set; } ="";
    }
}
