using BOCore;
using CPMCore.Configuration;
using CPMCore.Models.Projecten;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceCore.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 "Offertes & wijzigingen" (design-handoff 20b) — de hub-lijst: één gecombineerde
    /// worklist voor offertes (ChangeOrder.IsQuote) en wijzigingsopdrachten, rond dezelfde
    /// statuspijplijn als ChangeOrderDetailV2 (20d). Eigen bestand naast de legacy
    /// DetailsChangeOrder-actie, die ongewijzigd blijft.</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        public async Task<IActionResult> ChangeOrdersV2(int projectid)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var canWrite = _ps.HasWrite(PermissionCodes.ProjectsChangeOrders);

            var projectResponse = _projectService.GetProjectByID(projectid);
            if (!projectResponse.Success || projectResponse.Value is null) return NotFound();
            var project = projectResponse.Value;

            var vm = new ChangeOrdersV2Vm { ProjectId = projectid, ProjectName = project.Name, CanWrite = canWrite };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid = projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(ChangeOrdersV2), "Projecten", "Offertes & wijzigingen") { Parent = projectDetail, RouteValues = new { projectid = projectid } };

            var orders = await _db.ChangeOrder.AsNoTracking()
                .Where(c => c.ContractActivity.Contract.ProjectId == projectid)
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ChangeOrderPaymentTerm)
                .Include(c => c.ClientAccount)
                .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Company)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Zelfde statusfeiten als het scherm per stap (ChangeOrderFlowV2.cs) — één bron, zodat de
            // lijst en het detail nooit een andere status tonen.
            var facts = await LoadChangeOrderFlowFactsAsync(projectid, orders);
            vm.SigningEnabled = facts.SigningEnabled;
            var convertedTo = orders.Where(o => o.SourceKind == 3 && o.SourceChangeOrderId.HasValue)
                .GroupBy(o => o.SourceChangeOrderId!.Value)
                .ToDictionary(g => g.Key, g => g.Max(o => o.Id));

            foreach (var co in orders)
            {
                facts.LatestCase.TryGetValue(co.Id, out var signingCase);
                facts.Invoices.TryGetValue(co.Id, out var invoices);
                var status = ComputeFlowStatus(co, facts, today);

                var amount = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m));
                var companyName = co.ContractActivity?.Contract?.Company?.BedrijfsNaam;
                var unitName = ResolveUnitNameForAccount(co.ClientAccountId, projectid);
                var parties = signingCase?.Parties;
                var signedCount = parties?.Count(p => p.Status == (int)SigningPartyStatus.Signed) ?? 0;
                var anyOverdue = invoices?.Any(i => i.IsOverdue) == true;

                convertedTo.TryGetValue(co.Id, out var convertedToId);
                var isConverted = co.IsQuote && (convertedToId > 0 || co.QuoteConvertedAt.HasValue);
                var row = new ChangeOrderRowV2
                {
                    Id = co.Id,
                    IsQuote = co.IsQuote,
                    ConvertedToId = convertedToId > 0 ? convertedToId : null,
                    ClientName = co.ClientAccount?.Name ?? "",
                    UnitName = unitName,
                    Description = co.Description,
                    SupplierName = companyName,
                    Amount = amount,
                    Status = status,
                    DotPosition = DotPositionFor(status),
                    SigningCaseId = signingCase?.CaseId,
                    CanRemind = status == ChangeOrderStatus.Verzonden && signingCase?.Status == (int)SigningCaseStatus.Open,
                    ExpirationDate = co.ExpirationDate,
                };

                // Statuspil: zelfde woorden en kleuren als het scherm per stap (28) en de lijst in 29a.
                (row.PillLabel, row.PillTone) = status switch
                {
                    _ when isConverted => ("Offerte · omgezet", "is-positive"),
                    ChangeOrderStatus.Offerte when co.DateSendToClient.HasValue => ("Offerte · verzonden", "is-attention"),
                    ChangeOrderStatus.Offerte => ("Offerte · concept", "is-neutral"),
                    ChangeOrderStatus.Verlopen => ("Offerte · verlopen", "is-blocked"),
                    ChangeOrderStatus.Opgemaakt => ("Opgemaakt", "is-neutral"),
                    ChangeOrderStatus.Geannuleerd => ("Ingetrokken", "is-neutral"),
                    ChangeOrderStatus.Geweigerd => ("Geweigerd", "is-blocked"),
                    ChangeOrderStatus.Verzonden when signingCase?.Status == (int)SigningCaseStatus.Expired => ("Niet ondertekend", "is-blocked"),
                    ChangeOrderStatus.Verzonden => (signedCount > 0 ? "Wacht op handtekening" : "Verzonden", "is-attention"),
                    ChangeOrderStatus.Ondertekend or ChangeOrderStatus.Factureerbaar => (anyOverdue ? "Vervallen" : "Goedgekeurd", anyOverdue ? "is-blocked" : "is-positive"),
                    ChangeOrderStatus.Gefactureerd => (anyOverdue ? "Vervallen" : "Gefactureerd", anyOverdue ? "is-blocked" : "is-positive"),
                    ChangeOrderStatus.Betaald => ("Betaald", "is-solid"),
                    _ => (ChangeOrderStatusHelper.DisplayName(status), "is-neutral"),
                };

                if (co.IsQuote)
                {
                    row.SubText = isConverted ? $"omgezet naar WO-{convertedToId:000}"
                        : status == ChangeOrderStatus.Verlopen ? $"geldig tot {co.ExpirationDate:dd/MM/yyyy} — verlopen"
                        : $"{co.ChangeOrderDetail.Count} {(co.ChangeOrderDetail.Count == 1 ? "regel" : "regels")}"
                          + (co.DateSendToClient.HasValue ? $" · gemaild {co.DateSendToClient:dd/MM/yyyy}" : "");
                    row.SubTextIsWarning = !isConverted && status == ChangeOrderStatus.Verlopen;
                    row.Hint = isConverted ? null : co.DateSendToClient.HasValue ? "wacht op akkoord klant" : "nog te verzenden";
                    row.IsOpen = !isConverted && status != ChangeOrderStatus.Verlopen;
                    vm.Quotes.Add(row);
                }
                else
                {
                    row.SubText = status switch
                    {
                        ChangeOrderStatus.Verzonden => $"verzonden {co.DateSendToClient:dd/MM/yyyy}" + (signingCase != null ? " via klantenportaal" : ""),
                        ChangeOrderStatus.Ondertekend or ChangeOrderStatus.Factureerbaar => $"ondertekend {co.DateAgreement:dd/MM/yyyy}",
                        ChangeOrderStatus.Gefactureerd or ChangeOrderStatus.Betaald => $"ondertekend {co.DateAgreement:dd/MM/yyyy}",
                        ChangeOrderStatus.Geweigerd => "geweigerd door de klant",
                        ChangeOrderStatus.Geannuleerd => "ondertekening ingetrokken — opnieuw te verzenden",
                        _ => amount < 0 ? "minwerk · aan kostprijs, zonder commissie" : null,
                    };
                    row.SubTextIsWarning = status == ChangeOrderStatus.Verzonden && signingCase != null && signingCase.CreatedAt < DateTime.Now.AddDays(-7);
                    if (row.SubTextIsWarning) row.SubText = $"wacht {(DateTime.Now - signingCase!.CreatedAt).Days} dagen op handtekening";
                    row.Hint = status switch
                    {
                        ChangeOrderStatus.Opgemaakt or ChangeOrderStatus.Geannuleerd => "nog te verzenden",
                        ChangeOrderStatus.Verzonden when signingCase?.Status == (int)SigningCaseStatus.Expired => "opnieuw te verzenden",
                        ChangeOrderStatus.Verzonden when parties is { Count: > 0 } => $"{signedCount} van {parties.Count} getekend",
                        ChangeOrderStatus.Factureerbaar => "klaar om te factureren",
                        ChangeOrderStatus.Gefactureerd => anyOverdue ? "betaling te laat" : "wacht op betaling",
                        _ => null,
                    };
                    if (co.SourceChangeOrderId.HasValue)
                        row.SourceReference = co.SourceKind == 3 ? $"uit OF-{co.SourceChangeOrderId:000}"
                            : (co.SourceKind == 2 ? "versie van " : "kopie van ") + $"WO-{co.SourceChangeOrderId:000}";
                    row.IsOpen = status is not (ChangeOrderStatus.Betaald or ChangeOrderStatus.Geweigerd);
                    vm.Orders.Add(row);
                }
            }

            vm.Quotes = vm.Quotes.OrderByDescending(r => r.Status == ChangeOrderStatus.Offerte).ThenBy(r => r.ExpirationDate).ToList();
            vm.Orders = vm.Orders.OrderBy(r => r.DotPosition).ThenByDescending(r => r.Id).ToList();

            var all = vm.Quotes.Concat(vm.Orders).ToList();
            vm.Funnel = new List<ChangeOrderFunnelStepV2>
            {
                new() { Label = "Offerte", Count = vm.Quotes.Count(r => r.IsOpen) },
                new() { Label = "Opgemaakt", Count = all.Count(r => r.Status == ChangeOrderStatus.Opgemaakt) },
                new() { Label = "Verzonden", Count = all.Count(r => r.Status == ChangeOrderStatus.Verzonden), IsHighlighted = true },
                new() { Label = "Ondertekend", Count = all.Count(r => r.Status == ChangeOrderStatus.Ondertekend || r.Status == ChangeOrderStatus.Factureerbaar) },
                new() { Label = "Gefactureerd", Count = all.Count(r => r.Status == ChangeOrderStatus.Gefactureerd) },
                new() { Label = "Betaald", Count = all.Count(r => r.Status == ChangeOrderStatus.Betaald) },
            };

            return View(vm);
        }

        private static int DotPositionFor(ChangeOrderStatus status) => status switch
        {
            ChangeOrderStatus.Offerte or ChangeOrderStatus.Verlopen => 0,
            ChangeOrderStatus.Opgemaakt => 1,
            ChangeOrderStatus.Verzonden or ChangeOrderStatus.Geweigerd or ChangeOrderStatus.Geannuleerd => 2,
            ChangeOrderStatus.Ondertekend or ChangeOrderStatus.Factureerbaar => 3,
            ChangeOrderStatus.Gefactureerd => 4,
            ChangeOrderStatus.Betaald => 5,
            _ => 0,
        };

        private string? ResolveUnitNameForAccount(int clientAccountId, int projectId)
        {
            var unit = _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.ClientAccountId == clientAccountId)
                .Include(u => u.Type)
                .FirstOrDefault();
            if (unit is null) return null;
            return unit.Type != null ? $"{unit.Type.Name} {unit.Name}".Trim() : unit.Name;
        }
    }
}
