using BOCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ServiceCore.Translators;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var ownsTransaction = _db.Database.CurrentTransaction == null;
            IDbContextTransaction? startedTx = null;
            if (ownsTransaction)
            {
                startedTx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            }

            try
            {
                InvoiceSequence? sequence;
                if (ownsTransaction)
                {
                    sequence = await _db.InvoiceSequence
                        .FromSqlRaw(
                            "SELECT TOP (1) * FROM InvoiceSequence WITH (UPDLOCK, HOLDLOCK) WHERE SeriesId = {0} AND FiscalYear = {1}",
                            seriesId,
                            fiscalYear)
                        .AsTracking()
                        .FirstOrDefaultAsync(ct);
                }
                else
                {
                    sequence = await _db.InvoiceSequence
                        .FirstOrDefaultAsync(x => x.SeriesId == seriesId && x.FiscalYear == fiscalYear, ct);
                }

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

                if (ownsTransaction && startedTx is not null)
                    await startedTx.CommitAsync(ct);

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
            catch
            {
                if (ownsTransaction && startedTx is not null)
                    await startedTx.RollbackAsync(ct);
                throw;
            }
            finally
            {
                if (ownsTransaction && startedTx is not null)
                    await startedTx.DisposeAsync();
            }
        }

        private static string FormatPattern(string pattern, int num, DateTime date)
        {
            // simpele replace; je kunt dit uitbreiden
            return pattern
                .Replace("{num:0000}", num.ToString("0000"))
                .Replace("{date:MM}", date.ToString("MM")  )    // 01..12
                .Replace("{date:MMM}", date.ToString("MMM"))    // jan..dec
                .Replace("{date:MMMM}", date.ToString("MMMM"))  // januari..december
                .Replace("{date:yy}", date.ToString("yy"))      // 00..99
                .Replace("{date:yyyy}", date.ToString("yyyy")) // 4-cijferig jaar
                .Replace("{date:MM-yyyy}", date.ToString("MM-yyyy"));
        }

    }

}
