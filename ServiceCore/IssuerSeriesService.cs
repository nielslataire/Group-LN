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
    public class IssuerSeriesService : IIssuerSeriesService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public IssuerSeriesService(UnitOfWorkCore uow) { _uow = uow; _db = (cpmRunningContext)uow.Context; }

        public async Task<IReadOnlyList<InvoiceSeriesBO>> ListByIssuerAsync(int issuerCompanyId, CancellationToken ct = default)
            => await _db.InvoiceSeries.AsNoTracking()
                .Where(s => s.IssuerCompanyId == issuerCompanyId)
                .OrderByDescending(s => s.IsActive).ThenBy(s => s.Code)
                .Select(s => new InvoiceSeriesBO { Id = s.Id, IssuerCompanyId = s.IssuerCompanyId, Code = s.Code, Description = s.Description, IsCreditNote = s.IsCreditNote, IsActive = s.IsActive })
                .ToListAsync(ct);

        public async Task<InvoiceSeriesBO?> GetAsync(int id, CancellationToken ct = default)
            => await _db.InvoiceSeries.AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new InvoiceSeriesBO { Id = s.Id, IssuerCompanyId = s.IssuerCompanyId, Code = s.Code, Description = s.Description, IsCreditNote = s.IsCreditNote, IsActive = s.IsActive })
                .FirstOrDefaultAsync(ct);

        public async Task<int> CreateAsync(InvoiceSeriesBO bo, CancellationToken ct = default)
        {
            var e = new InvoiceSeries { IssuerCompanyId = bo.IssuerCompanyId, Code = bo.Code.Trim(), Description = bo.Description, IsCreditNote = bo.IsCreditNote, IsActive = true };
            _uow.InvoiceSeries.Add(e);
            await _uow.SaveChangesAsync(ct);
            return e.Id;
        }

        public async Task UpdateAsync(InvoiceSeriesBO bo, CancellationToken ct = default)
        {
            var e = await _db.InvoiceSeries.FirstAsync(x => x.Id == bo.Id, ct);
            e.Code = bo.Code.Trim();
            e.Description = bo.Description;
            e.IsCreditNote = bo.IsCreditNote;
            e.IsActive = bo.IsActive;
            await _uow.SaveChangesAsync(ct);
        }
        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.InvoiceSeries.FirstAsync(x => x.Id == id, ct);
            _uow.InvoiceSeries.Remove(e);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task DisableAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.InvoiceSeries.FirstAsync(x => x.Id == id, ct);
            e.IsActive = false;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<InvoiceSequenceBO>> ListSequencesAsync(int seriesId, CancellationToken ct = default)
           => await _db.InvoiceSequence.AsNoTracking()
               .Include(x => x.Bookyear)
               .Include(x => x.Journal)
               .Where(x => x.SeriesId == seriesId)
               .OrderByDescending(x => x.FiscalYear)
               .Select(x => new InvoiceSequenceBO
               {
                   Id = x.Id,
                   SeriesId = x.SeriesId,
                   BookyearId = x.BookyearId,
                   JournalId = x.JournalId,
                   FiscalYear = x.FiscalYear,
                   CurrentNumber = x.CurrentNumber,
                   BookyearDescription = x.Bookyear != null ? x.Bookyear.BookyearDescription : string.Empty,
                   JournalName = x.Journal != null ? x.Journal.Name : string.Empty
               })
               .ToListAsync(ct);

        public async Task<int> CreateSequenceAsync(int seriesId, int fiscalYear, int startAt, int? bookyearId = null, int? journalId = null, CancellationToken ct = default)
        {
            var e = new InvoiceSequence { SeriesId = seriesId, FiscalYear = fiscalYear, CurrentNumber = startAt, BookyearId = bookyearId, JournalId = journalId };
            _uow.InvoiceSequences.Add(e);
            await _uow.SaveChangesAsync(ct);
            return e.Id;
        }

        public async Task UpdateSequenceAsync(int id, int currentNumber, int? bookyearId = null, int? journalId = null, CancellationToken ct = default)
        {
            var e = await _db.InvoiceSequence.FirstAsync(x => x.Id == id, ct);
            e.CurrentNumber = currentNumber;
            e.BookyearId = bookyearId;
            e.JournalId = journalId;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task DeleteSequenceAsync(int id, CancellationToken ct = default)
        {
            var seq = await _db.InvoiceSequence.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (seq == null) throw new KeyNotFoundException("Sequence niet gevonden.");

            if (await IsSequenceInUseAsync(seq, ct))
                throw new InvalidOperationException("Deze sequentie is in gebruik door facturen.");

            _uow.InvoiceSequences.Remove(seq);
            await _uow.SaveChangesAsync(ct);
        }



        // ========== helper ==========
        private async Task<bool> IsSequenceInUseAsync(InvoiceSequence seq, CancellationToken ct)
        {
            // 1) rechtstreekse FK? (InvoiceSequenceId / SequenceId)
            try
            {
                if (await _db.Invoices.AnyAsync(i => EF.Property<int?>(i, "InvoiceSequenceId") == seq.Id, ct)) return true;
            }
            catch { /* property bestaat niet in jouw model; negeren */ }

            try
            {
                if (await _db.Invoices.AnyAsync(i => EF.Property<int?>(i, "SequenceId") == seq.Id, ct)) return true;
            }
            catch { }

            // 2) afleiding via Series + FiscalYear (kolom FiscalYear)
            try
            {
                if (await _db.Invoices.AnyAsync(i =>
                    (EF.Property<int?>(i, "SeriesId") == seq.SeriesId || EF.Property<int?>(i, "SeriesID") == seq.SeriesId) &&
                    EF.Property<int?>(i, "FiscalYear") == seq.FiscalYear, ct)) return true;
            }
            catch { }

            // 3) afleiding via Series + jaar(Invoicedate)
            try
            {
                if (await _db.Invoices.AnyAsync(i =>
                    (EF.Property<int?>(i, "SeriesId") == seq.SeriesId || EF.Property<int?>(i, "SeriesID") == seq.SeriesId) &&
                    EF.Property<DateTime?>(i, "Invoicedate").Value.Year == seq.FiscalYear, ct)) return true;
            }
            catch { }

            return false;
        }
    }

}
