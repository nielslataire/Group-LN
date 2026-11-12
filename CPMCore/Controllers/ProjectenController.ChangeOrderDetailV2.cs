using BOCore;
using CPMCore.Configuration;
using CPMCore.Documents;
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
    /// facturatieplan, ondertekening, facturen en knoppen. Sinds 2026-10-02 is hetzelfde scherm ook de
    /// offerte aan de klant (stap 1, ChangeOrder.IsQuote): opmaken, per mail verzenden, omzetten naar een
    /// nieuwe wijzigingsopdracht (BuildQuoteScreenState). Eigen bestand naast de legacy
    /// AddChangeOrder/EditChangeOrder-acties, die ongewijzigd blijven. Leest/schrijft rechtstreeks via
    /// _db (zelfde stijl als PaymentStagesV2.cs/InvoicingV2.cs) i.p.v. via ChangeOrderBO/
    /// ChangeOrderTranslator — de offerte-/facturatieplan-velden zitten niet in die oudere BO-laag en dit
    /// voorkomt een split-brain tussen twee opslagpaden voor dezelfde rij. Status en fase worden altijd
    /// afgeleid (ProjectenController.ChangeOrderFlowV2.cs), nooit opgeslagen.</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2(int projectid, int? clientid, int coid = 0, bool quote = false, bool send = false, bool convert = false)
        {
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var canWrite = _ps.HasWrite(PermissionCodes.ProjectsChangeOrders);

            var projectResponse = _projectService.GetProjectByID(projectid);
            if (!projectResponse.Success || projectResponse.Value is null) return NotFound();
            var project = projectResponse.Value;

            // Fouten van een geweigerde opslag (ChangeOrderDetailV2Save redirect) komen hier terug in ModelState,
            // zodat _ErrorSummaryV2 ze toont zoals bij elk ander gl-v2-formulier.
            if (TempData["GlV2FormErrors"] is string formErrors)
                foreach (var line in formErrors.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Split('\t', 2);
                    ModelState.AddModelError(parts.Length == 2 ? parts[0] : "CustomError", parts.Length == 2 ? parts[1] : parts[0]);
                }

            ChangeOrder co = null;
            if (coid > 0)
            {
                co = await _db.ChangeOrder.AsNoTracking()
                    .Include(c => c.ChangeOrderDetail)
                    .Include(c => c.ChangeOrderPaymentTerm)
                    .Include(c => c.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Company)
                    .Include(c => c.ContractActivity).ThenInclude(a => a.Activity)
                    .Include(c => c.Groups).ThenInclude(g => g.SupplierCompany)
                    .Include(c => c.Groups).ThenInclude(g => g.ContractActivity).ThenInclude(a => a.Activity)
                    .Include(c => c.Groups).ThenInclude(g => g.ContractActivity).ThenInclude(a => a.Contract).ThenInclude(k => k.Company)
                    .FirstOrDefaultAsync(c => c.Id == coid);
                if (co is null) return NotFound();
            }

            // Zelfde scherm voor de offerte aan de klant (stap 1, IsQuote) en voor de wijzigingsopdracht
            // (stap 2–7). Een nieuwe rij is een offerte als ze zo gestart werd ("+ Nieuw · Offerte opmaken").
            var isQuote = co?.IsQuote ?? quote;

            var vm = new ChangeOrderDetailV2Vm
            {
                ProjectId = projectid,
                ProjectName = project.Name,
                CanWrite = canWrite,
                CanDelete = co is not null && await CanDeleteChangeOrderAsync(co.IsQuote),
                IsNew = co is null,
                ChangeOrderId = co?.Id ?? 0,
                Number = co is null ? "" : CoNo(co),
                SavedAtText = co?.SavedAt?.ToString("dd/MM/yyyy HH:mm"),
                IsQuote = isQuote,
            };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid = projectid } };
            var listNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("ChangeOrdersV2", "Projecten", "Offertes & wijzigingen") { Parent = projectDetail, RouteValues = new { projectid = projectid } };
            ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(nameof(ChangeOrderDetailV2), "Projecten", vm.IsNew ? (isQuote ? "Nieuwe offerte" : "Nieuwe wijzigingsopdracht") : vm.Number) { Parent = listNode, RouteValues = new { projectid, coid } };

            vm.ClientAccountId = co?.ClientAccountId ?? clientid ?? 0;
            vm.Subject = co?.Subject;
            vm.Description = IsIntakePlaceholder(co?.Description) ? "" : co?.Description ?? "";
            vm.InvoiceableByBouwheer = co?.Invoiceable ?? true;
            vm.ContractActivityId = co?.ContractActivityId ?? 0;
            vm.QuoteSupplierReference = co?.QuoteSupplierReference;
            vm.QuoteVatPercentage = co?.QuoteVatPercentage;
            vm.QuoteDate = co?.Date ?? DateOnly.FromDateTime(DateTime.Today);
            vm.ExpirationDate = co?.ExpirationDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30));
            vm.QuoteConvertedAt = co?.QuoteConvertedAt;
            vm.ConditionsText = co?.ChangeOrderConditions ?? "";
            // Offerte: standaardvoorwaarden uit de vervaldatum zolang er niets eigens staat (nieuw of leeg).
            if (isQuote && ChangeOrderStandardTexts.IsQuoteStandardOrEmpty(vm.ConditionsText))
                vm.ConditionsText = ChangeOrderStandardTexts.QuoteConditions(co?.ExpirationDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30)));
            vm.DateSendToClient = co?.DateSendToClient;
            vm.DateAgreement = co?.DateAgreement;
            vm.VatKlantPercentage = 21m;

            if (vm.ClientAccountId > 0)
            {
                var clientResponse = _clientService.GetClientAccountById(vm.ClientAccountId);
                vm.ClientName = clientResponse.Success ? clientResponse.Values.FirstOrDefault()?.Name ?? "" : "";
                var vat = await ResolveVatForClientAsync(projectid, vm.ClientAccountId);
                vm.VatKlantPercentage = vat.Percentage;
                vm.VatKlantTypeId = vat.VatTypeId;
                vm.VatKlantLabel = vat.Label;
                vm.UnitName = vat.UnitName;
            }
            if (string.IsNullOrEmpty(vm.VatKlantLabel)) vm.VatKlantLabel = "volgt uit de betalingsgroep van de eenheid";
            vm.VatTypes = await LoadVatTypeOptionsAsync(projectid, vm.VatKlantTypeId);

            vm.ContractActivities = _projectService.GetProjectContractActivitiesForSelect(projectid) is { Success: true } actResp
                ? actResp.Values : new List<IdNameBO>();
            // "Leeg beginnen" (28b): klant · eenheid kies je in het scherm zelf — elke optie draagt de btw
            // van haar betalingsgroep, zodat de regels meteen met de juiste btw rekenen.
            if (vm.ClientAccountId <= 0)
                vm.ClientOptions = await LoadClientOptionsAsync(projectid);

            vm.Stages = await LoadClientStageOptionsAsync(projectid, vm.ClientAccountId);

            // ── Statusfeiten (zelfde bron als de lijst) + het ondertekendossier ────────────────────────
            var facts = await LoadChangeOrderFlowFactsAsync(projectid, co is null ? Array.Empty<ChangeOrder>() : new[] { co });
            vm.SigningEnabled = facts.SigningEnabled;
            vm.CanStartSigning = _ps.HasWrite(PermissionCodes.Signing);
            CaseStatusView latestCase = null;
            if (facts.SigningEnabled && co != null && !isQuote)
            {
                var signing = HttpContext.RequestServices.GetRequiredService<FacadeCore.Signing.ISigningService>();
                vm.ActiveSigningCase = await signing.GetActiveCaseForSourceAsync(Services.Signing.ChangeOrderSigningSource.Key, coid, HttpContext.RequestAborted);
                vm.CompletedSigningCase = await signing.GetCompletedCaseForSourceAsync(Services.Signing.ChangeOrderSigningSource.Key, coid, HttpContext.RequestAborted);
                facts.LatestCase.TryGetValue(co.Id, out latestCase);
                latestCase = vm.ActiveSigningCase ?? vm.CompletedSigningCase ?? latestCase;
                if (latestCase != null) facts.LatestCase[co.Id] = latestCase;
            }

            if (co != null && !isQuote)
                vm.SignedDocId = await _db.ProjectDocs.AsNoTracking()
                    .Where(d => d.ChangeOrderId == co.Id)
                    .OrderByDescending(d => d.Id).Select(d => (int?)d.Id).FirstOrDefaultAsync();

            if (co != null)
            {
                vm.Rows = co.ChangeOrderDetail.OrderBy(d => d.SortOrder ?? int.MaxValue).ThenBy(d => d.Id).Select(d => new ChangeOrderDetailRowV2
                {
                    Id = d.Id,
                    Description = d.Description,
                    MeasurementType = d.MeasurementType ?? (int)MeasurementType.Vermoedelijk,
                    MeasurementUnit = d.MeasurementUnit ?? (int)MeasurementUnit.stuk,
                    // Minwerk (28v): het teken staat in de knop, aantal en prijs tonen we als positief getal. Ook oude regels met een min
                    // (negatief aantal of negatieve prijs) zijn zo een minwerk.
                    IsMinwerk = d.Number * d.Price < 0 || d.DamagePercentage > 0,
                    Number = Math.Abs(d.Number),
                    // Met schadevergoeding (28v) staat in het scherm de WAARDE van het minwerk; opgeslagen is Price het eindbedrag voor de klant.
                    Price = d.DamagePercentage > 0 && d.ValuePrice.HasValue ? Math.Abs(d.ValuePrice.Value) : Math.Abs(d.Price),
                    DamagePercentage = d.DamagePercentage ?? 0m,
                    DamageGoesTo = d.DamageGoesTo ?? (byte)1,
                    Commission = d.Commission,
                    VatPercentage = d.VatPercentage ?? vm.VatKlantPercentage,
                    VatTypeId = d.VatTypeId,
                    NeedsReview = d.NeedsReview,
                    SourceImagePath = d.SourceImagePath,
                    // Uit de offerte overgenomen WO-regel: prijs, commissie en btw liggen vast (migratie 069).
                    PriceLocked = !co.IsQuote && d.SourceDetailId.HasValue,
                }).ToList();
                // Leveranciersgroepen (28t): de regels per groep; Rows blijft de platte lijst in groepsvolgorde.
                vm.Groups = await BuildGroupsAsync(co, vm.Rows, projectid);
                if (vm.Groups.Count > 0) vm.Rows = vm.Groups.SelectMany(g => g.Rows).ToList();

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
                    var source = await _db.ChangeOrder.AsNoTracking().Where(c => c.Id == sourceId).Select(c => new { c.Id, c.Description, c.IsQuote }).FirstOrDefaultAsync();
                    if (source != null)
                    {
                        vm.SourceChangeOrderId = source.Id;
                        vm.SourceChangeOrderLabel = $"{(source.IsQuote ? "OF" : "WO")}-{source.Id:000} · {source.Description}";
                    }
                }
                if (co.IsQuote)
                {
                    vm.ConvertedToChangeOrderId = await _db.ChangeOrder.AsNoTracking()
                        .Where(c => c.SourceChangeOrderId == co.Id && c.SourceKind == 3)
                        .OrderByDescending(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
                }
                vm.ReplacedByChangeOrderId = await _db.ChangeOrder.AsNoTracking()
                    .Where(c => c.SourceChangeOrderId == co.Id && c.SourceKind == 2)
                    .OrderByDescending(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
            }
            else
            {
                vm.Status = ChangeOrderStatus.Opgemaakt;
            }

            if (co is not null && isQuote)
                vm.Attachments = await LoadAttachmentsAsync(co.Id, HttpContext.RequestServices.GetRequiredService<DocStorageService>());

            vm.DefaultCommission = vm.Rows.Count > 0 ? vm.Rows[0].Commission : await DefaultCommissionAsync(projectid);
            vm.DefaultDamagePercentage = await DefaultDamagePercentageAsync();

            if (isQuote)
            {
                var recipients = vm.ClientAccountId > 0 ? await OwnerRecipientsAsync(vm.ClientAccountId) : new();
                BuildQuoteScreenState(vm, co, recipients);
            }
            else
            {
                var suggestedSigners = vm.ClientAccountId > 0
                    ? await SuggestedSignerNamesAsync(vm.ClientAccountId)
                    : new List<string>();
                BuildScreenState(vm, co, latestCase, suggestedSigners);
            }
            vm.OpenSendModal = send && !vm.IsNew && (vm.IsEditable || vm.Phase == "offerte-verzonden");
            vm.OpenConvertModal = convert && isQuote && !vm.IsNew && vm.Phase != "offerte-omgezet";
            vm.History = BuildHistory(vm, co, latestCase);

            ViewData["HelpScreenId"] = "wijzigingen-opmaken";
            return View(vm);
        }

        private static string Pct(decimal v) => v.ToString("0.##");

        private static string TriggerLabel(byte kind, byte triggerType, int? stageId, List<ChangeOrderStageOptionV2> stages) => triggerType switch
        {
            1 => "Na ondertekening",
            2 when kind == 3 || !stageId.HasValue => "Bij de laatste schijf",
            2 => "Bij schijf · " + (stages.FirstOrDefault(s => s.Id == stageId.Value)?.Name ?? "onbekend"),
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

        /// <summary>Het Stappenplan (punt 27, GlV2/_Steps, variant Lijn · Sm, niet klikbaar) van het scherm per
        /// stap. De staten komen expliciet uit BuildScreenState/BuildQuoteScreenState: done · current · todo ·
        /// wacht · warning · error · uit.</summary>
        private static GlV2StepsVm DetailSteps(string id, string[] labels, string[] states, string[] subs)
        {
            static GlV2StepState Map(string s) => s switch
            {
                "done" => GlV2StepState.Done,
                "current" => GlV2StepState.Current,
                "wacht" => GlV2StepState.Wacht,
                "warning" => GlV2StepState.Warning,
                "error" => GlV2StepState.Error,
                "uit" => GlV2StepState.Uit,
                _ => GlV2StepState.Todo,
            };
            var current = Array.FindIndex(states, s => s is "current" or "wacht" or "error" or "warning");
            if (current < 0) current = Math.Max(0, Array.FindLastIndex(states, s => s == "done"));
            return new GlV2StepsVm
            {
                Id = id,
                Variant = GlV2StepsVariant.Lijn,
                Size = GlV2StepsSize.Sm,
                Clickable = false,
                Current = current,
                Steps = labels.Select((label, i) => new GlV2StepItemVm { Label = label, State = Map(states[i]), Sub = subs[i] }).ToList(),
            };
        }

        /// <summary>De offerte aan de klant (stap 1): concept → verzonden per mail → omgezet naar een
        /// wijzigingsopdracht. Zelfde scherm als de WO, zonder facturatieplan en ondertekening; na verzenden
        /// ligt ze vast (terug bewerken kan via "Aanpassen"), na omzetten is ze enkel nog de bron.</summary>
        private void BuildQuoteScreenState(ChangeOrderDetailV2Vm vm, ChangeOrder co, List<Models.Signing.SigningStartPartyVm> recipients)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var converted = vm.ConvertedToChangeOrderId.HasValue || vm.QuoteConvertedAt.HasValue;
            var sent = vm.DateSendToClient.HasValue;
            var expired = co != null && !converted && vm.ExpirationDate < today;

            vm.Status = expired ? ChangeOrderStatus.Verlopen : ChangeOrderStatus.Offerte;
            vm.Phase = converted ? "offerte-omgezet" : sent ? "offerte-verzonden" : "offerte";
            vm.IsEditable = vm.CanWrite && vm.Phase == "offerte";
            vm.IsLocked = !vm.IsEditable;
            vm.LockText = vm.Phase switch
            {
                "offerte-verzonden" => "Verzonden",
                "offerte-omgezet" => "Omgezet",
                _ => vm.IsEditable ? null : "Alleen lezen",
            };
            (vm.StatusPillLabel, vm.StatusPillTone) = vm.Phase switch
            {
                "offerte-omgezet" => ("Offerte · omgezet", "is-positive"),
                _ when expired => ("Offerte · verlopen", "is-blocked"),
                "offerte-verzonden" => ("Offerte · verzonden", "is-attention"),
                _ => ("Offerte · concept", "is-neutral"),
            };

            var labels = new[] { "Offerte", "Opgemaakt", "Verzonden", "Ondertekend", "Factureerbaar", "Gefactureerd", "Betaald" };
            var firstSub = converted ? "omgezet" : sent ? $"verzonden {vm.DateSendToClient:dd/MM}" : "concept";
            vm.Stappenplan = DetailSteps("qo-steps", labels,
                labels.Select((_, i) => i == 0 ? (converted ? "done" : expired ? "warning" : "current") : "todo").ToArray(),
                labels.Select((_, i) => i == 0 ? firstSub : null).ToArray());
            vm.StepLabel = "Stap 1 van 7";

            foreach (var r in recipients)
            {
                var hasEmail = !string.IsNullOrWhiteSpace(r.Email);
                vm.Recipients.Add(new ChangeOrderSignerV2
                {
                    Name = r.DisplayName,
                    Initials = Initials(r.DisplayName),
                    StatusText = hasEmail ? r.Email : "geen e-mailadres",
                    Tone = hasEmail ? "is-grijs" : "is-rood",
                });
            }
            (vm.SigningHeadText, vm.SigningHeadTone) = vm.Phase switch
            {
                "offerte-omgezet" => ($"Omgezet naar {CoNo(vm.ConvertedToChangeOrderId)}", "is-groen"),
                "offerte-verzonden" => ($"Gemaild op {vm.DateSendToClient:dd/MM/yyyy}", "is-goud"),
                _ => ("Nog niet verzonden", "is-grijs"),
            };
            if (co != null) vm.SigningMeta.Add(new("Geldig tot", vm.ExpirationDate.ToString("dd/MM/yyyy")));
            if (sent) vm.SigningMeta.Add(new("Verzonden", vm.DateSendToClient.Value.ToString("dd/MM/yyyy")));

            switch (vm.Phase)
            {
                case "offerte-omgezet":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("success", "Omgezet naar een wijzigingsopdracht",
                        "Deze offerte is de bron van de wijzigingsopdracht en blijft bewaard zoals ze naar de klant ging. Verder werken doe je in de wijzigingsopdracht.");
                    break;
                case "offerte-verzonden" when expired:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("warning", "Offerte verlopen",
                        $"De geldigheid verstreek op {vm.ExpirationDate:dd/MM/yyyy}. Pas de offerte aan en verzend ze opnieuw, of zet ze om als de klant toch akkoord ging.");
                    break;
                case "offerte-verzonden":
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", "Verzonden naar de klant",
                        $"Gemaild op {vm.DateSendToClient:dd/MM/yyyy}; de offerte ligt nu vast. Gaat de klant akkoord, zet ze dan om naar een wijzigingsopdracht — daar kan je nog aantallen aanpassen, regels schrappen en het facturatieplan instellen.");
                    break;
                case "offerte" when vm.HasQuoteSource:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info",
                        "Offerte — leveranciersofferte" + (string.IsNullOrWhiteSpace(vm.QuoteSupplierReference) ? "" : " " + vm.QuoteSupplierReference) + " ingelezen",
                        "Controleer de ingelezen regels, zet de commissie en de omschrijving voor de klant en verzend de offerte per mail.");
                    break;
                default:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", "Offerte aan de klant",
                        "Vul de regels en prijzen in en verzend de offerte per mail. Na akkoord van de klant zet je ze om naar een wijzigingsopdracht ter ondertekening.");
                    break;
            }

            vm.RegelsSub = vm.IsEditable
                ? (vm.HasQuoteSource ? "Prijs leverancier uit de offerte · commissie en prijs klant zijn intern bij te sturen." : "Prijs leverancier zelf in te vullen · commissie is intern, de klant ziet enkel de prijs klant.")
                : "Kostprijs en commissie zijn intern — de klant ziet enkel de prijs klant.";
            vm.FooterText = vm.Phase switch
            {
                "offerte-verzonden" => "Akkoord van de klant? Zet de offerte om naar een wijzigingsopdracht.",
                "offerte-omgezet" => "Niets meer aan te passen — verder werken doe je in de wijzigingsopdracht.",
                _ => vm.IsEditable ? "Na verzenden ligt de offerte vast; omzetten maakt er een wijzigingsopdracht van." : "",
            };
        }

        /// <summary>Leidt uit status + ondertekendossier + facturen af wat het scherm toont (design-handoff
        /// 28a–28i): fase, pillen, stappenplan, melding, vergrendeling, facturatieplan-status per termijn,
        /// de kaart Ondertekening en de voettekst. De view beslist zelf niets.</summary>
        private void BuildScreenState(ChangeOrderDetailV2Vm vm, ChangeOrder co, CaseStatusView latestCase, List<string> suggestedSigners)
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
            var fromQuote = vm.SourceKind == 3; // gemaakt uit een offerte aan de klant
            var first = vm.HasQuoteSource || fromQuote ? "done" : "uit";
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
                fromQuote ? $"{CoNo(vm.SourceChangeOrderId)}"
                    : vm.HasQuoteSource ? (string.IsNullOrWhiteSpace(vm.QuoteSupplierReference) ? "ingelezen" : vm.QuoteSupplierReference) : "geen offerte",
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
            vm.Stappenplan = DetailSteps("co-steps", labels, states, subs);
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
            var replaced = vm.ReplacedByChangeOrderId.HasValue ? $" Vervangen door {CoNo(vm.ReplacedByChangeOrderId)}." : "";
            var signWord = suggestedSigners.Count > 1 ? "Alle eigenaars krijgen dan een link om te tekenen." : "De klant krijgt dan een link om te tekenen.";
            switch (vm.Phase)
            {
                case "concept" when fromQuote:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", $"Concept — uit offerte {CoNo(vm.SourceChangeOrderId)}",
                        "De prijzen uit de offerte liggen vast. Pas aantallen aan, schrap regels die de klant niet wil, voeg zo nodig regels toe en stel het facturatieplan in. " + signWord.Replace("krijgen dan", "krijgen na verzenden").Replace("krijgt dan", "krijgt na verzenden"));
                    break;
                case "concept" when vm.SourceKind == 2:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", $"Concept — nieuwe versie van {CoNo(vm.SourceChangeOrderId)}",
                        "Alles is overgenomen als nieuw concept. Pas regels en facturatieplan aan en verzend opnieuw; de vorige versie blijft bewaard.");
                    break;
                case "concept" when vm.SourceKind == 1:
                    (vm.NoticeType, vm.NoticeTitle, vm.NoticeText) = ("info", $"Concept — kopie van {CoNo(vm.SourceChangeOrderId)}",
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
                ? (fromQuote ? "Prijzen uit de offerte liggen vast · aantal en omschrijving zijn aanpasbaar, nieuwe regels krijgen een vrije prijs."
                    : vm.HasQuoteSource ? "Prijs leverancier uit de offerte · commissie en prijs klant zijn intern bij te sturen."
                    : "Prijs leverancier zelf in te vullen · commissie en prijs klant zijn intern.")
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

        private List<ChangeOrderHistoryItemV2> BuildHistory(ChangeOrderDetailV2Vm vm, ChangeOrder co, CaseStatusView latestCase)
        {
            var items = new List<ChangeOrderHistoryItemV2>();
            if (co is null) return items;

            var created = co.Date.ToDateTime(TimeOnly.MinValue);
            if (co.IsQuote) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = vm.HasQuoteSource ? "Offerte opgemaakt uit een leveranciersofferte" : "Offerte opgemaakt" });
            else if (vm.SourceKind == 3) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = $"Omgezet uit offerte {CoNo(co.SourceChangeOrderId)}" });
            else if (vm.SourceKind == 2) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = $"Nieuwe versie van {CoNo(co.SourceChangeOrderId)}" });
            else if (vm.SourceKind == 1) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = $"Kopie van {CoNo(co.SourceChangeOrderId)}" });
            else if (vm.QuoteConvertedAt.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = "Offerte ingelezen" });
            else items.Add(new ChangeOrderHistoryItemV2 { When = created, Label = "Wijzigingsopdracht aangemaakt" });

            if (vm.QuoteConvertedAt.HasValue)
                items.Add(new ChangeOrderHistoryItemV2 { When = vm.QuoteConvertedAt, Label = vm.ConvertedToChangeOrderId.HasValue ? $"Omgezet naar {CoNo(vm.ConvertedToChangeOrderId)}" : "Omgezet naar wijzigingsopdracht" });
            if (co.DateSendToClient.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = co.DateSendToClient.Value.ToDateTime(TimeOnly.MinValue), Label = co.IsQuote ? "Offerte gemaild naar de klant" : "Verzonden naar de klant" });
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
            if (vm.ReplacedByChangeOrderId.HasValue) items.Add(new ChangeOrderHistoryItemV2 { When = null, Label = $"Vervangen door {CoNo(vm.ReplacedByChangeOrderId)}" });

            return items.OrderByDescending(i => i.When ?? DateTime.MaxValue).ToList();
        }

        /// <summary>Btw-codes (Vattype) van het facturatiebedrijf dat de offerte/wijzigingsopdracht uitgeeft
        /// (<see cref="Services.Signing.ChangeOrderIssuerResolver"/>); de standaardcode van de klant (uit zijn
        /// betalingsgroep) staat er altijd bij, ook als die van een ander bedrijf zou zijn.</summary>
        private async Task<List<VatTypeOptionV2>> LoadVatTypeOptionsAsync(int projectId, int? klantTypeId)
        {
            var ct = HttpContext.RequestAborted;
            var project = await _db.Project.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);
            var issuerId = await Services.Signing.ChangeOrderIssuerResolver.ResolveIssuerCompanyIdAsync(_db, project, ct);
            var types = await _db.Vattype.AsNoTracking()
                .Where(v => (issuerId.HasValue && v.IssuerCompanyId == issuerId.Value) || (klantTypeId.HasValue && v.Id == klantTypeId.Value))
                .OrderBy(v => v.BasePercentage).ThenBy(v => v.Code)
                .ToListAsync(ct);
            return types.Select(v => new VatTypeOptionV2 { Id = v.Id, Code = v.Code, Description = v.Description, Percentage = v.BasePercentage }).ToList();
        }

        /// <summary>Schijven waarop een termijn kan wachten: enkel die van de betalingsgroep(en) van de eenheden van de klantenaccount, en enkel
        /// wat voor die eenheden nog niet gefactureerd is (zelfde "reeds gefactureerd"-regel als Facturatie: een schijfregel op een factuur van de eenheid).</summary>
        private async Task<List<ChangeOrderStageOptionV2>> LoadClientStageOptionsAsync(int projectId, int clientAccountId)
        {
            if (clientAccountId <= 0) return new();
            var unitIds = await _db.Units.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.ClientAccountId == clientAccountId).Select(u => u.Id).ToListAsync();
            if (unitIds.Count == 0) return new();
            var groupIds = await _db.UnitConstructionValue.AsNoTracking()
                .Where(v => unitIds.Contains(v.UnitId) && v.PaymentGroupId.HasValue).Select(v => v.PaymentGroupId.Value).Distinct().ToListAsync();
            var stages = await _db.InvoicingPaymentStages.AsNoTracking()
                .Where(s => groupIds.Contains(s.GroupId))
                .OrderBy(s => s.GroupId).ThenBy(s => s.Id)
                .Select(s => new { s.Id, s.Name, Group = s.Group.Name })
                .ToListAsync();
            var invoiced = (await _db.InvoicesDetails.AsNoTracking()
                .Where(d => d.LineType == "Stages" && d.PaymentStageId.HasValue && d.UnitId.HasValue && unitIds.Contains(d.UnitId.Value))
                .Select(d => d.PaymentStageId.Value).Distinct().ToListAsync()).ToHashSet();
            return stages.Where(s => !invoiced.Contains(s.Id))
                .Select(s => new ChangeOrderStageOptionV2 { Id = s.Id, Name = s.Name, Group = s.Group ?? "" }).ToList();
        }

        /// <summary>JSON voor de schijven-keuzelijst van de termijnen na het kiezen van een andere klant · eenheid (Leeg beginnen).</summary>
        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2Stages(int projectId, int clientAccountId)
        {
            var stages = await LoadClientStageOptionsAsync(projectId, clientAccountId);
            var multi = stages.Select(s => s.Group).Distinct().Count() > 1;
            return Json(stages.Select(s => new { value = s.Id.ToString(), text = s.Name, group = multi ? s.Group : null }));
        }

        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2AddRow(int index, decimal vatKlant, int projectId, int? vatKlantTypeId = null, decimal commission = 0)
        {
            ViewData["Index"] = index;
            ViewData["VatKlant"] = vatKlant;
            ViewData["VatTypes"] = await LoadVatTypeOptionsAsync(projectId, vatKlantTypeId);
            ViewData["IsLocked"] = false;
            var row = new ChangeOrderDetailRowV2 { Number = 1, VatPercentage = vatKlant, VatTypeId = vatKlantTypeId, Commission = commission, MeasurementType = (int)MeasurementType.Vermoedelijk, MeasurementUnit = (int)MeasurementUnit.stuk };
            return PartialView("Partials/_ChangeOrderDetailRowV2", row);
        }

        [HttpGet]
        public async Task<IActionResult> ChangeOrderDetailV2AddTerm(int index, int projectId, byte kind = 2, decimal? percentage = null, int clientAccountId = 0)
        {
            var stages = await LoadClientStageOptionsAsync(projectId, clientAccountId);
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

        /// <summary>Opslaan vanuit het scherm (offerte of wijzigingsopdracht in concept) én vanuit 20c
        /// (ReturnTo = "quote": de ingelezen regels van de leverancier). AfterSave = "send"/"convert" opent
        /// daarna meteen de verzend- of omzetmodal op de bewaarde toestand.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderDetailV2Save(ChangeOrderDetailV2SaveModel model)
        {
            var fromIntake = model.ReturnTo == "quote";
            IActionResult BackToOrigin() => fromIntake
                ? RedirectToAction(nameof(QuoteIntakeV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId })
                : RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId, quote = model.IsQuote });
            IActionResult Refuse(string message)
            {
                // Foutoverzicht bovenaan het scherm (DESIGN.md punt 24); de GET-actie zet dit terug in ModelState.
                AddFormError(message);
                return RedirectToAction(nameof(ChangeOrderDetailV2), new { projectid = model.ProjectId, coid = model.ChangeOrderId });
            }

            var activeSigning = await ActiveSigningCaseAsync(model.ChangeOrderId);
            if (activeSigning is not null) return Refuse(SigningLockedMessage);

            // Het contract is optioneel (offerte én wijzigingsopdracht): het project toont een waarschuwing zolang het ontbreekt.
            if (model.ClientAccountId <= 0)
            {
                if (fromIntake) AddMessage("error", "Kies een klant/eenheid.", "Kon niet opslaan"); // 20c heeft geen foutoverzicht
                else AddFormError("Kies een klant · eenheid.", "clientAccountId");
                // Terug naar het scherm van herkomst — bij een nog niet opgeslagen offerte uit 20c zou een
                // omleiding naar een leeg opmaakscherm de net ingelezen regels kwijtspelen.
                return BackToOrigin();
            }

            // Verplicht (foutoverzicht, DESIGN.md punt 24): een onderwerp en minstens één regel met een bedrag. 20c levert enkel regels en is uitgezonderd.
            if (!fromIntake)
            {
                var missing = false;
                if (string.IsNullOrWhiteSpace(model.Subject)) { AddFormError("Vul een onderwerp in.", "subject"); missing = true; }
                if (!(model.Rows ?? new List<ChangeOrderDetailRowV2>()).Any(r => !string.IsNullOrWhiteSpace(r.Description) && r.Price > 0))
                { AddFormError("Voeg minstens één regel met een bedrag toe.", "rows"); missing = true; }
                if (missing) return BackToOrigin();
            }

            ChangeOrder co;
            var isExisting = model.ChangeOrderId > 0;
            if (isExisting)
            {
                co = await _db.ChangeOrder
                    .Include(c => c.ChangeOrderDetail)
                    .Include(c => c.Groups)
                    .Include(c => c.ChangeOrderPaymentTerm)
                    .FirstOrDefaultAsync(c => c.Id == model.ChangeOrderId);
                if (co is null) return NotFound();
                if (co.DateAgreement.HasValue)
                    return Refuse("Deze wijzigingsopdracht is ondertekend en ligt vast. Een wijziging wordt een nieuwe wijzigingsopdracht.");
                if (co.IsQuote && co.QuoteConvertedAt.HasValue)
                    return Refuse("Deze offerte is omgezet naar een wijzigingsopdracht en ligt vast.");
                if (co.IsQuote && co.DateSendToClient.HasValue)
                    return Refuse("Deze offerte is verzonden en ligt vast. Kies \"Aanpassen\" om ze opnieuw te bewerken.");
            }
            else
            {
                co = new ChangeOrder
                {
                    ProjectId = model.ProjectId,
                    ClientAccountId = model.ClientAccountId,
                    // Een nieuwe rij is een offerte aan de klant (vanuit 20c altijd, anders zoals gestart via
                    // "+ Nieuw") of rechtstreeks een wijzigingsopdracht. Daarna verandert de soort nooit meer.
                    IsQuote = fromIntake || model.IsQuote,
                    Invoiceable = true,
                    Description = "",
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                };
                _db.ChangeOrder.Add(co);
            }

            // 20c levert enkel de leverancierskant (leverancier, referentie, regels, bronbestand). Op een
            // bestaande rij blijft de rest (omschrijving, voorwaarden, facturatieplan, commissie, btw) wat
            // ze was; nieuwe regels krijgen de commissie van de rij en de btw van de eenheid.
            var intakeOnExisting = fromIntake && isExisting;

            // Leveranciers (28t): per GROEP regels. De oude kolommen op het document spiegelen de eerste groep (MirrorFirstGroup).
            // Een formulier zonder groepen (20c, oude vorm) werkt op één groep: de eerste, of die waarvoor 20c inlas.
            var legacyGroups = model.Groups == null || model.Groups.Count == 0;
            Dictionary<string, ChangeOrderGroup> groupByKey = null;
            ChangeOrderGroup scopeGroup = null;
            if (legacyGroups) scopeGroup = await ApplyLegacyGroupAsync(co, model, fromIntake);
            else groupByKey = await SyncGroupsAsync(co, model.Groups);
            co.QuoteVatPercentage = model.QuoteVatPercentage;

            if (!intakeOnExisting)
            {
                var description = (model.Description ?? "").Trim();
                co.Description = description.Length > 250 ? description[..250] : description;
            }
            if (!fromIntake)
            {
                var subject = (model.Subject ?? "").Trim();
                co.Subject = subject.Length == 0 ? null : (subject.Length > 150 ? subject[..150] : subject);
                co.ChangeOrderConditions = model.ConditionsText;
                if (co.IsQuote && ChangeOrderStandardTexts.IsQuoteStandardOrEmpty(model.ConditionsText))
                    co.ChangeOrderConditions = ChangeOrderStandardTexts.QuoteConditions(model.ExpirationDate ?? co.ExpirationDate);
                if (model.ExpirationDate.HasValue) co.ExpirationDate = model.ExpirationDate.Value;
            }

            decimal intakeCommission = 0m, intakeVat = 21m;
            int? intakeVatTypeId = null;
            if (fromIntake)
            {
                intakeCommission = co.ChangeOrderDetail.Count > 0
                    ? co.ChangeOrderDetail.Select(d => d.Commission).FirstOrDefault()
                    : await DefaultCommissionAsync(model.ProjectId);
                var intakeResolved = await ResolveVatForClientAsync(model.ProjectId, co.ClientAccountId);
                intakeVat = intakeResolved.Percentage;
                intakeVatTypeId = intakeResolved.VatTypeId;
            }
            var postedVatTypeIds = (model.Rows ?? new List<ChangeOrderDetailRowV2>()).Where(r => r.VatTypeId.HasValue).Select(r => r.VatTypeId.Value).Distinct().ToList();
            var vatTypePercentages = postedVatTypeIds.Count == 0 ? new Dictionary<int, decimal>()
                : await _db.Vattype.AsNoTracking().Where(v => postedVatTypeIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, v => v.BasePercentage);
            SyncRows(co, model.Rows ?? new List<ChangeOrderDetailRowV2>(), fromIntake, intakeCommission, intakeVat, vatTypePercentages, intakeVatTypeId,
                groupByKey, scopeGroup, scopeToGroup: legacyGroups && fromIntake && co.Groups.Count > 1);
            // Factureren alle leveranciers zelf (geen enkele groep via ons), dan is er niets door ons te factureren: geen facturatieplan.
            if (!fromIntake && !co.IsQuote)
                SyncTerms(co, co.Groups.Count > 0 && co.Groups.All(g => !g.InvoicedByUs) ? new List<ChangeOrderTermV2>() : model.Terms ?? new List<ChangeOrderTermV2>());

            co.SavedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            var message = !co.IsQuote ? "Wijzigingsopdracht opgeslagen."
                : fromIntake ? "De regels zijn ingelezen. Zet de commissie en de omschrijving voor de klant en verzend de offerte."
                : "Offerte opgeslagen.";
            return await AfterSaveRedirectAsync(co, model.ProjectId, model.AfterSave, message, "Opgeslagen");
        }

        /// <summary>Server-vangnet voor een geweigerde opslag die naar het scherm terug redirect: ModelState overleeft een redirect niet,
        /// dus bewaren we de fout in TempData en zet de GET-actie ze terug (sleutel + tab, één fout per regel).</summary>
        private void AddFormError(string message, string key = "CustomError")
        {
            var existing = TempData["GlV2FormErrors"] as string;
            TempData["GlV2FormErrors"] = (string.IsNullOrEmpty(existing) ? "" : existing + "\n") + key + "\t" + message.Replace("\n", " ").Replace("\t", " ");
        }

        /// <param name="preserveInternal">Vanuit 20c: de intake-rijen kennen geen commissie/btw — bestaande
        /// regels houden hun waarden, nieuwe krijgen de meegegeven standaard.</param>
        private void SyncRows(ChangeOrder co, List<ChangeOrderDetailRowV2> posted, bool preserveInternal = false, decimal defaultCommission = 0m, decimal defaultVat = 21m,
            IReadOnlyDictionary<int, decimal> vatTypePercentages = null, int? defaultVatTypeId = null,
            IReadOnlyDictionary<string, ChangeOrderGroup> groupByKey = null, ChangeOrderGroup defaultGroup = null, bool scopeToGroup = false)
        {
            // Opgelet bij 20c op een document met meerdere groepen (scopeToGroup): enkel de regels van die ene groep worden vervangen.
            bool InScope(ChangeOrderDetail d) => !scopeToGroup || defaultGroup is null || d.Group == defaultGroup || (defaultGroup.Id > 0 && d.GroupId == defaultGroup.Id);
            var postedIds = posted.Where(r => r.Id > 0).Select(r => r.Id).ToHashSet();
            foreach (var stale in co.ChangeOrderDetail.Where(d => d.Id > 0 && !postedIds.Contains(d.Id) && InScope(d)).ToList())
                _db.ChangeOrderDetail.Remove(stale);

            // De volgorde van het formulier ís de volgorde (sleepgreep, 28a) — ook het document volgt ze.
            var sortOrder = scopeToGroup ? co.ChangeOrderDetail.Where(d => !InScope(d)).Select(d => d.SortOrder ?? -1).DefaultIfEmpty(-1).Max() + 1 : 0;
            var firstGroup = defaultGroup ?? co.Groups.OrderBy(g => g.SortOrder).FirstOrDefault();
            // Regels van dezelfde leverancier horen aaneen: het volgnummer volgt eerst de groep, dan de volgorde in het formulier (stabiel sorteren).
            // Anders kan een regel die later in een bestaande groep werd toegevoegd achteraan belanden en het document in stukken breken.
            if (groupByKey != null)
                posted = posted.Select((r, i) => (r, i))
                    .OrderBy(x => x.r.GroupKey != null && groupByKey.TryGetValue(x.r.GroupKey, out var g0) ? g0.SortOrder : int.MaxValue)
                    .ThenBy(x => x.i).Select(x => x.r).ToList();
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
                // Minwerk (28v): positief aantal, negatieve prijs, commissie 0 (aan kostprijs). Een uit de offerte overgenomen regel houdt haar teken.
                entity.Number = preserveInternal ? r.Number : entity.SourceDetailId.HasValue && !co.IsQuote ? (entity.Number < 0 ? -1 : 1) * Math.Abs(r.Number) : Math.Abs(r.Number);
                // Een uit de offerte overgenomen WO-regel (SourceDetailId, migratie 069): de prijs ligt vast —
                // wat het formulier ook postte, enkel aantal en omschrijving worden overgenomen.
                var priceLocked = !co.IsQuote && entity.SourceDetailId.HasValue;
                if (!priceLocked)
                {
                    entity.MeasurementType = r.MeasurementType;
                    entity.MeasurementUnit = r.MeasurementUnit;
                    entity.Price = preserveInternal ? r.Price : (r.IsMinwerk ? -Math.Abs(r.Price) : Math.Abs(r.Price));
                    if (!preserveInternal)
                    {
                        entity.Commission = r.IsMinwerk ? 0m : r.Commission;
                        // Schadevergoeding op minwerk (28v): intern. Price wordt het eindbedrag voor de klant (-waarde x (1 - %)); de waarde en het
                        // percentage bewaren we apart, zodat PDF, portaal, facturatie en btw het eindbedrag zien en niets anders.
                        var damagePct = r.IsMinwerk ? Math.Clamp(r.DamagePercentage, 0m, 100m) : 0m;
                        if (damagePct > 0)
                        {
                            entity.ValuePrice = Math.Abs(r.Price);
                            entity.DamagePercentage = damagePct;
                            entity.DamageGoesTo = r.DamageGoesTo == 2 ? (byte)2 : (byte)1;
                            entity.Price = -Math.Round(Math.Abs(r.Price) * (1m - damagePct / 100m), 4, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            entity.ValuePrice = null;
                            entity.DamagePercentage = null;
                            entity.DamageGoesTo = null;
                        }
                        // Gekozen btw-code: het percentage volgt de code (migratie 073), niet wat het formulier postte.
                        if (r.VatTypeId is int vtid && vatTypePercentages != null && vatTypePercentages.TryGetValue(vtid, out var vtPct))
                        {
                            entity.VatTypeId = vtid;
                            entity.VatPercentage = vtPct;
                        }
                        else
                        {
                            entity.VatTypeId = null;
                            entity.VatPercentage = r.VatPercentage;
                        }
                    }
                    else if (isNewRow)
                    {
                        entity.Commission = defaultCommission;
                        entity.VatPercentage = defaultVat;
                        entity.VatTypeId = defaultVatTypeId;
                    }
                }
                // Groep (28t): de regel hoort bij de leverancier waaronder ze stond; ook "via ons gefactureerd" volgt de groep.
                var group = r.GroupKey != null && groupByKey != null && groupByKey.TryGetValue(r.GroupKey, out var postedGroup) ? postedGroup : firstGroup;
                if (group != null)
                {
                    entity.Group = group;
                    if (!group.InvoicedByUs) entity.Invoicable = false;
                    else if (entity.Invoicable == false) entity.Invoicable = null;
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
