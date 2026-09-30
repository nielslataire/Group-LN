using BOCore;
using CPMCore.Models.Projecten;
using CPMCore.Models.Signing;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceCore;
using ServiceCore.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 Facturatie-pagina (design-handoff punt 20-22, "Facturatie"; eerste pagina van de
    /// bredere Financieel-herwerking, zie plan "smooth-greeting-origami"). Eigen bestand (partial)
    /// naast <c>Invoicing</c> in ProjectenController.cs, die ongewijzigd blijft tot deze pagina getest en
    /// bevestigd is. Hergebruikt dezelfde data-assemblage (<c>GetProjectInvoicableUnits</c>/
    /// <c>GetProjectInvoicableChangeOrders</c>) en dezelfde private statics
    /// (<see cref="BuildStageInvoiceDraft"/>/<see cref="BuildChangeOrderInvoiceDraft"/>, ProjectenController.cs)
    /// die <c>MakeInvoices</c>/<c>MakeInvoicesCO</c> ook gebruiken — <c>PreviewInvoices</c> hieronder roept
    /// ze enkel aan zonder <c>InvoiceCommandService.CreateWithLinesAsync</c>, dus zonder te schrijven.</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        public async Task<IActionResult> InvoicingV2(int projectid, CancellationToken ct)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var canWrite = _ps.HasWrite(PermissionCodes.ProjectsInvoicing);

            var projectResponse = _projectService.GetProjectByID(projectid);
            if (!projectResponse.Success || projectResponse.Value is null) return NotFound();
            var project = projectResponse.Value;

            var vm = new InvoicingV2Vm
            {
                ProjectId = projectid,
                ProjectName = project.Name,
                CanWrite = canWrite,
            };

            var cardsByAccount = new Dictionary<int, InvoicingAccountCardV2>();
            var unitNamesByAccount = new Dictionary<int, List<string>>();
            var groupNameByAccount = new Dictionary<int, string>();
            InvoicingAccountCardV2 CardFor(ClientAccountBO client)
            {
                if (cardsByAccount.TryGetValue(client.Id, out var existing)) return existing;

                var coowners = _db.ClientContacts.AsNoTracking()
                    .Where(cc => cc.ClientAccountId == client.Id && cc.IsCoOwner && cc.CoOwnerPercentage.HasValue)
                    .Select(cc => new { cc.Name, cc.Forename, cc.CoOwnerPercentage })
                    .ToList();

                // UI-weergavenaam (niet ClientAccountBO.DisplayName zelf — dat blijft de akte-naam voor
                // facturen/officiële documenten): met mede-eigenaars alle achternamen samen.
                var card = new InvoicingAccountCardV2
                {
                    ClientAccountId = client.Id,
                    DisplayName = ClientAccountDisplayHelper.WithCoOwners(client.Name, client.Firstname, coowners.Select(c => c.Name)),
                };
                var coOwnerTotal = coowners.Sum(c => c.CoOwnerPercentage ?? 0m);
                card.Owners.Add(new InvoicingOwnerShareV2 { DisplayName = client.DisplayName, Percentage = Math.Max(0m, 100m - coOwnerTotal), IsMainOwner = true });
                foreach (var co in coowners)
                    card.Owners.Add(new InvoicingOwnerShareV2 { DisplayName = $"{co.Name} {co.Forename}".Trim(), Percentage = co.CoOwnerPercentage ?? 0m });

                cardsByAccount[client.Id] = card;
                return card;
            }

            // Alle schijven van een groep (niet enkel de invoicable/nog-open zoals GetProjectInvoicableUnits
            // teruggeeft) — nodig voor zowel "schijf X van Y"/IsLastStage (structurele positie) als de
            // verloopbalk (die ook de nog-niet-vrijgegeven/Invoicable=false schijven moet meetellen). Per
            // groep één keer opgehaald en gecachet.
            var stagesByGroup = new Dictionary<int, List<InvoicingPaymentStages>>();
            List<InvoicingPaymentStages> StagesFor(int groupId)
            {
                if (!stagesByGroup.TryGetValue(groupId, out var stages))
                {
                    stages = _db.InvoicingPaymentStages.AsNoTracking().Include(s => s.Group)
                        .Where(s => s.GroupId == groupId).OrderBy(s => s.Id).ToList();
                    stagesByGroup[groupId] = stages;
                }
                return stages;
            }

            var progressInvoiced = new Dictionary<int, decimal>();
            var progressOpen = new Dictionary<int, decimal>();
            var progressNotReached = new Dictionary<int, decimal>();
            void AddProgress(Dictionary<int, decimal> bucket, int accountId, decimal amount)
                => bucket[accountId] = bucket.TryGetValue(accountId, out var existing) ? existing + amount : amount;

            // ===== Schijven (vordering der werken): ELK eenheid met een geldig klantenaccount, ook als er
            // niets meer open staat (Niels, 2026-09-30 — elk klantenaccount moet een kaart krijgen, net als
            // in 20a's "niets te factureren"-kaart). Eigen, bredere query i.p.v. GetProjectInvoicableUnits:
            // die sluit een eenheid al helemaal uit zodra ze niets meer open heeft. =====
            var cutoffDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
            var allUnits = _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectid && u.ClientAccountId > 0
                    && u.ClientAccount.DateDeedOfSale != null && u.ClientAccount.DateDeedOfSale.Value <= cutoffDate)
                .Include(u => u.ClientAccount)
                .Include(u => u.Type)
                .Include(u => u.UnitConstructionValue)
                .ToList();

            if (allUnits.Count > 0)
            {
                var accountIds = allUnits.Select(u => u.ClientAccountId!.Value).Distinct().ToList();
                var clientsResp = _clientService.GetClientAccountByIds(accountIds);
                var clientsById = clientsResp.Success ? clientsResp.Values.ToDictionary(c => c.Id) : new Dictionary<int, ClientAccountBO>();

                var unitIds = allUnits.Select(u => u.Id).ToList();
                var invoicedStagePairs = _db.InvoicesDetails.AsNoTracking()
                    .Where(d => d.LineType == "Stages" && d.PaymentStageId.HasValue && d.UnitId.HasValue && unitIds.Contains(d.UnitId.Value))
                    .Select(d => new { StageId = d.PaymentStageId!.Value, UnitId = d.UnitId!.Value })
                    .ToList()
                    .Select(x => (x.StageId, x.UnitId))
                    .ToHashSet();

                // Per-eenheid "bereikt" (migratie 062, PaymentStagesV2/21h) — deze pagina keek tot nu toe
                // enkel naar de groep-brede InvoicingPaymentStages.Invoicable-vlag, dus een schijf die via
                // Betalingsschijven per eenheid als bereikt aangeduid werd kwam hier nooit als "klaar om te
                // factureren" naar boven (Niels 2026-09-30, gemeld bug). Zelfde OR-logica als
                // ProjectenController.PaymentStagesV2.cs se IsReached.
                var reachedPairs = _db.UnitPaymentStageReached.AsNoTracking()
                    .Where(r => unitIds.Contains(r.UnitId))
                    .Select(r => new { r.PaymentStageId, r.UnitId })
                    .ToList()
                    .Select(x => (x.PaymentStageId, x.UnitId))
                    .ToHashSet();

                foreach (var u in allUnits)
                {
                    if (!u.ClientAccountId.HasValue || !clientsById.TryGetValue(u.ClientAccountId.Value, out var client)) continue;
                    var card = CardFor(client);
                    var unitName = u.Type != null ? $"{u.Type.Name} {u.Name}".Trim() : u.Name ?? "";
                    if (!string.IsNullOrWhiteSpace(unitName))
                    {
                        var names = unitNamesByAccount.TryGetValue(client.Id, out var l) ? l : unitNamesByAccount[client.Id] = new List<string>();
                        if (!names.Contains(unitName)) names.Add(unitName);
                    }

                    foreach (var cv in u.UnitConstructionValue.Where(v => v.ValueSold > 0 && v.PaymentGroupId.HasValue))
                    {
                        var stages = StagesFor(cv.PaymentGroupId!.Value);
                        for (var i = 0; i < stages.Count; i++)
                        {
                            var stage = stages[i];
                            var amount = Math.Round(cv.ValueSold!.Value * stage.Percentage / 100m, 2, MidpointRounding.AwayFromZero);
                            if (amount <= 0m) continue;
                            if (!groupNameByAccount.ContainsKey(client.Id) && !string.IsNullOrWhiteSpace(stage.Group?.Name))
                                groupNameByAccount[client.Id] = stage.Group!.Name;

                            var invoiced = invoicedStagePairs.Contains((stage.Id, u.Id));
                            var reached = stage.Invoicable || reachedPairs.Contains((stage.Id, u.Id));
                            var segmentState = invoiced ? "invoiced" : !reached ? "not-reached" : "open";
                            card.ProgressSegments.Add(new InvoicingProgressSegmentV2 { Percentage = stage.Percentage, State = segmentState });

                            if (invoiced) { AddProgress(progressInvoiced, client.Id, amount); continue; }
                            if (!reached) { AddProgress(progressNotReached, client.Id, amount); continue; }

                            AddProgress(progressOpen, client.Id, amount);
                            card.Rows.Add(new InvoicingPostRowV2
                            {
                                Kind = "Schijf",
                                Description = stage.Name,
                                SubText = $"schijf {i + 1} van {stages.Count}",
                                UnitName = unitName,
                                Amount = amount,
                                Percentage = stage.Percentage,
                                VatPercentage = stage.VatPercentage,
                                UnitId = u.Id,
                                StageId = stage.Id,
                                IsLastStage = i == stages.Count - 1,
                            });
                        }
                    }
                }
            }

            // ===== Meer-/minwerken die al klaar staan (DateAgreement != null — de ondertekening is rond) =====
            var respCo = _projectService.GetProjectInvoicableChangeOrders(projectid);
            if (respCo.Success && respCo.Values.Count > 0)
            {
                var accountIds = respCo.Values.Select(v => v.ClientAccountID).Distinct().ToList();
                var clientsResp = _clientService.GetClientAccountByIds(accountIds);
                var clientsById = clientsResp.Success ? clientsResp.Values.ToDictionary(c => c.Id) : new Dictionary<int, ClientAccountBO>();

                var detailIds = respCo.Values.SelectMany(co => co.Details ?? new List<ChangeOrderDetailBO>()).Select(d => d.Id).Distinct().ToList();
                var invoicedAmounts = _db.InvoicesDetails.AsNoTracking()
                    .Where(d => d.LineType == "ChangeOrders" && d.ChangeOrderDetailId.HasValue && detailIds.Contains(d.ChangeOrderDetailId.Value))
                    .GroupBy(d => d.ChangeOrderDetailId!.Value)
                    .Select(g => new { DetailId = g.Key, Amount = g.Sum(x => x.Price ?? 0m) })
                    .ToDictionary(x => x.DetailId, x => x.Amount);

                foreach (var co in respCo.Values)
                {
                    if (!clientsById.TryGetValue(co.ClientAccountID, out var client)) continue;
                    var card = CardFor(client);
                    var woLabel = $"WO-{co.Id}";
                    var signedSub = co.DateAgreement.HasValue ? $"ondertekend {co.DateAgreement.Value:dd/MM/yyyy}" : null;
                    foreach (var detail in co.Details.Where(d => d.Invoicable != false))
                    {
                        var total = detail.Totaal;
                        invoicedAmounts.TryGetValue(detail.Id, out var invoiced);
                        var remaining = Math.Round(total - invoiced, 2, MidpointRounding.AwayFromZero);
                        if (remaining <= 0m) continue;

                        var detailDesc = string.IsNullOrWhiteSpace(detail.Description) ? co.Description : detail.Description;
                        card.Rows.Add(new InvoicingPostRowV2
                        {
                            Kind = total < 0m ? "Minwerk" : "Meerwerk",
                            Description = $"{woLabel} · {detailDesc}",
                            SubText = signedSub,
                            Moment = "na ondertekening",
                            Amount = remaining,
                            Percentage = 100m,
                            VatPercentage = detail.VatPercentage ?? 21m,
                            ChangeOrderId = co.Id,
                            ChangeOrderDetailId = detail.Id,
                        });
                    }
                }
            }

            // ===== Geblokkeerde WO's (scherm 21b): zelfde filter als GetProjectInvoicableChangeOrders,
            // maar DateAgreement is nog null — de ondertekening moet nog rond komen. Staat, net als in
            // 20a, gewoon TUSSEN de andere posten van hetzelfde account (niet aanvinkbaar, rode rij). =====
            var blockedEntities = _db.ChangeOrder.AsNoTracking()
                .Where(m => m.ContractActivity.Contract.ProjectId == projectid
                    && m.Invoiceable
                    && m.DateAgreement == null
                    && m.ClientAccountId > 0
                    && m.ChangeOrderDetail.Any(i => i.Invoicable != false && i.Invoiced != true))
                .Select(m => new
                {
                    m.Id,
                    m.Description,
                    m.ClientAccountId,
                    Amount = m.ChangeOrderDetail.Where(d => d.Invoicable != false && d.Invoiced != true)
                        .Sum(d => (d.Number * d.Price) * (1m + d.Commission / 100m))
                })
                .ToList();

            if (blockedEntities.Count > 0)
            {
                var blockedAccountIds = blockedEntities.Select(b => b.ClientAccountId).Distinct().ToList();
                var blockedClientsResp = _clientService.GetClientAccountByIds(blockedAccountIds);
                var blockedClientsById = blockedClientsResp.Success ? blockedClientsResp.Values.ToDictionary(c => c.Id) : new Dictionary<int, ClientAccountBO>();
                var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();

                foreach (var b in blockedEntities)
                {
                    if (!blockedClientsById.TryGetValue(b.ClientAccountId, out var client)) continue;
                    var activeCase = await signing.GetActiveCaseForSourceAsync(CPMCore.Services.Signing.ChangeOrderSigningSource.Key, b.Id, ct);
                    var card = CardFor(client);
                    var statusLabel = activeCase is null ? "nog niet aangeboden om te tekenen" : SigningLabels.CaseStatus(activeCase.Status).ToLowerInvariant();
                    card.Rows.Add(new InvoicingPostRowV2
                    {
                        Kind = "Meerwerk",
                        Description = $"WO-{b.Id} · {b.Description ?? "Wijzigingsopdracht"}",
                        SubText = statusLabel,
                        Moment = "blokkeert",
                        Amount = b.Amount,
                        IsBlocked = true,
                        ChangeOrderId = b.Id,
                        SigningCaseId = activeCase?.CaseId,
                        SigningStatusLabel = statusLabel,
                    });
                }
            }

            // Elk klantenaccount krijgt een kaart, ook zonder open posten (Niels, 2026-09-30) — geen
            // Rows.Count-filter meer.
            vm.Accounts = cardsByAccount.Values.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var card in vm.Accounts)
            {
                var units = unitNamesByAccount.TryGetValue(card.ClientAccountId, out var un) ? string.Join(", ", un) : null;
                var group = groupNameByAccount.TryGetValue(card.ClientAccountId, out var gn) ? gn : null;
                card.Subtitle = string.Join(" · ", new[] { units, group is null ? null : $"betalingsgroep {group}" }.Where(s => !string.IsNullOrWhiteSpace(s)));

                var invoicedAmt = progressInvoiced.TryGetValue(card.ClientAccountId, out var pi) ? pi : 0m;
                var openAmt = progressOpen.TryGetValue(card.ClientAccountId, out var po) ? po : 0m;
                var notReachedAmt = progressNotReached.TryGetValue(card.ClientAccountId, out var pn) ? pn : 0m;
                var progressTotal = invoicedAmt + openAmt + notReachedAmt;
                if (progressTotal > 0m)
                {
                    card.ProgressInvoicedPct = Math.Round(invoicedAmt / progressTotal * 100m, 2, MidpointRounding.AwayFromZero);
                    card.ProgressOpenPct = Math.Round(openAmt / progressTotal * 100m, 2, MidpointRounding.AwayFromZero);
                    card.ProgressNotReachedPct = Math.Max(0m, 100m - card.ProgressInvoicedPct - card.ProgressOpenPct);
                }
            }

            // ===== Gefactureerd (21j): alle facturen van dit project met een schijf- of WO-lijn. =====
            var invoiceQuery = HttpContext.RequestServices.GetRequiredService<IInvoiceQueryService>();
            var allInvoices = await invoiceQuery.GetByProjectAsync(projectid, ct);
            var relevantInvoiceIds = allInvoices.Where(i => !i.IsSupplier).Select(i => i.Id).ToList();
            if (relevantInvoiceIds.Count > 0)
            {
                var lineTypesByInvoice = _db.InvoicesDetails.AsNoTracking()
                    .Where(d => relevantInvoiceIds.Contains(d.InvoiceId) && (d.LineType == "Stages" || d.LineType == "ChangeOrders"))
                    .Select(d => new { d.InvoiceId, d.LineType })
                    .ToList()
                    .GroupBy(d => d.InvoiceId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.LineType).Distinct().ToList());

                foreach (var inv in allInvoices.Where(i => !i.IsSupplier))
                {
                    if (!lineTypesByInvoice.TryGetValue(inv.Id, out var types)) continue;
                    var kind = types.Contains("Stages") && types.Contains("ChangeOrders") ? "gemengd"
                        : types.Contains("Stages") ? "schijven" : "wijzigingsopdrachten";
                    vm.InvoicedRows.Add(new InvoicingInvoiceRowV2
                    {
                        InvoiceId = inv.Id,
                        PublicId = inv.PublicId ?? "concept",
                        InvoiceDate = inv.InvoiceDate,
                        ClientName = inv.ClientName ?? "",
                        StatusLabel = inv.StatusName ?? "",
                        StatusTone = InvoiceStatusTone(inv.StatusId),
                        AmountExVat = inv.NetTotal ?? inv.GrossTotal ?? 0m,
                        Kind = kind,
                    });
                }
            }
            vm.InvoicedRows = vm.InvoicedRows.OrderByDescending(r => r.InvoiceDate).ToList();

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid = projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(InvoicingV2), "Projecten", "Facturatie") { Parent = projectDetail, RouteValues = new { projectid = projectid } };

            return View(vm);
        }

        private static string InvoiceStatusTone(int? statusId) => (InvoiceStatusId?)statusId switch
        {
            InvoiceStatusId.Paid or InvoiceStatusId.Booked => "is-positive",
            InvoiceStatusId.Issued or InvoiceStatusId.Sent or InvoiceStatusId.PartiallyPaid => "is-attention",
            InvoiceStatusId.Overdue or InvoiceStatusId.Cancelled => "is-blocked",
            _ => "is-neutral",
        };

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // "Facturen opmaken" — voorstel (21a/22i), puur berekend, schrijft niets. Hergebruikt dezelfde
        // BuildStageInvoiceDraft/BuildChangeOrderInvoiceDraft (ProjectenController.cs) als MakeInvoices/
        // MakeInvoicesCO, dus exact dezelfde eigenaar-splitsing — enkel zonder CreateWithLinesAsync.
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        public class PreviewInvoicesRequest
        {
            public List<ClientAccountUnitInvoiceBO> Stages { get; set; } = new();
            public List<ClientAccountChangeOrderInvoiceBO> ChangeOrders { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PreviewInvoices([FromBody] PreviewInvoicesRequest request)
        {
            request ??= new PreviewInvoicesRequest();
            var owners = new List<InvoicePreviewOwnerV2>();
            var notices = new List<string>();

            using var uow = _uow;
            var db = (cpmRunningContext)uow.Context;

            if (request.Stages.Count > 0)
            {
                var clientIds = request.Stages.Select(x => x.ClientAccountId).Distinct().ToList();
                var clientResp = _clientService.GetClientAccountByIds(clientIds);
                var clientAccounts = clientResp.Success ? clientResp.Values : new List<ClientAccountBO>();

                foreach (var client in clientAccounts)
                {
                    var iu = new List<UnitWithStagesBO>();
                    foreach (var unitId in request.Stages.Where(i => i.ClientAccountId == client.Id).Select(i => i.Unitid).Distinct())
                    {
                        var ru = _unitService.GetUnitById(unitId);
                        if (!ru.Success) continue;
                        var unitbo = new UnitWithStagesBO { Unit = ru.Value };
                        foreach (var item in request.Stages.Where(i => i.Unitid == unitbo.Unit.Id))
                        {
                            var rStage = _projectService.GetProjectPaymentStage(item.StageId);
                            if (rStage.Success) unitbo.PaymentStages.Add(rStage.Value);
                        }
                        iu.Add(unitbo);
                    }

                    var project = new ProjectBO();
                    var settings = new ProjectSalesSettingsBO();
                    if (iu.Count > 0)
                    {
                        var pr = _projectService.GetProjectByID(iu.First().Unit.ProjectId);
                        if (pr.Success)
                        {
                            project = pr.Value;
                            var sr = _projectService.GetSalesSettings(project.Id);
                            if (sr.Success) settings = sr.Value;
                        }
                    }

                    var stageIds = request.Stages.Where(i => i.ClientAccountId == client.Id).Select(i => i.StageId).Distinct().ToList();
                    if (stageIds.Count == 0 || project.Id <= 0) continue;

                    var issuerCompanyId = project.IssuerCompanyIdBuilder ?? request.Stages.First().CompanyId;
                    if (issuerCompanyId <= 0) { notices.Add($"{client.DisplayName}: geen facturatiebedrijf ingesteld voor het project."); continue; }

                    var coowners = db.ClientContacts.AsNoTracking()
                        .Where(cc => cc.ClientAccountId == client.Id && cc.IsCoOwner && cc.CoOwnerPercentage.HasValue)
                        .Select(cc => new { cc.Id, cc.Name, cc.Forename, cc.CoOwnerPercentage })
                        .ToList();
                    var coOwnerTotal = coowners.Sum(c => c.CoOwnerPercentage ?? 0m);
                    var mainOwnerShare = Math.Max(0m, 100m - coOwnerTotal);
                    var paymentGroupId = iu.SelectMany(u => u.PaymentStages).FirstOrDefault(s => stageIds.Contains(s.Id))?.GroupId;

                    var mainDraft = BuildStageInvoiceDraft(issuerCompanyId, client.Id, null, stageIds, project, settings, iu, paymentGroupId, mainOwnerShare, db);
                    if (mainDraft != null) owners.Add(ToPreviewOwner(client.DisplayName, mainOwnerShare, mainDraft));

                    foreach (var co in coowners)
                    {
                        if (co.CoOwnerPercentage.GetValueOrDefault() <= 0m) continue;
                        var coDraft = BuildStageInvoiceDraft(issuerCompanyId, null, co.Id, stageIds, project, settings, iu, paymentGroupId, co.CoOwnerPercentage ?? 0m, db);
                        if (coDraft != null) owners.Add(ToPreviewOwner($"{co.Name} {co.Forename}".Trim(), co.CoOwnerPercentage ?? 0m, coDraft));
                    }
                }
            }

            if (request.ChangeOrders.Count > 0)
            {
                var clientIds = request.ChangeOrders.Select(x => x.ClientAccountId).Distinct().ToList();
                var clientResp = _clientService.GetClientAccountByIds(clientIds);
                var clientAccounts = clientResp.Success ? clientResp.Values : new List<ClientAccountBO>();
                var selectedDetailIds = request.ChangeOrders.Select(i => i.ChangeOrderDetailId).Distinct().ToList();
                var alreadyInvoicedByDetail = db.InvoicesDetails.AsNoTracking()
                    .Where(d => d.LineType == "ChangeOrders" && d.ChangeOrderDetailId.HasValue && selectedDetailIds.Contains(d.ChangeOrderDetailId.Value))
                    .GroupBy(d => d.ChangeOrderDetailId!.Value)
                    .Select(g => new { DetailId = g.Key, Amount = g.Sum(x => x.Price ?? 0m) })
                    .ToDictionary(x => x.DetailId, x => x.Amount);

                foreach (var client in clientAccounts)
                {
                    var changeOrders = new List<ChangeOrderBO>();
                    foreach (var coId in request.ChangeOrders.Where(i => i.ClientAccountId == client.Id).Select(i => i.ChangeOrderId).Distinct())
                    {
                        var rco = _projectService.GetChangeOrder(coId);
                        if (!rco.Success) continue;
                        var co = rco.Value;
                        for (int i = co.Details.Count - 1; i >= 0; i--)
                        {
                            var keep = request.ChangeOrders.Any(l => l.ChangeOrderDetailId == co.Details[i].Id);
                            if (!keep) co.Details.RemoveAt(i);
                        }
                        changeOrders.Add(co);
                    }

                    var project = new ProjectBO();
                    if (changeOrders.Count > 0)
                    {
                        var pr = _projectService.GetProjectByID(changeOrders.First().ProjectId);
                        if (pr.Success) project = pr.Value;
                    }
                    if (changeOrders.Count == 0 || project.Id <= 0) continue;

                    var issuerCompanyId = project.IssuerCompanyIdBuilder ?? request.ChangeOrders.FirstOrDefault()?.CompanyId;
                    if (!issuerCompanyId.HasValue || issuerCompanyId.Value <= 0) { notices.Add($"{client.DisplayName}: geen facturatiebedrijf ingesteld voor het project."); continue; }

                    var selectedRows = request.ChangeOrders.Where(i => i.ClientAccountId == client.Id).ToList();
                    var coowners = db.ClientContacts.AsNoTracking()
                        .Where(cc => cc.ClientAccountId == client.Id && cc.IsCoOwner && cc.CoOwnerPercentage.HasValue)
                        .Select(cc => new { cc.Id, cc.Name, cc.Forename, cc.CoOwnerPercentage })
                        .ToList();
                    var coOwnerTotal = coowners.Sum(c => c.CoOwnerPercentage ?? 0m);
                    var mainOwnerShare = Math.Max(0m, 100m - coOwnerTotal);

                    var mainDraft = BuildChangeOrderInvoiceDraft(issuerCompanyId, client.Id, null, mainOwnerShare, changeOrders, selectedRows, alreadyInvoicedByDetail, project);
                    if (mainDraft != null) owners.Add(ToPreviewOwner(client.DisplayName, mainOwnerShare, mainDraft));

                    foreach (var co in coowners)
                    {
                        if (co.CoOwnerPercentage.GetValueOrDefault() <= 0m) continue;
                        var coDraft = BuildChangeOrderInvoiceDraft(issuerCompanyId, null, co.Id, co.CoOwnerPercentage ?? 0m, changeOrders, selectedRows, alreadyInvoicedByDetail, project);
                        if (coDraft != null) owners.Add(ToPreviewOwner($"{co.Name} {co.Forename}".Trim(), co.CoOwnerPercentage ?? 0m, coDraft));
                    }
                }
            }

            var vm = new InvoicePreviewV2Vm { Owners = owners, Notices = notices };
            return PartialView("Modals/_ModalMakeInvoicesV2", vm);
        }

        private static InvoicePreviewOwnerV2 ToPreviewOwner(string ownerName, decimal percentage, InvoiceDraftBO draft) => new()
        {
            OwnerName = ownerName,
            Percentage = percentage,
            Lines = draft.Lines.Select(l => new InvoicePreviewLineV2 { Text = l.Text, Amount = l.Price }).ToList(),
            Total = draft.Lines.Sum(l => l.Price),
        };
    }
}
