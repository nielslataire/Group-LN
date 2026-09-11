using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class CookieConsentStatsService : ICookieConsentStatsService
    {
        private readonly cpmRunningContext _db;

        public CookieConsentStatsService(UnitOfWorkCore uow)
        {
            _db = (cpmRunningContext)uow.Context;
        }

        public async Task<CookieConsentStatsBO> GetStatsAsync(DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default)
        {
            var counts = await _db.CookieConsentEvent.AsNoTracking()
                .Where(e => e.OccurredAtUtc >= periodStartUtc && e.OccurredAtUtc < periodEndUtc)
                .GroupBy(e => e.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return new CookieConsentStatsBO
            {
                PeriodStartUtc = periodStartUtc,
                PeriodEndUtc = periodEndUtc,
                ShownCount = counts.FirstOrDefault(c => c.EventType == "Shown")?.Count ?? 0,
                AcceptedCount = counts.FirstOrDefault(c => c.EventType == "Accepted")?.Count ?? 0,
                RejectedCount = counts.FirstOrDefault(c => c.EventType == "Rejected")?.Count ?? 0
            };
        }
    }
}
