using BOCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ServiceCore
{
    public class ConnectionSettlementService: IConnectionSettlementService
    {
        private readonly cpmRunningContext _db;
        private readonly IConnectionCostService _pool;
        private readonly IInvoiceCommandService _invoiceCmd;

        public ConnectionSettlementService(
            cpmRunningContext db,
            IConnectionCostService pool,
            IInvoiceCommandService invoiceCmd)
        {
            _db = db; _pool = pool; _invoiceCmd = invoiceCmd;
        }

        public async Task<ConnectionSettlementResult> PreviewSettlementAsync(
                int projectId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
        {
            // Pool
            var poolRows = await _pool.GetOpenConnectionCostPoolAsync(projectId, fromDate, toDate, ct);
            var poolTotal = poolRows.Sum(x => x.RemainingExcl);
            if (poolTotal <= 0m)
                return new ConnectionSettlementResult(0, 0m,
                    new Dictionary<int, decimal>(), new Dictionary<int, decimal>());

            // Weights
            var keys = await _db.ProjectConnectionKey.AsNoTracking()
                .Where(k => k.ProjectId == projectId)
                .Select(k => new { k.UnitId, k.Weight })
                .ToListAsync(ct);
            var totalWeight = keys.Sum(k => k.Weight);
            if (totalWeight <= 0m)
                throw new InvalidOperationException("Geen verdeelsleutel voor dit project.");

            // Units → ClientAccount
            var units = await _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.ClientAccountId != null)
                .Select(u => new { u.Id, u.ClientAccountId })
                .ToDictionaryAsync(u => u.Id, u => u.ClientAccountId!.Value, ct);

            // Aandeel per unit (pro rata)
            var perUnit = keys
                .Where(k => units.ContainsKey(k.UnitId))
                .Select(k => new {
                    k.UnitId,
                    Amount = Math.Round(poolTotal * (k.Weight / totalWeight), 2, MidpointRounding.AwayFromZero)
                })
                .Where(x => x.Amount > 0m)
                .ToList();

            var allocPerClient = perUnit
                .GroupBy(x => units[x.UnitId])
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            // Uitgaande factuurlijnen die Utility-voorschotten zijn, gegroepeerd op hoofdklant (ClientAccount)
            var advanceLinesPath1 = (
                from l in _db.InvoicesDetails.AsNoTracking()
                join h in _db.Invoices.AsNoTracking() on l.InvoiceId equals h.Id
                where h.ProjectId == projectId
                      && h.CompanyId == null                  // klantfacturen, geen leveranciers
                      && l.UtilityCost == true
                      && l.UtilityIsAdvance == true
                      && h.ClientType == 1                    // rechtstreeks naar ClientAccount
                      && h.ClientId != null
                select new
                {
                    LineId = l.Id,
                    ClientAccountId = h.ClientId!.Value,      // hoofdklant
                    AmountExcl = l.Price ?? 0m,
                    InvoiceDate = h.Date
                }
            );

            var advanceLinesPath2 = (
                from l in _db.InvoicesDetails.AsNoTracking()
                join h in _db.Invoices.AsNoTracking() on l.InvoiceId equals h.Id
                join cc in _db.ClientContacts.AsNoTracking() on h.ClientId equals cc.Id
                where h.ProjectId == projectId
                      && h.CompanyId == null                  // klantfacturen
                      && l.UtilityCost == true
                      && l.UtilityIsAdvance == true
                      && h.ClientType == 2                    // co-owner → resolve naar hoofdklant
                select new
                {
                    LineId = l.Id,
                    ClientAccountId = cc.ClientAccountId,     // hoofdklant via co-owner
                    AmountExcl = l.Price ?? 0m,
                    InvoiceDate = h.Date
                }
            );

            // Union All
            var advanceLines = await advanceLinesPath1
                .Concat(advanceLinesPath2)
                .OrderBy(x => x.InvoiceDate)
                .ThenBy(x => x.LineId)
                .ToListAsync(ct);

            var appliedByLine = await _db.ConnectionAdvanceApplication.AsNoTracking()
                .GroupBy(a => a.InvoiceLineId)
                .Select(g => new { g.Key, Used = g.Sum(x => x.AmountUsedExcl) })
                .ToDictionaryAsync(x => x.Key, x => x.Used, ct);

            var advRemainingByClient = advanceLines
                .GroupBy(a => a.ClientAccountId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(a => a.AmountExcl - (appliedByLine.TryGetValue(a.LineId, out var used) ? used : 0m))
                );

            var netPerClient = allocPerClient.ToDictionary(
                kv => kv.Key,
                kv =>
                {
                    var adv = advRemainingByClient.TryGetValue(kv.Key, out var a) ? a : 0m;
                    var net = kv.Value - adv;
                    return net > 0m ? Math.Round(net, 2, MidpointRounding.AwayFromZero) : 0m;
                });

            return new ConnectionSettlementResult(0, poolTotal, allocPerClient, netPerClient);
        }

        public async Task<ConnectionSettlementResult> CreateSettlementAsync(
            int projectId,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? periodLabel,
            int issuerCompanyId,
            bool issueInvoicesNow,
            CancellationToken ct = default)
        {
            // 1) Preview berekenen
            var preview = await PreviewSettlementAsync(projectId, fromDate, toDate, ct);
            if (preview.PoolAmount <= 0m)
                return preview;

            // 2) Bronnen (pool) + advance lijnen opnieuw ophalen (FIFO toepassen)
            var poolRows = await _pool.GetOpenConnectionCostPoolAsync(projectId, fromDate, toDate, ct);

            // Voorschot-lijnen (uitgaande klantfacturen), samengebracht per hoofdklant (ClientAccount)

            var advanceLinesPath1 =
                from l in _db.InvoicesDetails.AsNoTracking()
                join h in _db.Invoices.AsNoTracking() on l.InvoiceId equals h.Id
                where h.ProjectId == projectId
                      && h.CompanyId == null                 // klantfactuur
                      && l.UtilityCost == true
                      && l.UtilityIsAdvance == true
                      && h.ClientType == 1                   // rechtstreeks ClientAccount
                      && h.ClientId != null
                select new
                {
                    LineId = l.Id,
                    ClientAccountId = h.ClientId!.Value,     // hoofdklant
                    AmountExcl = l.Price ?? 0m,
                    InvoiceDate = h.Date                     // kolom heet 'Date'
                };

            var advanceLinesPath2 =
                from l in _db.InvoicesDetails.AsNoTracking()
                join h in _db.Invoices.AsNoTracking() on l.InvoiceId equals h.Id
                join cc in _db.ClientContacts.AsNoTracking() on h.ClientId equals cc.Id
                where h.ProjectId == projectId
                      && h.CompanyId == null                 // klantfactuur
                      && l.UtilityCost == true
                      && l.UtilityIsAdvance == true
                      && h.ClientType == 2                   // co-owner → resolve naar hoofdklant
                select new
                {
                    LineId = l.Id,
                    ClientAccountId = cc.ClientAccountId,    // hoofdklant via co-owner
                    AmountExcl = l.Price ?? 0m,
                    InvoiceDate = h.Date
                };

            var advanceLines = await advanceLinesPath1
                .Concat(advanceLinesPath2)
                .OrderBy(x => x.InvoiceDate)
                .ThenBy(x => x.LineId)
                .ToListAsync(ct);

            var appliedByLine = await _db.ConnectionAdvanceApplication.AsNoTracking()
                .GroupBy(a => a.InvoiceLineId)
                .Select(g => new { g.Key, Used = g.Sum(x => x.AmountUsedExcl) })
                .ToDictionaryAsync(x => x.Key, x => x.Used, ct);

            // 3) Start settlement (transactie)
            using var tx = await _db.Database.BeginTransactionAsync(ct);

            var settlement = new ConnectionSettlement
            {
                ProjectId = projectId,
                PeriodLabel = periodLabel
            };
            _db.ConnectionSettlement.Add(settlement);
            await _db.SaveChangesAsync(ct);

            // 3a) Log bronnen FIFO tot pool op is
            decimal remainingPool = preview.PoolAmount;
            foreach (var pr in poolRows)
            {
                if (remainingPool <= 0m) break;
                var use = Math.Min(pr.RemainingExcl, remainingPool);
                _db.ConnectionSettlementSource.Add(new ConnectionSettlementSource
                {
                    SettlementId = settlement.Id,
                    InvoiceDetailId = pr.DetailId,
                    AmountUsedExcl = use
                });
                remainingPool -= use;
            }
            await _db.SaveChangesAsync(ct);

            // 3b) Per klant: voorschotten toepassen (Application loggen)
            foreach (var kv in preview.AllocPerClient)
            {
                var clientId = kv.Key;
                var shareNeed = kv.Value;
                if (shareNeed <= 0m) continue;

                // hoeveel voorschot willen we max toepassen?
                var net = preview.NetPerClient.TryGetValue(clientId, out var n) ? n : shareNeed;
                var advToApply = Math.Max(0m, shareNeed - net);
                if (advToApply <= 0m) continue;

                var fifo = advanceLines.Where(a => a.ClientAccountId == clientId).ToList();
                foreach (var a in fifo)
                {
                    if (advToApply <= 0m) break;
                    var used = appliedByLine.TryGetValue(a.LineId, out var u) ? u : 0m;
                    var avail = a.AmountExcl - used;
                    if (avail <= 0m) continue;

                    var use = Math.Min(avail, advToApply);
                    _db.ConnectionAdvanceApplication.Add(new ConnectionAdvanceApplication
                    {
                        SettlementId = settlement.Id,
                        InvoiceLineId = a.LineId,
                        AmountUsedExcl = use
                    });

                    appliedByLine[a.LineId] = used + use;
                    advToApply -= use;
                }
            }
            await _db.SaveChangesAsync(ct);

            // 3c) Facturen per klant (alleen netto > 0)
            foreach (var kv in preview.NetPerClient.Where(x => x.Value > 0m))
            {
                var clientAccountId = kv.Key;
                var amount = kv.Value;

                var bo = new InvoiceDraftBO
                {
                    IssuerCompanyId = issuerCompanyId,
                    InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                    Mode = InvoiceMode.Utilities,
                    HeaderDescription = BuildHeader(periodLabel),
                    ProjectId = projectId,
                    Lines = new List<InvoiceLineBO> {
                    new InvoiceLineBO {
                        Text          = BuildLineText(periodLabel),
                        Price         = amount,
                        VatPercentage = 21m,                 // pas aan indien nodig
                        LineType      = "Utilities",
                        GroupName     = "Aansluitkosten",
                        UtilityCost   = true,                // afrekenlijn (géén voorschot)
                        // UtilityIsAdvance = false (default)
                    }
                }
                };
                // klant koppelen
                bo.CompanyId = null;
                bo.ClientType = 1;     // ClientAccount
                bo.ClientId = clientAccountId;

                await _invoiceCmd.CreateWithLinesAsync(bo, issueNow: issueInvoicesNow, ct);
            }

            await tx.CommitAsync(ct);

            return preview with { SettlementId = settlement.Id };
        }

        private static string BuildHeader(string? label)
            => string.IsNullOrWhiteSpace(label)
               ? "Aansluitkosten – afrekening"
               : $"Aansluitkosten – afrekening {label}";

        private static string BuildLineText(string? label)
            => string.IsNullOrWhiteSpace(label)
               ? "Aansluitkosten project – afrekening"
               : $"Aansluitkosten project – afrekening {label}";
    }
}
