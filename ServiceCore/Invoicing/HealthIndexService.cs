using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.Invoicing
{
    /// <summary>
    /// De gezondheidsindex van Statbel (open data, JSON met de laatste 13 maanden in vier basisjaren). Elke maand komt er een nieuwe bij; we bewaren alles wat we ooit
    /// opgehaald hebben, zodat de historiek langer wordt dan de 13 maanden die Statbel zelf toont. Het CRM rekent met basis 2013 = 100.
    /// </summary>
    public class HealthIndexService : IHealthIndexService
    {
        public const string Url = "https://bestat.statbel.fgov.be/bestat/api/views/e8fad1c0-5270-4d62-9118-0999ee8cb323/result/JSON";
        private readonly cpmRunningContext _db;
        private readonly HttpClient _http;
        private readonly ILogger<HealthIndexService> _logger;

        public HealthIndexService(UnitOfWorkCore uow, HttpClient http, ILogger<HealthIndexService> logger)
        {
            _db = (cpmRunningContext)uow.Context;
            _http = http;
            _logger = logger;
        }

        public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetAsync(CancellationToken ct = default)
            => await _db.HealthIndexValue.AsNoTracking().Where(h => h.BaseYear == HealthIndexValue.CalcBase).ToDictionaryAsync(h => h.Month, h => h.Value, ct);

        public async Task<(DateOnly? Latest, DateTime? FetchedAt)> StatusAsync(CancellationToken ct = default)
        {
            var rows = _db.HealthIndexValue.AsNoTracking().Where(h => h.BaseYear == HealthIndexValue.CalcBase);
            var latest = await rows.OrderByDescending(h => h.Month).Select(h => (DateOnly?)h.Month).FirstOrDefaultAsync(ct);
            var fetched = await rows.MaxAsync(h => (DateTime?)h.FetchedAt, ct);
            return (latest, fetched);
        }

        public async Task<HealthIndexRefreshResult> RefreshAsync(CancellationToken ct = default)
        {
            try
            {
                using var response = await _http.GetAsync(Url, ct);
                response.EnsureSuccessStatusCode();
                var parsed = Parse(await response.Content.ReadAsStringAsync(ct));
                if (parsed.Count == 0)
                    return new HealthIndexRefreshResult(false, "Statbel gaf geen gezondheidsindex terug.", 0, null);

                var existing = await _db.HealthIndexValue.ToListAsync(ct);
                var byKey = existing.ToDictionary(e => (e.Month, e.BaseYear));
                var changed = 0;
                foreach (var p in parsed)
                {
                    if (byKey.TryGetValue((p.Month, p.BaseYear), out var row))
                    {
                        if (row.Value == p.Value) continue;
                        row.Value = p.Value; row.FetchedAt = DateTime.UtcNow; changed++;
                    }
                    else
                    {
                        _db.HealthIndexValue.Add(new HealthIndexValue { Month = p.Month, BaseYear = p.BaseYear, Value = p.Value, FetchedAt = DateTime.UtcNow });
                        changed++;
                    }
                }
                await _db.SaveChangesAsync(ct);
                var latest = parsed.Where(p => p.BaseYear == HealthIndexValue.CalcBase).Select(p => (DateOnly?)p.Month).Max();
                return new HealthIndexRefreshResult(true, null, changed, latest);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gezondheidsindex ophalen bij Statbel mislukt");
                return new HealthIndexRefreshResult(false, "De gezondheidsindex kon niet opgehaald worden bij Statbel.", 0, null);
            }
        }

        /// <summary>Leest de JSON van Statbel: { facts: [ { Month: "October 2025", "Level 1": "Health index", "Base year": "2013 = 100", "Consumer price index": 135.76 } ] }.</summary>
        public static List<(DateOnly Month, short BaseYear, decimal Value)> Parse(string json)
        {
            var list = new List<(DateOnly, short, decimal)>();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("facts", out var facts)) return list;
            foreach (var f in facts.EnumerateArray())
            {
                if (!f.TryGetProperty("Level 1", out var level) || !string.Equals(level.GetString(), "Health index", StringComparison.OrdinalIgnoreCase)) continue;
                if (!DateTime.TryParseExact(f.GetProperty("Month").GetString(), "MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)) continue;
                var baseText = f.GetProperty("Base year").GetString() ?? "";
                if (!short.TryParse(baseText.Split(' ')[0], out var baseYear)) continue;
                if (!f.TryGetProperty("Consumer price index", out var v) || v.ValueKind != JsonValueKind.Number) continue;
                list.Add((new DateOnly(month.Year, month.Month, 1), baseYear, Math.Round(v.GetDecimal(), 2, MidpointRounding.AwayFromZero)));
            }
            return list;
        }
    }
}
