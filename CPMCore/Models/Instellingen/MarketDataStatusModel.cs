using System;
using System.Collections.Generic;

namespace CPMCore.Models.Instellingen
{
    public class MarketDataSourceStatusVM
    {
        public string SourceName { get; set; } = "";
        public bool IsActive { get; set; }

        public bool IsRunning { get; set; }
        public string? CurrentPhase { get; set; }
        public string? CurrentProgress { get; set; }

        public DateTime? LastAttemptedCrawlAt { get; set; }
        public DateTime? LastSuccessfulCrawlAt { get; set; }
        public DateTime? LastFailedCrawlAt { get; set; }
        public DateTime? NextCrawlAt { get; set; }

        public int? LastDurationSeconds { get; set; }
        public int? LastResultFound { get; set; }
        public int? LastResultNew { get; set; }
        public int? LastResultUpdated { get; set; }
        public int? LastResultErrors { get; set; }
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// True als de meest recente afgeronde poging een fout was
        /// (LastFailedCrawlAt is recenter dan LastSuccessfulCrawlAt).
        /// </summary>
        public bool LastRunFailed =>
            LastFailedCrawlAt.HasValue &&
            (!LastSuccessfulCrawlAt.HasValue || LastFailedCrawlAt.Value > LastSuccessfulCrawlAt.Value);

        public bool HasEverRun => LastAttemptedCrawlAt.HasValue;
    }

    public class MarketDataRunVM
    {
        public long Id { get; set; }
        public string SourceName { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Status { get; set; } = "";
        public int ListingsFound { get; set; }
        public int ListingsCreated { get; set; }
        public int ListingsUpdated { get; set; }
        public int Errors { get; set; }
        public string? LogMessage { get; set; }
    }

    public class MarketDataStatusModel
    {
        public List<MarketDataSourceStatusVM> Sources { get; set; } = new();
        public List<MarketDataRunVM> RecenteRuns { get; set; } = new();
    }
}
