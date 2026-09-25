using BOCore;
using ClosedXML.Excel;
using CPMCore.Models.Projecten;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceCore;
using ServiceCore.Invoicing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    /// <summary>gl-v2 coördinatiepagina (design-handoff punt 18, "Coördinatie — combinatie schijven + regie",
    /// 18a/18b). Eigen bestand (partial) i.p.v. ProjectenController.cs verder te laten groeien: de legacy
    /// <c>DetailCoordinatie</c>/<c>CoordinatieInstellingen</c>-acties blijven daar staan en delen enkel
    /// <see cref="BuildCoordinatieModel"/> met deze pagina.</summary>
    public partial class ProjectenController
    {
        private static readonly CultureInfo CoordBe = CultureInfo.GetCultureInfo("nl-BE");

        // ── Model (gedeeld door legacy en V2) ────────────────────────────────────────────────────────

        /// <summary>Bouwt het gedeelde model van Projecten/DetailCoordinatie: schijven, tarieven, prestaties.
        /// Een gefactureerde prestatie toont het tarief waarmee ze gefactureerd werd (migratie 050); een
        /// open prestatie altijd het actuele projecttarief — zo raakt een tariefwijziging enkel wat nog
        /// niet gefactureerd is.</summary>
        /// <summary>Maakt koppelingen los die naar een factuur wijzen die niet meer bestaat. Kwam voor zolang het
        /// verwijderen van een concept-/proformafactuur de schijven en prestaties niet losmaakte
        /// (InvoiceCommandService.DeleteAsync doet dat nu wel): ze bleven "gefactureerd" op een verdwenen factuur
        /// en waren niet meer te factureren. Draait bij het laden van de pagina — goedkoop en idempotent.</summary>
        private void ReleaseOrphanedCoordinationLinks(int projectid)
        {
            _db.ProjectContractSlice
                .Where(s => s.ProjectId == projectid && s.InvoiceId != null && !_db.Invoices.Any(i => i.Id == s.InvoiceId))
                .ExecuteUpdate(u => u.SetProperty(s => s.InvoiceId, (int?)null));
            _db.ProjectRegieUur
                .Where(r => r.ProjectId == projectid && r.InvoiceId != null && !_db.Invoices.Any(i => i.Id == r.InvoiceId))
                .ExecuteUpdate(u => u
                    .SetProperty(r => r.InvoiceId, (int?)null)
                    .SetProperty(r => r.HourlyRateInvoiced, (decimal?)null));
        }

        private ProjectCoordinatieModel BuildCoordinatieModel(int projectid, ProjectBO proj)
        {
            ReleaseOrphanedCoordinationLinks(projectid);
            var model = new ProjectCoordinatieModel
            {
                ProjectId                   = projectid,
                ProjectName                 = proj.Name,
                ContractType                = proj.ContractType,
                ProjectDistanceKm           = proj.ProjectDistanceKm,
                KmAllowance                 = proj.KmAllowance,
                CoordinationIssuerCompanyId = proj.CoordinationIssuerCompanyId,
                ProjectManagerUserId        = proj.AspNetUserID,
            };

            model.ContractPrice = _db.Contract
                .AsNoTracking()
                .Where(c => c.ProjectId == projectid && c.ContractActivity.Any(a => a.ActivityId == 277))
                .SelectMany(c => c.ContractActivity.Where(a => a.ActivityId == 277).Select(a => a.Price))
                .FirstOrDefault();

            var slicesResp = _projectService.GetContractSlices(projectid);
            if (slicesResp.Success)
            {
                var contractPrice = model.ContractPrice ?? 0m;
                model.ContractSlices = slicesResp.Values.Select(s => new ProjectContractSliceVM
                {
                    Id               = s.Id,
                    Description      = s.Description,
                    Percentage       = s.Percentage,
                    Amount           = Math.Round(contractPrice * s.Percentage / 100m, 2, MidpointRounding.AwayFromZero),
                    InvoiceId        = s.InvoiceId,
                    InvoicePublicId  = s.InvoicePublicId
                }).ToList();
            }

            // Gefactureerd bedrag:
            // 1) Directe schijf-factuurkoppeling (InvoiceId op schijf) — meest nauwkeurig
            var linkedInvoiceIds = model.ContractSlices
                .Where(s => s.InvoiceId.HasValue)
                .Select(s => s.InvoiceId!.Value)
                .ToList();

            model.InvoicedAmount = model.ContractSlices
                .Where(s => s.IsInvoiced)
                .Sum(s => s.Amount);

            // 2) Fallback voor bestaande facturen zonder directe schijfkoppeling
            if (model.CoordinationIssuerCompanyId.HasValue)
            {
                var fallbackQuery = _db.Invoices
                    .Where(i => i.ProjectId == projectid
                             && i.IssuerCompanyId == model.CoordinationIssuerCompanyId.Value
                             && i.StatusId != 7); // 7 = Cancelled

                if (linkedInvoiceIds.Count > 0)
                    fallbackQuery = fallbackQuery.Where(i => !linkedInvoiceIds.Contains(i.Id));

                model.InvoicedAmount += fallbackQuery
                    .SelectMany(i => i.InvoicesDetails)
                    .Where(d => d.LineType == "detail")
                    .Sum(d => (decimal?)d.Price) ?? 0m;
            }

            var ratesResp = _projectService.GetProjectHourlyRates(projectid);
            if (ratesResp.Success)
                model.HourlyRates = ratesResp.Values.Select(r => new ProjectHourlyRateVM
                {
                    UserId       = r.UserId,
                    UserFullName = r.UserFullName,
                    HourlyRate   = r.HourlyRate
                }).ToList();

            var regieResp = _projectService.GetRegieUren(projectid);
            if (regieResp.Success)
            {
                var rateMap = model.HourlyRates.ToDictionary(r => r.UserId, r => r.HourlyRate);
                model.RegieUren = regieResp.Values.Select(r => new ProjectRegieUurVM
                {
                    Id              = r.Id,
                    UserId          = r.UserId,
                    UserFullName    = r.UserFullName,
                    HourlyRate      = r.InvoiceId.HasValue && r.HourlyRateInvoiced.HasValue
                        ? r.HourlyRateInvoiced.Value
                        : (rateMap.TryGetValue(r.UserId, out var rate) ? rate : 0m),
                    Date            = r.Date,
                    Hours           = r.Hours,
                    WithTravel      = r.WithTravel,
                    TravelKm        = r.TravelKm,
                    Description     = r.Description,
                    InvoiceId       = r.InvoiceId,
                    InvoicePublicId = r.InvoicePublicId
                }).ToList();
            }

            return model;
        }

        private static string CoordInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])).Take(2));
        }

        /// <summary>Effectieve verplaatsing van een prestatie: de ingevoerde km, anders (oude rijen met enkel
        /// een vinkje) de standaardafstand heen en terug, anders geen.</summary>
        private static decimal CoordEntryKm(ProjectRegieUurVM e, decimal roundTripKm)
            => e.TravelKm.HasValue && e.TravelKm > 0 ? e.TravelKm.Value : (e.WithTravel ? roundTripKm : 0m);

        private static (string Label, string Tone) CoordInvoiceStatus(byte? statusId) => statusId switch
        {
            1 => ("Concept", "is-neutral"),
            2 => ("Uitgegeven", "is-attention"),
            3 => ("Verzonden", "is-attention"),
            4 => ("Deels betaald", "is-attention"),
            5 => ("Betaald", "is-positive"),
            6 => ("Vervallen", "is-blocked"),
            8 => ("Geboekt", "is-positive"),
            9 => ("Bezig", "is-neutral"),
            _ => ("Onbekend", "is-neutral")
        };

        // ── V2-viewmodel ─────────────────────────────────────────────────────────────────────────────

        private DetailCoordinatieV2Vm BuildDetailCoordinatieV2Vm(ProjectCoordinatieModel model, ProjectBO proj, bool canWrite)
        {
            var type = model.ContractType;
            var vm = new DetailCoordinatieV2Vm
            {
                ProjectId                   = model.ProjectId,
                ProjectName                 = model.ProjectName ?? "",
                IsCoordinationProject       = proj.IsOnlyCoordinationProject,
                CanWrite                    = canWrite,
                ContractType                = type,
                ShowSchijven                = type == CoordinationContractType.Schijven || type == CoordinationContractType.Gemengd,
                ShowRegie                   = type == CoordinationContractType.Regie || type == CoordinationContractType.Gemengd,
                ContractPrice               = model.ContractPrice ?? 0m,
                KmAllowance                 = model.KmAllowance ?? 0m,
                DistanceOneWayKm            = model.ProjectDistanceKm,
                DefaultKm                   = Math.Round((model.ProjectDistanceKm ?? 0m) * 2m, 1, MidpointRounding.AwayFromZero),
                CoordinationIssuerCompanyId = model.CoordinationIssuerCompanyId,
                CoordinationReference       = proj.CoordinationReference,
                IssuerCompanies             = GetIssuerCompanies(),
            };
            vm.IssuerCompanyName = vm.IssuerCompanies.FirstOrDefault(i => i.Id == vm.CoordinationIssuerCompanyId)?.Name;
            var street = string.Join(" ", new[] { proj.Street, proj.HouseNumber }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var town = string.Join(" ", new[] { proj.Postalcode?.Postcode, proj.Postalcode?.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)));
            vm.ProjectAddress = string.Join(", ", new[] { street, town }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var fake = new CoordinatieInstellingenVM();
            FillInAvailableUsersForCoord(fake);
            vm.AvailableUsers = fake.AvailableUsers;

            // Waarom er (nog) niet gefactureerd kan worden — de selectiebalk zegt het i.p.v. stil te falen.
            var builderId = _db.Project.AsNoTracking().Where(p => p.ProjectId == model.ProjectId).Select(p => p.BuilderId).FirstOrDefault();
            if (!vm.CoordinationIssuerCompanyId.HasValue)
                vm.InvoiceBlockedReason = "Stel eerst het coördinatiebedrijf in — dat is de factuurhouder.";
            else if (!builderId.HasValue)
                vm.InvoiceBlockedReason = "Dit project heeft geen bouwheer — die komt als klant op de factuur.";

            // Schijven
            var number = 0;
            foreach (var s in model.ContractSlices)
            {
                number++;
                vm.Slices.Add(new CoordSliceRowV2
                {
                    Id = s.Id, Number = number, Description = s.Description ?? "", Percentage = s.Percentage,
                    Amount = s.Amount, InvoiceId = s.InvoiceId, InvoicePublicId = s.InvoicePublicId
                });
            }

            // Regie
            var roundTrip = (model.ProjectDistanceKm ?? 0m) * 2m;
            var allowance = model.KmAllowance ?? 0m;
            var rateUsers = model.HourlyRates.Select(r => r.UserId).ToHashSet();
            foreach (var e in model.RegieUren)
            {
                var km = CoordEntryKm(e, roundTrip);
                vm.Regie.Add(new CoordRegieRowV2
                {
                    Id = e.Id, Date = e.Date, UserId = e.UserId, UserName = e.UserFullName ?? "",
                    Initials = CoordInitials(e.UserFullName), Description = e.Description, Hours = e.Hours,
                    Km = km,
                    // Standaard = de afstand heen en terug; oudere rijen zijn met de opgeronde waarde opgeslagen.
                    KmIsCustom = km > 0m && roundTrip > 0m
                        && Math.Abs(km - roundTrip) > 0.05m && Math.Abs(km - Math.Ceiling(roundTrip)) > 0.05m,
                    Rate = e.HourlyRate, HasRate = e.IsInvoiced || rateUsers.Contains(e.UserId),
                    KmAmount = Math.Round(km * allowance, 2, MidpointRounding.AwayFromZero),
                    InvoiceId = e.InvoiceId, InvoicePublicId = e.InvoicePublicId
                });
            }
            vm.Rates = model.HourlyRates.Select(r => new CoordRateRowV2
            {
                UserId = r.UserId, Name = r.UserFullName ?? "", Initials = CoordInitials(r.UserFullName), Rate = r.HourlyRate
            }).ToList();

            // Facturen: alles wat aan een schijf/prestatie hangt, plus wat het coördinatiebedrijf verder op dit
            // project factureerde (dat laatste is precies wat na een verloren koppeling "niet gekoppeld" blijft).
            var linkedIds = vm.Slices.Where(s => s.InvoiceId.HasValue).Select(s => s.InvoiceId!.Value)
                .Concat(vm.Regie.Where(r => r.InvoiceId.HasValue).Select(r => r.InvoiceId!.Value))
                .Distinct().ToList();
            var issuerId = vm.CoordinationIssuerCompanyId ?? -1;
            var invoices = _db.Invoices.AsNoTracking()
                .Where(i => i.StatusId != 7 && (linkedIds.Contains(i.Id) || (i.ProjectId == model.ProjectId && i.IssuerCompanyId == issuerId)))
                .OrderByDescending(i => i.Date).ThenByDescending(i => i.Id)
                .Select(i => new
                {
                    i.Id, i.PublicId, i.Date, i.StatusId,
                    // Alle lijnen, niet enkel LineType "detail": een handmatig in de facturatiemodule gemaakte factuur heeft
                    // lijnen van het type "Free" en kwam zo op € 0,00 uit.
                    Amount = i.InvoicesDetails.Sum(d => (decimal?)d.Price) ?? 0m
                })
                .ToList();
            foreach (var i in invoices)
            {
                var (label, tone) = CoordInvoiceStatus(i.StatusId);
                var regieRows = vm.Regie.Where(r => r.InvoiceId == i.Id).ToList();
                vm.Invoices.Add(new CoordInvoiceRowV2
                {
                    Id = i.Id, PublicId = string.IsNullOrWhiteSpace(i.PublicId) ? "Concept" : i.PublicId, Date = i.Date,
                    StatusLabel = label, StatusTone = tone, AmountExVat = i.Amount,
                    SliceCount = vm.Slices.Count(s => s.InvoiceId == i.Id),
                    RegieCount = regieRows.Count, RegieHours = regieRows.Sum(r => r.Hours)
                });
            }
            return vm;
        }

        // ── Instellingen (zijpaneel) ─────────────────────────────────────────────────────────────────

        public class CoordSettingsV2Form
        {
            public int ProjectId { get; set; }
            public CoordinationContractType? ContractType { get; set; }
            public int? CoordinationIssuerCompanyId { get; set; }
            public decimal? ContractPrice { get; set; }
            public string? CoordinationReference { get; set; }
            public decimal? KmAllowance { get; set; }
            public List<ProjectHourlyRateVM> HourlyRates { get; set; } = new();
        }

        /// <summary>Slaat het instellingen-zijpaneel op. Raakt de schijven bewust NIET aan (die hebben hun
        /// eigen acties) — de bug waarbij elke opslag alle schijven verwijderde en zonder factuurkoppeling
        /// terugzette zat precies in een formulier dat schijven meepostte zonder hun Id.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCoordinatieInstellingenV2([FromForm] CoordSettingsV2Form form)
        {
            var projectid = form.ProjectId;
            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success) return NotFound();
            var proj = projResp.Value;

            var issuerChanged = proj.CoordinationIssuerCompanyId != form.CoordinationIssuerCompanyId;

            proj.IsCoordinationProject       = true;
            proj.CoordinationIssuerCompanyId = form.CoordinationIssuerCompanyId;
            proj.ContractType                = form.ContractType;
            proj.KmAllowance                 = form.KmAllowance;
            proj.CoordinationReference       = string.IsNullOrWhiteSpace(form.CoordinationReference) ? null : form.CoordinationReference.Trim();

            // De afstand hangt aan bureel (facturatiebedrijf) en werf: herberekenen wanneer die er nog niet is
            // of het bedrijf wijzigde — niet bij élke opslag (dat is een externe routeaanvraag per klik).
            if (proj.CoordinationIssuerCompanyId.HasValue && proj.Postalcode?.PostcodeId > 0
                && (issuerChanged || !proj.ProjectDistanceKm.HasValue))
            {
                var (distKm, durSec) = await _projectService.CalculateRouteAsync(
                    proj.CoordinationIssuerCompanyId.Value, (int)proj.Postalcode.PostcodeId.Value);
                if (distKm.HasValue)
                {
                    proj.ProjectDistanceKm    = distKm;
                    proj.RouteDurationSeconds = durSec;
                }
            }

            _projectService.InsertUpdate(proj);

            _projectService.SaveProjectHourlyRates(projectid, (form.HourlyRates ?? new())
                .Where(r => !string.IsNullOrWhiteSpace(r.UserId))
                .GroupBy(r => r.UserId)
                .Select(g => new ProjectHourlyRateBO { UserId = g.Key, HourlyRate = g.Last().HourlyRate })
                .ToList());

            UpsertCoordinationContract(projectid, form.CoordinationIssuerCompanyId, form.ContractPrice, overwritePrice: false);

            AddMessage("success", "De coördinatie-instellingen zijn opgeslagen.", "Gelukt!");
            return RedirectToAction(nameof(DetailCoordinatie), new { projectid });
        }

        /// <summary>Maakt het coördinatiecontract (lot 277) aan of werkt het bij. <paramref name="overwritePrice"/>
        /// = false laat een bestaand bedrag staan wanneer er geen nieuw meegepost werd (het zijpaneel toont het
        /// bedragveld enkel bij schijven; wie tijdelijk op "regie" staat mag het bedrag niet kwijtraken).</summary>
        private void UpsertCoordinationContract(int projectid, int? issuerCompanyId, decimal? price, bool overwritePrice)
        {
            if (!issuerCompanyId.HasValue) return;

            // Gebruik LegacyCompanyInfoId als directe link; anders fallback via CompanyIssuerCompany
            var linkedCompanyId = _db.IssuerCompany
                .AsNoTracking()
                .Where(ic => ic.Id == issuerCompanyId.Value)
                .Select(ic => ic.LegacyCompanyInfoId)
                .FirstOrDefault()
                ?? _db.CompanyIssuerCompany
                       .Where(c => c.IssuerCompanyId == issuerCompanyId.Value)
                       .Select(c => (int?)c.CompanyId)
                       .FirstOrDefault();

            if (!linkedCompanyId.HasValue) return;

            var existingContract = _db.Contract
                .Include(c => c.ContractActivity)
                .Where(c => c.ProjectId == projectid && c.ContractActivity.Any(a => a.ActivityId == 277))
                .FirstOrDefault();

            if (existingContract == null)
            {
                var contractBo = new ContractBO
                {
                    ProjectId      = projectid,
                    VatPercentage  = 21,
                    PaymentTerm    = 14,
                    ContractSigned = true,
                    GuaranteeType  = ContractGuaranteeType.NoGuarantee
                };
                contractBo.Company.ID = linkedCompanyId.Value;
                contractBo.Activities.Add(new ContractActivityBO
                {
                    Activity = new ActivityBO { ID = 277 },
                    Price    = price
                });
                _projectService.InsertUpdateProjectContract(contractBo);
            }
            else
            {
                existingContract.CompanyId      = linkedCompanyId.Value;
                existingContract.VatPercentage  = 21;
                existingContract.PaymentTerm    = 14;
                existingContract.ContractSigned = true;
                var coordActivity = existingContract.ContractActivity.FirstOrDefault(a => a.ActivityId == 277);
                if (coordActivity != null && (overwritePrice || price.HasValue))
                    coordActivity.Price = price;
                _db.SaveChanges();
            }
        }

        // ── Schijven: toevoegen/bewerken, verwijderen, factuur koppelen ────────────────────────────────

        private List<ProjectContractSliceBO> LoadCoordSlicesForSave(int projectId)
        {
            var resp = _projectService.GetContractSlices(projectId);
            return resp.Success ? resp.Values.ToList() : new List<ProjectContractSliceBO>();
        }

        /// <summary>Nieuwe schijf (<paramref name="sliceId"/> = 0) of een bestaande wijzigen. Gaat via
        /// <c>SaveContractSlices</c> met álle bestaande Id's erbij, zodat factuurkoppelingen behouden blijven.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveCoordSlice(int projectId, int sliceId, string? description, decimal percentage)
        {
            var slices = LoadCoordSlicesForSave(projectId);
            description = description?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                AddMessage("error", "Geef de schijf een omschrijving.", "Niet opgeslagen");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }
            if (percentage <= 0m || percentage > 100m)
            {
                AddMessage("error", "Het percentage moet tussen 0,01 en 100 liggen.", "Niet opgeslagen");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            if (sliceId > 0)
            {
                var existing = slices.FirstOrDefault(s => s.Id == sliceId);
                if (existing == null) return NotFound();
                if (existing.InvoiceId.HasValue)
                {
                    AddMessage("error", "Een gefactureerde schijf kan niet meer gewijzigd worden.", "Niet opgeslagen");
                    return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
                }
                existing.Description = description;
                existing.Percentage = percentage;
            }
            else
            {
                slices.Add(new ProjectContractSliceBO { Id = 0, Description = description, Percentage = percentage });
            }

            if (slices.Sum(s => s.Percentage) > 100.005m)
            {
                AddMessage("error", "De schijven mogen samen niet boven 100 % uitkomen.", "Niet opgeslagen");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            var resp = _projectService.SaveContractSlices(projectId, slices);
            AddMessage(resp.Success ? "success" : "error",
                resp.Success ? "De schijf is opgeslagen." : "De schijf kon niet opgeslagen worden.",
                resp.Success ? "Gelukt!" : "Fout!");
            return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCoordSlice(int projectId, int sliceId)
        {
            var slices = LoadCoordSlicesForSave(projectId);
            var target = slices.FirstOrDefault(s => s.Id == sliceId);
            if (target == null) return NotFound();
            if (target.InvoiceId.HasValue)
            {
                AddMessage("error", "Een gefactureerde schijf kan niet verwijderd worden.", "Niet verwijderd");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            slices.Remove(target);
            var resp = _projectService.SaveContractSlices(projectId, slices);
            AddMessage(resp.Success ? "success" : "error",
                resp.Success ? "De schijf is verwijderd." : "De schijf kon niet verwijderd worden.",
                resp.Success ? "Gelukt!" : "Fout!");
            return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
        }

        /// <summary>Koppelt een bestaande factuur aan een schijf (of, zonder <paramref name="invoiceId"/>, haalt de
        /// koppeling weg). Voor facturen die buiten deze pagina om aangemaakt werden of wier koppeling verloren
        /// ging — de pagina zegt zelf welke facturen van het coördinatiebedrijf nergens aan hangen.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkCoordSliceInvoice(int projectId, int sliceId, int? invoiceId)
        {
            var slice = await _db.ProjectContractSlice.FirstOrDefaultAsync(s => s.Id == sliceId && s.ProjectId == projectId);
            if (slice == null) return NotFound();

            if (invoiceId.HasValue)
            {
                var proj = _db.Project.AsNoTracking().Where(p => p.ProjectId == projectId)
                    .Select(p => new { p.CoordinationIssuerCompanyId }).FirstOrDefault();
                var ok = await _db.Invoices.AsNoTracking().AnyAsync(i => i.Id == invoiceId.Value && i.ProjectId == projectId
                    && i.IssuerCompanyId == proj.CoordinationIssuerCompanyId && i.StatusId != 7);
                if (!ok)
                {
                    AddMessage("error", "Die factuur hoort niet bij dit project en dit coördinatiebedrijf.", "Niet gekoppeld");
                    return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
                }
            }

            slice.InvoiceId = invoiceId;
            await _db.SaveChangesAsync();
            AddMessage("success", invoiceId.HasValue ? "De factuur is aan de schijf gekoppeld." : "De koppeling met de factuur is verwijderd.", "Gelukt!");
            return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
        }

        // ── Eén factuur voor schijven én regie (18a §6) ───────────────────────────────────────────────

        /// <summary>Maakt uit de geselecteerde schijven en/of regie-prestaties één conceptfactuur. Zelfde
        /// factuurregels als <c>MakeCoordSliceInvoices</c>/<c>MakeCoordRegieInvoice</c> (die de legacy pagina
        /// blijft gebruiken), maar in één factuur — "zo factureer je een schijf en de regie van die maand in
        /// één keer". Het tarief van elke prestatie wordt bij het factureren vastgelegd.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCoordInvoice(int projectId, List<int> sliceIds, List<int> regieUurIds)
        {
            sliceIds ??= new(); regieUurIds ??= new();
            var back = RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            if (sliceIds.Count == 0 && regieUurIds.Count == 0) return back;

            var proj = _db.Project.AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => new { p.CoordinationIssuerCompanyId, p.KmAllowance, p.ProjectDistanceKm, p.BuilderId, p.CoordinationReference, p.ProjectName })
                .FirstOrDefault();

            if (proj == null || !proj.CoordinationIssuerCompanyId.HasValue)
            {
                AddMessage("error", "Geen coördinatiebedrijf ingesteld.", "Fout!");
                return back;
            }
            if (!proj.BuilderId.HasValue)
            {
                AddMessage("error", "Geen bouwheer ingesteld voor dit project.", "Fout!");
                return back;
            }

            var kmAllowance = proj.KmAllowance ?? 0m;
            var roundTripKm = (proj.ProjectDistanceKm ?? 0m) * 2m;

            // Enkel wat nog niet gefactureerd is — een tweede klik of een verouderde pagina factureert niets dubbel.
            var contractPrice = _db.Contract.AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.ContractActivity.Any(a => a.ActivityId == 277))
                .SelectMany(c => c.ContractActivity.Where(a => a.ActivityId == 277).Select(a => a.Price))
                .FirstOrDefault() ?? 0m;
            var slices = new List<(int Id, string Description, decimal Percentage)>();
            if (sliceIds.Count > 0)
            {
                slices = _db.ProjectContractSlice.AsNoTracking()
                    .Where(s => s.ProjectId == projectId && sliceIds.Contains(s.Id) && s.InvoiceId == null)
                    .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                    .Select(s => new { s.Id, s.Description, s.Percentage })
                    .ToList().Select(s => (s.Id, s.Description, s.Percentage)).ToList();
            }
            if (slices.Count > 0 && contractPrice <= 0m)
            {
                AddMessage("error", "Stel eerst het contractbedrag in om schijven te kunnen factureren.", "Fout!");
                return back;
            }

            var entries = regieUurIds.Count == 0
                ? new List<ProjectRegieUur>()
                : _db.ProjectRegieUur.Include(r => r.User)
                    .Where(r => r.ProjectId == projectId && regieUurIds.Contains(r.Id) && r.InvoiceId == null)
                    .OrderBy(r => r.Date).ThenBy(r => r.UserId)
                    .ToList();

            if (slices.Count == 0 && entries.Count == 0)
            {
                AddMessage("warning", "Er was niets meer te factureren in deze selectie.", "Opgelet");
                return back;
            }

            var ratesResp = _projectService.GetProjectHourlyRates(projectId);
            var rateMap = ratesResp.Success
                ? ratesResp.Values.ToDictionary(r => r.UserId, r => r.HourlyRate)
                : new Dictionary<string, decimal>();

            var cutoffDate = entries.Count > 0 ? entries.Max(e => e.Date) : DateOnly.FromDateTime(DateTime.Today);
            var cutoffStr = cutoffDate.ToString("dd/MM/yyyy");

            using var uow = _uow;
            var cmd = new InvoiceCommandService(uow, new InvoiceNumberingService(uow));

            var header = entries.Count > 0 && slices.Count > 0
                ? $"Coördinatie voor het project {proj.ProjectName}: contractschijven en prestaties tot {cutoffStr}"
                : entries.Count > 0 ? $"Prestaties voor het project {proj.ProjectName} tot {cutoffStr}" : null;

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId   = proj.CoordinationIssuerCompanyId.Value,
                InvoiceDate       = DateOnly.FromDateTime(DateTime.Today),
                Mode              = InvoiceMode.Free,
                ProjectId         = projectId,
                CompanyId         = proj.BuilderId,
                HeaderDescription = header,
                DetailDescription = string.IsNullOrWhiteSpace(proj.CoordinationReference) ? null : proj.CoordinationReference.Trim()
            };

            foreach (var slice in slices)
            {
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text          = $"Projectcoördinatie – {slice.Description} ({slice.Percentage:0.##}%)",
                    Price         = Math.Round(contractPrice * slice.Percentage / 100m, 2, MidpointRounding.AwayFromZero),
                    VatPercentage = 21m,
                    LineType      = "detail"
                });
            }

            var perUser = entries
                .GroupBy(e => e.UserId)
                .Select(g =>
                {
                    var rate = rateMap.TryGetValue(g.Key, out var r) ? r : 0m;
                    return new
                    {
                        FullName   = $"{g.First().User?.Voornaam} {g.First().User?.Familienaam}".Trim(),
                        HourlyRate = rate,
                        TotalHours = g.Sum(e => e.Hours),
                        Entries    = g.OrderBy(e => e.Date).ToList()
                    };
                })
                .ToList();

            foreach (var u in perUser)
            {
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text          = $"Prestaties {u.FullName}",
                    Quantity      = u.TotalHours,
                    UnitPrice     = u.HourlyRate,
                    Price         = Math.Round(u.TotalHours * u.HourlyRate, 2, MidpointRounding.AwayFromZero),
                    VatPercentage = 21m,
                    LineType      = "detail"
                });
            }

            var travelEntries = entries.Where(e => e.WithTravel || (e.TravelKm.HasValue && e.TravelKm > 0)).ToList();
            var tripCount = travelEntries.Count;
            var totalKm = travelEntries.Sum(e => e.TravelKm.HasValue && e.TravelKm > 0 ? e.TravelKm.Value : roundTripKm);
            var totalKmCost = Math.Round(totalKm * kmAllowance, 2, MidpointRounding.AwayFromZero);
            if (tripCount > 0 && totalKm > 0)
            {
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text          = $"{tripCount} verplaatsing{(tripCount == 1 ? "" : "en")} – {totalKm:0} km totaal aan {kmAllowance.ToString("0.00", CultureInfo.InvariantCulture)}€/km",
                    Quantity      = totalKm,
                    UnitPrice     = kmAllowance,
                    Price         = totalKmCost,
                    VatPercentage = 21m,
                    LineType      = "detail"
                });
            }

            try
            {
                var (invoiceId, _) = await cmd.CreateWithLinesAsync(draft, issueNow: false);

                if (slices.Count > 0)
                {
                    var ids = slices.Select(s => s.Id).ToList();
                    var sliceEntities = _db.ProjectContractSlice.Where(s => s.ProjectId == projectId && ids.Contains(s.Id)).ToList();
                    foreach (var s in sliceEntities) s.InvoiceId = invoiceId;
                    await _db.SaveChangesAsync();
                }

                if (entries.Count > 0)
                {
                    _projectService.MarkRegieUrenAsInvoiced(entries.Select(e => e.Id).ToList(), invoiceId, rateMap);

                    var appendixBytes = BuildRegieAppendix(
                        proj.ProjectName, cutoffStr, perUser.Select(u => new RegieAppendixUser
                        {
                            FullName   = u.FullName,
                            HourlyRate = u.HourlyRate,
                            TotalHours = u.TotalHours,
                            Entries    = u.Entries.Select(e => new RegieAppendixEntry
                            {
                                Date       = e.Date,
                                Hours      = e.Hours,
                                EntryKm    = e.TravelKm.HasValue && e.TravelKm > 0 ? e.TravelKm.Value : (e.WithTravel ? roundTripKm : 0m),
                                HourAmount = e.Hours * u.HourlyRate
                            }).ToList()
                        }).ToList(),
                        tripCount, kmAllowance, totalKmCost);

                    var invoice = await _db.Invoices.FindAsync(invoiceId);
                    if (invoice != null && appendixBytes.Length > 0)
                    {
                        invoice.PdfAppendixFileName = $"Prestatielijst_{cutoffDate:yyyyMMdd}.pdf";
                        invoice.PdfAppendixContent  = appendixBytes;
                        await _db.SaveChangesAsync();
                    }
                }

                var parts = new List<string>();
                if (slices.Count > 0) parts.Add($"{slices.Count} schijf" + (slices.Count == 1 ? "" : "en"));
                if (entries.Count > 0) parts.Add($"{entries.Count} prestatie" + (entries.Count == 1 ? "" : "s"));
                AddMessage("success", $"Conceptfactuur aangemaakt voor {string.Join(" en ", parts)}.", "Gelukt!");
            }
            catch (Exception ex)
            {
                AddMessage("error", $"Factuur kon niet worden aangemaakt: {ex.Message}", "Fout!");
            }

            return back;
        }

        // ── Export ───────────────────────────────────────────────────────────────────────────────────

        /// <summary>Eén Excel-bestand met een blad per gebruikt contracttype. Vervangt de client-side
        /// DataTables-export van de legacy pagina, die enkel de schijventabel meenam en met de
        /// regietabel niets kon.</summary>
        [HttpGet]
        public IActionResult ExportCoordinatieExcel(int projectid)
        {
            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success) return NotFound();
            var proj = projResp.Value;
            var vm = BuildDetailCoordinatieV2Vm(BuildCoordinatieModel(projectid, proj), proj, canWrite: false);

            using var wb = new XLWorkbook();
            void Header(IXLWorksheet ws, params string[] headers)
            {
                for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
                var range = ws.Range(1, 1, 1, headers.Length);
                range.Style.Font.Bold = true;
                range.Style.Fill.BackgroundColor = XLColor.FromHtml("#00532D");
                range.Style.Font.FontColor = XLColor.White;
            }

            if (vm.ShowSchijven)
            {
                var ws = wb.Worksheets.Add("Schijven");
                Header(ws, "Nr", "Schijf", "Percentage", "Bedrag (excl. btw)", "Status", "Factuur");
                var row = 2;
                foreach (var s in vm.Slices)
                {
                    ws.Cell(row, 1).Value = s.Number;
                    ws.Cell(row, 2).Value = s.Description;
                    ws.Cell(row, 3).Value = (double)s.Percentage;
                    ws.Cell(row, 3).Style.NumberFormat.Format = "0.00\" %\"";
                    ws.Cell(row, 4).Value = (double)s.Amount;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "€ #,##0.00";
                    ws.Cell(row, 5).Value = s.IsInvoiced ? "Gefactureerd" : "Te factureren";
                    ws.Cell(row, 6).Value = s.IsInvoiced ? (s.InvoicePublicId ?? "Concept") : "";
                    row++;
                }
                ws.Columns().AdjustToContents();
            }

            if (vm.ShowRegie)
            {
                var ws = wb.Worksheets.Add("Regie");
                Header(ws, "Datum", "Medewerker", "Omschrijving", "Uren", "Uurtarief", "Km", "Bedrag uren", "Bedrag km", "Totaal (excl. btw)", "Status", "Factuur");
                var row = 2;
                foreach (var r in vm.Regie.OrderBy(r => r.Date).ThenBy(r => r.Id))
                {
                    ws.Cell(row, 1).Value = r.Date.ToDateTime(TimeOnly.MinValue);
                    ws.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                    ws.Cell(row, 2).Value = r.UserName;
                    ws.Cell(row, 3).Value = r.Description ?? "";
                    ws.Cell(row, 4).Value = (double)r.Hours;
                    ws.Cell(row, 5).Value = (double)r.Rate;
                    ws.Cell(row, 6).Value = (double)r.Km;
                    ws.Cell(row, 7).Value = (double)r.HoursAmount;
                    ws.Cell(row, 8).Value = (double)r.KmAmount;
                    ws.Cell(row, 9).Value = (double)r.Amount;
                    foreach (var c in new[] { 5, 7, 8, 9 }) ws.Cell(row, c).Style.NumberFormat.Format = "€ #,##0.00";
                    ws.Cell(row, 10).Value = r.IsInvoiced ? "Gefactureerd" : "Nog niet gefactureerd";
                    ws.Cell(row, 11).Value = r.IsInvoiced ? (r.InvoicePublicId ?? "Concept") : "";
                    row++;
                }
                ws.Columns().AdjustToContents();
            }

            if (wb.Worksheets.Count == 0) wb.Worksheets.Add("Coördinatie");

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Coordinatie - {vm.ProjectName} {DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}
