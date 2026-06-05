using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Persistence.SeedData;

public static class CrawlerSourceSeed
{
    public static IEnumerable<CrawlerSource> GetSeedData()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return new[]
        {
            // ── Fase 1 actief ───────────────────────────────────────────────
            new CrawlerSource
            {
                Id = 1,
                Name = "Immoweb",
                BaseUrl = "https://www.immoweb.be",
                IsActive = true,      // ACTIEF - volledig geïmplementeerd
                CrawlFrequencyHours = 12,
                CreatedAt = now,
                UpdatedAt = now
            },

            // ── Fase 1 stubs - nog niet geïmplementeerd ─────────────────────
            new CrawlerSource
            {
                Id = 2,
                Name = "Zimmo",
                BaseUrl = "https://www.zimmo.be",
                IsActive = false,     // INACTIEF - implementatie volgt
                CrawlFrequencyHours = 12,
                CreatedAt = now,
                UpdatedAt = now
            },
            new CrawlerSource
            {
                Id = 3,
                Name = "Immoscoop",
                BaseUrl = "https://www.immoscoop.be",
                IsActive = false,     // INACTIEF - implementatie volgt
                CrawlFrequencyHours = 24,
                CreatedAt = now,
                UpdatedAt = now
            },

            // ── Fase 2 - niet actief ────────────────────────────────────────
            new CrawlerSource
            {
                Id = 4,
                Name = "Immovlan",
                BaseUrl = "https://www.immovlan.be",
                IsActive = false,
                CrawlFrequencyHours = 24,
                CreatedAt = now,
                UpdatedAt = now
            },
            new CrawlerSource
            {
                Id = 5,
                Name = "Realo",
                BaseUrl = "https://www.realo.be",
                IsActive = false,
                CrawlFrequencyHours = 24,
                CreatedAt = now,
                UpdatedAt = now
            },
            new CrawlerSource
            {
                Id = 6,
                Name = "Biddit",
                BaseUrl = "https://www.biddit.be",
                IsActive = false,
                CrawlFrequencyHours = 168,
                CreatedAt = now,
                UpdatedAt = now
            },
            new CrawlerSource
            {
                Id = 7,
                Name = "ImmoNotaire",
                BaseUrl = "https://www.immonotaire.be",
                IsActive = false,
                CrawlFrequencyHours = 168,
                CreatedAt = now,
                UpdatedAt = now
            }
        };
    }
}
