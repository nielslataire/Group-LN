using BOCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FacadeCore
{
    public interface ICookieConsentStatsService
    {
        Task<CookieConsentStatsBO> GetStatsAsync(DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken ct = default);
    }
}
