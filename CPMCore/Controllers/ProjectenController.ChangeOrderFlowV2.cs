using BOCore;
using CPMCore.Configuration;
using CPMCore.Helpers;
using CPMCore.Models.Projecten;
using CPMCore.Services;
using DALCore.Models;
using FacadeCore;
using FacadeCore.Signing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceCore.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 "Offertes &amp; wijzigingen" — de flow rond de schermen (design-handoff punt 28/29):
    /// de gedeelde statusfeiten voor lijst (20b) én scherm per stap (28a–28i), de drie routes naar het
    /// opmaakscherm (29b: omzetten vanuit een offerte = 21c, leeg beginnen, kopie) en de acties die het
    /// scherm per stap aanbiedt (herinneren, intrekken, getekende versie opladen, versie 2, termijn
    /// vrijgeven). Leest/schrijft rechtstreeks via _db, zelfde stijl als ChangeOrderDetailV2.cs.</summary>
    public partial class ProjectenController
    {
        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // 1. Gedeelde statusfeiten — één bron voor lijst en detail, zodat ze nooit uit elkaar lopen
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        private sealed class ChangeOrderFlowFacts
        {
            public bool SigningEnabled { get; set; }
            /// <summary>Het recentste ondertekendossier per ChangeOrder-id (ook gesloten dossiers).</summary>
            public Dictionary<int, CaseStatusView> LatestCase { get; } = new();
            /// <summary>Per termijn-id: is de trigger vervuld (getekend + na ondertekening/schijf
            /// bereikt/manueel vrijgegeven)?</summary>
            public Dictionary<int, bool> TermTriggered { get; } = new();
            /// <summary>Per termijn-id: de factuur waarop die termijn staat (geannuleerde niet meegeteld).</summary>
            public Dictionary<int, ChangeOrderInvoiceV2> TermInvoice { get; } = new();
            /// <summary>Per ChangeOrder-id: alle facturen (termijnfacturen + oude facturen-in-één-keer).</summary>
            public Dictionary<int, List<ChangeOrderInvoiceV2>> Invoices { get; } = new();
            /// <summary>WO's die via de oude weg (MakeInvoicesCO, ChangeOrderDetail.Invoiced) volledig
            /// gefactureerd zijn — zonder termijn-koppeling.</summary>
            public HashSet<int> LegacyInvoicedOrders { get; } = new();
        }

        /// <summary>Haalt in een vast aantal queries (niet per WO) alles op wat de status bepaalt:
        /// ondertekendossiers, bereikte schijven per eenheid, en facturen per termijn. <paramref name="orders"/>
        /// moet ChangeOrderDetail en ChangeOrderPaymentTerm al geladen hebben.</summary>
        private async Task<ChangeOrderFlowFacts> LoadChangeOrderFlowFactsAsync(int projectId, IReadOnlyCollection<ChangeOrder> orders)
        {
            var facts = new ChangeOrderFlowFacts();
            var features = HttpContext.RequestServices.GetRequiredService<IOptions<FeatureFlagsOptions>>().Value;
            facts.SigningEnabled = features.EnableSigning;
            if (orders.Count == 0) return facts;

            var ct = HttpContext.RequestAborted;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var orderIds = orders.Select(o => o.Id).ToHashSet();

            if (features.EnableSigning)
            {
                var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
                var cases = await signing.ListCasesAsync(projectId, null, 1000, ct);
                foreach (var g in cases
                    .Where(c => c.DocumentType == Services.Signing.ChangeOrderSigningSource.Key && orderIds.Contains(c.SourceEntityId))
                    .GroupBy(c => c.SourceEntityId))
                {
                    facts.LatestCase[g.Key] = g.OrderByDescending(c => c.CreatedAt).First();
                }
            }

            // ── Triggers: "bij schijf" is vervuld zodra die schijf bereikt is voor een eenheid van de klant
            //    (zelfde IsReached-regel als PaymentStagesV2: schijf-breed factureerbaar OF per eenheid
            //    aangeduid). Zonder StageId = de structureel laatste schijf van de betalingsgroep (saldo).
            var clientIds = orders.Select(o => o.ClientAccountId).Distinct().ToList();
            var units = await _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.ClientAccountId.HasValue && clientIds.Contains(u.ClientAccountId.Value))
                .Select(u => new
                {
                    u.Id,
                    ClientAccountId = u.ClientAccountId.Value,
                    GroupIds = u.UnitConstructionValue.Where(v => v.PaymentGroupId.HasValue).Select(v => v.PaymentGroupId.Value).ToList(),
                })
                .ToListAsync(ct);
            var stages = await _db.InvoicingPaymentStages.AsNoTracking()
                .Where(s => s.Group.ProjectId == projectId)
                .Select(s => new { s.Id, s.GroupId, s.Invoicable })
                .ToListAsync(ct);
            var unitIds = units.Select(u => u.Id).ToList();
            var reachedRows = await _db.UnitPaymentStageReached.AsNoTracking()
                .Where(r => unitIds.Contains(r.UnitId))
                .Select(r => new { r.UnitId, r.PaymentStageId })
                .ToListAsync(ct);
            var reached = reachedRows.Select(r => (r.UnitId, r.PaymentStageId)).ToHashSet();

            bool StageReachedFor(int clientAccountId, int? stageId)
            {
                foreach (var u in units.Where(x => x.ClientAccountId == clientAccountId))
                {
                    if (stageId.HasValue)
                    {
                        var st = stages.FirstOrDefault(s => s.Id == stageId.Value);
                        if (st is null || !u.GroupIds.Contains(st.GroupId)) continue;
                        if (st.Invoicable || reached.Contains((u.Id, st.Id))) return true;
                    }
                    else
                    {
                        foreach (var groupId in u.GroupIds)
                        {
                            var last = stages.Where(s => s.GroupId == groupId).OrderBy(s => s.Id).LastOrDefault();
                            if (last != null && (last.Invoicable || reached.Contains((u.Id, last.Id)))) return true;
                        }
                    }
                }
                return false;
            }

            foreach (var co in orders)
            {
                foreach (var t in co.ChangeOrderPaymentTerm)
                {
                    facts.TermTriggered[t.Id] = co.DateAgreement.HasValue && t.TriggerType switch
                    {
                        1 => true,
                        2 => StageReachedFor(co.ClientAccountId, t.TriggerStageId),
                        3 => t.ReleasedAt.HasValue,
                        _ => false,
                    };
                }
            }

            // ── Facturen: per termijn (LineType 'ChangeOrderTerm') en — voor oudere WO's — per regel.
            var termToOrder = orders.SelectMany(o => o.ChangeOrderPaymentTerm.Select(t => new { t.Id, OrderId = o.Id })).ToDictionary(x => x.Id, x => x.OrderId);
            var detailToOrder = orders.SelectMany(o => o.ChangeOrderDetail.Select(d => new { d.Id, OrderId = o.Id })).ToDictionary(x => x.Id, x => x.OrderId);
            var termIds = termToOrder.Keys.ToList();
            var detailIds = detailToOrder.Keys.ToList();
            const byte cancelled = (byte)InvoiceStatus.Cancelled;

            var lines = await _db.InvoicesDetails.AsNoTracking()
                .Where(d => (d.ChangeOrderPaymentTermId.HasValue && termIds.Contains(d.ChangeOrderPaymentTermId.Value))
                         || (d.ChangeOrderDetailId.HasValue && detailIds.Contains(d.ChangeOrderDetailId.Value)))
                .Where(d => d.Invoice.StatusId != cancelled)
                .Select(d => new
                {
                    d.ChangeOrderPaymentTermId,
                    d.ChangeOrderDetailId,
                    d.InvoiceId,
                    d.Invoice.PublicId,
                    d.Invoice.StatusId,
                    d.Invoice.ExpirationDate,
                    d.Price,
                    d.Text,
                })
                .ToListAsync(ct);

            var invoiceById = new Dictionary<(int OrderId, int InvoiceId), ChangeOrderInvoiceV2>();
            foreach (var line in lines)
            {
                var orderId = line.ChangeOrderPaymentTermId.HasValue
                    ? termToOrder[line.ChangeOrderPaymentTermId.Value]
                    : detailToOrder[line.ChangeOrderDetailId!.Value];

                if (!invoiceById.TryGetValue((orderId, line.InvoiceId), out var inv))
                {
                    var isPaid = line.StatusId == (byte)InvoiceStatus.Paid;
                    var isDraft = line.StatusId is null or (byte)InvoiceStatus.Draft or (byte)InvoiceStatus.Generating;
                    var isOverdue = !isPaid && !isDraft
                        && (line.StatusId == (byte)InvoiceStatus.Overdue || (line.ExpirationDate.HasValue && line.ExpirationDate.Value < today));
                    inv = new ChangeOrderInvoiceV2
                    {
                        InvoiceId = line.InvoiceId,
                        Number = string.IsNullOrWhiteSpace(line.PublicId) ? "concept" : line.PublicId,
                        Description = line.Text ?? "",
                        IsPaid = isPaid,
                        IsOverdue = isOverdue,
                        DueDate = line.ExpirationDate,
                        StatusLabel = isPaid ? "Betaald"
                            : isDraft ? "Concept"
                            : isOverdue ? (line.ExpirationDate.HasValue ? $"Vervallen {line.ExpirationDate:dd/MM}" : "Vervallen")
                            : (line.ExpirationDate.HasValue ? $"Open · vervalt {line.ExpirationDate:dd/MM}" : "Open"),
                        StatusTone = isPaid ? "is-solid" : isDraft ? "is-neutral" : isOverdue ? "is-blocked" : "is-positive",
                    };
                    invoiceById[(orderId, line.InvoiceId)] = inv;
                    if (!facts.Invoices.TryGetValue(orderId, out var list)) facts.Invoices[orderId] = list = new List<ChangeOrderInvoiceV2>();
                    list.Add(inv);
                }
                inv.Amount += line.Price ?? 0m;
                if (line.ChangeOrderPaymentTermId.HasValue) facts.TermInvoice[line.ChangeOrderPaymentTermId.Value] = inv;
            }

            foreach (var co in orders)
            {
                if (co.ChangeOrderDetail.Count > 0 && co.ChangeOrderDetail.All(d => d.Invoiced == true))
                    facts.LegacyInvoicedOrders.Add(co.Id);
            }

            return facts;
        }

        private static ChangeOrderStatus ComputeFlowStatus(ChangeOrder co, ChangeOrderFlowFacts facts, DateOnly today)
        {
            facts.LatestCase.TryGetValue(co.Id, out var signingCase);
            var terms = co.ChangeOrderPaymentTerm;
            var allTermsInvoiced = facts.LegacyInvoicedOrders.Contains(co.Id)
                || (terms.Count > 0 && terms.All(t => facts.TermInvoice.ContainsKey(t.Id)));
            // Zonder facturatieplan (oudere WO): factureerbaar zodra getekend, mits wij ze factureren.
            var hasInvoicable = !allTermsInvoiced && co.DateAgreement.HasValue && (terms.Count == 0
                ? co.Invoiceable
                : terms.Any(t => !facts.TermInvoice.ContainsKey(t.Id) && facts.TermTriggered.GetValueOrDefault(t.Id)));
            facts.Invoices.TryGetValue(co.Id, out var invoices);
            var allPaid = allTermsInvoiced && invoices is { Count: > 0 } && invoices.All(i => i.IsPaid);

            return ChangeOrderStatusHelper.Compute(new ChangeOrderStatusInput(
                IsQuote: co.IsQuote,
                ExpirationDate: co.ExpirationDate,
                DateSendToClient: co.DateSendToClient,
                DateAgreement: co.DateAgreement,
                SigningCaseStatus: signingCase?.Status,
                HasInvoicableTerm: hasInvoicable,
                AllTermsInvoiced: allTermsInvoiced,
                AllInvoicesPaid: allPaid), today);
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // 2. Klant · eenheid · btw — voor de keuzelijsten van 21c/kopie en het opmaakscherm
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>Elk klantenaccount van het project met zijn eenheid(en) en de btw uit de betalingsgroep
        /// van die eenheid (btw klant is nooit vrij te kiezen — wijzigen gebeurt in Betalingsschijven).</summary>
        private async Task<List<ConvertClientOptionV2>> LoadClientOptionsAsync(int projectId)
        {
            var ct = HttpContext.RequestAborted;
            var accounts = _clientService.GetClientAccountsByProjectIdForSelect(projectId) is { Success: true } resp
                ? resp.Values.OrderBy(c => c.Display).ToList() : new List<IdNameBO>();
            var units = await _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.ClientAccountId.HasValue)
                .Select(u => new
                {
                    u.Name,
                    ClientAccountId = u.ClientAccountId.Value,
                    GroupId = u.UnitConstructionValue.Where(v => v.PaymentGroupId.HasValue).Select(v => v.PaymentGroupId).FirstOrDefault(),
                })
                .ToListAsync(ct);
            var groups = await _db.InvoicingPaymentGroup.AsNoTracking()
                .Where(g => g.ProjectId == projectId)
                .Select(g => new { g.Id, g.Name, g.VatPercentage })
                .ToListAsync(ct);
            var accountIds = accounts.Select(a => a.ID).ToList();
            var coOwners = await _db.ClientContacts.AsNoTracking()
                .Where(c => c.IsCoOwner && accountIds.Contains(c.ClientAccountId))
                .Select(c => new { c.ClientAccountId, c.Name, c.Forename })
                .ToListAsync(ct);

            var options = new List<ConvertClientOptionV2>();
            foreach (var a in accounts)
            {
                var own = units.Where(u => u.ClientAccountId == a.ID).OrderBy(u => u.Name).ToList();
                var group = own.Where(u => u.GroupId.HasValue).Select(u => groups.FirstOrDefault(g => g.Id == u.GroupId)).FirstOrDefault(g => g != null);
                var vat = group?.VatPercentage ?? 21m;
                var owners = coOwners.Where(c => c.ClientAccountId == a.ID)
                    .Select(c => string.Join(" ", new[] { c.Name, c.Forename }.Where(s => !string.IsNullOrWhiteSpace(s))))
                    .Where(s => s.Length > 0).ToList();
                options.Add(new ConvertClientOptionV2
                {
                    Id = a.ID,
                    Display = a.Display,
                    UnitName = own.Count > 0 ? string.Join(", ", own.Select(u => u.Name)) : null,
                    VatPercentage = vat,
                    VatLabel = group != null ? $"{Pct(vat)} % — {group.Name}" : "21 % — geen betalingsgroep gekoppeld",
                    OwnersHint = owners.Count > 0 ? "mede-eigenaars: " + string.Join(" · ", owners) : null,
                });
            }
            return options;
        }

        private async Task<(decimal Percentage, string Label, string UnitName)> ResolveVatForClientAsync(int projectId, int clientAccountId)
        {
            var unit = await _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.ClientAccountId == clientAccountId)
                .Include(u => u.Type)
                .Include(u => u.UnitConstructionValue)
                .OrderBy(u => u.Id)
                .ToListAsync(HttpContext.RequestAborted);
            if (unit.Count == 0) return (21m, "onbekend — koppel een eenheid aan een betalingsgroep", "");

            var first = unit[0];
            var unitName = first.Type != null ? $"{first.Type.Name} {first.Name}".Trim() : first.Name;
            var groupId = unit.SelectMany(u => u.UnitConstructionValue).Select(v => v.PaymentGroupId).FirstOrDefault(g => g.HasValue);
            if (groupId is int id)
            {
                var group = await _db.InvoicingPaymentGroup.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, HttpContext.RequestAborted);
                if (group != null)
                {
                    var pct = group.VatPercentage ?? 21m;
                    return (pct, $"{Pct(pct)} % — {group.Name}", unitName);
                }
            }
            return (21m, "onbekend — koppel een eenheid aan een betalingsgroep", unitName);
        }

        /// <summary>Voorstel voor de commissie op een nieuwe WO: wat laatst gebruikt werd in dit project
        /// (er bestaat geen projectinstelling voor) — 0 als er nog geen WO met commissie is.</summary>
        private async Task<decimal> DefaultCommissionAsync(int projectId)
        {
            return await _db.ChangeOrderDetail.AsNoTracking()
                .Where(d => d.ChangeOrder.ContractActivity.Contract.ProjectId == projectId && !d.ChangeOrder.IsQuote && d.Commission > 0)
                .OrderByDescending(d => d.Id)
                .Select(d => d.Commission)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // 3. 21c — Omzetten naar wijzigingsopdracht
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>Inhoud van de 21c-modal. changeOrderId = 0 kan (20c op een nog niet opgeslagen
        /// offerte): de pagina vult kostprijs/regelaantal dan zelf in vanuit haar formulier.</summary>
        [HttpGet]
        public async Task<IActionResult> ConvertQuoteModalV2(int projectId, int changeOrderId = 0)
        {
            var features = HttpContext.RequestServices.GetRequiredService<IOptions<FeatureFlagsOptions>>().Value;
            var vm = new ConvertQuoteModalV2Vm
            {
                ProjectId = projectId,
                ChangeOrderId = changeOrderId,
                Clients = await LoadClientOptionsAsync(projectId),
                DefaultCommission = await DefaultCommissionAsync(projectId),
                SigningEnabled = features.EnableSigning,
                NextNumberLabel = changeOrderId > 0 ? $"WO-{changeOrderId:000}" : "WO — nummer volgt bij aanmaken",
            };

            if (changeOrderId > 0)
            {
                var co = await _db.ChangeOrder.AsNoTracking()
                    .Include(c => c.ChangeOrderDetail)
                    .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Company)
                    .FirstOrDefaultAsync(c => c.Id == changeOrderId);
                if (co is null) return NotFound();

                vm.ClientAccountId = co.ClientAccountId;
                vm.CostTotal = co.ChangeOrderDetail.Sum(d => d.Number * d.Price);
                vm.RowCount = co.ChangeOrderDetail.Count;
                // De voorlopige naam uit 20c ("Offerte 2025-118") is geen omschrijving voor de klant.
                vm.Description = co.Description != null && co.Description.StartsWith("Offerte") ? "" : co.Description ?? "";
                var supplier = co.ContractActivity?.Contract?.Company?.BedrijfsNaam;
                vm.Subtitle = string.Join(" · ", new[]
                {
                    $"OF-{co.Id:000}",
                    supplier,
                    $"{vm.RowCount} {(vm.RowCount == 1 ? "regel" : "regels")}",
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            return PartialView("Modals/_ModalConvertQuoteV2", vm);
        }

        /// <summary>21c bevestigen op een al opgeslagen offerte (vanuit de lijst). Vanuit 20c loopt
        /// dezelfde omzetting via ChangeOrderDetailV2Save (opslaan + omzetten in één POST).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderConvertV2(ChangeOrderConvertV2Model model)
        {
            var co = await _db.ChangeOrder
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ChangeOrderPaymentTerm)
                .FirstOrDefaultAsync(c => c.Id == model.ChangeOrderId);
            if (co is null) return NotFound();

            if (!co.IsQuote)
            {
                AddMessage("info", "Deze offerte is al omgezet.", "Al omgezet");
                return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = co.Id });
            }
            if (model.ConvertClientAccountId <= 0 && co.ClientAccountId <= 0)
            {
                AddMessage("error", "Kies een klantenaccount · eenheid.", "Kon niet omzetten");
                return RedirectToAction(nameof(ChangeOrdersV2), new { projectid = model.ProjectId });
            }

            await ApplyConversionAsync(co, model.ProjectId, model.ConvertClientAccountId, model.ConvertCommission,
                model.ConvertPlan, model.ConvertDescription, model.ConvertConditions);
            await _db.SaveChangesAsync();

            return await AfterSaveRedirectAsync(co, model.ProjectId, model.AfterSave, "Omgezet naar wijzigingsopdracht.", "Omgezet");
        }

        /// <summary>De eigenlijke omzetting (in-place, geen kopieerstap — offerte en WO zijn dezelfde rij):
        /// klant, commissie op elke regel, btw uit de betalingsgroep, omschrijving, voorwaarden en het
        /// facturatieplan volgens de gekozen snelkeuze. Minwerk (negatieve kostprijs) wordt aan kostprijs
        /// verrekend: commissie vast op 0 %.</summary>
        private async Task ApplyConversionAsync(ChangeOrder co, int projectId, int clientAccountId, decimal commission,
            string plan, string description, string conditions)
        {
            if (clientAccountId > 0) co.ClientAccountId = clientAccountId;
            co.IsQuote = false;
            co.QuoteConvertedAt = DateTime.Now;
            co.Invoiceable = true; // standaard factureren wij; de schakelaar op het opmaakscherm zet dit uit

            var details = co.ChangeOrderDetail.Where(d => _db.Entry(d).State != EntityState.Deleted).ToList();
            var cost = details.Sum(d => d.Number * d.Price);
            var appliedCommission = cost < 0 ? 0m : Math.Clamp(commission, 0m, 999m);
            var vat = (await ResolveVatForClientAsync(projectId, co.ClientAccountId)).Percentage;
            foreach (var d in details)
            {
                d.Commission = appliedCommission;
                d.VatPercentage = vat;
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                var desc = description.Trim();
                co.Description = desc.Length > 250 ? desc[..250] : desc;
            }
            if (!string.IsNullOrWhiteSpace(conditions))
            {
                var cond = conditions.Trim();
                co.ChangeOrderConditions = cond.Length > 1000 ? cond[..1000] : cond;
            }

            foreach (var stale in co.ChangeOrderPaymentTerm.ToList())
                _db.ChangeOrderPaymentTerm.Remove(stale);
            void AddTerm(byte kind, decimal pct, byte trigger, int order) => co.ChangeOrderPaymentTerm.Add(new ChangeOrderPaymentTerm
            {
                Kind = kind,
                Percentage = pct,
                TriggerType = trigger,
                SortOrder = order,
                CreatedAt = DateTime.Now,
            });
            if (plan == "voorschot-saldo")
            {
                AddTerm(1, 30m, 1, 0);
                AddTerm(3, 70m, 2, 1);
            }
            else
            {
                // "laatste-schijf" én "eigen": start met alles op het saldo; bij "eigen" verdeelt de
                // gebruiker het verder op het opmaakscherm (28a).
                AddTerm(3, 100m, 2, 0);
            }
        }

        /// <summary>Waar de gebruiker na opslaan/omzetten terechtkomt: het scherm zelf ("open"), of het
        /// scherm met de 21d-verzendmodal meteen open ("send") — de modal werkt altijd op de zopas
        /// bewaarde toestand, dus het document dat verzonden wordt is wat op het scherm staat.</summary>
        private Task<IActionResult> AfterSaveRedirectAsync(ChangeOrder co, int projectId, string afterSave, string message, string title)
        {
            if (afterSave != "send")
            {
                AddMessage("success", message, title);
                return Task.FromResult<IActionResult>(RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = co.Id }));
            }

            var problem = SendProblem(co);
            if (problem != null)
            {
                AddMessage("warning", problem + " De wijzigingsopdracht is wel opgeslagen.", "Nog niet verzonden");
                return Task.FromResult<IActionResult>(RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = co.Id }));
            }
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = co.Id, send = true }));
        }

        /// <summary>Wat verzenden nog in de weg staat, of null als alles in orde is.</summary>
        private string SendProblem(ChangeOrder co)
        {
            var details = co.ChangeOrderDetail.Where(d => _db.Entry(d).State != EntityState.Deleted).ToList();
            var terms = co.ChangeOrderPaymentTerm.Where(t => _db.Entry(t).State != EntityState.Deleted).ToList();
            if (details.Count == 0) return "Voeg minstens één regel toe.";
            if (details.Any(d => d.NeedsReview)) return "Minstens één regel staat nog op \"controleer\".";
            if (terms.Count > 0 && Math.Abs(terms.Sum(t => t.Percentage ?? 0m) - 100m) > 0.01m) return "Het facturatieplan moet samen 100 % zijn.";
            if (string.IsNullOrWhiteSpace(co.Description)) return "Vul een omschrijving voor de klant in.";
            return null;
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // 3b. 21d — Verzenden ter ondertekening
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>Inhoud van de 21d-modal. Ontvangers, ondertekenregel en vervaldatum komen van de
        /// ondertekenmodule zelf (ISigningDocumentSource.BuildAsync + SigningPolicy) — precies het voorstel
        /// dat SigningAdmin/Start toont, zodat beide ingangen hetzelfde dossier opleveren.</summary>
        [HttpGet]
        public async Task<IActionResult> SendChangeOrderModalV2(int projectId, int changeOrderId)
        {
            var ct = HttpContext.RequestAborted;
            var co = await _db.ChangeOrder.AsNoTracking()
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ChangeOrderPaymentTerm)
                .Include(c => c.ClientAccount)
                .FirstOrDefaultAsync(c => c.Id == changeOrderId, ct);
            if (co is null || co.IsQuote) return NotFound();

            var be = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");
            var incl = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m) * (1 + (d.VatPercentage ?? 0m) / 100m));
            var vm = new SendChangeOrderModalV2Vm
            {
                ProjectId = projectId,
                ChangeOrderId = co.Id,
                Number = $"WO-{co.Id:000}",
                Subtitle = string.Join(" · ", new[]
                {
                    co.Description,
                    "€ " + incl.ToString("N2", be) + " incl. btw",
                    co.ClientAccount != null ? "klantenaccount " + Services.Signing.ChangeOrderPdfBuilder.DisplayName(co.ClientAccount) : null,
                }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Problem = SendProblem(co),
                PdfUrl = Url.Action("ChangeOrderPDF", "Projecten", new { changeorderid = co.Id }),
                ExpiresOn = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            };

            var features = HttpContext.RequestServices.GetRequiredService<IOptions<FeatureFlagsOptions>>().Value;
            if (!features.EnableSigning) vm.CannotSignReason = "Elektronisch ondertekenen staat uit.";
            else if (!CanManageSigning()) vm.CannotSignReason = "Je hebt geen recht om een ondertekening te starten.";
            else
            {
                try
                {
                    var registry = HttpContext.RequestServices.GetRequiredService<ServiceCore.Signing.SigningRegistry>();
                    var source = registry.Source(Services.Signing.ChangeOrderSigningSource.Key);
                    var package = await source.BuildAsync(co.Id, User.GetCpmUserId() ?? 0, ct);
                    var policy = await _db.SigningPolicy.AsNoTracking().FirstOrDefaultAsync(p => p.DocumentType == source.DocumentType && p.IsActive, ct);

                    vm.CanSign = true;
                    vm.Parties = package.SuggestedParties.OrderBy(p => p.SortOrder).Select(p => new Models.Signing.SigningStartPartyVm
                    {
                        Include = !string.IsNullOrWhiteSpace(p.Email),
                        PartyType = p.PartyType,
                        SourceRefId = p.SourceRefId,
                        DisplayName = p.DisplayName,
                        Email = p.Email,
                        PhoneMasked = p.PhoneMasked,
                        Capacity = p.Capacity,
                    }).ToList();
                    vm.SigningRule = package.SuggestedRule ?? policy?.SigningRule ?? (int)SigningRule.All;
                    vm.ExpiresOn = DateOnly.FromDateTime(DateTime.Today.AddDays(policy?.LinkValidityDays ?? 30));
                    vm.ReminderAfterDays = policy?.ReminderAfterDays;
                    vm.PreviewUrl = Url.Action("Preview", "SigningAdmin", new { documentType = source.DocumentType, sourceId = co.Id });
                    vm.TestRecipient = HttpContext.RequestServices.GetRequiredService<IOptions<ServiceCore.Signing.SigningOptions>>().Value.TestRecipientOverride;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "21d: ondertekenpakket van wijzigingsopdracht {ChangeOrderId} kon niet opgebouwd worden.", co.Id);
                    vm.CannotSignReason = "Het document kon niet opgebouwd worden voor ondertekening.";
                }
            }

            return PartialView("Modals/_ModalSendChangeOrderV2", vm);
        }

        /// <summary>21d bevestigen. "sign": maakt het ondertekendossier en biedt het aan (zelfde twee stappen
        /// als SigningAdmin/Start) — de gebruiker blijft op de wijzigingsopdracht. "pdf": enkel de datum
        /// "verzonden"; het document gaat buiten het systeem naar de klant en het akkoord registreer je
        /// later op het scherm ("Akkoord ontvangen").</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderSendV2(ChangeOrderSendV2Model model)
        {
            var ct = HttpContext.RequestAborted;
            var co = await _db.ChangeOrder
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ChangeOrderPaymentTerm)
                .FirstOrDefaultAsync(c => c.Id == model.ChangeOrderId, ct);
            if (co is null || co.IsQuote) return NotFound();

            string error = null;
            if (co.DateAgreement.HasValue) error = "Deze wijzigingsopdracht is al ondertekend.";
            else if (await ActiveSigningCaseAsync(co.Id) is not null) error = "Er loopt al een ondertekening voor deze wijzigingsopdracht.";
            else error = SendProblem(co);
            if (error != null)
            {
                AddMessage("error", error, "Niet verzonden");
                return BackToChangeOrder(model.ProjectId, co.Id);
            }

            if (model.Channel == "pdf")
            {
                co.DateSendToClient = DateOnly.FromDateTime(DateTime.Today);
                await _db.SaveChangesAsync(ct);
                AddMessage("success", "Gemarkeerd als verzonden. Bezorg de PDF zelf aan de klant en registreer het akkoord zodra je de getekende versie terug hebt.", "Verzonden");
                return BackToChangeOrder(model.ProjectId, co.Id);
            }

            if (!CanManageSigning()) return Forbid();

            var parties = (model.Parties ?? new())
                .Where(p => p.Include)
                .Select((p, i) => new SigningPartyInput(p.PartyType, p.SourceRefId, (p.DisplayName ?? "").Trim(), p.Email?.Trim(), p.PhoneMasked, p.Capacity?.Trim(), i))
                .ToList();
            DateTime? expiresAt = model.ExpiresOn.HasValue
                ? model.ExpiresOn.Value.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Local).ToUniversalTime()
                : null;
            if (parties.Count == 0) error = "Kies minstens één ontvanger.";
            else if (parties.Any(p => string.IsNullOrWhiteSpace(p.Email))) error = "Elke gekozen ontvanger heeft een e-mailadres nodig.";
            else if (expiresAt is not null && expiresAt <= DateTime.UtcNow) error = "De vervaldatum moet in de toekomst liggen.";
            if (error != null)
            {
                AddMessage("error", error, "Niet verzonden");
                return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = co.Id, send = true });
            }

            var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
            var created = await signing.CreateCaseAsync(
                new CreateSigningCaseRequest(Services.Signing.ChangeOrderSigningSource.Key, co.Id, parties, model.SigningRule, expiresAt, User.GetCpmUserId() ?? 0, User.GetCpmDisplayName(), model.InvitationMessage),
                SigningCtx(), ct);
            if (!created.Success)
            {
                AddMessage("error", created.Error ?? "Het ondertekendossier kon niet aangemaakt worden.", "Niet verzonden");
                return BackToChangeOrder(model.ProjectId, co.Id);
            }
            var opened = await signing.OpenCaseAsync(created.CaseId!.Value, SigningCtx(), ct);
            if (!opened.Success)
            {
                // Het dossier bestaat (nog niet aangeboden): toon het dossier met de fout, daar kan de
                // gebruiker annuleren of opnieuw aanbieden i.p.v. een tweede dossier te maken.
                AddMessage("error", opened.Error ?? "Het dossier kon niet aangeboden worden.", "Niet aangeboden");
                return RedirectToAction("Dossier", "SigningAdmin", new { id = created.CaseId });
            }

            AddMessage("success", "De uitnodigingen om te tekenen zijn verstuurd naar " + string.Join(" en ", parties.Select(p => p.DisplayName)) + ".", "Verzonden ter ondertekening");
            return BackToChangeOrder(model.ProjectId, co.Id);
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // 4. Kopie (29b) en versie 2 (28c/28h)
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CopyChangeOrderModalV2(int projectId, int changeOrderId = 0)
        {
            var orders = await _db.ChangeOrder.AsNoTracking()
                .Where(c => c.ContractActivity.Contract.ProjectId == projectId && !c.IsQuote)
                .OrderByDescending(c => c.Id)
                .Select(c => new { c.Id, c.Description, Client = c.ClientAccount.Name })
                .ToListAsync();

            var vm = new CopyChangeOrderModalV2Vm
            {
                ProjectId = projectId,
                ChangeOrderId = changeOrderId,
                Orders = orders.Select(o => new IdNameBO { ID = o.Id, Display = $"WO-{o.Id:000} · {o.Description} · {o.Client}" }).ToList(),
                Clients = await LoadClientOptionsAsync(projectId),
            };
            return PartialView("Modals/_ModalCopyChangeOrderV2", vm);
        }

        /// <summary>Kopie voor een andere eenheid (asVersion=false: "Regels en plan worden overgenomen,
        /// handtekeningen niet") of een nieuwe versie van dezelfde WO (asVersion=true: "Versie 2 neemt alles
        /// over als nieuw concept" — een nog lopende ondertekening van de bron wordt dan ingetrokken).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderCopyV2(int projectId, int changeOrderId, int clientAccountId = 0, bool asVersion = false)
        {
            var src = await _db.ChangeOrder.AsNoTracking()
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ChangeOrderPaymentTerm)
                .FirstOrDefaultAsync(c => c.Id == changeOrderId);
            if (src is null || src.IsQuote)
            {
                AddMessage("error", "Kies een wijzigingsopdracht om te kopiëren.", "Kon niet kopiëren");
                return RedirectToAction(nameof(ChangeOrdersV2), new { projectid = projectId });
            }

            var targetClientId = asVersion ? src.ClientAccountId : clientAccountId;
            if (targetClientId <= 0)
            {
                AddMessage("error", "Kies de eenheid waarvoor de kopie dient.", "Kon niet kopiëren");
                return RedirectToAction(nameof(ChangeOrdersV2), new { projectid = projectId });
            }

            if (asVersion)
            {
                if (src.DateAgreement.HasValue)
                {
                    AddMessage("error", "Deze wijzigingsopdracht is al ondertekend — een wijziging wordt nu een nieuwe wijzigingsopdracht.", "Geen versie 2");
                    return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = src.Id });
                }
                var active = await ActiveSigningCaseAsync(src.Id);
                if (active is not null)
                {
                    var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
                    var cancel = await signing.CancelCaseAsync(active.CaseId, "Vervangen door een nieuwe versie", SigningCtx(), HttpContext.RequestAborted);
                    if (!cancel.Success)
                    {
                        AddMessage("error", cancel.Error ?? "De lopende ondertekening kon niet ingetrokken worden.", "Geen versie 2");
                        return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = src.Id });
                    }
                }
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var sameClient = targetClientId == src.ClientAccountId;
            var vat = sameClient ? (decimal?)null : (await ResolveVatForClientAsync(projectId, targetClientId)).Percentage;
            // Een schijf-trigger hoort bij de betalingsgroep van de bron-eenheid; voor een andere eenheid
            // valt hij terug op "na ondertekening" (aan te passen in het facturatieplan van de kopie).
            var copy = new ChangeOrder
            {
                ClientAccountId = targetClientId,
                Description = src.Description,
                Date = today,
                ExpirationDate = today.AddDays(30),
                Comment = src.Comment,
                Invoiceable = src.Invoiceable,
                ContractActivityId = src.ContractActivityId,
                ChangeOrderConditions = src.ChangeOrderConditions,
                IsQuote = false,
                QuoteSupplierReference = src.QuoteSupplierReference,
                QuoteVatPercentage = src.QuoteVatPercentage,
                SourceChangeOrderId = src.Id,
                SourceKind = asVersion ? (byte)2 : (byte)1,
            };
            var copyOrder = 0;
            foreach (var d in src.ChangeOrderDetail.OrderBy(d => d.SortOrder ?? int.MaxValue).ThenBy(d => d.Id))
            {
                copy.ChangeOrderDetail.Add(new ChangeOrderDetail
                {
                    SortOrder = copyOrder++,
                    Description = d.Description,
                    MeasurementType = d.MeasurementType,
                    MeasurementUnit = d.MeasurementUnit,
                    Number = d.Number,
                    Price = d.Price,
                    Commission = d.Commission,
                    VatPercentage = vat ?? d.VatPercentage,
                    SourceImagePath = d.SourceImagePath,
                });
            }
            foreach (var t in src.ChangeOrderPaymentTerm.OrderBy(t => t.SortOrder))
            {
                var keepStage = sameClient || t.Kind == 3;
                copy.ChangeOrderPaymentTerm.Add(new ChangeOrderPaymentTerm
                {
                    Kind = t.Kind,
                    Percentage = t.Percentage,
                    FixedAmount = t.FixedAmount,
                    TriggerType = keepStage || t.TriggerType != 2 ? t.TriggerType : (byte)1,
                    TriggerStageId = keepStage ? t.TriggerStageId : null,
                    SortOrder = t.SortOrder,
                    CreatedAt = DateTime.Now,
                });
            }
            _db.ChangeOrder.Add(copy);
            await _db.SaveChangesAsync();

            AddMessage("success",
                asVersion ? $"Nieuwe versie aangemaakt als WO-{copy.Id:000}. Pas aan en verzend opnieuw." : $"Kopie aangemaakt als WO-{copy.Id:000}. Regels en facturatieplan zijn overgenomen, handtekeningen niet.",
                asVersion ? "Nieuwe versie" : "Kopie gemaakt");
            return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = copy.Id });
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // 5. Acties op het scherm per stap (28c–28i) — de gebruiker blijft op de wijzigingsopdracht
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        private SigningRequestContext SigningCtx() => new(
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            null,
            User.GetCpmUserId(),
            User.GetCpmDisplayName());

        private bool CanManageSigning()
        {
            var features = HttpContext.RequestServices.GetRequiredService<IOptions<FeatureFlagsOptions>>().Value;
            var ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            return features.EnableSigning && ps.HasWrite(PermissionCodes.Signing);
        }

        private IActionResult BackToChangeOrder(int projectId, int changeOrderId)
            => RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = projectId, coid = changeOrderId });

        /// <summary>28c/28d "Herinnering sturen" — naar één ondertekenaar (partyId) of naar iedereen die nog
        /// niet tekende.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2Remind(int projectId, int changeOrderId, int partyId = 0)
        {
            if (!CanManageSigning()) return Forbid();
            var active = await ActiveSigningCaseAsync(changeOrderId);
            if (active is null)
            {
                AddMessage("error", "Er loopt geen ondertekening voor deze wijzigingsopdracht.", "Geen herinnering");
                return BackToChangeOrder(projectId, changeOrderId);
            }

            var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
            var open = active.Parties
                .Where(p => p.Status is (int)SigningPartyStatus.Invited or (int)SigningPartyStatus.Opened or (int)SigningPartyStatus.Verified)
                .Where(p => partyId <= 0 || p.PartyId == partyId)
                .ToList();
            var sent = new List<string>();
            string error = null;
            foreach (var p in open)
            {
                var result = await signing.SendReminderAsync(p.PartyId, SigningCtx(), HttpContext.RequestAborted);
                if (result.Success) sent.Add(p.DisplayName);
                else error ??= result.Error;
            }

            if (sent.Count > 0) AddMessage("success", "Herinnering verzonden naar " + string.Join(" en ", sent) + ".", "Herinnering");
            if (sent.Count == 0 || error != null) AddMessage("error", error ?? "Er is niemand meer die nog moet tekenen.", "Geen herinnering");
            return BackToChangeOrder(projectId, changeOrderId);
        }

        /// <summary>28c/28d "Intrekken" — annuleert het lopende ondertekendossier; de wijzigingsopdracht
        /// wordt daarna weer bewerkbaar (status afgeleid: geen actief dossier meer).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2Withdraw(int projectId, int changeOrderId)
        {
            if (!CanManageSigning()) return Forbid();
            var active = await ActiveSigningCaseAsync(changeOrderId);
            if (active is null)
            {
                AddMessage("info", "Er loopt geen ondertekening meer voor deze wijzigingsopdracht.", "Niets in te trekken");
                return BackToChangeOrder(projectId, changeOrderId);
            }

            var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
            var result = await signing.CancelCaseAsync(active.CaseId, "Ingetrokken vanuit de wijzigingsopdracht", SigningCtx(), HttpContext.RequestAborted);
            if (result.Success) AddMessage("success", "De ondertekening is ingetrokken. Pas aan en verzend opnieuw.", "Ingetrokken");
            else AddMessage("error", result.Error ?? "Intrekken is mislukt.", "Niet ingetrokken");
            return BackToChangeOrder(projectId, changeOrderId);
        }

        /// <summary>28c "Getekende versie opladen" — een op papier getekend exemplaar sluit het dossier af.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2UploadSigned(int projectId, int changeOrderId, IFormFile file)
        {
            if (!CanManageSigning()) return Forbid();
            var active = await ActiveSigningCaseAsync(changeOrderId);
            if (active is null || file is null || file.Length == 0)
            {
                AddMessage("error", active is null ? "Er loopt geen ondertekening voor deze wijzigingsopdracht." : "Kies een PDF-bestand.", "Niet opgeladen");
                return BackToChangeOrder(projectId, changeOrderId);
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
            var result = await signing.UploadSignedDocumentAsync(active.CaseId, ms.ToArray(), file.FileName, SigningCtx(), HttpContext.RequestAborted);
            if (result.Success) AddMessage("success", "De getekende versie is opgeladen; de wijzigingsopdracht is ondertekend.", "Ondertekend");
            else AddMessage("error", result.Error ?? "Opladen is mislukt.", "Niet opgeladen");
            return BackToChangeOrder(projectId, changeOrderId);
        }

        /// <summary>Enkel wanneer elektronisch ondertekenen uitstaat (of er nooit een dossier liep): de
        /// stappen Verzonden/Ondertekend met de hand zetten, zoals het oude scherm deed met datumvelden.
        /// step = "sent" | "signed" | "concept".</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2SetStep(int projectId, int changeOrderId, string step)
        {
            var co = await _db.ChangeOrder.FirstOrDefaultAsync(c => c.Id == changeOrderId);
            if (co is null) return NotFound();
            if (await ActiveSigningCaseAsync(changeOrderId) is not null)
            {
                AddMessage("error", SigningLockedMessage, "Niet gewijzigd");
                return BackToChangeOrder(projectId, changeOrderId);
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            switch (step)
            {
                case "sent":
                    co.DateSendToClient ??= today;
                    AddMessage("success", "Gemarkeerd als verzonden.", "Verzonden");
                    break;
                case "signed":
                    co.DateSendToClient ??= today;
                    co.DateAgreement ??= today;
                    AddMessage("success", "Akkoord van de klant geregistreerd.", "Ondertekend");
                    break;
                case "concept" when !co.DateAgreement.HasValue:
                    co.DateSendToClient = null;
                    AddMessage("success", "Terug naar concept — regels en facturatieplan zijn weer aanpasbaar.", "Concept");
                    break;
            }
            await _db.SaveChangesAsync();
            return BackToChangeOrder(projectId, changeOrderId);
        }

        /// <summary>Facturatieplan, trigger "Manueel vrijgeven": maakt de termijn factureerbaar.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2ReleaseTerm(int projectId, int changeOrderId, int termId)
        {
            var term = await _db.ChangeOrderPaymentTerm.Include(t => t.ChangeOrder)
                .FirstOrDefaultAsync(t => t.Id == termId && t.ChangeOrderId == changeOrderId);
            if (term is null) return NotFound();

            if (!term.ChangeOrder.DateAgreement.HasValue)
                AddMessage("error", "Een termijn vrijgeven kan pas na ondertekening.", "Niet vrijgegeven");
            else if (term.TriggerType != 3)
                AddMessage("info", "Deze termijn volgt zijn eigen moment en hoeft niet vrijgegeven te worden.", "Niet nodig");
            else
            {
                term.ReleasedAt ??= DateTime.Now;
                await _db.SaveChangesAsync();
                AddMessage("success", "Termijn vrijgegeven — hij staat nu klaar in Facturatie.", "Vrijgegeven");
            }
            return BackToChangeOrder(projectId, changeOrderId);
        }
    }
}
