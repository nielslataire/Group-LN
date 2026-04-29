using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.IncomingInvoices
{
    /// <summary>
    /// Zoekt open contracten voor een project + leverancier.
    /// Geeft een gescoorde lijst terug van kandidaat-contracten.
    /// </summary>
    public class ContractMatchingService : IContractMatchingService
    {
        private readonly cpmRunningContext _db;
        private readonly ILogger<ContractMatchingService> _logger;

        public ContractMatchingService(cpmRunningContext db, ILogger<ContractMatchingService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ContractMatchResult>> FindMatchesAsync(
            int projectId,
            int? companyId,
            string invoiceDescription = null,
            CancellationToken ct = default)
        {
            var query = _db.Contract
                .Include(c => c.Company)
                .Include(c => c.Project)
                .Where(c => c.ProjectId == projectId)
                .AsNoTracking();

            var contracts = await query.ToListAsync(ct);

            if (!contracts.Any())
                return Array.Empty<ContractMatchResult>();

            var normalizedDesc = string.IsNullOrWhiteSpace(invoiceDescription)
                ? string.Empty
                : ProjectMatchingService.Normalize(invoiceDescription);

            var results = new List<ContractMatchResult>();

            foreach (var c in contracts)
            {
                var score = 0;
                var reasons = new List<string>();

                // +50: zelfde leverancier
                if (companyId.HasValue && c.CompanyId == companyId.Value)
                {
                    score += 50;
                    reasons.Add("zelfde leverancier");
                }
                else if (!companyId.HasValue)
                {
                    // Geen leverancier bekend: neutraal
                    score += 10;
                }

                // +15: contractnaam gevonden in omschrijving
                if (!string.IsNullOrWhiteSpace(c.ContractName) && !string.IsNullOrWhiteSpace(normalizedDesc))
                {
                    var normalizedContractName = ProjectMatchingService.Normalize(c.ContractName);
                    if (normalizedDesc.Contains(normalizedContractName, StringComparison.Ordinal))
                    {
                        score += 15;
                        reasons.Add("contractnaam in omschrijving");
                    }
                }

                results.Add(new ContractMatchResult
                {
                    ContractId = c.Id,
                    ContractName = c.ContractName ?? $"Contract #{c.Id}",
                    ProjectId = c.ProjectId,
                    ProjectName = c.Project?.ProjectName,
                    Score = score,
                    ConfidenceLevel = score >= 50 ? "High" : score >= 25 ? "Medium" : "Low",
                    MatchReason = string.Join(", ", reasons),
                    SupplierName = c.Company?.BedrijfsNaam
                });
            }

            return results
                .OrderByDescending(r => r.Score)
                .ToList();
        }
    }
}
