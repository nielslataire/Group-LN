using System;
using System.Collections.Generic;
using System.Linq;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace DALCore
{
    /// <summary>Publieke nummering van offertes en wijzigingsopdrachten (migratie 070): per project, per jaar,
    /// per type een lopende teller, met een versiesuffix voor bijgewerkte versies. Het interne Id verschijnt
    /// nergens meer naar buiten. Verwijderde nummers worden niet hergebruikt (teller = max + 1).</summary>
    public static class ChangeOrderNumbering
    {
        /// <summary>"OF-2026-014", vanaf versie 2 "OF-2026-014-v2". Rijen zonder nummer (zou na de migratie
        /// niet meer voorkomen) vallen terug op het oude "OF-014" zodat er nooit een lege tekst staat.</summary>
        public static string Format(bool isQuote, int? year, int? seq, int version, int fallbackId)
        {
            var prefix = isQuote ? "OF" : "WO";
            if (year is null || seq is null) return $"{prefix}-{fallbackId:000}";
            var number = $"{prefix}-{year}-{seq:000}";
            return version > 1 ? $"{number}-v{version}" : number;
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

            var pendingMax = new Dictionary<(int project, bool quote, int year), int>();
            var pendingVersion = new Dictionary<int, int>();

            foreach (var co in added)
            {
                var projectId = ResolveProjectId(db, co);
                co.NumberProjectId = projectId;
                if (projectId is null) continue;   // geen project te bepalen: blijft zonder nummer (fallback op Id)

                // Nieuwe versie van een bestaand document: zelfde jaar/teller, versie + 1.
                if (co.SourceKind == 2 && co.SourceChangeOrderId.HasValue)
                {
                    var src = db.ChangeOrder.AsNoTracking().Where(c => c.Id == co.SourceChangeOrderId.Value)
                        .Select(c => new { c.Id, c.RootChangeOrderId, c.NumberYear, c.NumberSeq, c.NumberProjectId }).FirstOrDefault();
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
                        co.NumberYear = src.NumberYear;
                        co.NumberSeq = src.NumberSeq;
                        co.VersionNo = maxVersion;
                        continue;
                    }
                }

                var year = DateTime.Today.Year;
                var k = (projectId.Value, co.IsQuote, year);
                if (!pendingMax.TryGetValue(k, out var max))
                    max = db.ChangeOrder.AsNoTracking()
                        .Where(c => c.NumberProjectId == projectId && c.IsQuote == co.IsQuote && c.NumberYear == year)
                        .Max(c => (int?)c.NumberSeq) ?? 0;
                max++;
                pendingMax[k] = max;
                co.NumberYear = year;
                co.NumberSeq = max;
                co.VersionNo = 1;
                co.RootChangeOrderId = null;
            }
        }

        private static int? ResolveProjectId(cpmRunningContext db, ChangeOrder co)
        {
            if (co.ContractActivityId <= 0) return null;
            return db.ContractActivity.AsNoTracking()
                .Where(a => a.Id == co.ContractActivityId)
                .Select(a => (int?)a.Contract.ProjectId)
                .FirstOrDefault();
        }
    }
}
