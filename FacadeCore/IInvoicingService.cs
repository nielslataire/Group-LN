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
        /// <summary>Zelfde lijst, beperkt tot facturen met een factuurdatum in dit bereik (een boekjaar): veel sneller dan alles laden. Null = geen grens.</summary>
        Task<IReadOnlyList<InvoiceListItemBO>> GetByCompanyAsync(int issuerCompanyId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
        //Filter op project — recente vorderingsstaten voor de Detail-hub.
        Task<IReadOnlyList<InvoiceListItemBO>> GetByProjectAsync(int projectId, CancellationToken ct = default);
        Task<InvoiceDetailBO> GetDetailAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>Openstaand/vervallen-overzicht voor de Boekhouding- en CeoCfo-dashboards.</summary>
        Task<InvoiceDashboardSummaryBO> GetDashboardSummaryAsync(CancellationToken ct = default);

        /// <summary>Zelfde cijfers, beperkt tot de opgegeven facturatiebedrijven (dashboard Boekhouding).</summary>
        Task<InvoiceDashboardSummaryBO> GetDashboardSummaryForIssuersAsync(IReadOnlyCollection<int> issuerCompanyIds, CancellationToken ct = default);

        /// <summary>Openstaand/vervallen-overzicht voor één project (Detail-hub).</summary>
        Task<InvoiceDashboardSummaryBO> GetDashboardSummaryForProjectAsync(int projectId, CancellationToken ct = default);
    }
}

