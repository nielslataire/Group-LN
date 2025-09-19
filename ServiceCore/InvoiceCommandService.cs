using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class InvoiceCommandService : IInvoiceCommandService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;
        private readonly IInvoiceNumberingService _num; 

        public InvoiceCommandService(UnitOfWorkCore uow) { _uow = uow; _db = (cpmRunningContext)uow.Context; }

        public async Task<(int invoiceId, string? publicId)> CreateWithLinesAsync(InvoiceDraftBO bo, bool issueNow, CancellationToken ct = default)
        {
            // issuer + default term
            var issuer = await _db.IssuerCompany.FirstAsync(x => x.Id == bo.IssuerCompanyId && x.IsActive, ct);
            int? defaultDays = null;
            if (issuer.DefaultPaymentTermId.HasValue)
                defaultDays = await _db.PaymentTerms.Where(t => t.Id == issuer.DefaultPaymentTermId.Value).Select(t => (int?)t.Days).FirstOrDefaultAsync(ct);

            var due = bo.ExpirationDate ?? (defaultDays.HasValue ? bo.InvoiceDate.AddDays(defaultDays.Value) : (DateOnly?)null);

            // check partij (zacht, FK’s doen de rest)
            if (bo.CompanyId.HasValue)
                await _db.CompanyInfo.FirstAsync(s => s.CompanyId == bo.CompanyId.Value, ct);
            else
            {
                if (bo.ClientType is 1)
                    await _db.ClientAccount.FirstAsync(c => c.Id == bo.ClientId, ct);
                else if (bo.ClientType is 2)
                    await _db.ClientContacts.FirstAsync(c => c.Id == bo.ClientId, ct);
                else
                    throw new InvalidOperationException("Kies een klant (type 1/2) of leverancier.");
            }

            // status ids
            var draftId = await _db.InvoiceStatusLookup.Where(s => s.Name == "Draft").Select(s => (byte?)s.Id).FirstOrDefaultAsync(ct) ?? (byte)1;
            var issuedId = await _db.InvoiceStatusLookup.Where(s => s.Name == "Issued").Select(s => (byte?)s.Id).FirstOrDefaultAsync(ct) ?? (byte)2;

            await using var tx = await _uow.BeginTransactionAsync(ct);
            try
            {
                var inv = new Invoices
                {
                    IssuerCompanyId = bo.IssuerCompanyId,
                    Date = bo.InvoiceDate,
                    ExpirationDate = due,
                    StatusId = draftId,
                    PublicId = null,
                    ClientType = bo.CompanyId.HasValue ? null : bo.ClientType,
                    ClientId = bo.CompanyId.HasValue ? null : bo.ClientId,
                    CompanyId = bo.CompanyId
                };
                _uow.Invoices.Add(inv);
                await _uow.SaveChangesAsync(ct); // Id is nu bekend

                // lines
                foreach (var l in bo.Lines)
                {
                    // normaliseer: als enkel % is opgegeven, reken bedrag uit; als enkel bedrag is opgegeven, reken % uit
                    decimal? discAmt = l.DiscountAmount;
                    decimal? discPct = l.DiscountPercent;

                    if (discPct.HasValue && !discAmt.HasValue)
                        discAmt = Math.Round(l.Price * (discPct.Value / 100m), 4, MidpointRounding.AwayFromZero);
                    else if (discAmt.HasValue && !discPct.HasValue && l.Price != 0)
                        discPct = Math.Round((discAmt.Value / l.Price) * 100m, 4, MidpointRounding.AwayFromZero);

                    _uow.InvoiceDetails.Add(new InvoicesDetails
                    {
                        InvoiceId = inv.Id,
                        Text = (l.Text ?? "").Trim().Length > 200 ? (l.Text ?? "").Trim().Substring(0, 200) : (l.Text ?? "").Trim(),
                        Price = l.Price,
                        VatPercentage = l.VatPercentage,
                        DiscountPercent = discPct,
                        DiscountAmount = discAmt,
                        UnitId = l.UnitId,
                        PaymentStageId = l.PaymentStageId,
                        LineType = string.IsNullOrWhiteSpace(l.LineType) ? null : l.LineType.Trim(),
                        GroupName = string.IsNullOrWhiteSpace(l.GroupName) ? null : l.GroupName.Trim(),
                        UtilityCost = l.UtilityCost,
                        // ConstructionValued / ChangeOrderDetailId laat ik null staan (geen info nu)
                    });
                }
                await _uow.SaveChangesAsync(ct);

                string? publicId = null;
                if (issueNow)
                {
                    // volgende stap: user kiest reeks; hier pakken we (voor nu) default reeks of 1
                    // Laat dit even staan; in stap 3b voegen we Series-keuze toe.
                    var seriesId = await _db.InvoiceSeries
                        .Where(s => s.IssuerCompanyId == bo.IssuerCompanyId && s.IsActive)
                        .OrderBy(s => s.Id)
                        .Select(s => (int?)s.Id)
                        .FirstOrDefaultAsync(ct)
                        ?? throw new InvalidOperationException("Geen actieve nummerreeks voor dit bedrijf.");

                    var fiscalYear = bo.InvoiceDate.Year;
                    var issue = await _num.IssueAsync(inv.Id, seriesId, new DateTime(bo.InvoiceDate.Year, bo.InvoiceDate.Month, bo.InvoiceDate.Day), fiscalYear, ct);
                    publicId = issue.publicId;

                    inv.StatusId = issuedId;
                    inv.PublicId = publicId;
                    await _uow.SaveChangesAsync(ct);
                }

                await _uow.CommitTransactionAsync(_uow.CurrentTransaction!, ct);
                return (inv.Id, publicId);
            }
            catch
            {
                await _uow.RollbackTransactionAsync(_uow.CurrentTransaction!, ct);
                throw;
            }
        }


    }

}
