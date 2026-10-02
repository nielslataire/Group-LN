using BOCore;
using CPMCore.Configuration;
using CPMCore.Models.GlV2;
using CPMCore.Models.Projecten;
using CPMCore.Services;
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
    /// <summary>gl-v2 "Wijzigingsopdracht" — het scherm per stap (design-handoff punt 28, 28a–28i; de
    /// opvolger van 20d): één scherm, de fase bepaalt wat het toont — melding, vergrendeling,
    /// facturatieplan, ondertekening, facturen en knoppen. Eigen bestand naast de legacy
    /// AddChangeOrder/EditChangeOrder-acties, die ongewijzigd blijven. Leest/schrijft rechtstreeks via
    /// _db (zelfde stijl als PaymentStagesV2.cs/InvoicingV2.cs) i.p.v. via ChangeOrderBO/
    /// ChangeOrderTranslator — de offerte-/facturatieplan-velden zitten niet in die oudere BO-laag en dit
    /// voorkomt een split-brain tussen twee opslagpaden voor dezelfde rij. Status en fase worden altijd
    /// afgeleid (ProjectenController.ChangeOrderFlowV2.cs), nooit opgeslagen.</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2(int projectid, int? clientid, int coid = 0, bool send = false)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var canWrite = _ps.HasWrite(PermissionCodes.ProjectsChangeOrders);

            var projectResponse = _projectService.GetProjectByID(projectid);
            if (!projectResponse.Success || projectResponse.Value is null) return NotFound();
            var project = projectResponse.Value;

            ChangeOrder co = null;
            if (coid > 0)
            {
                co = await _db.ChangeOrder.AsNoTracking()
                    .Include(c => c.ChangeOrderDetail)
                    .Include(c => c.ChangeOrderPaymentTerm)
                    .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Company)
                    .FirstOrDefaultAsync(c => c.Id == coid);
                if (co is null) return NotFound();
                // Een offerte hoort op 20c (stap 1) — dit scherm begint bij "Opgemaakt" (stap 2).
                if (co.IsQuote) return RedirectToAction(nameof(QuoteIntakeV2), new { projectid, coid });
            }

            var vm = new ChangeOrderDetailV2Vm
            {
                ProjectId = projectid,
                ProjectName = project.Name,
                CanWrite = canWrite,
                IsNew = co is null,
                ChangeOrderId = co?.Id ?? 0,
                Number = co is null ? "" : $"WO-{co.Id:000}",
            };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid = projectid } };
            var listNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("ChangeOrdersV2", "Projecten", "Offertes & wijzigingen") { Parent = projectDetail, RouteValues = new { projectid = projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(ChangeOrderDetailV2), "Projecten", vm.IsNew ? "Nieuwe wijzigingsopdracht" : vm.Number) { Parent = listNode, RouteValues = new { projectid, coid } };

            vm.ClientAccountId = co?.ClientAccountId ?? clientid ?? 0;
            vm.IsQuote = false;
            vm.Description = co?.Description ?? "";
            vm.InvoiceableByBouwheer = co?.Invoiceable ?? true;
            vm.ContractActivityId = co?.ContractActivityId ?? 0;
            vm.QuoteSupplierReference = co?.QuoteSupplierReference;
            vm.QuoteVatPercentage = co?.QuoteVatPercentage;
            vm.QuoteDate = co?.Date ?? DateOnly.FromDateTime(DateTime.Today);
            vm.ExpirationDate = co?.ExpirationDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30));
            vm.QuoteConvertedAt = co?.QuoteConvertedAt;
            vm.ConditionsText = co?.ChangeOrderConditions ?? "";
            vm.DateSendToClient = co?.DateSendToClient;
            vm.DateAgreement = co?.DateAgreement;
            vm.VatKlantPercentage = 21m;

            if (vm.ClientAccountId > 0)
            {
                var clientResponse = _clientService.GetClientAccountById(vm.ClientAccountId);
                vm.ClientName = clientResponse.Success ? clientResponse.Values.FirstOrDefault()?.Name ?? "" : "";
                var vat = await ResolveVatForClientAsync(projectid, vm.ClientAccountId);
                vm.VatKlantPercentage = vat.Percentage;
                vm.VatKlantLabel = vat.Label;
                vm.UnitName = vat.UnitName;
            }
            if (string.IsNullOrEmpty(vm.VatKlantLabel)) vm.VatKlantLabel = "volgt uit de betalingsgroep van de eenheid";

            vm.ContractActivities = _projectService.GetProjectContractActivitiesForSelect(projectid) is { Success: true } actResp
                ? actResp.Values : new List<IdNameBO>();
            // "Leeg beginnen" (28b): klant · eenheid kies je in het scherm zelf — elke optie draagt de btw
            // van haar betalingsgroep, zodat de regels meteen met de juiste btw rekenen.
            if (vm.ClientAccountId <= 0)
                vm.ClientOptions = await LoadClientOptionsAsync(projectid);

            vm.Stages = _db.InvoicingPaymentStages.AsNoTracking()
                .Where(s => s.Group.ProjectId == projectid)
                .OrderBy(s => s.Id)
                .Select(s => new IdNameBO { ID = s.Id, Display = s.Name })
                .ToList();

            // ── Statusfeiten (zelfde bron als de lijst) + het ondertekendossier ────────────────────────
            var facts = await LoadChangeOrderFlowFactsAsync(projectid, co is null ? Array.Empty<ChangeOrder>() : new[] { co });
            vm.SigningEnabled = facts.SigningEnabled;
            vm.CanStartSigning = _ps.HasWrite(PermissionCodes.Signing);
            CaseStatusView latestCase = null;
            if (facts.SigningEnabled && co != null)
            {
                var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
                vm.ActiveSigningCase = await signing.GetActiveCaseForSourceAsync(Services.Signing.ChangeOrderSigningSource.Key, coid, HttpContext.RequestAborted);
                vm.CompletedSigningCase = await signing.GetCompletedCaseForSourceAsync(Services.Signing.ChangeOrderSigningSource.Key, coid, HttpContext.RequestAborted);
                facts.LatestCase.TryGetValue(co.Id, out latestCase);
                latestCase = vm.ActiveSigningCase ?? vm.CompletedSigningCase ?? latestCase;
                if (latestCase != null) facts.LatestCase[co.Id] = latestCase;
            }

            if (co != null)
            {
                vm.Rows = co.ChangeOrderDetail.OrderBy(d => d.SortOrder ?? int.MaxValue).ThenBy(d => d.Id).Select(d => new ChangeOrderDetailRowV2
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

                var totalExcl = co.ChangeOrderDetail.Sum(d => d.Number * d.Price * (1 + d.Commission / 100m));
                vm.CostTotal = co.ChangeOrderDetail.Sum(d => d.Number * d.Price);
                vm.IsMinwerk = totalExcl < 0;
                vm.Terms = co.ChangeOrderPaymentTerm.OrderBy(t => t.SortOrder).Select(t =>
                {
                    facts.TermInvoice.TryGetValue(t.Id, out var invoice);
                    return new ChangeOrderTermV2
                    {
                        Id = t.Id,
                        Kind = t.Kind,
                        Percentage = t.Percentage,
                        FixedAmount = t.FixedAmount,
                        TriggerType = t.TriggerType,
                        TriggerStageId = t.TriggerStageId,
                        TriggerLabel = TriggerLabel(t.Kind, t.TriggerType, t.TriggerStageId, vm.Stages),
                        AmountExVat = t.Percentage.HasValue ? Math.Round(totalExcl * t.Percentage.Value / 100m, 2, MidpointRounding.AwayFromZero) : (t.FixedAmount ?? 0m),
                        IsInvoiced = invoice != null || facts.LegacyInvoicedOrders.Contains(co.Id),
                        IsTriggered = facts.TermTriggered.GetValueOrDefault(t.Id),
                        ReleasedAt = t.ReleasedAt,
                        InvoiceId = invoice?.InvoiceId,
                        InvoiceNumber = invoice?.Number,
                    };
                }).ToList();
                vm.FacturatieplanPreset = PresetFor(vm.Terms);

                if (facts.Invoices.TryGetValue(co.Id, out var invoices)) vm.Invoices = invoices;
                vm.Status = ComputeFlowStatus(co, facts, DateOnly.FromDateTime(DateTime.Today));

                // Bron: de offerte van de leverancier en/of de WO waarvan dit een kopie/nieuwe versie is.
                vm.SupplierName = co.ContractActivity?.Contract?.Company?.BedrijfsNaam;
                vm.HasQuoteSource = co.QuoteConvertedAt.HasValue || !string.IsNullOrWhiteSpace(co.QuoteSourcePath) || !string.IsNullOrWhiteSpace(co.QuoteSupplierReference);
                vm.QuoteSourceFileName = co.QuoteSourceFileName;
                if (!string.IsNullOrWhiteSpace(co.QuoteSourcePath))
                {
                    var storage = HttpContext.RequestServices.GetRequiredService<DocStorageService>();
                    vm.QuoteSourceUrl = await storage.GetSignedUrlAsync(co.QuoteSourcePath, "quotes");
                    var ext = System.IO.Path.GetExtension(co.QuoteSourceFileName ?? co.QuoteSourcePath).ToLowerInvariant();
                    vm.QuoteSourceIsImage = ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif";
                }
                vm.SourceKind = co.SourceKind;
                if (co.SourceChangeOrderId is int sourceId)
                {
                    var source = await _db.ChangeOrder.AsNoTracking().Where(c => c.Id == sourceId).Select(c => new { c.Id, c.Description }).FirstOrDefaultAsync();
                    if (source != null)
                    {
                        vm.SourceChangeOrderId = source.Id;
                        vm.SourceChangeOrderLabel = $"WO-{source.Id:000} · {source.Description}";
                    }
                }
                vm.ReplacedByChangeOrderId = await _db.ChangeOrder.AsNoTracking()
                    .Where(c => c.SourceChangeOrderId == co.Id && c.SourceKind == 2)
                    .OrderByDescending(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
            }
            else
            {
                vm.Status = ChangeOrderStatus.Opgemaakt;
            }

            vm.DefaultCommission = vm.Rows.Count > 0 ? vm.Rows[0].Commission : await DefaultCommissionAsync(projectid);

            var suggestedSigners = vm.ClientAccountId > 0
                ? await SuggestedSignerNamesAsync(vm.ClientAccountId)
                : new List<string>();
            BuildScreenState(vm, co, latestCase, suggestedSigners);
            vm.OpenSendModal = send && vm.IsEditable && !vm.IsNew;
            vm.History = BuildHistory(vm, co, latestCase);

            return View(vm);
        }

        private static string Pct(decimal v) => v.ToString("0.##");

        private static string TriggerLabel(byte kind, byte triggerType, int? stageId, List<IdNameBO> stages) => triggerType switch
        {
            1 => "Na ondertekening",
            2 when kind == 3 || !stageId.HasValue => "Bij de laatste schijf",
            2 => "Bij schijf · " + (stages.FirstOrDefault(s => s.ID == stageId.Value)?.Display ?? "onbekend"),
            3 => "Manueel vrijgeven",
            _ => "",
        };

        /// <summary>Welke snelkeuze hoort bij de bewaarde termijnen (enkel voor de actieve pil in de UI).</summary>
        private static string PresetFor(List<ChangeOrderTermV2> terms)
        {
            if (terms.Count == 0) return "";
            if (terms.Count == 1 && terms[0].Kind == 3) return "laatste-schijf";
            if (terms.Count == 2 && terms[0].Kind == 1 && terms[1].Kind == 3 && terms[0].TriggerType == 1)
                return (terms[1].Percentage ?? 0) == 0 ? "na-ondertekening" : "voorschot-saldo";
            return "eigen";
        }

        /// <summary>Wie zal tekenen zolang er nog geen dossier is: het klantenaccount (eigenaar 1) en de
        /// mede-eigenaars — zelfde voorstel als ChangeOrderSigningSource.SuggestPartiesAsync.</summary>
        private async Task<List<string>> SuggestedSignerNamesAsync(int clientAccountId)
        {
            var names = new List<string>();
            var account = await _db.ClientAccount.AsNoTracking().FirstOrDefaultAsync(a => a.Id == clientAccountId);
            if (account != null) names.Add(Services.Signing.ChangeOrderPdfBuilder.DisplayName(account));
            var contacts = await _db.ClientContacts.AsNoTracking()
                .Where(c => c.ClientAccountId == clientAccountId && c.IsCoOwner)
                .OrderByDescending(c => c.IsPrimaryContact).ThenBy(c => c.Id)
                .Select(c => new { c.Name, c.Forename, c.CompanyName })
                .ToListAsync();
            foreach (var c in contacts)
            {
                var name = string.Join(" ", new[] { c.Name, c.Forename }.Where(s => !string.IsNullOrWhiteSpace(s)));
                names.Add(string.IsNullOrWhiteSpace(name) ? c.CompanyName ?? "Mede-eigenaar" : name);
            }
            return names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        }

        private static string Initials(string name)
        {
            var parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            return (parts.Length == 1 ? parts[0][..1] : parts[0][..1] + parts[1][..1]).ToUpperInvariant();
        }

        private static string JoinNames(IEnumerable<string> names)
        {
            var list = names.ToList();
            return list.Count switch
            {
                0 => "",
                1 => list[0],
                _ => string.Join(", ", list.Take(list.Count - 1)) + " en " + list[^1],
            };
        }

        /// <summary>Leidt uit status + ondertekendossier + facturen af wat het scherm toont (design-handoff
        /// 28a–28i): fase, pillen, stappenplan, melding, vergrendeling, facturatieplan-status per termijn,
        /// de kaart Ondertekening en de voettekst. De view beslist zelf niets.</summary>
        private static void BuildScreenState(ChangeOrderDetailV2Vm vm, ChangeOrder co, CaseStatusView latestCase, List<string> suggestedSigners)
        {
            var parties = latestCase?.Parties ?? (IReadOnlyList<PartyStatusView>)Array.Empty<PartyStatusView>();
            var signedCount = parties.Count(p => p.Status == (int)SigningPartyStatus.Signed);
            var anyOverdue = vm.Invoices.Any(i => i.IsOverdue);

            vm.Phase = vm.Status switch
            {
                ChangeOrderStatus.Geannuleerd => "ingetrokken",
                ChangeOrderStatus.Geweigerd => "geweigerd",
                ChangeOrderStatus.Verzonden when latestCase?.Status == (int)SigningCaseStatus.Expired => "verlopen",
                ChangeOrderStatus.Verzonden => signedCount > 0 ? "wacht" : "verzonden",
                ChangeOrderStatus.Ondertekend => anyOverdue ? "telaat" : "ondertekend",
                ChangeOrderStatus.Factureerbaar => anyOverdue ? "telaat" : "factureerbaar",
                ChangeOrderStatus.Gefactureerd => anyOverdue ? "telaat" : "gefactureerd",
                ChangeOrderStatus.Betaald => "betaald",
                _ => "concept",
            };

            var isDraftPhase = vm.Phase is "concept" or "ingetrokken" or "verlopen";
            vm.IsEditable = vm.CanWrite && isDraftPhase && vm.ActiveSigningCase is null;
            vm.IsLocked = !vm.IsEditable;
            vm.LockText = vm.Phase switch
            {
                "verzonden" or "wacht" => "Vergrendeld",
                "betaald" => "Archief",
                "concept" or "ingetrokken" or "verlopen" => vm.IsEditable ? null : "Alleen lezen",
                _ => "Afgesloten",
            };

            (vm.StatusPillLabel, vm.StatusPillTone) = vm.Phase switch
            {
                "ingetrokken" => ("Ingetrokken", "is-neutral"),
                "verlopen" => ("Niet ondertekend", "is-blocked"),
                "verzonden" => ("Verzonden", "is-attention"),
                "wacht" => ("Wacht op handtekening", "is-attention"),
                "ondertekend" or "factureerbaar" => ("Goedgekeurd", "is-positive"),
                "gefactureerd" => ("Gefactureerd", "is-positive"),
                "betaald" => ("Betaald", "is-solid"),
                "geweigerd" => ("Geweigerd", "is-blocked"),
                "telaat" => ("Vervallen", "is-blocked"),
                _ => ("Concept", "is-neutral"),
            };

            // ── Stappenplan (punt 27, variant lijn · sm): stap → scherm, zie 29b ────────────────────────
            var first = vm.HasQuoteSource ? "done" : "uit";
            var (states, stepNr) = vm.Phase switch
            {
                "verzonden" => (new[] { first, "done", "current", "todo", "todo", "todo", "todo" }, 3),
                "wacht" => (new[] { first, "done", "done", "wacht", "todo", "todo", "todo" }, 4),
                "geweigerd" => (new[] { first, "done", "done", "error", "uit", "uit", "uit" }, 4),
                "ondertekend" => (new[] { first, "done", "done", "current", "todo", "todo", "todo" }, 4),
                "factureerbaar" => (new[] { first, "done", "done", "done", "current", "todo", "todo" }, 5),
                "gefactureerd" => (new[] { first, "done", "done", "done", "done", "current", "todo" }, 6),
                "telaat" => (new[] { first, "done", "done", "done", "done", "error", "todo" }, 6),
                "betaald" => (new[] { first, "done", "done", "done", "done", "done", "done" }, 7),
                _ => (new[] { first, "current", "todo", "todo", "todo", "todo", "todo" }, 2),
            };
            var nextTerm = vm.DateAgreement.HasValue ? vm.Terms.FirstOrDefault(t => !t.IsInvoiced && t.IsTriggered) : null;
            vm.NextInvoiceableTerm = nextTerm;
            vm.OpenInvoice = vm.Invoices.Where(i => !i.IsPaid).OrderBy(i => i.DueDate ?? DateOnly.MaxValue).FirstOrDefault();
            var subs = new[]
            {
                vm.HasQuoteSource ? (string.IsNullOrWhiteSpace(vm.QuoteSupplierReference) ? "ingelezen" : vm.QuoteSupplierReference) : "geen offerte",
                co is null ? "nieuw" : co.Date.ToString("dd/MM"),
                vm.Phase is "ingetrokken" ? "ingetrokken" : vm.DateSendToClient?.ToString("dd/MM"),
                vm.Phase switch
                {
                    "wacht" => $"{signedCount} van {parties.Count}",
                    "geweigerd" => "geweigerd",
                    _ => vm.DateAgreement?.ToString("dd/MM"),
                },
                nextTerm != null && vm.Phase == "factureerbaar" ? nextTerm.KindLabel.ToLowerInvariant() : null,
                vm.Invoices.Count > 0 ? string.Join(" · ", vm.Invoices.Select(i => i.Number).Take(2)) : null,
                null,
            };
            var labels = new[] { "Offerte", "Opgemaakt", "Verzonden", "Ondertekend", "Factureerbaar", "Gefactureerd", "Betaald" };
            vm.Stappenplan = new GlV2StappenplanVm
            {
                Size = "sm",
                AriaLabel = "Verloop van de wijzigingsopdracht",
                Steps = labels.Select((label, i) => new GlV2StapVm { Label = label, State = states[i], Sub = subs[i] }).ToList(),
            };
            vm.StepLabel = vm.Phase switch
            {
                "betaald" => "Stap 7 · afgerond",
                "geweigerd" => "Uitzondering · geweigerd",
                "telaat" => "Uitzondering · te laat betaald",
                _ => $"Stap {stepNr} van 7",
            };

            // ── Facturatieplan: status per termijn ─────────────────────────────────────────────────────
            foreach (var t in vm.Terms)
            {
                var invoice = t.InvoiceId.HasValue ? vm.Invoices.FirstOrDefault(i => i.InvoiceId == t.InvoiceId) : null;
                (t.StatusLabel, t.StatusTone) = vm.Phase switch
                {
                    "concept" or "ingetrokken" or "verlopen" => ("Concept", "is-neutral"),
                    "verzonden" or "wacht" => (t.TriggerType == 1 ? "Na handtekening" : "Gepland", "is-neutral"),
                    "geweigerd" => ("Vervallen", "is-neutral"),
                    _ when invoice != null => (invoice.StatusLabel, invoice.StatusTone),
                    _ when t.IsInvoiced => ("Gefactureerd", "is-positive"),
                    _ when t.IsTriggered => ("Te factureren", "is-attention"),
                    _ => (t.TriggerType switch
                    {
                        3 => "Wacht op vrijgave",
                        2 => t.TriggerLabel.Replace("Bij", "Met"),
                        _ => "Gepland",
                    }, "is-neutral"),
                };
            }

            // ── Ondertekening (zijkolom) ───────────────────────────────────────────────────────────────
            vm.SigningCaseId = latestCase?.CaseId;
            var showCase = latestCase != null && !isDraftPhase;
            if (showCase)
            {
                foreach (var p in parties.OrderBy(p => p.SortOrder))
                {
                    var (text, tone) = p.Status switch
                    {
                        (int)SigningPartyStatus.Signed => ("getekend" + (p.SignedAt.HasValue ? $" op {p.SignedAt.Value.ToLocalTime():dd/MM · HH:mm}" : ""), "is-groen"),
                        (int)SigningPartyStatus.Declined => ("geweigerd" + (p.DeclinedAt.HasValue ? $" op {p.DeclinedAt.Value.ToLocalTime():dd/MM}" : ""), "is-rood"),
                        (int)SigningPartyStatus.Opened or (int)SigningPartyStatus.Verified => ("document bekeken, nog niet getekend", "is-goud"),
                        (int)SigningPartyStatus.Invited => ("link verzonden" + (p.ReminderCount > 0 ? $" · {p.ReminderCount}× herinnerd" : ""), "is-goud"),
                        (int)SigningPartyStatus.Expired => ("link verlopen", "is-rood"),
                        (int)SigningPartyStatus.Revoked => ("ingetrokken", "is-grijs"),
                        _ => ("nog niet uitgenodigd", "is-grijs"),
                    };
                    vm.Signers.Add(new ChangeOrderSignerV2
                    {
                        PartyId = p.PartyId,
                        Name = p.DisplayName,
                        Initials = Initials(p.DisplayName),
                        StatusText = text,
                        Tone = tone,
                        CanRemind = vm.ActiveSigningCase != null && p.Status is (int)SigningPartyStatus.Invited or (int)SigningPartyStatus.Opened or (int)SigningPartyStatus.Verified,
                    });
                }
                var sentAt = latestCase.OpenedAt ?? latestCase.CreatedAt;
                vm.SigningMeta.Add(new("Verzonden", sentAt.ToLocalTime().ToString("dd/MM/yyyy")));
                if (latestCase.CompletedAt.HasValue) vm.SigningMeta.Add(new("Ondertekend", latestCase.CompletedAt.Value.ToLocalTime().ToString("dd/MM/yyyy")));
                else if (latestCase.ExpiresAt.HasValue) vm.SigningMeta.Add(new(vm.Phase == "geweigerd" ? "Verviel" : "Vervalt", latestCase.ExpiresAt.Value.ToLocalTime().ToString("dd/MM/yyyy")));
                var reminders = parties.Sum(p => p.ReminderCount);
                if (reminders > 0) vm.SigningMeta.Add(new("Herinneringen", reminders.ToString()));
            }
            else
            {
                foreach (var name in suggestedSigners)
                    vm.Signers.Add(new ChangeOrderSignerV2 { Name = name, Initials = Initials(name), StatusText = isDraftPhase ? "krijgt een link" : "", Tone = "is-grijs" });
                if (vm.DateSendToClient.HasValue && !isDraftPhase) vm.SigningMeta.Add(new("Verzonden", vm.DateSendToClient.Value.ToString("dd/MM/yyyy")));
                if (vm.DateAgreement.HasValue) vm.SigningMeta.Add(new("Akkoord", vm.DateAgreement.Value.ToString("dd/MM/yyyy")));
                if (isDraftPhase) vm.SigningMeta.Add(new("Geldig tot", vm.ExpirationDate.ToString("dd/MM/yyyy")));
            }

            var waiting = vm.Signers.Where(s => s.Tone == "is-goud").Select(s => s.Name).ToList();
            var owners = suggestedSigners.Count > 1 ? $"de {suggestedSigners.Count} eigenaars" : "de klant";
            (vm.SigningHeadText, vm.SigningHeadTone) = vm.Phase switch
            {
                "verzonden" => (showCase ? $"Wacht op {parties.Count} {(parties.Count == 1 ? "handtekening" : "handtekeningen")}" : "Verzonden — wacht op akkoord", "is-goud"),
                "wacht" => ($"{signedCount} van {parties.Count} getekend", "is-goud"),
                "geweigerd" => ("Geweigerd", "is-rood"),
                "verlopen" => ("Niet tijdig ondertekend", "is-rood"),
                "ingetrokken" => ("Ingetrokken · nog niet opnieuw verzonden", "is-grijs"),
                "concept" => ("Nog niet verzonden" + (vm.ClientAccountId > 0 ? $" · gaat naar {owners}" + (string.IsNullOrWhiteSpace(vm.UnitName) ? "" : $" van {vm.UnitName}") : ""), "is-grijs"),
                _ => ("Volledig ondertekend", "is-groen"),
            };

            // ── Melding bovenaan + subtitels + voettekst ───────────────────────────────────────────────
            var replaced = vm.ReplacedByChangeOrderId.HasValue ? $" Vervangen door WO-{vm.ReplacedByChangeOrderId:000}." : "";
            var signWord = suggestedSigners.Count > 1 ? "Alle eigenaars krijgen dan een link om te tekenen." : "De klant krijgt dan een link om te tekenen.";
            switch (vm.Phase)
            {
                case "concept" when vm.SourceKind == 2:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", $"Concept — nieuwe versie van WO-{vm.SourceChangeOrderId:000}",
                        "Alles is overgenomen als nieuw concept. Pas regels en facturatieplan aan en verzend opnieuw; de vorige versie blijft bewaard.");
                    break;
                case "concept" when vm.SourceKind == 1:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", $"Concept — kopie van WO-{vm.SourceChangeOrderId:000}",
                        "Regels en facturatieplan zijn overgenomen, handtekeningen niet. Controleer de btw en het plan voor deze eenheid en verzend.");
                    break;
                case "concept" when vm.HasQuoteSource:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info",
                        "Concept — offerte" + (string.IsNullOrWhiteSpace(vm.QuoteSupplierReference) ? "" : " " + vm.QuoteSupplierReference) + " ingelezen",
                        "Controleer de ingelezen gegevens bovenaan, pas regels en facturatieplan aan en verzend. " + signWord);
                    break;
                case "concept":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", "Concept — zonder offerte",
                        "Vul per regel de prijs van de leverancier in; commissie en prijs klant rekenen mee. Een offerte kan je later nog koppelen in Bron.");
                    break;
                case "ingetrokken":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("warning", "Ingetrokken",
                        "De ondertekening werd ingetrokken; de links van de klant werken niet meer. Pas aan en verzend opnieuw." + replaced);
                    break;
                case "verlopen":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("warning", "Niet tijdig ondertekend",
                        "De termijn om te tekenen is verstreken. Controleer de wijzigingsopdracht en verzend opnieuw." + replaced);
                    break;
                case "verzonden":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", "Verzonden naar de klant",
                        (vm.DateSendToClient.HasValue ? $"Verzonden op {vm.DateSendToClient:dd/MM/yyyy}. " : "")
                        + (showCase ? $"{JoinNames(vm.Signers.Select(s => s.Name))} {(vm.Signers.Count == 1 ? "kreeg" : "kregen")} een link om te tekenen. " : "")
                        + "Regels en facturatieplan liggen vast tot de klant tekent.");
                    break;
                case "wacht":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("warning", "Wacht op " + JoinNames(waiting),
                        $"{signedCount} van {parties.Count} getekend. Zolang niet iedereen tekende kan er niets gefactureerd worden.");
                    break;
                case "ondertekend":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("success", "Volledig ondertekend",
                        (vm.DateAgreement.HasValue ? $"Ondertekend op {vm.DateAgreement:dd/MM/yyyy}. " : "")
                        + (vm.InvoiceableByBouwheer ? "Factureren volgt het facturatieplan: er staat nu nog geen termijn klaar." : "De onderaannemer factureert dit werk zelf aan de klant."));
                    break;
                case "factureerbaar":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("success", "Volledig ondertekend",
                        (vm.DateAgreement.HasValue ? $"Ondertekend op {vm.DateAgreement:dd/MM/yyyy}. " : "")
                        + (nextTerm != null ? $"{nextTerm.KindLabel} ({Pct(nextTerm.Percentage ?? 0)} %) staat klaar om te factureren." : "Het bedrag staat klaar om te factureren."));
                    break;
                case "gefactureerd":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", "Gefactureerd",
                        vm.OpenInvoice != null ? $"Alles is gefactureerd. Wacht op betaling van {vm.OpenInvoice.Number}." : "Alles is gefactureerd. Wacht op betaling.");
                    break;
                case "betaald":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("success", "Betaald en afgerond", "Alle facturen van deze wijzigingsopdracht zijn betaald.");
                    break;
                case "geweigerd":
                    var decliner = parties.FirstOrDefault(p => p.Status == (int)SigningPartyStatus.Declined);
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("danger", "Geweigerd" + (decliner != null ? " door " + decliner.DisplayName : ""),
                        (string.IsNullOrWhiteSpace(decliner?.DeclineReason ?? latestCase?.CloseReason) ? "De klant gaf geen reden op." : "Reden: " + (decliner?.DeclineReason ?? latestCase?.CloseReason))
                        + (replaced.Length > 0 ? replaced : " Maak een versie 2 om een aangepast voorstel te sturen."));
                    break;
                case "telaat":
                    var late = vm.Invoices.First(i => i.IsOverdue);
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("danger", $"{late.Number} niet betaald",
                        (late.DueDate.HasValue ? $"De factuur verviel op {late.DueDate:dd/MM/yyyy}. " : "") + "Herinnering en aanmaning lopen via Facturatie.");
                    break;
            }

            vm.RegelsSub = vm.IsEditable
                ? (vm.HasQuoteSource ? "Prijs leverancier uit de offerte · commissie en prijs klant zijn intern bij te sturen." : "Prijs leverancier zelf in te vullen · commissie en prijs klant zijn intern.")
                : "Kostprijs en commissie zijn intern — de klant ziet enkel de prijs klant.";
            vm.PlanSub = vm.Phase switch
            {
                "concept" or "ingetrokken" or "verlopen" => "Hoe het bedrag gefactureerd wordt — het saldo ten laatste bij de laatste schijf.",
                "verzonden" or "wacht" => "Ligt vast sinds verzending.",
                "geweigerd" => "Vervallen — niet ondertekend.",
                _ => "Ligt vast sinds ondertekening.",
            };
            vm.FooterText = vm.Phase switch
            {
                "concept" or "ingetrokken" or "verlopen" => vm.IsEditable ? "Na verzenden liggen regels en facturatieplan vast." : "",
                "verzonden" => "Regels aanpassen maakt een nieuwe versie en vraagt opnieuw een handtekening.",
                "wacht" => "Aanpassen maakt een nieuwe versie en wist de handtekening die er al staat.",
                "ondertekend" or "factureerbaar" => "Een wijziging wordt nu een nieuwe wijzigingsopdracht.",
                "gefactureerd" => vm.OpenInvoice != null ? $"Wacht op betaling van {vm.OpenInvoice.Number}." : "Wacht op betaling.",
                "betaald" => "Niets meer aan te passen.",
                "geweigerd" => "Versie 2 neemt alles over als nieuw concept.",
                "telaat" => "Herinnering en aanmaning lopen via Facturatie.",
                _ => "",
            };
        }

        private static List<ChangeOrderHistoryItemV2> BuildHistory(ChangeOrderDetailV2Vm vm, ChangeOrder co, CaseStatusView latestCase)
        {
            var items = new List<ChangeOrderHistoryItemV2>();
            if (co is null) return items;

            var created = co.Date.ToDateTime(TimeOnly.MinValue);
            if (vm.SourceKind == 2) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = $"Nieuwe versie van WO-{co.SourceChangeOrderId:000}" });
            else if (vm.SourceKind == 1) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = $"Kopie van WO-{co.SourceChangeOrderId:000}" });
            else if (vm.QuoteConvertedAt.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = "Offerte ingelezen" });
            else items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = "Wijzigingsopdracht aangemaakt" });

            if (vm.QuoteConvertedAt.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = vm.QuoteConvertedAt, Label = "Omgezet naar wijzigingsopdracht" });
            if (co.DateSendToClient.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = co.DateSendToClient.Value.ToDateTime(TimeOnly.MinValue), Label = "Verzonden naar de klant" });
            if (latestCase != null)
            {
                foreach (var p in latestCase.Parties)
                {
                    if (p.SignedAt.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = p.SignedAt.Value.ToLocalTime(), Label = $"Getekend door {p.DisplayName}" });
                    if (p.DeclinedAt.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = p.DeclinedAt.Value.ToLocalTime(), Label = $"Geweigerd door {p.DisplayName}" });
                }
                if (latestCase.Status == (int)SigningCaseStatus.Cancelled && latestCase.ClosedAt.HasValue)
                    items.Add(new ChangeOrderHistoryItemV2 { When = latestCase.ClosedAt.Value.ToLocalTime(), Label = "Ondertekening ingetrokken" });
            }
            if (co.DateAgreement.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = co.DateAgreement.Value.ToDateTime(TimeOnly.MaxValue), Label = "Volledig ondertekend" });
            foreach (var t in vm.Terms.Where(t => t.ReleasedAt.HasValue))
                items.Add(new ChangeOrderHistoryItemV2 { When = t.ReleasedAt, Label = $"{t.KindLabel} vrijgegeven" });
            if (vm.ReplacedByChangeOrderId.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = null, Label = $"Vervangen door WO-{vm.ReplacedByChangeOrderId:000}" });

            return items.OrderByDescending(i => i.When ?? DateTime.MaxValue).ToList();
        }

        [HttpGet]
        public IActionResult ChangeOrderDetailV2AddRow(int index, decimal vatKlant, decimal commission = 0)
        {
            ViewData["Index"] = index;
            ViewData["VatKlant"] = vatKlant;
            ViewData["IsLocked"] = false;
            var row = new ChangeOrderDetailRowV2 { Number = 1, VatPercentage = vatKlant, Commission = commission, MeasurementType = (int)MeasurementType.Forfait, MeasurementUnit = (int)MeasurementUnit.stuk };
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

        /// <summary>Opslaan vanuit het opmaakscherm (28a/28b) én vanuit 20c (ReturnTo = "quote"). Vanuit
        /// 20c kan dezelfde POST ook meteen omzetten (21c, ConvertAfterSave) en/of doorsturen naar
        /// verzenden (AfterSave = "send").</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2Save(ChangeOrderDetailV2SaveModel model)
        {
            var fromIntake = model.ReturnTo == "quote";
            IActionResult BackToOrigin() => fromIntake
                ? RedirectToAction(nameof(QuoteIntakeV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId })
                : RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId });

            var activeSigning = await ActiveSigningCaseAsync(model.ChangeOrderId);
            if (activeSigning is not null)
            {
                AddMessage("error", SigningLockedMessage, "Niet bewaard");
                return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId });
            }

            // Bij omzetten vanuit 20c kiest de 21c-modal de klant; anders komt hij uit het formulier zelf.
            var clientAccountId = model.ConvertAfterSave && model.ConvertClientAccountId > 0 ? model.ConvertClientAccountId : model.ClientAccountId;
            if (clientAccountId <= 0 || model.ContractActivityId <= 0)
            {
                AddMessage("error", "Kies een klant/eenheid en een leverancier·contract.", "Kon niet opslaan");
                // Terug naar het scherm van herkomst — bij een nog niet opgeslagen offerte uit 20c zou een
                // omleiding naar een leeg opmaakscherm de net ingelezen regels kwijtspelen.
                return BackToOrigin();
            }

            ChangeOrder co;
            if (model.ChangeOrderId > 0)
            {
                co = await _db.ChangeOrder
                    .Include(c => c.ChangeOrderDetail)
                    .Include(c => c.ChangeOrderPaymentTerm)
                    .FirstOrDefaultAsync(c => c.Id == model.ChangeOrderId);
                if (co is null) return NotFound();
                if (co.DateAgreement.HasValue)
                {
                    AddMessage("error", "Deze wijzigingsopdracht is ondertekend en ligt vast. Een wijziging wordt een nieuwe wijzigingsopdracht.", "Niet bewaard");
                    return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = co.Id });
                }
            }
            else
            {
                co = new ChangeOrder
                {
                    ClientAccountId = clientAccountId,
                    // Vanuit 20c start een nieuwe rij als offerte; "Leeg beginnen" (29b) is meteen een WO.
                    IsQuote = fromIntake,
                    Invoiceable = true,
                    Description = "",
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                };
                _db.ChangeOrder.Add(co);
            }

            // 20c op een bestaande WO ("offerte koppelen of inlezen" vanuit Bron): enkel de offerte-kant
            // (leverancier, referentie, regels, bronbestand) komt van dat scherm — omschrijving,
            // facturatieplan, commissie en btw van de WO blijven wat ze waren.
            var intakeOnExistingOrder = fromIntake && !co.IsQuote;

            co.ContractActivityId = model.ContractActivityId;
            co.QuoteSupplierReference = string.IsNullOrWhiteSpace(model.QuoteSupplierReference) ? null : model.QuoteSupplierReference.Trim();
            co.QuoteVatPercentage = model.QuoteVatPercentage;
            if (!string.IsNullOrWhiteSpace(model.QuoteSourcePath))
            {
                co.QuoteSourcePath = model.QuoteSourcePath.Trim();
                co.QuoteSourceFileName = string.IsNullOrWhiteSpace(model.QuoteSourceFileName) ? null : model.QuoteSourceFileName.Trim();
            }

            if (!intakeOnExistingOrder)
            {
                var description = (model.Description ?? "").Trim();
                co.Description = description.Length > 250 ? description[..250] : description;
                if (!fromIntake)
                {
                    co.Invoiceable = model.InvoiceableByBouwheer;
                    co.ChangeOrderConditions = model.ConditionsText;
                }
            }

            decimal intakeCommission = 0m, intakeVat = 21m;
            if (intakeOnExistingOrder)
            {
                intakeCommission = co.ChangeOrderDetail.Select(d => d.Commission).FirstOrDefault();
                intakeVat = (await ResolveVatForClientAsync(model.ProjectId, co.ClientAccountId)).Percentage;
            }
            SyncRows(co, model.Rows ?? new List<ChangeOrderDetailRowV2>(), intakeOnExistingOrder, intakeCommission, intakeVat);
            if (!fromIntake) SyncTerms(co, model.Terms ?? new List<ChangeOrderTermV2>());

            var converted = false;
            if (model.ConvertAfterSave && co.IsQuote)
            {
                await ApplyConversionAsync(co, model.ProjectId, model.ConvertClientAccountId, model.ConvertCommission,
                    model.ConvertPlan, model.ConvertDescription, model.ConvertConditions);
                converted = true;
            }

            await _db.SaveChangesAsync();

            if (co.IsQuote)
            {
                // "Offerte zonder omzetten" (29a): enkel bewaren — later omzetten vanuit de lijst.
                AddMessage("success", "De offerte is bewaard. Omzetten naar een wijzigingsopdracht kan later vanuit de lijst.", "Offerte bewaard");
                return RedirectToAction(nameof(ChangeOrdersV2), new { projectid = model.ProjectId });
            }

            return await AfterSaveRedirectAsync(co, model.ProjectId, model.AfterSave,
                converted ? "Omgezet naar wijzigingsopdracht. Controleer regels en facturatieplan en verzend naar de klant." : "Wijzigingsopdracht opgeslagen.",
                converted ? "Omgezet" : "Opgeslagen");
        }

        /// <param name="preserveInternal">20c op een bestaande WO: de intake-rijen kennen geen commissie/btw
        /// — bestaande regels houden hun waarden, nieuwe krijgen de meegegeven standaard.</param>
        private void SyncRows(ChangeOrder co, List<ChangeOrderDetailRowV2> posted, bool preserveInternal = false, decimal defaultCommission = 0m, decimal defaultVat = 21m)
        {
            var postedIds = posted.Where(r => r.Id > 0).Select(r => r.Id).ToHashSet();
            foreach (var stale in co.ChangeOrderDetail.Where(d => d.Id > 0 && !postedIds.Contains(d.Id)).ToList())
                _db.ChangeOrderDetail.Remove(stale);

            // De volgorde van het formulier ís de volgorde (sleepgreep, 28a) — ook het document volgt ze.
            var sortOrder = 0;
            foreach (var r in posted)
            {
                if (string.IsNullOrWhiteSpace(r.Description)) continue;
                var entity = r.Id > 0 ? co.ChangeOrderDetail.FirstOrDefault(d => d.Id == r.Id) : null;
                var isNewRow = entity is null;
                if (entity is null)
                {
                    entity = new ChangeOrderDetail();
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
                if (!preserveInternal)
                {
                    entity.Commission = r.Commission;
                    entity.VatPercentage = r.VatPercentage;
                }
                else if (isNewRow)
                {
                    entity.Commission = defaultCommission;
                    entity.VatPercentage = defaultVat;
                }
                entity.NeedsReview = r.NeedsReview;
                entity.SourceImagePath = string.IsNullOrWhiteSpace(r.SourceImagePath) ? null : r.SourceImagePath;
                entity.SortOrder = sortOrder++;
            }
        }

        private void SyncTerms(ChangeOrder co, List<ChangeOrderTermV2> posted)
        {
            var postedIds = posted.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();
            foreach (var stale in co.ChangeOrderPaymentTerm.Where(t => t.Id > 0 && !postedIds.Contains(t.Id)).ToList())
                _db.ChangeOrderPaymentTerm.Remove(stale);

            var sortOrder = 0;
            foreach (var t in posted)
            {
                var entity = t.Id > 0 ? co.ChangeOrderPaymentTerm.FirstOrDefault(x => x.Id == t.Id) : null;
                if (entity is null)
                {
                    entity = new ChangeOrderPaymentTerm { CreatedAt = DateTime.Now };
                    co.ChangeOrderPaymentTerm.Add(entity);
                }
                entity.Kind = t.Kind;
                entity.Percentage = t.Percentage;
                entity.FixedAmount = t.FixedAmount;
                if (t.Kind == 3)
                {
                    // Saldo = altijd "bij de laatste schijf", waar die ook ligt (geen vaste StageId —
                    // dat zou uit sync raken als de groep later een schijf bijkrijgt): TriggerType=2
                    // zonder StageId = de structureel laatste schijf van de betalingsgroep, zie
                    // LoadChangeOrderFlowFactsAsync.
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
