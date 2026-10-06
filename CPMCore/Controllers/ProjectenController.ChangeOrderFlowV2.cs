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
    /// de gedeelde statusfeiten voor lijst (20b) én scherm per stap (28a–28i), de routes naar het
    /// opmaakscherm (29b: offerte aan de klant → 21c omzetten naar een nieuwe WO, rechtstreeks een WO,
    /// kopie), het mailen van een offerte en de acties die het
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
                .Select(g => new { g.Id, g.Name, g.VatPercentage, g.VatTypeId })
                .ToListAsync(ct);
            var groupTypeIds = groups.Where(g => g.VatTypeId.HasValue).Select(g => g.VatTypeId.Value).Distinct().ToList();
            var groupTypes = await _db.Vattype.AsNoTracking().Where(v => groupTypeIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Code, v.BasePercentage }).ToListAsync(ct);
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
                var groupType = group?.VatTypeId is int gt ? groupTypes.FirstOrDefault(t => t.Id == gt) : null;
                var vat = groupType?.BasePercentage ?? group?.VatPercentage ?? 21m;
                var owners = coOwners.Where(c => c.ClientAccountId == a.ID)
                    .Select(c => string.Join(" ", new[] { c.Name, c.Forename }.Where(s => !string.IsNullOrWhiteSpace(s))))
                    .Where(s => s.Length > 0).ToList();
                options.Add(new ConvertClientOptionV2
                {
                    Id = a.ID,
                    Display = a.Display,
                    UnitName = own.Count > 0 ? string.Join(", ", own.Select(u => u.Name)) : null,
                    VatPercentage = vat,
                    VatTypeId = groupType?.Id,
                    VatLabel = group != null ? $"{(groupType != null ? groupType.Code + " · " : "")}{Pct(vat)} % — {group.Name}" : "21 % — geen betalingsgroep gekoppeld",
                    OwnersHint = owners.Count > 0 ? "mede-eigenaars: " + string.Join(" · ", owners) : null,
                });
            }
            return options;
        }

        private async Task<(decimal Percentage, string Label, string UnitName, int? VatTypeId)> ResolveVatForClientAsync(int projectId, int clientAccountId)
        {
            var unit = await _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.ClientAccountId == clientAccountId)
                .Include(u => u.Type)
                .Include(u => u.UnitConstructionValue)
                .OrderBy(u => u.Id)
                .ToListAsync(HttpContext.RequestAborted);
            if (unit.Count == 0) return (21m, "onbekend — koppel een eenheid aan een betalingsgroep", "", null);

            var first = unit[0];
            var unitName = first.Type != null ? $"{first.Type.Name} {first.Name}".Trim() : first.Name;
            var groupId = unit.SelectMany(u => u.UnitConstructionValue).Select(v => v.PaymentGroupId).FirstOrDefault(g => g.HasValue);
            if (groupId is int id)
            {
                var group = await _db.InvoicingPaymentGroup.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, HttpContext.RequestAborted);
                if (group != null)
                {
                    var type = group.VatTypeId is int tid
                        ? await _db.Vattype.AsNoTracking().FirstOrDefaultAsync(v => v.Id == tid, HttpContext.RequestAborted) : null;
                    var pct = type?.BasePercentage ?? group.VatPercentage ?? 21m;
                    return (pct, $"{(type != null ? type.Code + " · " : "")}{Pct(pct)} % — {group.Name}", unitName, type?.Id);
                }
            }
            return (21m, "onbekend — koppel een eenheid aan een betalingsgroep", unitName, null);
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
        // 3. 21c — Omzetten: van offerte aan de klant naar wijzigingsopdracht
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>Inhoud van de 21c-modal. De offerte is op dit moment bewaard; klant, regels en prijzen
        /// komen eruit en liggen vast — de modal vraagt enkel wat bij de wijzigingsopdracht hoort
        /// (facturatieplan, omschrijving, voorwaarden).</summary>
        [HttpGet]
        public async Task<IActionResult> ConvertQuoteModalV2(int projectId, int changeOrderId)
        {
            var co = await _db.ChangeOrder.AsNoTracking()
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ClientAccount)
                .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Company)
                .FirstOrDefaultAsync(c => c.Id == changeOrderId);
            if (co is null || !co.IsQuote) return NotFound();

            var vat = await ResolveVatForClientAsync(projectId, co.ClientAccountId);
            var cost = co.ChangeOrderDetail.Sum(d => d.Number * d.Price);
            var excl = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m));
            var incl = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m) * (1 + (d.VatPercentage ?? vat.Percentage) / 100m));
            var rows = co.ChangeOrderDetail.Count;
            var vm = new ConvertQuoteModalV2Vm
            {
                ProjectId = projectId,
                ChangeOrderId = co.Id,
                Subtitle = string.Join(" · ", new[]
                {
                    $"{CoNo(co.Id)}",
                    co.ContractActivity?.Contract?.Company?.BedrijfsNaam,
                    $"{rows} {(rows == 1 ? "regel" : "regels")}",
                }.Where(x => !string.IsNullOrWhiteSpace(x))),
                ClientLabel = string.Join(" · ", new[] { Services.Signing.ChangeOrderPdfBuilder.DisplayName(co.ClientAccount), vat.UnitName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                VatLabel = vat.Label,
                VatPercentage = vat.Percentage,
                CostTotal = cost,
                CommissionTotal = excl - cost,
                ExclTotal = excl,
                InclTotal = incl,
                // De voorlopige naam uit 20c ("Offerte 2025-118") is geen omschrijving voor de klant.
                Description = IsIntakePlaceholder(co.Description) ? "" : co.Description ?? "",
                Conditions = DefaultChangeOrderConditions, // niet de offertevoorwaarden (zie hierboven)
                Problem = await ConvertProblemAsync(co),
            };
            return PartialView("Modals/_ModalConvertQuoteV2", vm);
        }

        /// <summary>Wat het omzetten van een offerte in de weg staat, of null.</summary>
        private async Task<string> ConvertProblemAsync(ChangeOrder quote)
        {
            if (quote.ChangeOrderDetail.Count == 0) return "De offerte heeft nog geen regels.";
            if (quote.ChangeOrderDetail.Any(d => d.NeedsReview)) return "Minstens één regel staat nog op \"controleer\".";
            var existing = await _db.ChangeOrder.AsNoTracking()
                .Where(c => c.SourceChangeOrderId == quote.Id && c.SourceKind == 3)
                .Select(c => (int?)c.Id).FirstOrDefaultAsync();
            if (existing.HasValue) return $"Deze offerte is al omgezet naar {CoNo(existing)}.";
            return null;
        }

        /// <summary>21c bevestigen: maakt uit de offerte een NIEUWE wijzigingsopdracht (de offerte blijft
        /// ongewijzigd bestaan als bron — beslissing Niels 2026-10-02). Elke regel wordt overgenomen met
        /// haar prijs, commissie en btw en verwijst naar de offerteregel (SourceDetailId): die prijzen
        /// liggen in de WO vast, het aantal en de omschrijving niet, en de regel mag daar nog weg.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderConvertV2(ChangeOrderConvertV2Model model)
        {
            var quote = await _db.ChangeOrder
                .Include(c => c.ChangeOrderDetail)
                .FirstOrDefaultAsync(c => c.Id == model.ChangeOrderId);
            if (quote is null || !quote.IsQuote) return NotFound();

            var problem = await ConvertProblemAsync(quote);
            if (problem != null)
            {
                AddMessage("error", problem, "Kon niet omzetten");
                return BackToChangeOrder(model.ProjectId, quote.Id);
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var description = string.IsNullOrWhiteSpace(model.ConvertDescription) ? quote.Description ?? "" : model.ConvertDescription.Trim();
            // De offertetekst ("Deze offerte is geldig tot…") hoort niet op de wijzigingsopdracht: standaardtekst van een WO.
            var conditions = string.IsNullOrWhiteSpace(model.ConvertConditions) ? DefaultChangeOrderConditions : model.ConvertConditions.Trim();
            var order = new ChangeOrder
            {
                ClientAccountId = quote.ClientAccountId,
                Description = description.Length > 250 ? description[..250] : description,
                Subject = quote.Subject,
                Date = today,
                ExpirationDate = quote.ExpirationDate >= today ? quote.ExpirationDate : today.AddDays(30),
                Comment = quote.Comment,
                Invoiceable = quote.Invoiceable,
                ContractActivityId = quote.ContractActivityId,
                ChangeOrderConditions = conditions is { Length: > 1000 } ? conditions[..1000] : conditions,
                IsQuote = false,
                QuoteSupplierReference = quote.QuoteSupplierReference,
                QuoteVatPercentage = quote.QuoteVatPercentage,
                QuoteSourcePath = quote.QuoteSourcePath,
                QuoteSourceFileName = quote.QuoteSourceFileName,
                SourceChangeOrderId = quote.Id,
                SourceKind = 3,
            };
            var sortOrder = 0;
            foreach (var d in quote.ChangeOrderDetail.OrderBy(d => d.SortOrder ?? int.MaxValue).ThenBy(d => d.Id))
            {
                order.ChangeOrderDetail.Add(new ChangeOrderDetail
                {
                    Description = d.Description,
                    MeasurementType = d.MeasurementType,
                    MeasurementUnit = d.MeasurementUnit,
                    Number = d.Number,
                    Price = d.Price,
                    Commission = d.Commission,
                    VatPercentage = d.VatPercentage,
                    VatTypeId = d.VatTypeId,
                    SourceImagePath = d.SourceImagePath,
                    SourceDetailId = d.Id,
                    SortOrder = sortOrder++,
                });
            }

            void AddTerm(byte kind, decimal pct, byte trigger, int termOrder) => order.ChangeOrderPaymentTerm.Add(new ChangeOrderPaymentTerm
            {
                Kind = kind,
                Percentage = pct,
                TriggerType = trigger,
                SortOrder = termOrder,
                CreatedAt = DateTime.Now,
            });
            if (model.ConvertPlan == "voorschot-saldo")
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

            quote.QuoteConvertedAt = DateTime.Now;
            _db.ChangeOrder.Add(order);
            await _db.SaveChangesAsync();

            return await AfterSaveRedirectAsync(order, model.ProjectId, model.AfterSave,
                $"Wijzigingsopdracht {CoNo(order.Id)} aangemaakt uit {CoNo(quote.Id)}. De prijzen liggen vast; aantallen, omschrijvingen en het facturatieplan kan je nog aanpassen.", "Omgezet");
        }

        /// <summary>Waar de gebruiker na opslaan/omzetten terechtkomt: het scherm zelf, of het scherm met
        /// meteen een modal open — "send" de verzendmodal (21d voor een WO, de offertemail voor een
        /// offerte), "convert" de 21c-omzetmodal. De modal werkt zo altijd op de zopas bewaarde toestand:
        /// wat verzonden of omgezet wordt is wat op het scherm staat.</summary>
        private Task<IActionResult> AfterSaveRedirectAsync(ChangeOrder co, int projectId, string afterSave, string message, string title)
        {
            IActionResult Back(object extra = null) => RedirectToAction(nameof(ChangeOrderDetailV2), extra ?? new { projectid = projectId, coid = co.Id });

            if (afterSave == "list")
            {
                AddMessage("success", message, title);
                return Task.FromResult<IActionResult>(RedirectToAction(nameof(ChangeOrdersV2), new { projectid = projectId }));
            }
            if (afterSave != "send" && afterSave != "convert")
            {
                AddMessage("success", message, title);
                return Task.FromResult(Back());
            }

            var problem = SendProblem(co);
            if (problem != null)
            {
                AddMessage("warning", problem + " Wat je invulde is wel opgeslagen.", afterSave == "send" ? "Nog niet verzonden" : "Nog niet omgezet");
                return Task.FromResult(Back());
            }
            return Task.FromResult(afterSave == "send"
                ? Back(new { projectid = projectId, coid = co.Id, send = true })
                : Back(new { projectid = projectId, coid = co.Id, convert = true }));
        }

        /// <summary>De voorlopige naam die 20c aan een ingelezen offerte geeft ("Leveranciersofferte 2025-118")
        /// zolang de gebruiker nog geen omschrijving voor de klant invulde.</summary>
        private static bool IsIntakePlaceholder(string description) => description != null && description.StartsWith("Leveranciersofferte");

        /// <summary>Wat verzenden (of omzetten) nog in de weg staat, of null als alles in orde is.</summary>
        private string SendProblem(ChangeOrder co)
        {
            var details = co.ChangeOrderDetail.Where(d => _db.Entry(d).State != EntityState.Deleted).ToList();
            var terms = co.ChangeOrderPaymentTerm.Where(t => _db.Entry(t).State != EntityState.Deleted).ToList();
            if (details.Count == 0) return "Voeg minstens één regel toe.";
            if (details.Any(d => d.NeedsReview)) return "Minstens één regel staat nog op \"controleer\".";
            if (!co.IsQuote && terms.Count > 0 && Math.Abs(terms.Sum(t => t.Percentage ?? 0m) - 100m) > 0.01m) return "Het facturatieplan moet samen 100 % zijn.";
            if (string.IsNullOrWhiteSpace(co.Description) || IsIntakePlaceholder(co.Description)) return "Vul een omschrijving voor de klant in.";
            return null;
        }

        /// <summary>De eigenaars van een klantenaccount met hun e-mailadres — het klantenaccount zelf
        /// (eigenaar 1) en de mede-eigenaars; zelfde volgorde en ontdubbeling op e-mail als het voorstel
        /// van de ondertekenmodule (ChangeOrderSigningSource.SuggestPartiesAsync).</summary>
        private async Task<List<Models.Signing.SigningStartPartyVm>> OwnerRecipientsAsync(int clientAccountId)
        {
            var list = new List<Models.Signing.SigningStartPartyVm>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var account = await _db.ClientAccount.AsNoTracking().FirstOrDefaultAsync(a => a.Id == clientAccountId);
            if (account != null)
            {
                var email = account.Email?.Trim();
                list.Add(new Models.Signing.SigningStartPartyVm { Include = !string.IsNullOrWhiteSpace(email), DisplayName = Services.Signing.ChangeOrderPdfBuilder.DisplayName(account), Email = email, Capacity = "Klant" });
                if (!string.IsNullOrWhiteSpace(email)) seen.Add(email);
            }
            var contacts = await _db.ClientContacts.AsNoTracking()
                .Where(c => c.ClientAccountId == clientAccountId && c.IsCoOwner)
                .OrderByDescending(c => c.IsPrimaryContact).ThenBy(c => c.Id)
                .Select(c => new { c.Name, c.Forename, c.CompanyName, c.Email })
                .ToListAsync();
            foreach (var c in contacts)
            {
                var email = c.Email?.Trim();
                if (!string.IsNullOrWhiteSpace(email) && !seen.Add(email)) continue;
                var name = string.Join(" ", new[] { c.Name, c.Forename }.Where(x => !string.IsNullOrWhiteSpace(x)));
                list.Add(new Models.Signing.SigningStartPartyVm { Include = !string.IsNullOrWhiteSpace(email), DisplayName = string.IsNullOrWhiteSpace(name) ? c.CompanyName ?? "Mede-eigenaar" : name, Email = email, Capacity = "Mede-eigenaar" });
            }
            return list;
        }

        // ═══════════════════════════════════════════════════════════════════════════════════════════
        // 3a. Offerte aan de klant — verzenden per mail (PDF als bijlage, geen handtekening)
        // ═══════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>Mogelijke afzenders van de offertemail: de ingelogde gebruiker (standaard) en de
        /// projectleider van het project. Enkel wie een e-mailadres heeft.</summary>
        private async Task<List<QuoteSenderV2>> QuoteSendersAsync(ChangeOrder co)
        {
            var list = new List<QuoteSenderV2>();
            var myEmail = User.GetCpmEmail();
            if (!string.IsNullOrWhiteSpace(myEmail))
                list.Add(new QuoteSenderV2 { Key = "me", Label = $"Mezelf ({User.GetCpmDisplayName()})", Email = myEmail.Trim() });
            var lead = await _db.ChangeOrder.AsNoTracking()
                .Where(c => c.Id == co.Id)
                .Select(c => c.ContractActivity.Contract.Project.AspNetUser)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
            if (lead != null && !string.IsNullOrWhiteSpace(lead.Email) && !list.Any(s => string.Equals(s.Email, lead.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                var name = string.Join(" ", new[] { lead.Voornaam, lead.Familienaam }.Where(x => !string.IsNullOrWhiteSpace(x)));
                list.Add(new QuoteSenderV2 { Key = "lead", Label = "Projectleider" + (string.IsNullOrWhiteSpace(name) ? "" : $" ({name})"), Email = lead.Email.Trim() });
            }
            return list;
        }

        [HttpGet]
        public async Task<IActionResult> SendQuoteModalV2(int projectId, int changeOrderId)
        {
            try
            {
                return await BuildSendQuoteModalV2Async(projectId, changeOrderId);
            }
            catch (Exception ex)
            {
                // Het formulier wordt via AJAX in een modal geladen: een rauwe 500 gaf enkel "kon niet laden". Nu staat de
                // oorzaak in het log én (kort) in de modal zelf.
                _logger?.LogError(ex, "Offerte-verzendformulier laden mislukt voor {ChangeOrderId}", changeOrderId);
                return Content("<div class=\"gl-v2-co-send-loading\">Kon het verzendformulier niet laden: " + System.Net.WebUtility.HtmlEncode(ex.GetBaseException().Message) + "</div>", "text/html");
            }
        }

        /// <summary>Onderwerp van de offertemail: "Offerte OF-2026-014: {onderwerp}" (het onderwerp van de offerte, niet de
        /// omschrijving voor de klant); zonder onderwerp enkel "Offerte OF-2026-014".</summary>
        private string QuoteMailSubject(ChangeOrder co) =>
            $"Offerte {CoNo(co)}" + (string.IsNullOrWhiteSpace(co.Subject) ? "" : ": " + co.Subject.Trim());

        private async Task<IActionResult> BuildSendQuoteModalV2Async(int projectId, int changeOrderId)
        {
            var co = await _db.ChangeOrder.AsNoTracking()
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ChangeOrderPaymentTerm)
                .Include(c => c.ClientAccount)
                .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Project)
                .FirstOrDefaultAsync(c => c.Id == changeOrderId);
            if (co is null || !co.IsQuote)
            {
                // Geen kale 404 (de modal zou enkel "niet laden" tonen): zeg wat er mis is.
                var why = co is null ? $"Offerte met id {changeOrderId} bestaat niet (meer) — sla de offerte eerst op." : $"{CoNo(co.Id)} is geen offerte maar een wijzigingsopdracht — die verstuur je via de ondertekening.";
                return Content("<div class=\"gl-v2-co-send-loading\">" + System.Net.WebUtility.HtmlEncode(why) + "</div>", "text/html");
            }

            var be = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");
            var incl = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m) * (1 + (d.VatPercentage ?? 0m) / 100m));
            var projectName = co.ContractActivity?.Contract?.Project?.ProjectName;
            var vm = new SendQuoteModalV2Vm
            {
                ProjectId = projectId,
                ChangeOrderId = co.Id,
                Number = $"{CoNo(co.Id)}",
                Subtitle = string.Join(" · ", new[]
                {
                    co.Subject?.Trim(),
                    "€ " + incl.ToString("N2", be) + " incl. btw",
                    co.ClientAccount != null ? "klantenaccount " + Services.Signing.ChangeOrderPdfBuilder.DisplayName(co.ClientAccount) : null,
                }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Problem = SendProblem(co),
                Parties = await OwnerRecipientsAsync(co.ClientAccountId),
                Senders = await QuoteSendersAsync(co),
                Subject = QuoteMailSubject(co) + (string.IsNullOrWhiteSpace(projectName) ? "" : $" — {projectName}"),
                PdfUrl = Url.Action("ChangeOrderPDF", "Projecten", new { changeorderid = co.Id }),
                TestRecipient = HttpContext.RequestServices.GetRequiredService<IOptions<ServiceCore.Signing.SigningOptions>>().Value.TestRecipientOverride,
            };
            return PartialView("Modals/_ModalSendQuoteV2", vm);
        }

        /// <summary>De offerte per mail naar de gekozen eigenaars: één mail per ontvanger met eigen aanhef,
        /// het vrije bericht en de offerte als PDF-bijlage. Daarna staat de offerte op "verzonden" en ligt
        /// ze vast (terug aanpassen kan via "Aanpassen"). Testmodus van de ondertekenmodule
        /// (Signing:TestRecipientOverride) geldt ook hier, zodat een testomgeving nooit een klant mailt.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderQuoteSendV2(ChangeOrderQuoteSendV2Model model)
        {
            var ct = HttpContext.RequestAborted;
            var co = await _db.ChangeOrder
                .Include(c => c.ChangeOrderDetail)
                .Include(c => c.ChangeOrderPaymentTerm)
                .FirstOrDefaultAsync(c => c.Id == model.ChangeOrderId, ct);
            if (co is null || !co.IsQuote) return NotFound();

            var recipients = (model.Parties ?? new())
                .Where(p => p.Include && !string.IsNullOrWhiteSpace(p.Email))
                .Select(p => (Name: (p.DisplayName ?? "").Trim(), Email: p.Email.Trim()))
                .ToList();
            var senders = await QuoteSendersAsync(co);
            var sender = senders.FirstOrDefault(s => s.Key == model.Sender) ?? senders.FirstOrDefault();
            var error = sender == null ? "Er is geen e-mailadres bekend voor jou of de projectleider om de offerte van te versturen."
                : co.QuoteConvertedAt.HasValue ? "Deze offerte is al omgezet naar een wijzigingsopdracht."
                : SendProblem(co) ?? (recipients.Count == 0 ? "Kies minstens één ontvanger met een e-mailadres." : null);
            if (error != null)
            {
                AddMessage("error", error, "Niet verzonden");
                return BackToChangeOrder(model.ProjectId, co.Id);
            }

            var builder = HttpContext.RequestServices.GetRequiredService<Services.Signing.ChangeOrderPdfBuilder>();
            var pdfModel = await builder.LoadAsync(co.Id, ct);
            byte[] pdf;
            // Altijd de gl-v2-documentlayout: offertes bestaan enkel in gl-v2, ongeacht de cookie van wie verzendt.
            try { pdf = builder.Render(pdfModel, useGlV2Layout: true); }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Offerte-PDF genereren mislukt voor {ChangeOrderId}", co.Id);
                AddMessage("error", "De offerte kon niet als PDF opgemaakt worden.", "Niet verzonden");
                return BackToChangeOrder(model.ProjectId, co.Id);
            }
            var attachment = new EmailAttachment(Services.Signing.ChangeOrderPdfBuilder.FileName(pdfModel), pdf, "application/pdf");

            var subject = string.IsNullOrWhiteSpace(model.Subject) ? QuoteMailSubject(co) : model.Subject.Trim();
            static string H(string x) => System.Net.WebUtility.HtmlEncode(x ?? "");
            var message = string.IsNullOrWhiteSpace(model.Message)
                ? ""
                : $"<p>{H(model.Message.Trim()).Replace("\r\n", "\n").Replace("\n", "<br/>")}</p>";

            var signingOptions = HttpContext.RequestServices.GetRequiredService<IOptions<ServiceCore.Signing.SigningOptions>>().Value;
            var email = HttpContext.RequestServices.GetRequiredService<IEmailSender>();
            var graphMail = HttpContext.RequestServices.GetRequiredService<CPMCore.Services.GraphMailSender>();
            var sent = new List<string>();
            var failed = new List<string>();
            foreach (var r in recipients)
            {
                // Zelfde huisstijl-omslag als de mails van de ondertekenmodule.
                var body = ServiceCore.Signing.SigningNotifier.MailLayout($"Beste {H(r.Name)},",
                    message +
                    $"<p>In bijlage vindt u onze offerte {(string.IsNullOrWhiteSpace(co.Subject) ? "" : "<strong>" + H(co.Subject.Trim()) + "</strong> ")}({CoNo(co.Id)}), geldig tot {co.ExpirationDate:dd/MM/yyyy}.</p>" +
                    "<p>Gaat u akkoord of hebt u vragen, antwoord dan gerust op deze e-mail. Na uw akkoord ontvangt u een wijzigingsopdracht ter ondertekening.</p>");
                var address = r.Email;
                var mailSubject = subject;
                if (signingOptions.IsTestMode)
                {
                    mailSubject = $"[TEST → {address}] {subject}";
                    address = signingOptions.TestRecipientOverride;
                }
                try
                {
                    // Eerst echt namens de mailbox van de afzender (Graph, komt in diens "Verzonden items");
                    // lukt dat niet (niet geconfigureerd, recht ontbreekt), dan via de vaste SMTP-account met
                    // het adres van de afzender als From.
                    var viaGraph = await graphMail.TrySendAsync(sender!.Email, address, mailSubject, body, new[] { attachment }, ct);
                    if (!viaGraph) await email.SendEmailAsync(address, mailSubject, body, new[] { attachment }, fromEmail: sender.Email);
                    sent.Add(r.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Offertemail voor {ChangeOrderId} kon niet verstuurd worden.", co.Id);
                    failed.Add(r.Name);
                }
            }

            if (sent.Count > 0)
            {
                co.DateSendToClient = DateOnly.FromDateTime(DateTime.Today);
                await _db.SaveChangesAsync(ct);
                AddMessage("success", "De offerte is gemaild naar " + string.Join(" en ", sent) + ".", "Offerte verzonden");
            }
            if (failed.Count > 0)
                AddMessage("error", "De mail naar " + string.Join(" en ", failed) + " kon niet verstuurd worden.", "Niet verzonden");
            return BackToChangeOrder(model.ProjectId, co.Id);
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
                Number = $"{CoNo(co.Id)}",
                Subtitle = string.Join(" · ", new[]
                {
                    co.Subject?.Trim(),
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
                Orders = orders.Select(o => new IdNameBO { ID = o.Id, Display = $"{CoNo(o.Id)} · {o.Description} · {o.Client}" }).ToList(),
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
            decimal? vat = null;
            int? vatTypeId = null;
            if (!sameClient)
            {
                var targetVat = await ResolveVatForClientAsync(projectId, targetClientId);
                vat = targetVat.Percentage;
                vatTypeId = targetVat.VatTypeId;
            }
            // Een schijf-trigger hoort bij de betalingsgroep van de bron-eenheid; voor een andere eenheid
            // valt hij terug op "na ondertekening" (aan te passen in het facturatieplan van de kopie).
            var copy = new ChangeOrder
            {
                ClientAccountId = targetClientId,
                Description = src.Description,
                Subject = src.Subject,
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
                    VatTypeId = sameClient ? d.VatTypeId : vatTypeId,
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
                asVersion ? $"Nieuwe versie aangemaakt als {CoNo(copy.Id)}. Pas aan en verzend opnieuw." : $"Kopie aangemaakt als {CoNo(copy.Id)}. Regels en facturatieplan zijn overgenomen, handtekeningen niet.",
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
                case "sent" when !co.IsQuote:
                    co.DateSendToClient ??= today;
                    AddMessage("success", "Gemarkeerd als verzonden.", "Verzonden");
                    break;
                case "signed" when !co.IsQuote:
                    co.DateSendToClient ??= today;
                    co.DateAgreement ??= today;
                    AddMessage("success", "Akkoord van de klant geregistreerd.", "Ondertekend");
                    break;
                case "concept" when !co.DateAgreement.HasValue && !(co.IsQuote && co.QuoteConvertedAt.HasValue):
                    co.DateSendToClient = null;
                    AddMessage("success", co.IsQuote ? "De offerte is weer een concept. Verzend ze opnieuw zodra ze aangepast is." : "Terug naar concept — regels en facturatieplan zijn weer aanpasbaar.", "Concept");
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
