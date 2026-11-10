using System;
using System.Collections.Generic;
using System.Linq;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace DALCore
{
    /// <summary>Publieke nummering van offertes en wijzigingsopdrachten (migratie 070): per project, per jaar,
    /// één lopende teller voor offertes en wijzigingsopdrachten samen, met een versiesuffix voor bijgewerkte
    /// versies. Een offerte die een wijzigingsopdracht wordt behoudt haar nummer (enkel OF → WO). Het interne Id
    /// verschijnt nergens meer naar buiten. Verwijderde nummers worden niet hergebruikt (teller = max + 1).</summary>
    public static class ChangeOrderNumbering
    {
        /// <summary>"OF-2026-014", vanaf versie 2 "OF-2026-014-v2". Rijen zonder nummer (zou na de migratie
        /// niet meer voorkomen) vallen terug op het oude "OF-014" zodat er nooit een lege tekst staat.</summary>
        public static string Format(bool isQuote, int? year, int? seq, int version, int fallbackId, string tag = null)
        {
            var prefix = isQuote ? "OF" : "WO";
            if (year is null || seq is null) return $"{prefix}-{fallbackId:000}";
            var number = string.IsNullOrWhiteSpace(tag) ? $"{prefix}-{year}-{seq:000}" : $"{prefix}-{tag}-{year}-{seq:000}";
            return version > 1 ? $"{number}-v{version}" : number;
        }

        private static readonly HashSet<string> TagStopWords = new(StringComparer.OrdinalIgnoreCase)
            { "de", "het", "een", "van", "der", "den", "en", "te", "t", "la", "le" };

        /// <summary>Afkorting van de projectnaam voor in het nummer: eerste letter van de eerste 4 woorden
        /// (zonder lidwoorden/voorzetsels); bij één woord de eerste 3 letters; leeg → "P" + projectId.
        /// Zelfde regel als migratie 139.</summary>
        public static string ProjectTag(string name, int projectId)
        {
            var words = new List<string>();
            var cur = new System.Text.StringBuilder();
            void Flush()
            {
                if (cur.Length == 0) return;
                var w = cur.ToString(); cur.Clear();
                if (!TagStopWords.Contains(w)) words.Add(w);
            }
            foreach (var ch in (name ?? "").Normalize(System.Text.NormalizationForm.FormD))
            {
                if (ch < 128 && char.IsLetterOrDigit(ch)) cur.Append(ch);
                else if (ch >= 0x300 && ch <= 0x36F) continue;   // accent losmaken (é → e)
                else Flush();
            }
            Flush();
            if (words.Count == 0) return "P" + projectId;
            if (words.Count == 1) return words[0].Substring(0, Math.Min(3, words[0].Length)).ToUpperInvariant();
            return string.Concat(words.Take(4).Select(w => char.ToUpperInvariant(w[0])));
        }

        /// <summary>Kent aan elke nieuwe ChangeOrder (Added, nog zonder teller) een nummer toe. Aangeroepen
        /// vanuit cpmRunningContext.SaveChanges, dus ongeacht welke service/controller de rij aanmaakt.</summary>
        internal static void AssignPending(cpmRunningContext db)
        {
            var added = db.ChangeTracker.Entries<ChangeOrder>()
                .Where(e => e.State == EntityState.Added && e.Entity.NumberSeq == null)
                .Select(e => e.Entity)
                .ToList();
            if (added.Count == 0) return;

            var pendingMax = new Dictionary<(int project, int year), int>();
            var pendingVersion = new Dictionary<int, int>();
            var tags = new Dictionary<int, string>();

            foreach (var co in added)
            {
                var projectId = ResolveProjectId(db, co);
                co.NumberProjectId = projectId;
                if (projectId is null) continue;   // geen project te bepalen: blijft zonder nummer (fallback op Id)

                // Nieuwe versie van een bestaand document: zelfde jaar/teller, versie + 1.
                if (co.SourceKind == 2 && co.SourceChangeOrderId.HasValue)
                {
                    var src = db.ChangeOrder.AsNoTracking().Where(c => c.Id == co.SourceChangeOrderId.Value)
                        .Select(c => new { c.Id, c.RootChangeOrderId, c.NumberYear, c.NumberSeq, c.NumberProjectId, c.NumberProjectTag }).FirstOrDefault();
                    if (src?.NumberSeq != null && src.NumberYear != null)
                    {
                        var rootId = src.RootChangeOrderId ?? src.Id;
                        var key = rootId;
                        if (!pendingVersion.TryGetValue(key, out var maxVersion))
                            maxVersion = db.ChangeOrder.AsNoTracking().Where(c => c.Id == rootId || c.RootChangeOrderId == rootId).Max(c => (int?)c.VersionNo) ?? 1;
                        maxVersion++;
                        pendingVersion[key] = maxVersion;
                        co.RootChangeOrderId = rootId;
                        co.NumberProjectId = src.NumberProjectId ?? projectId;
                        co.NumberProjectTag = !string.IsNullOrWhiteSpace(src.NumberProjectTag) ? src.NumberProjectTag : TagFor(db, projectId.Value, tags);
                        co.NumberYear = src.NumberYear;
                        co.NumberSeq = src.NumberSeq;
                        co.VersionNo = maxVersion;
                        continue;
                    }
                }

                // Offerte omgezet naar een wijzigingsopdracht (SourceKind 3): zelfde jaar en volgnummer, enkel
                // het voorvoegsel wijzigt (OF-2026-014 → WO-2026-014). Bestaat dat nummer al als WO (oude,
                // aparte tellers), dan krijgt de WO een nieuw nummer uit de gedeelde teller.
                if (co.SourceKind == 3 && co.SourceChangeOrderId.HasValue && !co.IsQuote)
                {
                    var src = db.ChangeOrder.AsNoTracking().Where(c => c.Id == co.SourceChangeOrderId.Value)
                        .Select(c => new { c.NumberYear, c.NumberSeq, c.NumberProjectId }).FirstOrDefault();
                    if (src?.NumberSeq != null && src.NumberYear != null)
                    {
                        var np = src.NumberProjectId ?? projectId;
                        var taken = db.ChangeOrder.AsNoTracking().Any(c => c.NumberProjectId == np && c.IsQuote == false
                                && c.NumberYear == src.NumberYear && c.NumberSeq == src.NumberSeq && c.VersionNo == 1)
                            || added.Any(a => !ReferenceEquals(a, co) && a.NumberProjectId == np && !a.IsQuote
                                && a.NumberYear == src.NumberYear && a.NumberSeq == src.NumberSeq && a.VersionNo == 1);
                        if (!taken)
                        {
                            co.NumberProjectId = np;
                            co.NumberProjectTag = TagFor(db, np ?? projectId.Value, tags);
                            co.NumberYear = src.NumberYear;
                            co.NumberSeq = src.NumberSeq;
                            co.VersionNo = 1;
                            co.RootChangeOrderId = null;
                            continue;
                        }
                    }
                }

                // Eén doorlopende teller per project en jaar, voor offertes én wijzigingsopdrachten samen.
                var year = DateTime.Today.Year;
                var k = (projectId.Value, year);
                if (!pendingMax.TryGetValue(k, out var max))
                    max = db.ChangeOrder.AsNoTracking()
                        .Where(c => c.NumberProjectId == projectId && c.NumberYear == year)
                        .Max(c => (int?)c.NumberSeq) ?? 0;
                max++;
                pendingMax[k] = max;
                co.NumberProjectTag = TagFor(db, projectId.Value, tags);
                co.NumberYear = year;
                co.NumberSeq = max;
                co.VersionNo = 1;
                co.RootChangeOrderId = null;
            }
        }

        private static string TagFor(cpmRunningContext db, int projectId, Dictionary<int, string> cache)
        {
            if (cache.TryGetValue(projectId, out var t)) return t;
            var name = db.Project.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => p.ProjectName).FirstOrDefault();
            return cache[projectId] = ProjectTag(name, projectId);
        }

        private static int? ResolveProjectId(cpmRunningContext db, ChangeOrder co)
        {
            if (co.ProjectId > 0) return co.ProjectId;
            if (co.ContractActivityId is not int activityId || activityId <= 0) return null;
            var fromContract = db.ContractActivity.AsNoTracking()
                .Where(a => a.Id == activityId)
                .Select(a => (int?)a.Contract.ProjectId)
                .FirstOrDefault();
            if (fromContract.HasValue) co.ProjectId = fromContract.Value;   // legacy-paden die enkel een contract meegeven
            return fromContract;
        }
    }
}
