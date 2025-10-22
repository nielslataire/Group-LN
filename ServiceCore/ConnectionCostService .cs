using FacadeCore;
using BOCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class ConnectionCostService : IConnectionCostService
    {
        private readonly cpmRunningContext _db;
        public ConnectionCostService(cpmRunningContext db) => _db = db;

        public async Task<IReadOnlyList<ConnectionPoolRow>> GetOpenConnectionCostPoolAsync(
         int projectId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
        {
            var q =
                from d in _db.IncommingInvoiceDetail.AsNoTracking()
                join h in _db.IncommingInvoices.AsNoTracking() on d.IncommingInvoiceId equals h.Id
                join s in _db.CompanyInfo.AsNoTracking() on h.CompanyId equals s.CompanyId
                where h.ProjectId == projectId
                   && d.IsConnectionCost == true
                   && (!fromDate.HasValue || h.Date >= fromDate.Value)
                   && (!toDate.HasValue || h.Date <= toDate.Value)
                // hoeveel van deze detail-lijn werd al opgebruikt in settlements?
                let already = _db.ConnectionSettlementSource
                                  .Where(x => x.InvoiceDetailId == d.Id)
                                  .Sum(x => (decimal?)x.AmountUsedExcl) ?? 0m
                let price = d.Price ?? 0m
                let remaining = price - already
                where remaining > 0m
                select new ConnectionPoolRow(
                    d.Id,
                    h.Id,
                    h.Date,
                    s.BedrijfsNaam,
                    d.Description,
                    remaining
                );

            return await q.OrderBy(x => x.InvoiceDate)
                          .ThenBy(x => x.IncomingInvoiceId)
                          .ToListAsync(ct);
        }
    }
}
