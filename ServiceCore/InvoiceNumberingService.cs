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
using System.Data;

namespace ServiceCore
{
    public class InvoiceNumberingService : IInvoiceNumberingService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;


        public InvoiceNumberingService(UnitOfWorkCore uow) { _uow = uow; _db = (cpmRunningContext)uow.Context; }

        public async Task<(string publicId, int currentNumber)> IssueAsync(
           int invoiceId, int seriesId, DateTime invoiceDate, int fiscalYear,
           CancellationToken ct = default)
        {
            // 1) zoek of maak de sequentie en verhoog ze transactioneel
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var sequence = await _db.InvoiceSequence
                .FirstOrDefaultAsync(x => x.SeriesId == seriesId && x.FiscalYear == fiscalYear, ct);

            int number;
            if (sequence == null)
            {
                number = 1;
                sequence = new InvoiceSequence
                {
                    SeriesId = seriesId,
                    FiscalYear = fiscalYear,
                    CurrentNumber = number
                };
                _uow.InvoiceSequences.Add(sequence);
            }
            else
            {
                number = sequence.CurrentNumber + 1;
                sequence.CurrentNumber = number;
            }

            await _uow.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // 2) format volgens pattern van issuer
            var issuer = await _db.Invoices
                .Include(i => i.IssuerCompany)
                .Where(i => i.Id == invoiceId)
                .Select(i => i.IssuerCompany)
                .FirstAsync(ct);

            var pattern = issuer.InvoiceNumberPattern ?? "{num:0000}/{date:yyyy}";
            var formatted = FormatPattern(pattern, number, invoiceDate);

            // 3) opslaan op factuur
            var invoice = await _db.Invoices.FirstAsync(i => i.Id == invoiceId, ct);
            invoice.PublicId = formatted;
            await _uow.SaveChangesAsync(ct);

            return (formatted, number);
        }

        private static string FormatPattern(string pattern, int num, DateTime date)
        {
            // simpele replace; je kunt dit uitbreiden
            return pattern
                .Replace("{num:0000}", num.ToString("0000"))
                .Replace("{date:yyyy}", date.Year.ToString())
                .Replace("{date:MM-yyyy}", date.ToString("MM-yyyy"));
        }

    }

}
