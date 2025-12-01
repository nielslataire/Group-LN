using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class OctopusBookyearService : IOctopusBookyearService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public OctopusBookyearService(UnitOfWorkCore uow)
        {
            _uow = uow;
            _db = (cpmRunningContext)uow.Context;
        }

        public async Task SyncAsync(int issuerId, IEnumerable<OctopusBookyearBO> bookyears, CancellationToken ct = default)
        {
            var existing = await _db.OctopusBookyears
                .Include(x => x.OctopusBookyearPeriods)
                .Include(x => x.OctopusJournals)
                .Where(x => x.IssuerCompanyId == issuerId)
                .ToListAsync(ct);

            var byKey = existing.ToDictionary(x => x.BookyearKeyId);

            foreach (var bo in bookyears)
            {
                if (!byKey.TryGetValue(bo.BookyearKeyId, out var entity))
                {
                    entity = new OctopusBookyears { IssuerCompanyId = issuerId, BookyearKeyId = bo.BookyearKeyId };
                    _uow.OctopusBookyears.Add(entity);
                }

                entity.BookyearDescription = bo.BookyearDescription;
                entity.StartDate = bo.StartDate;
                entity.EndDate = bo.EndDate;
                entity.Closed = bo.Closed;

                // periods sync
                var periodsByNumber = entity.OctopusBookyearPeriods.ToDictionary(p => p.BookyearPeriodNr);
                foreach (var p in bo.Periods)
                {
                    if (!periodsByNumber.TryGetValue(p.BookyearPeriod, out var period))
                    {
                        period = new OctopusBookyearPeriods { Bookyear = entity, BookyearPeriodNr = p.BookyearPeriod };
                        entity.OctopusBookyearPeriods.Add(period);
                    }

                    period.StartDate = p.StartDate;
                    period.EndDate = p.EndDate;
                }

                foreach (var obsolete in entity.OctopusBookyearPeriods.Where(p => !bo.Periods.Any(bp => bp.BookyearPeriod == p.BookyearPeriodNr)).ToList())
                {
                    _uow.OctopusBookyearPeriods.Remove(obsolete);
                }

                // journals sync
                var journalsByKey = entity.OctopusJournals.ToDictionary(j => j.JournalKey);
                foreach (var j in bo.Journals)
                {
                    if (!journalsByKey.TryGetValue(j.JournalKey, out var journal))
                    {
                        journal = new OctopusJournals { Bookyear = entity, JournalKey = j.JournalKey };
                        entity.OctopusJournals.Add(journal);
                    }

                    journal.BookyearKeyId = j.BookyearKeyId;
                    journal.Name = j.Name;
                    journal.Closed = j.Closed;
                    journal.CurrencyCode = j.CurrencyCode;
                    journal.LastBookedDocumentNr = j.LastBookedDocumentNr;
                    journal.ProtectedPeriod = j.ProtectedPeriod;
                    journal.ClosedPeriod = j.ClosedPeriod;
                    journal.InsertionType = j.InsertionType;
                    journal.CustomFieldListJson = j.CustomFieldListJson;
                    journal.CustomFieldLineListJson = j.CustomFieldLineListJson;
                }

                foreach (var obsolete in entity.OctopusJournals.Where(j => !bo.Journals.Any(bj => bj.JournalKey == j.JournalKey)).ToList())
                {
                    _uow.OctopusJournals.Remove(obsolete);
                }
            }

            foreach (var obsolete in existing.Where(e => !bookyears.Any(b => b.BookyearKeyId == e.BookyearKeyId)))
            {
                _uow.OctopusBookyears.Remove(obsolete);
            }

            await _uow.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<OctopusBookyearBO>> ListByIssuerAsync(int issuerId, CancellationToken ct = default)
        {
            var items = await _db.OctopusBookyears
                .AsNoTracking()
                .Include(x => x.OctopusBookyearPeriods)
                .Include(x => x.OctopusJournals)
                .Where(x => x.IssuerCompanyId == issuerId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync(ct);

            return items.Select(Map).ToList();
        }

        public async Task<OctopusBookyearBO?> GetAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.OctopusBookyears
                .AsNoTracking()
                .Include(x => x.OctopusBookyearPeriods)
                .Include(x => x.OctopusJournals)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            return entity == null ? null : Map(entity);
        }

        private static OctopusBookyearBO Map(OctopusBookyears entity)
        {
            return new OctopusBookyearBO
            {
                Id = entity.Id,
                IssuerCompanyId = entity.IssuerCompanyId,
                BookyearKeyId = entity.BookyearKeyId,
                BookyearDescription = entity.BookyearDescription,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Closed = entity.Closed,
                Periods = entity.OctopusBookyearPeriods.Select(p => new OctopusBookyearPeriodBO
                {
                    Id = p.Id,
                    BookyearId = p.BookyearId,
                    BookyearPeriod = p.BookyearPeriodNr,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                }).ToList(),
                Journals = entity.OctopusJournals.Select(j => new OctopusJournalBO
                {
                    Id = j.Id,
                    BookyearId = j.BookyearId,
                    BookyearKeyId = j.BookyearKeyId,
                    JournalKey = j.JournalKey,
                    Name = j.Name,
                    Closed = j.Closed,
                    CurrencyCode = j.CurrencyCode ?? string.Empty,
                    LastBookedDocumentNr = j.LastBookedDocumentNr,
                    ProtectedPeriod = j.ProtectedPeriod,
                    ClosedPeriod = j.ClosedPeriod,
                    InsertionType = j.InsertionType,
                    CustomFieldListJson = j.CustomFieldListJson ?? string.Empty,
                    CustomFieldLineListJson = j.CustomFieldLineListJson ?? string.Empty
                }).ToList()
            };
        }
    }
}