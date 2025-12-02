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
        Task<IReadOnlyList<VatTypeBO>> ListVatTypeAsync(int issuerId, CancellationToken ct = default);
        Task SyncVatTypesAsync(int issuerId, IEnumerable<VatTypeBO> vatTypes, CancellationToken ct = default);
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
        public int IssuerCompanyId { get; set; }
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public int Type { get; set; }
        public decimal BasePercentage { get; set; }
        public int? DefaultSellBookingAccountNr { get; set; }
    }
}
