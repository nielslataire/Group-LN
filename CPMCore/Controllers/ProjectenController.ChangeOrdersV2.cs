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
                .Include(c => c.ClientAccount)
                .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Company)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            var features = HttpContext.RequestServices.GetRequiredService<IOptions<FeatureFlagsOptions>>().Value;
            var signingByOrderId = new Dictionary<int, FacadeCore.Signing.CaseStatusView>();
            if (features.EnableSigning && orders.Count > 0)
            {
                var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
                var cases = await signing.ListCasesAsync(projectid, null, 1000, HttpContext.RequestAborted);
                signingByOrderId = cases
                    .Where(c => c.DocumentType == Services.Signing.ChangeOrderSigningSource.Key)
                    .GroupBy(c => c.SourceEntityId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).First());
            }

            var orderIds = orders.Select(o => o.Id).ToHashSet();
            var termIdsByOrder = await _db.ChangeOrderPaymentTerm.AsNoTracking()
                .Where(t => orderIds.Contains(t.ChangeOrderId))
                .Select(t => new { t.Id, t.ChangeOrderId })
                .ToListAsync();
            var invoicedTermIds = await _db.InvoicesDetails.AsNoTracking()
                .Where(d => d.LineType == "ChangeOrderTerm" && d.ChangeOrderPaymentTermId.HasValue)
                .Select(d => d.ChangeOrderPaymentTermId!.Value)
                .ToListAsync();
            var invoicedTermIdSet = invoicedTermIds.ToHashSet();
            var allTermsInvoicedByOrder = termIdsByOrder
                .GroupBy(t => t.ChangeOrderId)
                .ToDictionary(g => g.Key, g => g.All(t => invoicedTermIdSet.Contains(t.Id)));
            var hasAnyTermByOrder = termIdsByOrder.Select(t => t.ChangeOrderId).ToHashSet();

            foreach (var co in orders)
            {
                signingByOrderId.TryGetValue(co.Id, out var signingCase);
                var hasTerms = hasAnyTermByOrder.Contains(co.Id);
                allTermsInvoicedByOrder.TryGetValue(co.Id, out var allInvoiced);

                var input = new ChangeOrderStatusInput(
                    IsQuote: co.IsQuote,
                    ExpirationDate: co.ExpirationDate,
                    DateSendToClient: co.DateSendToClient,
                    DateAgreement: co.DateAgreement,
                    SigningCaseStatus: signingCase?.Status,
                    HasInvoicableTerm: co.DateAgreement.HasValue && hasTerms && !allInvoiced,
                    AllTermsInvoiced: hasTerms && allInvoiced,
                    AllInvoicesPaid: false); // stap 5 (facturatie-integratie)
                var status = ChangeOrderStatusHelper.Compute(input, today);

                var amount = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m));
                var companyName = co.ContractActivity?.Contract?.Company?.BedrijfsNaam;
                var unitName = ResolveUnitNameForAccount(co.ClientAccountId, projectid);

                var row = new ChangeOrderRowV2
                {
                    Id = co.Id,
                    IsQuote = co.IsQuote,
                    ClientName = co.ClientAccount?.Name ?? "",
                    UnitName = unitName,
                    Description = co.Description,
                    SupplierName = companyName,
                    Amount = amount,
                    Status = status,
                    Steps = ChangeOrderStepsBuilder.BuildRow(status, co.Id),
                    SigningCaseId = signingCase?.CaseId,
                    CanRemind = status == ChangeOrderStatus.Verzonden,
                    ExpirationDate = co.ExpirationDate,
                };

                if (co.IsQuote)
                {
                    row.SubText = status == ChangeOrderStatus.Verlopen
                        ? $"geldig tot {co.ExpirationDate:dd/MM/yyyy} — verlopen"
                        : $"ingelezen · {co.ChangeOrderDetail.Count} {(co.ChangeOrderDetail.Count == 1 ? "regel" : "regels")}";
                    row.SubTextIsWarning = status == ChangeOrderStatus.Verlopen;
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
                        ChangeOrderStatus.Geannuleerd => "geannuleerd",
                        _ => amount < 0 ? "minwerk · aan kostprijs, zonder commissie" : null,
                    };
                    row.SubTextIsWarning = status == ChangeOrderStatus.Verzonden && signingCase != null && signingCase.CreatedAt < DateTime.Now.AddDays(-7);
                    if (row.SubTextIsWarning) row.SubText = $"wacht {(DateTime.Now - signingCase!.CreatedAt).Days} dagen op handtekening";
                    vm.Orders.Add(row);
                }
            }

            vm.Quotes = vm.Quotes.OrderByDescending(r => r.Status == ChangeOrderStatus.Offerte).ThenBy(r => r.ExpirationDate).ToList();
            vm.Orders = vm.Orders.OrderBy(r => ChangeOrderStepsBuilder.RowPosition(r.Status)).ThenByDescending(r => r.Id).ToList();

            var all = vm.Quotes.Concat(vm.Orders).ToList();
            vm.Funnel = new List<ChangeOrderFunnelStepV2>
            {
                new() { Label = "Offerte", Count = all.Count(r => r.Status == ChangeOrderStatus.Offerte || r.Status == ChangeOrderStatus.Verlopen) },
                new() { Label = "Opgemaakt", Count = all.Count(r => r.Status == ChangeOrderStatus.Opgemaakt) },
                new() { Label = "Verzonden", Count = all.Count(r => r.Status == ChangeOrderStatus.Verzonden), IsHighlighted = true },
                new() { Label = "Ondertekend", Count = all.Count(r => r.Status == ChangeOrderStatus.Ondertekend || r.Status == ChangeOrderStatus.Factureerbaar) },
                new() { Label = "Gefactureerd", Count = all.Count(r => r.Status == ChangeOrderStatus.Gefactureerd) },
                new() { Label = "Betaald", Count = all.Count(r => r.Status == ChangeOrderStatus.Betaald) },
            };

            return View(vm);
        }

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
