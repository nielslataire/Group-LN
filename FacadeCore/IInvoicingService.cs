using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;


namespace FacadeCore
{
    public interface IInvoicingService
    {
        GetResponse<InvoiceBO> GetInvoices();
        GetResponse<InvoiceBO> GetClientInvoices(int id, int itype = 1);

        //GetResponse<InvoiceBO> GetInvoiceByID(int id);
        GetResponse<InvoiceFileBO> GetInvoiceFileByFilename(string name);
     
    }
    public interface IInvoiceQueryService
    {
        Task<IReadOnlyList<InvoiceListItemBO>> GetAllAsync(CancellationToken ct = default);
        //Filter op bedrijf
        Task<IReadOnlyList<InvoiceListItemBO>> GetByCompanyAsync(int issuerCompanyId, CancellationToken ct = default);
        //Filter op project — recente vorderingsstaten voor de Detail-hub.
        Task<IReadOnlyList<InvoiceListItemBO>> GetByProjectAsync(int projectId, CancellationToken ct = default);
        Task<InvoiceDetailBO> GetDetailAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>Openstaand/vervallen-overzicht voor de Boekhouding- en CeoCfo-dashboards.</summary>
        Task<InvoiceDashboardSummaryBO> GetDashboardSummaryAsync(CancellationToken ct = default);

        /// <summary>Openstaand/vervallen-overzicht voor één project (Detail-hub).</summary>
        Task<InvoiceDashboardSummaryBO> GetDashboardSummaryForProjectAsync(int projectId, CancellationToken ct = default);
    }
}

