using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

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
                invoice.SeriesId = seriesId;
                invoice.FiscalYear = fiscalYear;
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

        private static readonly Regex PatternTokenRegex = new(@"\{(num|date):([^}]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string FormatPattern(string? pattern, int num, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = "{num:0000}/{date:yyyy}";

            var builder = new StringBuilder();
            var cursor = 0;

            foreach (Match token in PatternTokenRegex.Matches(pattern))
            {
                builder.Append(pattern, cursor, token.Index - cursor);

                var type = token.Groups[1].Value;
                var format = token.Groups[2].Value;

                if (type.Equals("num", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var fmt = string.IsNullOrWhiteSpace(format) ? null : format;
                        builder.Append(fmt is null
                            ? num.ToString(CultureInfo.InvariantCulture)
                            : num.ToString(fmt, CultureInfo.InvariantCulture));
                    }
                    catch (FormatException)
                    {
                        builder.Append(num.ToString(CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    try
                    {
                        builder.Append(date.ToString(format, CultureInfo.InvariantCulture));
                    }
                    catch (FormatException)
                    {
                        builder.Append(date.ToString("yyyy", CultureInfo.InvariantCulture));
                    }
                }

                cursor = token.Index + token.Length;
            }

            if (cursor < pattern.Length)
                builder.Append(pattern, cursor, pattern.Length - cursor);

            return builder.ToString();
        }

    }

}
