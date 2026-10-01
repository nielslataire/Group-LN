using BOCore;
using CPMCore.Configuration;
using CPMCore.Models.Projecten;
using DALCore.Models;
using FacadeCore;
using FacadeCore.Signing;
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
    /// <summary>gl-v2 "Wijzigingsopdracht" (design-handoff 20d) — detail/bewerken van één WO, inclusief
    /// het facturatieplan (migratie 064). Eigen bestand naast de legacy AddChangeOrder/EditChangeOrder-
    /// acties, die ongewijzigd blijven. Leest/schrijft rechtstreeks via _db (zelfde stijl als
    /// PaymentStagesV2.cs/InvoicingV2.cs) i.p.v. via ChangeOrderBO/ChangeOrderTranslator — de nieuwe
    /// offerte-/facturatieplan-velden zitten niet in die oudere BO-laag en dit voorkomt een split-brain
    /// tussen twee opslagpaden voor dezelfde rij.</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2(int projectid, int? clientid, int coid = 0)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var canWrite = _ps.HasWrite(PermissionCodes.ProjectsChangeOrders);

            var projectResponse = _projectService.GetProjectByID(projectid);
            if (!projectResponse.Success || projectResponse.Value is null) return NotFound();
            var project = projectResponse.Value;

            var vm = new ChangeOrderDetailV2Vm { ProjectId = projectid, ProjectName = project.Name, CanWrite = canWrite, IsNew = coid <= 0 };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid = projectid } };
            var listNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("ChangeOrdersV2", "Projecten", "Offertes & wijzigingen") { Parent = projectDetail, RouteValues = new { projectid = projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(ChangeOrderDetailV2), "Projecten", vm.IsNew ? "Nieuw" : "Bewerken") { Parent = listNode, RouteValues = new { projectid, coid } };

            ChangeOrder co = null;
            if (coid > 0)
            {
                co = await _db.ChangeOrder.AsNoTracking()
                    .Include(c => c.ChangeOrderDetail)
                    .Include(c => c.ChangeOrderPaymentTerm)
                    .FirstOrDefaultAsync(c => c.Id == coid);
                if (co is null) return NotFound();
                vm.ChangeOrderId = co.Id;
            }

            vm.ClientAccountId = co?.ClientAccountId ?? clientid ?? 0;
            vm.IsQuote = co?.IsQuote ?? true; // een nieuwe, nog niet opgeslagen rij start als offerte
            vm.Description = co?.Description ?? "";
            vm.InvoiceableByBouwheer = co?.Invoiceable ?? false;
            vm.ContractActivityId = co?.ContractActivityId ?? 0;
            vm.QuoteSupplierReference = co?.QuoteSupplierReference;
            vm.QuoteVatPercentage = co?.QuoteVatPercentage;
            vm.QuoteDate = co?.Date ?? DateOnly.FromDateTime(DateTime.Today);
            vm.ExpirationDate = co?.ExpirationDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30));
            vm.QuoteConvertedAt = co?.QuoteConvertedAt;
            vm.ConditionsText = co?.ChangeOrderConditions ?? "";

            if (vm.ClientAccountId > 0)
            {
                var clientResponse = _clientService.GetClientAccountById(vm.ClientAccountId);
                vm.ClientName = clientResponse.Success ? clientResponse.Values.FirstOrDefault()?.Name ?? "" : "";

                var unit = await _db.Units.AsNoTracking()
                    .Where(u => u.ProjectId == projectid && u.ClientAccountId == vm.ClientAccountId)
                    .Include(u => u.Type)
                    .Include(u => u.UnitConstructionValue)
                    .FirstOrDefaultAsync();
                if (unit != null)
                {
                    vm.UnitName = unit.Type != null ? $"{unit.Type.Name} {unit.Name}".Trim() : unit.Name;
                    var cv = unit.UnitConstructionValue.FirstOrDefault(v => v.PaymentGroupId.HasValue);
                    if (cv?.PaymentGroupId is int groupId)
                    {
                        var group = await _db.InvoicingPaymentGroup.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId);
                        if (group != null)
                        {
                            vm.VatKlantPercentage = group.VatPercentage ?? 21m;
                            vm.VatKlantLabel = $"{Pct(vm.VatKlantPercentage)} % — {group.Name}";
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(vm.VatKlantLabel)) vm.VatKlantLabel = "onbekend — koppel een eenheid aan een betalingsgroep";

            vm.ContractActivities = _projectService.GetProjectContractActivitiesForSelect(projectid) is { Success: true } actResp
                ? actResp.Values : new List<IdNameBO>();
            if (vm.ClientAccountId <= 0)
                vm.ClientAccounts = _clientService.GetClientAccountsByProjectIdForSelect(projectid) is { Success: true } clResp
                    ? clResp.Values.OrderBy(c => c.Display).ToList() : new List<IdNameBO>();

            vm.Stages = _db.InvoicingPaymentStages.AsNoTracking()
                .Where(s => s.Group.ProjectId == projectid)
                .OrderBy(s => s.Id)
                .Select(s => new IdNameBO { ID = s.Id, Display = s.Name })
                .ToList();

            if (co != null)
            {
                vm.Rows = co.ChangeOrderDetail.Select(d => new ChangeOrderDetailRowV2
                {
                    Id = d.Id,
                    Description = d.Description,
                    MeasurementType = d.MeasurementType ?? (int)MeasurementType.Forfait,
                    MeasurementUnit = d.MeasurementUnit ?? (int)MeasurementUnit.stuk,
                    Number = d.Number,
                    Price = d.Price,
                    Commission = d.Commission,
                    VatPercentage = d.VatPercentage ?? vm.VatKlantPercentage,
                    NeedsReview = d.NeedsReview,
                    SourceImagePath = d.SourceImagePath,
                }).ToList();

                var invoicedTermIds = await _db.InvoicesDetails.AsNoTracking()
                    .Where(x => x.LineType == "ChangeOrderTerm" && x.ChangeOrderPaymentTermId.HasValue
                        && co.ChangeOrderPaymentTerm.Select(t => t.Id).Contains(x.ChangeOrderPaymentTermId.Value))
                    .Select(x => x.ChangeOrderPaymentTermId!.Value)
                    .ToListAsync();

                var totalExcl = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m));
                vm.Terms = co.ChangeOrderPaymentTerm.OrderBy(t => t.SortOrder).Select(t => new ChangeOrderTermV2
                {
                    Id = t.Id,
                    Kind = t.Kind,
                    Percentage = t.Percentage,
                    FixedAmount = t.FixedAmount,
                    TriggerType = t.TriggerType,
                    TriggerStageId = t.TriggerStageId,
                    TriggerLabel = TriggerLabel(t.TriggerType, t.TriggerStageId, vm.Stages),
                    AmountExVat = t.Percentage.HasValue ? Math.Round(totalExcl * t.Percentage.Value / 100m, 2, MidpointRounding.AwayFromZero) : (t.FixedAmount ?? 0m),
                    IsInvoiced = invoicedTermIds.Contains(t.Id),
                }).ToList();
            }

            // Ondertekening (rechterkolom) — rechtstreeks de signing-status, geen eigen kopie.
            var features = HttpContext.RequestServices.GetRequiredService<IOptions<FeatureFlagsOptions>>().Value;
            vm.SigningEnabled = features.EnableSigning;
            int? signingStatus = null;
            if (features.EnableSigning && coid > 0)
            {
                var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
                vm.ActiveSigningCase = await signing.GetActiveCaseForSourceAsync(Services.Signing.ChangeOrderSigningSource.Key, coid, HttpContext.RequestAborted);
                vm.CompletedSigningCase = await signing.GetCompletedCaseForSourceAsync(Services.Signing.ChangeOrderSigningSource.Key, coid, HttpContext.RequestAborted);
                vm.CanStartSigning = _ps.HasWrite(PermissionCodes.Signing);
                signingStatus = vm.ActiveSigningCase?.Status ?? vm.CompletedSigningCase?.Status;
            }
            vm.IsLocked = vm.ActiveSigningCase != null;
            if (vm.IsLocked) vm.LockedReason = "Er loopt een elektronische ondertekening voor deze wijzigingsopdracht.";

            var hasTerms = vm.Terms.Count > 0;
            var hasInvoicableTerm = vm.Terms.Any(t => !t.IsInvoiced && IsTermTriggered(t));
            var allTermsInvoiced = hasTerms && vm.Terms.All(t => t.IsInvoiced);
            var input = new ChangeOrderStatusInput(
                IsQuote: vm.IsQuote,
                ExpirationDate: vm.ExpirationDate,
                DateSendToClient: co?.DateSendToClient,
                DateAgreement: co?.DateAgreement,
                SigningCaseStatus: signingStatus,
                HasInvoicableTerm: hasInvoicableTerm,
                AllTermsInvoiced: allTermsInvoiced,
                AllInvoicesPaid: false); // TODO stap 5 (facturatie-integratie): koppelen aan Invoices.StatusId
            vm.Status = ChangeOrderStatusHelper.Compute(input, DateOnly.FromDateTime(DateTime.Today));
            vm.Steps = BuildSteps(vm, co);

            vm.History = BuildHistory(vm, co);

            return View(vm);
        }

        private static string Pct(decimal v) => v.ToString("0.##");

        private static string TriggerLabel(byte triggerType, int? stageId, List<IdNameBO> stages) => triggerType switch
        {
            1 => "Na ondertekening",
            2 => "Bij schijf" + (stageId.HasValue ? $" · {stages.FirstOrDefault(s => s.ID == stageId.Value)?.Display}" : ""),
            3 => "Manueel vrijgeven",
            _ => "",
        };

        /// <summary>Een termijn is "bereikt" (klaar om te factureren) zodra haar trigger vervuld is —
        /// NaOndertekening geldt zodra de WO getekend is, BijSchijf zodra die schijf bereikt/gefactureerd
        /// is (PaymentStagesV2-stijl), Manueel enkel na een expliciete vrijgave (ReleasedAt).</summary>
        private bool IsTermTriggered(ChangeOrderTermV2 t) => t.TriggerType switch
        {
            1 => true, // getekend-check gebeurt al op WO-niveau vóór deze helper aangeroepen wordt
            3 => false, // vrijgave gebeurt via een aparte actie, hier optimistisch false tot dat gebouwd is
            _ => false, // "BijSchijf" verificatie volgt in stap 5 (facturatie-integratie)
        };

        private static List<ChangeOrderStepV2> BuildSteps(ChangeOrderDetailV2Vm vm, ChangeOrder co)
        {
            var steps = new List<ChangeOrderStepV2>();
            string StateFor(ChangeOrderStatus at) => vm.Status == at ? "current" : vm.Status > at ? "done" : "future";

            if (vm.IsQuote)
            {
                steps.Add(new ChangeOrderStepV2 { Label = "Offerte", State = vm.Status == ChangeOrderStatus.Verlopen ? "current" : "current", Hint = vm.Status == ChangeOrderStatus.Verlopen ? "verlopen" : null });
            }
            else
            {
                steps.Add(new ChangeOrderStepV2 { Label = "Offerte", State = "done", Hint = vm.QuoteSupplierReference });
            }
            steps.Add(new ChangeOrderStepV2 { Label = "Opgemaakt", State = vm.IsQuote ? "future" : StateFor(ChangeOrderStatus.Opgemaakt) });
            steps.Add(new ChangeOrderStepV2 { Label = "Verzonden", State = vm.IsQuote ? "future" : StateFor(ChangeOrderStatus.Verzonden), Hint = co?.DateSendToClient?.ToString("dd/MM/yyyy") });
            steps.Add(new ChangeOrderStepV2 { Label = "Ondertekend", State = vm.IsQuote ? "future" : StateFor(ChangeOrderStatus.Ondertekend), Hint = co?.DateAgreement?.ToString("dd/MM/yyyy") });
            steps.Add(new ChangeOrderStepV2 { Label = "Factureerbaar", State = vm.IsQuote ? "future" : StateFor(ChangeOrderStatus.Factureerbaar) });
            steps.Add(new ChangeOrderStepV2 { Label = "Gefactureerd", State = vm.IsQuote ? "future" : StateFor(ChangeOrderStatus.Gefactureerd) });
            steps.Add(new ChangeOrderStepV2 { Label = "Betaald", State = vm.IsQuote ? "future" : StateFor(ChangeOrderStatus.Betaald) });

            if (vm.Status == ChangeOrderStatus.Geweigerd || vm.Status == ChangeOrderStatus.Geannuleerd)
            {
                steps.Add(new ChangeOrderStepV2 { Label = vm.Status == ChangeOrderStatus.Geweigerd ? "Geweigerd" : "Geannuleerd", State = "current" });
            }
            return steps;
        }

        private static List<ChangeOrderHistoryItemV2> BuildHistory(ChangeOrderDetailV2Vm vm, ChangeOrder co)
        {
            var items = new List<ChangeOrderHistoryItemV2>();
            if (co is null) return items;
            items.Add(new ChangeOrderHistoryItemV2 { When = vm.QuoteDate.ToDateTime(TimeOnly.MinValue), Label = vm.IsQuote || vm.QuoteConvertedAt.HasValue ? "Offerte ingelezen" : "Wijzigingsopdracht aangemaakt" });
            if (vm.QuoteConvertedAt.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = vm.QuoteConvertedAt, Label = "Omgezet naar wijzigingsopdracht" });
            if (co.DateSendToClient.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = co.DateSendToClient.Value.ToDateTime(TimeOnly.MinValue), Label = "Verzonden naar klant" });
            if (co.DateAgreement.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = co.DateAgreement.Value.ToDateTime(TimeOnly.MinValue), Label = "Ondertekend" });
            return items.OrderByDescending(i => i.When).ToList();
        }

        [HttpGet]
        public IActionResult ChangeOrderDetailV2AddRow(int index, decimal vatKlant)
        {
            ViewData["Index"] = index;
            ViewData["VatKlant"] = vatKlant;
            ViewData["IsLocked"] = false;
            var row = new ChangeOrderDetailRowV2 { Number = 1, VatPercentage = vatKlant };
            return PartialView("Partials/_ChangeOrderDetailRowV2", row);
        }

        [HttpGet]
        public IActionResult ChangeOrderDetailV2AddTerm(int index, int projectId, byte kind = 2, decimal? percentage = null)
        {
            var stages = _db.InvoicingPaymentStages.AsNoTracking()
                .Where(s => s.Group.ProjectId == projectId)
                .OrderBy(s => s.Id)
                .Select(s => new IdNameBO { ID = s.Id, Display = s.Name })
                .ToList();
            ViewData["Index"] = index;
            ViewData["Stages"] = stages;
            ViewData["IsLocked"] = false;
            var term = new ChangeOrderTermV2
            {
                Kind = kind,
                TriggerType = kind == 3 ? (byte)2 : (byte)1,
                Percentage = percentage,
            };
            return PartialView("Partials/_ChangeOrderTermRowV2", term);
        }

        /// <summary>21c ("omzetten") — in-place: IsQuote wordt false, QuoteConvertedAt wordt nu. Geen
        /// kopieerstap (beslissing Niels 2026-09-30): dit blijft dezelfde ChangeOrder-rij.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2Convert(int projectId, int changeOrderId)
        {
            var co = await _db.ChangeOrder.FirstOrDefaultAsync(c => c.Id == changeOrderId);
            if (co is null) return NotFound();

            if (!co.IsQuote)
            {
                AddMessage("info", "Deze offerte is al omgezet.", "Al omgezet");
            }
            else
            {
                co.IsQuote = false;
                co.QuoteConvertedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                AddMessage("success", "Omgezet naar wijzigingsopdracht. Vul commissie, facturatieplan en omschrijving voor de klant aan vóór verzenden.", "Omgezet");
            }
            return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = changeOrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2Save(ChangeOrderDetailV2SaveModel model)
        {
            var activeSigning = await ActiveSigningCaseAsync(model.ChangeOrderId);
            if (activeSigning is not null)
            {
                AddMessage("error", SigningLockedMessage, "Niet bewaard");
                return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId });
            }

            if (model.ClientAccountId <= 0 || model.ContractActivityId <= 0)
            {
                AddMessage("error", "Kies een klant/eenheid en een leverancier·contract.", "Kon niet opslaan");
                // Terug naar het scherm van herkomst — bij een nog niet opgeslagen offerte uit 20c zou een
                // omleiding naar een leeg 20d de net ingelezen regels kwijtspelen.
                return model.ReturnTo == "quote"
                    ? RedirectToAction(nameof(QuoteIntakeV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId })
                    : RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId });
            }

            ChangeOrder co;
            if (model.ChangeOrderId > 0)
            {
                co = await _db.ChangeOrder
                    .Include(c => c.ChangeOrderDetail)
                    .Include(c => c.ChangeOrderPaymentTerm)
                    .FirstOrDefaultAsync(c => c.Id == model.ChangeOrderId);
                if (co is null) return NotFound();
            }
            else
            {
                co = new ChangeOrder
                {
                    ClientAccountId = model.ClientAccountId,
                    IsQuote = true,
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                };
                _db.ChangeOrder.Add(co);
            }

            co.Description = (model.Description ?? "").Trim();
            co.ContractActivityId = model.ContractActivityId;
            co.Invoiceable = model.InvoiceableByBouwheer;
            co.QuoteSupplierReference = string.IsNullOrWhiteSpace(model.QuoteSupplierReference) ? null : model.QuoteSupplierReference.Trim();
            co.QuoteVatPercentage = model.QuoteVatPercentage;
            co.ChangeOrderConditions = model.ConditionsText;

            SyncRows(co, model.Rows ?? new List<ChangeOrderDetailRowV2>());
            SyncTerms(co, model.Terms ?? new List<ChangeOrderTermV2>());

            var converted = false;
            if (model.ConvertAfterSave && co.IsQuote)
            {
                co.IsQuote = false;
                co.QuoteConvertedAt = DateTime.Now;
                converted = true;
            }

            await _db.SaveChangesAsync();

            AddMessage("success", converted ? "Omgezet naar wijzigingsopdracht. Vul commissie, facturatieplan en omschrijving voor de klant aan vóór verzenden." : "Wijzigingsopdracht opgeslagen.", converted ? "Omgezet" : "Opgeslagen");
            return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = co.Id });
        }

        private void SyncRows(ChangeOrder co, List<ChangeOrderDetailRowV2> posted)
        {
            var postedIds = posted.Where(r => r.Id > 0).Select(r => r.Id).ToHashSet();
            foreach (var stale in co.ChangeOrderDetail.Where(d => !postedIds.Contains(d.Id)).ToList())
                _db.ChangeOrderDetail.Remove(stale);

            foreach (var r in posted)
            {
                if (string.IsNullOrWhiteSpace(r.Description)) continue;
                var entity = r.Id > 0 ? co.ChangeOrderDetail.FirstOrDefault(d => d.Id == r.Id) : null;
                if (entity is null)
                {
                    entity = new ChangeOrderDetail { ChangeOrderId = co.Id };
                    co.ChangeOrderDetail.Add(entity);
                }
                // Kolom is nvarchar(1000) sinds migratie 065 (was 250 — te kort zodra 20c de specificatie-
                // rijen in de omschrijving vouwt); ingekort i.p.v. een SQL-afkappingsfout.
                // \r\n (textarea-submit) → \n: één vorm in de databank, QuestPDF/weergave rekenen op \n.
                var desc = r.Description.Replace("\r\n", "\n").Trim();
                entity.Description = desc.Length > 1000 ? desc[..1000] : desc;
                entity.MeasurementType = r.MeasurementType;
                entity.MeasurementUnit = r.MeasurementUnit;
                entity.Number = r.Number;
                entity.Price = r.Price;
                entity.Commission = r.Commission;
                entity.VatPercentage = r.VatPercentage;
                entity.NeedsReview = r.NeedsReview;
                entity.SourceImagePath = string.IsNullOrWhiteSpace(r.SourceImagePath) ? null : r.SourceImagePath;
            }
        }

        private void SyncTerms(ChangeOrder co, List<ChangeOrderTermV2> posted)
        {
            var postedIds = posted.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();
            foreach (var stale in co.ChangeOrderPaymentTerm.Where(t => !postedIds.Contains(t.Id)).ToList())
                _db.ChangeOrderPaymentTerm.Remove(stale);

            var sortOrder = 0;
            foreach (var t in posted)
            {
                var entity = t.Id > 0 ? co.ChangeOrderPaymentTerm.FirstOrDefault(x => x.Id == t.Id) : null;
                if (entity is null)
                {
                    entity = new ChangeOrderPaymentTerm { ChangeOrderId = co.Id, CreatedAt = DateTime.Now };
                    co.ChangeOrderPaymentTerm.Add(entity);
                }
                entity.Kind = t.Kind;
                entity.Percentage = t.Percentage;
                entity.FixedAmount = t.FixedAmount;
                if (t.Kind == 3)
                {
                    // Saldo = altijd "bij de laatste schijf", waar die ook ligt (geen vaste StageId —
                    // dat zou uit sync raken als de groep later een schijf bijkrijgt). Stap 5
                    // (facturatie-integratie) lost dit op als "TriggerType=2 zonder StageId" = de
                    // structureel laatste schijf van de betalingsgroep.
                    entity.TriggerType = 2;
                    entity.TriggerStageId = null;
                }
                else
                {
                    entity.TriggerType = t.TriggerType;
                    entity.TriggerStageId = t.TriggerType == 2 ? t.TriggerStageId : null;
                }
                entity.SortOrder = sortOrder++;
            }
        }
    }
}
