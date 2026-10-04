using BOCore;
using CPMCore.Documents;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Klanten;
using CPMCore.Models.Leveranciers;
using CPMCore.Models.Projecten;
using FacadeCore;
using DALCore;
using DALCore.Models;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using ServiceCore;
using ServiceCore.Budget;
using ServiceCore.Invoicing;
using BOCore.Budget;
using CPMCore.Models.Budget;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ClosedXML.Excel;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CPMCore.Controllers
{
    /// <summary>Coördinatieproject (legacy): coördinatie-overzicht, instellingen (schijven/tarieven), schijf- en regie-facturatie, regie-uren. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Coordinatie/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        [HttpGet]
        [Breadcrumb("Coördinatie", FromAction = "Detail")]
        public IActionResult DetailCoordinatie(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success)
                return NotFound();

            var proj = projResp.Value;
            var model = BuildCoordinatieModel(projectid, proj);
            var useGlV2 = ViewData["UseGlV2Layout"] as bool? == true;

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = Index };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", proj.Name)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailCoordinatie", "Projecten", "Coördinatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid }
            };
            // gl-v2: de kruimel stopt bij de projectnaam — "Coördinatie" is al de paginatitel (design-handoff
            // punt 13, regel 2: het laatste kruimelitem herhaalt nooit de titel).
            ViewData["BreadcrumbNode"] = useGlV2 ? projectDetail : lastnode;

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Coördinatie");

            if (useGlV2)
            {
                var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
                model.GlV2 = BuildDetailCoordinatieV2Vm(model, proj, _ps.HasWrite(PermissionCodes.ProjectsDetail));
                return View("DetailCoordinatieV2", model);
            }
            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Coördinatie-instellingen", FromAction = "DetailCoordinatie")]
        public IActionResult CoordinatieInstellingen(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success)
                return NotFound();

            var proj = projResp.Value;
            var model = new Models.Projecten.CoordinatieInstellingenVM
            {
                ProjectId                   = projectid,
                ProjectName                 = proj.Name,
                CoordinationIssuerCompanyId = proj.CoordinationIssuerCompanyId,
                ContractType                = proj.ContractType,
                KmAllowance                 = proj.KmAllowance,
                CoordinationReference       = proj.CoordinationReference,
                ProjectDistanceKm           = proj.ProjectDistanceKm,
                RouteDurationSeconds        = proj.RouteDurationSeconds,
                IssuerCompanies             = GetIssuerCompanies(),
            };

            FillInAvailableUsersForCoord(model);

            var slicesResp = _projectService.GetContractSlices(projectid);
            if (slicesResp.Success)
                model.ContractSlices = slicesResp.Values.Select(s => new Models.Projecten.ProjectContractSliceVM
                {
                    Id          = s.Id,
                    Description = s.Description,
                    Percentage  = s.Percentage
                }).ToList();

            var ratesResp = _projectService.GetProjectHourlyRates(projectid);
            if (ratesResp.Success)
                model.HourlyRates = ratesResp.Values.Select(r => new Models.Projecten.ProjectHourlyRateVM
                {
                    UserId       = r.UserId,
                    UserFullName = r.UserFullName,
                    HourlyRate   = r.HourlyRate
                }).ToList();

            // Laad contractprijs van het coördinatiecontract (lot 277 = projectcoordinatie)
            model.ContractPrice = _db.Contract
                .AsNoTracking()
                .Where(c => c.ProjectId == projectid && c.ContractActivity.Any(a => a.ActivityId == 277))
                .SelectMany(c => c.ContractActivity.Where(a => a.ActivityId == 277).Select(a => a.Price))
                .FirstOrDefault();

            SetCoordinatieBreadcrumb(projectid, proj.Name);
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Coördinatie-instellingen");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoordinatieInstellingen(Models.Projecten.CoordinatieInstellingenVM vm)
        {
            var projectid = vm.ProjectId;

            // Laad het bestaande project om alle velden te bewaren
            var projResp = _projectService.GetProjectByID(projectid);
            if (!projResp.Success)
                return NotFound();

            var proj = projResp.Value;

            // Coördinatie-velden bijwerken
            proj.IsCoordinationProject      = true;
            proj.CoordinationIssuerCompanyId = vm.CoordinationIssuerCompanyId;
            proj.ContractType               = vm.ContractType;
            proj.KmAllowance                = vm.KmAllowance;
            proj.CoordinationReference      = string.IsNullOrWhiteSpace(vm.CoordinationReference) ? null : vm.CoordinationReference.Trim();

            // Route herberekenen indien coördinatiebedrijf en postcode beschikbaar
            if (proj.CoordinationIssuerCompanyId.HasValue && proj.Postalcode?.PostcodeId > 0)
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

            // Schijven en uurtarieven opslaan
            _projectService.SaveContractSlices(projectid, (vm.ContractSlices ?? new()).Select(s => new BOCore.ProjectContractSliceBO
            {
                Id          = s.Id, // zonder Id werd elke schijf verwijderd en opnieuw aangemaakt — met verlies van de factuurkoppeling
                Description = s.Description,
                Percentage  = s.Percentage
            }).ToList());

            _projectService.SaveProjectHourlyRates(projectid, (vm.HourlyRates ?? new())
                .Where(r => !string.IsNullOrWhiteSpace(r.UserId))
                .Select(r => new BOCore.ProjectHourlyRateBO
                {
                    UserId    = r.UserId,
                    HourlyRate = r.HourlyRate
                }).ToList());

            // Coördinatiecontract aanmaken/bijwerken indien facturatiebedrijf geselecteerd
            UpsertCoordinationContract(projectid, vm.CoordinationIssuerCompanyId, vm.ContractPrice, overwritePrice: true);

            return RedirectToAction(nameof(DetailCoordinatie), new { projectid });
        }

        [HttpGet]
        public IActionResult GetIssuerCompanyDefaults(int issuerCompanyId)
        {
            var company = _db.IssuerCompany
                .AsNoTracking()
                .Where(c => c.Id == issuerCompanyId)
                .Select(c => new
                {
                    ratePerKm = c.RatePerKm,
                    userRates = c.IssuerCompanyUserRate.Select(r => new { userId = r.UserId, hourlyRate = r.HourlyRate }).ToList()
                })
                .FirstOrDefault();

            if (company == null)
                return NotFound();

            return Json(company);
        }

        [HttpGet]
        public PartialViewResult BlankSliceRow()
        {
            var viewData = new ViewDataDictionary<Models.Projecten.ProjectContractSliceVM>(
                ViewData, new Models.Projecten.ProjectContractSliceVM());
            return new PartialViewResult
            {
                ViewName = "Partials/_SliceRow",
                ViewData = viewData
            };
        }

        [HttpGet]
        public PartialViewResult BlankRateRow()
        {
            var internalUserIds = _db.PermissionPerUser.Select(p => p.UserId).Distinct();
            var users = _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && internalUserIds.Contains(u.Id))
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserId,
                    Text = (u.Voornaam + " " + u.Familienaam).Trim()
                })
                .ToList();

            var viewData = new ViewDataDictionary<Models.Projecten.ProjectHourlyRateVM>(
                ViewData, new Models.Projecten.ProjectHourlyRateVM())
            {
                { "AvailableUsers", users }
            };
            return new PartialViewResult
            {
                ViewName = "Partials/_RateRow",
                ViewData = viewData
            };
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCoordSliceInvoices(int projectId, List<int> sliceIds)
        {
            if (sliceIds == null || sliceIds.Count == 0)
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            var proj = _db.Project
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => new { p.ProjectId, p.CoordinationIssuerCompanyId, p.BuilderId })
                .FirstOrDefault();

            if (proj == null || !proj.CoordinationIssuerCompanyId.HasValue)
            {
                AddMessage("error", "Geen coördinatiebedrijf ingesteld.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            if (!proj.BuilderId.HasValue)
            {
                AddMessage("error", "Geen bouwheer ingesteld voor dit project.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            var issuerCompanyId = proj.CoordinationIssuerCompanyId.Value;

            // Ontvanger: bouwheer van het project
            var linkedCompanyId = proj.BuilderId;

            // Contractprijs ophalen
            var contractPrice = _db.Contract
                .AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.ContractActivity.Any(a => a.ActivityId == 277))
                .SelectMany(c => c.ContractActivity.Where(a => a.ActivityId == 277).Select(a => a.Price))
                .FirstOrDefault() ?? 0m;

            // Geselecteerde schijven ophalen
            var slices = _db.ProjectContractSlice
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId && sliceIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Description, s.Percentage })
                .ToList();

            if (!slices.Any())
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            using var uow = _uow;
            var cmd = new InvoiceCommandService(uow, new InvoiceNumberingService(uow));

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId = issuerCompanyId,
                InvoiceDate     = DateOnly.FromDateTime(DateTime.Today),
                Mode            = InvoiceMode.Free,
                ProjectId       = projectId,
                CompanyId       = linkedCompanyId
            };

            foreach (var slice in slices)
            {
                var amount = contractPrice * slice.Percentage / 100m;
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text           = $"Projectcoördinatie – {slice.Description} ({slice.Percentage:0.##}%)",
                    Price          = Math.Round(amount, 2, MidpointRounding.AwayFromZero),
                    VatPercentage  = 21m,
                    LineType       = "detail"
                });
            }

            try
            {
                var (invoiceId, _) = await cmd.CreateWithLinesAsync(draft, issueNow: false);

                // Koppel de nieuwe factuur aan de geselecteerde schijven
                var sliceEntities = _db.ProjectContractSlice
                    .Where(s => s.ProjectId == projectId && sliceIds.Contains(s.Id))
                    .ToList();
                foreach (var s in sliceEntities)
                    s.InvoiceId = invoiceId;
                await _db.SaveChangesAsync();

                AddMessage("success", "Het conceptfactuur is aangemaakt.", "Gelukt!");
            }
            catch (Exception ex)
            {
                AddMessage("error", $"Factuur kon niet worden aangemaakt: {ex.Message}", "Fout!");
            }

            return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
        }

        // ── Regie-uren factureren ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeCoordRegieInvoice(int projectId, List<int> regieUurIds)
        {
            if (regieUurIds == null || regieUurIds.Count == 0)
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            var proj = _db.Project
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => new {
                    p.CoordinationIssuerCompanyId, p.KmAllowance, p.ProjectDistanceKm,
                    p.BuilderId, p.CoordinationReference, p.ProjectName
                })
                .FirstOrDefault();

            if (proj == null || !proj.CoordinationIssuerCompanyId.HasValue)
            {
                AddMessage("error", "Geen coördinatiebedrijf ingesteld.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            if (!proj.BuilderId.HasValue)
            {
                AddMessage("error", "Geen bouwheer ingesteld voor dit project.", "Fout!");
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
            }

            var issuerCompanyId = proj.CoordinationIssuerCompanyId.Value;
            var kmAllowance     = proj.KmAllowance ?? 0m;
            var roundTripKm     = (proj.ProjectDistanceKm ?? 0m) * 2m;

            // Niet-gefactureerde regie-uren ophalen
            var entries = _db.ProjectRegieUur
                .Include(r => r.User)
                .Where(r => r.ProjectId == projectId && regieUurIds.Contains(r.Id) && r.InvoiceId == null)
                .OrderBy(r => r.Date).ThenBy(r => r.UserId)
                .ToList();

            if (!entries.Any())
                return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });

            // Uurtarieven ophalen
            var ratesResp = _projectService.GetProjectHourlyRates(projectId);
            var rateMap   = ratesResp.Success
                ? ratesResp.Values.ToDictionary(r => r.UserId, r => r.HourlyRate)
                : new Dictionary<string, decimal>();

            // Cutoff datum = meest recente datum in de selectie
            var cutoffDate = entries.Max(e => e.Date);
            var cutoffStr  = cutoffDate.ToString("dd/MM/yyyy");

            // Groepeer per medewerker
            var perUser = entries
                .GroupBy(e => e.UserId)
                .Select(g =>
                {
                    var hourlyRate = rateMap.TryGetValue(g.Key, out var r) ? r : 0m;
                    var fullName   = $"{g.First().User?.Voornaam} {g.First().User?.Familienaam}".Trim();
                    return new
                    {
                        FullName   = fullName,
                        HourlyRate = hourlyRate,
                        TotalHours = g.Sum(e => e.Hours),
                        Entries    = g.OrderBy(e => e.Date).ToList()
                    };
                })
                .ToList();

            // Verplaatsingen — gebruik TravelKm per rij; fallback op roundTripKm voor oude rijen
            var travelEntries = entries.Where(e => e.WithTravel || (e.TravelKm.HasValue && e.TravelKm > 0)).ToList();
            var tripCount    = travelEntries.Count;
            var totalKm      = travelEntries.Sum(e => e.TravelKm.HasValue && e.TravelKm > 0 ? e.TravelKm.Value : roundTripKm);
            var totalKmCost  = Math.Round(totalKm * kmAllowance, 2, MidpointRounding.AwayFromZero);

            using var uow = _uow;
            var cmd = new InvoiceCommandService(uow, new InvoiceNumberingService(uow));

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId   = issuerCompanyId,
                InvoiceDate       = DateOnly.FromDateTime(DateTime.Today),
                Mode              = InvoiceMode.Free,
                ProjectId         = projectId,
                CompanyId         = proj.BuilderId,
                HeaderDescription = $"Prestaties voor het project {proj.ProjectName} tot {cutoffStr}",
                DetailDescription = string.IsNullOrWhiteSpace(proj.CoordinationReference) ? null : proj.CoordinationReference.Trim()
            };

            // Één lijn per medewerker
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

            // Km-lijn (enkel indien er verplaatsingen zijn)
            if (tripCount > 0 && totalKm > 0)
            {
                draft.Lines.Add(new InvoiceLineBO
                {
                    Text          = $"{tripCount} verplaatsing{(tripCount == 1 ? "" : "en")} \u2013 {totalKm:0}\u00a0km totaal aan {kmAllowance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}\u20ac/km",
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
                _projectService.MarkRegieUrenAsInvoiced(entries.Select(e => e.Id).ToList(), invoiceId, rateMap);

                // Detailbijlage genereren en opslaan
                var appendixBytes = BuildRegieAppendix(
                    proj.ProjectName, cutoffStr, perUser.Select(u => new RegieAppendixUser
                    {
                        FullName   = u.FullName,
                        HourlyRate = u.HourlyRate,
                        TotalHours = u.TotalHours,
                        Entries    = u.Entries.Select(e => new RegieAppendixEntry
                        {
                            Date        = e.Date,
                            Hours       = e.Hours,
                            EntryKm     = e.TravelKm.HasValue && e.TravelKm > 0 ? e.TravelKm.Value : (e.WithTravel ? roundTripKm : 0m),
                            HourAmount  = e.Hours * (rateMap.TryGetValue(e.UserId, out var hr) ? hr : 0m)
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

                AddMessage("success", $"Conceptfactuur aangemaakt voor {entries.Count} prestaties.", "Gelukt!");
            }
            catch (Exception ex)
            {
                AddMessage("error", $"Factuur kon niet worden aangemaakt: {ex.Message}", "Fout!");
            }

            return RedirectToAction(nameof(DetailCoordinatie), new { projectid = projectId });
        }

        // ── Regie-factuur bijlage (QuestPDF) ─────────────────────────────────

        private sealed class RegieAppendixUser
        {
            public string  FullName   { get; set; }
            public decimal HourlyRate { get; set; }
            public decimal TotalHours { get; set; }
            public List<RegieAppendixEntry> Entries { get; set; } = new();
        }

        private sealed class RegieAppendixEntry
        {
            public DateOnly Date       { get; set; }
            public decimal  Hours      { get; set; }
            public decimal  EntryKm    { get; set; }   // 0 = geen verplaatsing
            public decimal  HourAmount { get; set; }
        }

        private static byte[] BuildRegieAppendix(
            string projectName, string cutoffStr,
            List<RegieAppendixUser> users,
            int tripCount, decimal kmAllowance, decimal totalKmCost)
        {
            var culture = new System.Globalization.CultureInfo("nl-BE");
            string Eur(decimal v)  => "\u20ac\u00a0" + v.ToString("N2", culture);
            string Num(decimal v)  => v.ToString("N2", culture);

            var headerColor = QuestPDF.Helpers.Colors.Grey.Lighten3;
            var borderColor = QuestPDF.Helpers.Colors.Grey.Medium;

            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"Prestatielijst \u2013 {projectName}")
                            .Bold().FontSize(13);
                        col.Item().Text($"Periode tot en met {cutoffStr}")
                            .FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(borderColor);
                    });

                    page.Content().PaddingTop(12).Column(main =>
                    {
                        // ── Per medewerker ─────────────────────────────────
                        foreach (var user in users)
                        {
                            main.Item().PaddingBottom(12).Column(userCol =>
                            {
                                // Sectieheader medewerker
                                userCol.Item().Background(headerColor).Padding(4)
                                    .Text(user.FullName).Bold().FontSize(10);

                                // Tabel
                                userCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(2);  // Datum
                                        c.RelativeColumn(1);  // Uren
                                        c.RelativeColumn(2);  // Verplaatsing
                                        c.RelativeColumn(2);  // Bedrag uren
                                        c.RelativeColumn(2);  // Totaal
                                    });

                                    // Header rij
                                    void HeaderCell(string text, bool right = false)
                                    {
                                        var cell = table.Cell().Background(headerColor).Padding(3);
                                        if (right) cell.AlignRight().Text(text).Bold().FontSize(8);
                                        else       cell.Text(text).Bold().FontSize(8);
                                    }

                                    HeaderCell("Datum");
                                    HeaderCell("Uren");
                                    HeaderCell("Verplaatsing");
                                    HeaderCell("Bedrag uren", right: true);
                                    HeaderCell("Totaal",      right: true);

                                    // Datarijen
                                    foreach (var e in user.Entries)
                                    {
                                        void DataCell(string text, bool right = false)
                                        {
                                            var cell = table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(3);
                                            if (right) cell.AlignRight().Text(text);
                                            else       cell.Text(text);
                                        }

                                        DataCell(e.Date.ToString("dd/MM/yyyy"));
                                        DataCell(Num(e.Hours));         // Uren: links uitgelijnd
                                        DataCell(e.EntryKm > 0 ? $"{e.EntryKm:0}\u00a0km" : "\u2014");
                                        DataCell(Eur(e.HourAmount), right: true);
                                        DataCell(Eur(e.HourAmount), right: true);  // Totaal = alleen uursbedrag
                                    }

                                    // Subtotaalrij (totaal = som uursbedragen)
                                    var subTotal = user.Entries.Sum(e => e.HourAmount);
                                    table.Cell().ColumnSpan(4).Padding(3).AlignRight()
                                         .Text($"Subtotaal {user.FullName}:").Bold();
                                    table.Cell().Padding(3).AlignRight()
                                         .Text(Eur(subTotal)).Bold();
                                });
                            });
                        }

                        // ── Verplaatsingen ─────────────────────────────────
                        if (tripCount > 0 && totalKmCost > 0)
                        {
                            main.Item().PaddingBottom(12).Column(kmCol =>
                            {
                                kmCol.Item().Background(headerColor).Padding(4)
                                    .Text("Verplaatsingen").Bold().FontSize(10);
                                kmCol.Item().Table(kmTable =>
                                {
                                    kmTable.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(3);
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(2);
                                        c.RelativeColumn(2);
                                    });

                                    void KH(string t, bool right = false)
                                    {
                                        var cell = kmTable.Cell().Background(headerColor).Padding(3);
                                        if (right) cell.AlignRight().Text(t).Bold().FontSize(8);
                                        else       cell.Text(t).Bold().FontSize(8);
                                    }
                                    KH("Omschrijving"); KH("Aantal", right: true); KH("Prijs/eenheid", right: true); KH("Totaal", right: true);

                                    var totalKmAll = users.SelectMany(u => u.Entries).Sum(e => e.EntryKm);
                                    kmTable.Cell().Padding(3).Text($"{totalKmAll:0}\u00a0km totaal");
                                    kmTable.Cell().Padding(3).AlignRight().Text($"{tripCount}\u00d7");
                                    kmTable.Cell().Padding(3).AlignRight().Text($"{Eur(kmAllowance)}/km");
                                    kmTable.Cell().Padding(3).AlignRight().Text(Eur(totalKmCost)).Bold();
                                });
                            });
                        }

                        // ── Eindtotaal ─────────────────────────────────────
                        var grandTotal = users.Sum(u => u.Entries.Sum(e => e.HourAmount)) + totalKmCost;
                        main.Item().PaddingTop(4).LineHorizontal(1).LineColor(borderColor);
                        main.Item().PaddingTop(6).AlignRight()
                            .Text($"Totaal excl. btw: {Eur(grandTotal)}").Bold().FontSize(11);
                    });

                    page.Footer().AlignRight()
                        .Text(t =>
                        {
                            t.Span("Pagina ").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            t.CurrentPageNumber().FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            t.Span(" van ").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            t.TotalPages().FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                        });
                });
            }).GeneratePdf();
        }

        // ── Regie-uren AJAX endpoints ─────────────────────────────────────────

        public class AddRegieUurRequest
        {
            public int ProjectId { get; set; }
            public string UserId { get; set; }
            public string Date { get; set; }
            public decimal Hours { get; set; }
            public decimal? TravelKm { get; set; }
            public string Description { get; set; }
        }

        [HttpPost]
        public IActionResult AddRegieUur([FromBody] AddRegieUurRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserId) || req.Hours <= 0)
                return BadRequest(new { error = "Ongeldige invoer" });

            if (!DateOnly.TryParse(req.Date, out var date))
                return BadRequest(new { error = "Ongeldige datum" });

            var ratesResp = _projectService.GetProjectHourlyRates(req.ProjectId);
            var hourlyRate = ratesResp.Success
                ? ratesResp.Values.FirstOrDefault(r => r.UserId == req.UserId)?.HourlyRate ?? 0m
                : 0m;

            var bo = new BOCore.ProjectRegieUurBO
            {
                ProjectId   = req.ProjectId,
                UserId      = req.UserId,
                Date        = date,
                Hours       = req.Hours,
                TravelKm    = req.TravelKm.HasValue && req.TravelKm > 0 ? req.TravelKm : null,
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
            };

            var resp = _projectService.AddRegieUur(bo);
            if (!resp.Success || !resp.Values.Any())
                return StatusCode(500, new { error = "Opslaan mislukt" });

            var saved = resp.Values.First();
            return Ok(new
            {
                id           = saved.Id,
                userId       = saved.UserId,
                userFullName = saved.UserFullName,
                date         = saved.Date.ToString("yyyy-MM-dd"),
                hours        = saved.Hours,
                travelKm     = saved.TravelKm,
                description  = saved.Description,
                hourlyRate   = hourlyRate
            });
        }

        [HttpPost]
        public IActionResult DeleteRegieUur(int id)
        {
            var resp = _projectService.DeleteRegieUur(id);
            if (resp.Success)
                return Ok();
            var msg = resp.Messages?.FirstOrDefault()?.Message ?? "Verwijderen mislukt";
            return StatusCode(resp.Messages?.Any(m => m.Message?.Contains("gefactureerd") == true) == true ? 409 : 500,
                new { error = msg });
        }

        private static InvoiceDraftBO? BuildStageInvoiceDraft(
                                  int issuerCompanyId,
                                  int? clientAccountId,
                                  int? clientContactId,
                                  IEnumerable<int> stageIds,
                                  ProjectBO project,
                                  ProjectSalesSettingsBO settings,
                                  IEnumerable<UnitWithStagesBO> units,
                                  int? paymentGroupId,
                                  decimal? ownerPercentageOverride,
                                  DALCore.Models.cpmRunningContext db)
        {
            var groupedStageIds = stageIds?.Distinct().ToList() ?? new List<int>();

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId = issuerCompanyId,
                InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                Mode = InvoiceMode.Stages,
                StageIds = groupedStageIds,
                ProjectId = project.Id,
                PaymentGroupId = paymentGroupId
            };

            decimal ownerPercentage = ownerPercentageOverride ?? 100m;
            string? invoiceExtra = null;

            if (clientAccountId.HasValue)
            {
                (draft.CompanyId, draft.ClientType, draft.ClientId) = (null, (int)InvoicePartyType.ClientAccount, clientAccountId);

                var accountInfo = db.ClientAccount
                    .AsNoTracking()
                    .Where(c => c.Id == clientAccountId.Value)
                    .Select(c => new { c.OwnerPercentage, c.InvoiceExtra })
                    .FirstOrDefault();

                if (!ownerPercentageOverride.HasValue)
                    ownerPercentage = accountInfo?.OwnerPercentage ?? 100m;
                invoiceExtra = accountInfo?.InvoiceExtra;
            }
            else if (clientContactId.HasValue)
            {
                (draft.CompanyId, draft.ClientType, draft.ClientId) = (null, (int)InvoicePartyType.ClientContact, clientContactId);

                var contactInfo = db.ClientContacts
                    .AsNoTracking()
                    .Where(c => c.Id == clientContactId.Value)
                    .Select(c => new
                    {
                        c.CoOwnerPercentage,
                        AccountOwnerPct = c.ClientAccount.OwnerPercentage,
                        AccountInvoiceExtra = c.ClientAccount.InvoiceExtra
                    })
                    .FirstOrDefault();

                if (!ownerPercentageOverride.HasValue)
                    ownerPercentage = contactInfo?.CoOwnerPercentage ?? contactInfo?.AccountOwnerPct ?? 100m;
                invoiceExtra = contactInfo?.AccountInvoiceExtra;
            }

            if (!string.IsNullOrWhiteSpace(invoiceExtra))
                draft.FooterDescription = invoiceExtra;

            if (ownerPercentage <= 0m)
                return null;

            int? issuerBankAccountId = settings?.BankAccountId;
            if (!issuerBankAccountId.HasValue && !string.IsNullOrWhiteSpace(settings?.BankAccountNumber))
            {
                issuerBankAccountId = db.IssuerBankAccount
                    .Where(b => b.IssuerCompanyId == issuerCompanyId && b.Iban == settings.BankAccountNumber)
                    .Select(b => (int?)b.Id)
                    .FirstOrDefault();
            }

            draft.IssuerBankAccountId = issuerBankAccountId;

            var group = paymentGroupId.HasValue
                ? db.InvoicingPaymentGroup.FirstOrDefault(g => g.Id == paymentGroupId.Value)
                : null;

            if (group?.VatTypeId is int vatTypeId)
                draft.SelectedVatTypeId = vatTypeId;

            var groupVat = group?.VatPercentage;
            var stageLines = new List<InvoiceLineBO>();
            var detailLines = new List<string>();
            string? headerUnitDescription = null;
            string? headerAddress = null;
            string? headerCity = project?.Postalcode?.Gemeente;

            foreach (var unit in units)
            {
                var unitName = unit.Unit?.Type != null
                    ? $"{unit.Unit.Type.Name} {unit.Unit.Name}".Trim()
                    : unit.Unit?.Name ?? string.Empty;

                var unitBaseValue = unit.Unit?.ConstructionValues?
                    .Where(v => v.ValueSold > 0 && v.PaymentGroupId == paymentGroupId)
                    .Sum(v => v.ValueSold ?? 0m) ?? 0m;

                if (unitBaseValue > 0)
                {
                    var ownerPortion = Math.Round(unitBaseValue * ownerPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                    detailLines.Add($"{ownerPercentage:0.##}% van de bouwwaarde van {unitName} : {ownerPortion:N2} €");

                    headerUnitDescription ??= unitName;
                    headerAddress ??= string.Join(" ", new[]
                    {
                        unit.Unit?.Street,
                        unit.Unit?.HouseNumber,
                        unit.Unit?.BusNumber
                    }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
                }

                foreach (var cv in unit.Unit?.ConstructionValues?.Where(v => v.ValueSold > 0 && v.PaymentGroupId == paymentGroupId) ?? Enumerable.Empty<UnitConstructionValueBO>())
                {
                    foreach (var stage in unit.PaymentStages.Where(s => groupedStageIds.Contains(s.Id) && s.GroupId == paymentGroupId))
                    {
                        var stagePercentage = stage.Percentage;
                        var price = Math.Round(
                            (cv.ValueSold ?? 0m) * ownerPercentage / 100m * stagePercentage / 100m,
                            2,
                            MidpointRounding.AwayFromZero);
                        var vatPct = stage.VatPercentage.HasValue && stage.VatPercentage.Value != 0
                            ? stage.VatPercentage.Value
                            : (groupVat ?? 21m);

                        stageLines.Add(new InvoiceLineBO
                        {
                            Text = $"{stage.Percentage:0.##}% - {stage.Name}",
                            Price = price,
                            VatPercentage = vatPct,
                            VatTypeId = draft.SelectedVatTypeId,
                            PaymentStageId = stage.Id,
                            GroupName = unitName,
                            LineType = "Stages",
                            UnitId = unit.Unit?.Id
                        });
                    }
                }
            }

            if (stageLines.Count > 0)
            {
                draft.Lines = stageLines;

                var unitDescriptor = headerUnitDescription ?? string.Empty;
                var projectPart = string.IsNullOrWhiteSpace(project?.Name) ? string.Empty : $" in project {project.Name}";
                var addressPart = string.IsNullOrWhiteSpace(headerAddress) ? string.Empty : $", {headerAddress}";
                var cityPart = string.IsNullOrWhiteSpace(headerCity) ? string.Empty : $" te {headerCity}";
                var headerText = $"Voor de bouwwaarde van {unitDescriptor}{projectPart}{addressPart}{cityPart} ingevolge verkoopsovereenkomst.".Trim();
                draft.HeaderDescription = headerText;
                draft.DetailDescription = string.Join("\n", detailLines);
            }

            return draft;
        }

        /// <summary>Bouwt het factuurvoorstel voor één partij (hoofdaccount óf één mede-eigenaar) op
        /// een selectie WO-details. <paramref name="ownerPercentage"/> is het aandeel van díe partij
        /// (100 min de mede-eigenaars-percentages voor het hoofdaccount, anders
        /// <c>ClientContacts.CoOwnerPercentage</c>) — zelfde aandeel-logica als
        /// <see cref="BuildStageInvoiceDraft"/> voor schijven (Niels, 2026-09-29): de "restbedrag"-
        /// controle blijft op het volledige detail gebeuren (alle partijen samen mogen nooit meer dan
        /// het restbedrag factureren), maar elke partij krijgt enkel haar eigen aandeel als factuurregel.</summary>
        private static InvoiceDraftBO? BuildChangeOrderInvoiceDraft(
            int? issuerCompanyId,
            int? clientAccountId,
            int? clientContactId,
            decimal ownerPercentage,
            IEnumerable<ChangeOrderBO> changeOrders,
 IEnumerable<ClientAccountChangeOrderInvoiceBO> selectedRows,
            IDictionary<int, decimal> alreadyInvoicedByDetail,
            ProjectBO project)
        {
            if (!issuerCompanyId.HasValue || issuerCompanyId.Value <= 0)
                return null;
            if (ownerPercentage <= 0m)
                return null;

            var selectedByDetail = selectedRows
               .Where(x => x.ChangeOrderDetailId > 0)
               .GroupBy(x => x.ChangeOrderDetailId)
               .ToDictionary(g => g.Key, g => g.First());
            var lines = new List<InvoiceLineBO>();


            foreach (var order in changeOrders)
            {
                var detailRows = new List<(int DetailId, string Description, decimal Amount, decimal Vat)>();

                foreach (var detail in order.Details.Where(d => selectedByDetail.ContainsKey(d.Id)))
                {
                    var selection = selectedByDetail[detail.Id];
                    var percentage = Math.Min(100m, Math.Max(0m, selection.Percentage));
                    var totalAmount = detail.Totaal;
                    alreadyInvoicedByDetail.TryGetValue(detail.Id, out var alreadyInvoiced);
                    var remaining = Math.Round(totalAmount - alreadyInvoiced, 2, MidpointRounding.AwayFromZero);
                    if (remaining <= 0m)
                        continue;

                    var maxPercentage = totalAmount == 0m
                        ? 0m
                        : Math.Round((remaining / totalAmount) * 100m, 4, MidpointRounding.AwayFromZero);

                    if (percentage > maxPercentage)
                        percentage = maxPercentage;

                    var selectedAmount = Math.Round(totalAmount * (percentage / 100m), 2, MidpointRounding.AwayFromZero);
                    if (selectedAmount <= 0m)
                        continue;

                    if (selectedAmount > remaining)
                        selectedAmount = remaining;

                    var ownerAmount = Math.Round(selectedAmount * ownerPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                    if (ownerAmount <= 0m)
                        continue;

                    detailRows.Add((
                        DetailId: detail.Id,
                        Description: string.IsNullOrWhiteSpace(detail.Description) ? order.Description : detail.Description,
                        Amount: ownerAmount,
                        Vat: detail.VatPercentage ?? 21m));
                }

                var groupedByVat = detailRows
                    .GroupBy(r => r.Vat)
                    .Select(g => new
                    {
                        Vat = g.Key,
                        Total = g.Sum(x => x.Amount),
                        Details = g.ToList()
                    })
                    .ToList();

                foreach (var vatGroup in groupedByVat)
                {
                    var orderDescription = string.IsNullOrWhiteSpace(order.Description)
                        ? $"Wijzigingsopdracht #{order.Id}"
                        : order.Description;
                    var lineText = groupedByVat.Count == 1
                        ? orderDescription
                        : $"{orderDescription} ({vatGroup.Vat:0.##}% BTW)";

                    var detailIds = vatGroup.Details.Select(d => d.DetailId).Distinct().ToList();

                    lines.Add(new InvoiceLineBO
                    {
                        Text = lineText,
                        Price = vatGroup.Total,
                        VatPercentage = vatGroup.Vat,
                        LineType = "ChangeOrders",
                        GroupName = "Wijzigingsopdrachten",
                        ChangeOrderDetailId = detailIds.Count == 1 ? detailIds[0] : null
                    });
                }
            }

            if (lines.Count == 0)
                return null;

            var draft = new InvoiceDraftBO
            {
                IssuerCompanyId = issuerCompanyId.Value,
                InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                Mode = InvoiceMode.ChangeOrders,
                Lines = lines,
                ProjectId = project.Id
            };
            if (clientAccountId.HasValue)
                (draft.ClientType, draft.ClientId) = ((int)InvoicePartyType.ClientAccount, clientAccountId);
            else if (clientContactId.HasValue)
                (draft.ClientType, draft.ClientId) = ((int)InvoicePartyType.ClientContact, clientContactId);

            return draft;
        }

        //SHARED
        public void ChangeOrderFillInSelectList(ProjectChangeOrderAddUpdateModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var projectService = _projectService;
            var clientService = _clientService;

            var cresponse = clientService.GetClientAccountsByProjectIdForSelect(model.ProjectId);
            model.ClientAccounts = cresponse.Success
                ? cresponse.Values.OrderBy(c => c.Display).ToList()
                : model.ClientAccounts;

            var aresponse = projectService.GetProjectContractActivitiesForSelect(model.ProjectId);
            model.ProjectContractActivities = aresponse.Success ? aresponse.Values : model.ProjectContractActivities;
        }
        private void FillInAddSelectLists(ProjectModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            model.IssuerCompanies = GetIssuerCompanies();

            var cservice = _countryService;
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if (cresponse.Success && cresponse.Values is not null)
            {
                model.Countries = cresponse.Values;
                var defCountry = model.Countries.FirstOrDefault(m => m.Group == "19");
                if (defCountry != null)
                {
                    model.SelectedCountry = defCountry.ID;
                }
            }
        }

        private void FillInAddSelectListsDetailEdit(EditProjectDetail model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            model.IssuerCompanies = GetIssuerCompanies();

            var cservice = _countryService;
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if (cresponse.Success && cresponse.Values is not null)
            {
                model.Countries = cresponse.Values;
                var defCountry = model.Countries.FirstOrDefault(m => m.Group == "19");
                if (defCountry != null && model.SelectedCountry == 0)
                {
                    model.SelectedCountry = defCountry.ID;
                }
            }

            var service = _projectService;
            var response = service.GetStatusesForSelect();
            if (response.Success && response.Values is not null)
            {
                model.Statuses = response.Values;
            }

        }

        private List<ProjectIssuerCompanyOptionVM> GetIssuerCompanies()
        {
            var uow = _uow;
            return uow.IssuerCompanies.GetNoTracking()
                .OrderBy(i => i.Name)
                .Select(i => new ProjectIssuerCompanyOptionVM { Id = i.Id, Name = i.Name })
                .ToList();
        }

        private void FillInAvailableUsers(ProjectModel model)
        {
            model.AvailableUsers = _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new IdNameBO { ID = 0, Display = (u.Voornaam + " " + u.Familienaam).Trim(), Group = u.UserId })
                .ToList();
        }

        private void FillInAvailableUsers(EditProjectDetail model)
        {
            model.AvailableUsers = _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new IdNameBO { ID = 0, Display = (u.Voornaam + " " + u.Familienaam).Trim(), Group = u.UserId })
                .ToList();
        }

        private void FillInAvailableUsersForCoord(Models.Projecten.CoordinatieInstellingenVM model)
        {
            var internalUserIds = _db.PermissionPerUser.Select(p => p.UserId).Distinct();
            model.AvailableUsers = _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive && internalUserIds.Contains(u.Id))
                .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
                .Select(u => new IdNameBO { ID = 0, Display = (u.Voornaam + " " + u.Familienaam).Trim(), Group = u.UserId })
                .ToList();
        }

        private void SetCoordinatieBreadcrumb(int projectid, string projectName)
        {
            var idx = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projIdx = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = idx };
            var detail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName)
            {
                Parent = projIdx,
                RouteValues = new { projectid }
            };
            var coord = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailCoordinatie", "Projecten", "Coördinatie")
            {
                Parent = detail,
                RouteValues = new { projectid }
            };
            var instellingen = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("CoordinatieInstellingen", "Projecten", "Instellingen")
            {
                Parent = coord,
                RouteValues = new { projectid }
            };
            ViewData["BreadcrumbNode"] = instellingen;
        }

        private void FillInAddSelectListsDetail(ref ShowProjectDetail model)
        {
            // get the activities
            var cservice = _countryService;
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if ((cresponse.Success))
                model.Countries = cresponse.Values;
            var defCountry = model.Countries.Where(m => m.Group == "19").FirstOrDefault();
            if ((defCountry != null))
                model.SelectedCountry = defCountry.ID;
            // Get statuses
            var service = _projectService;
            var response = service.GetStatusesForSelect();
            if ((response.Success))
                model.Statuses = response.Values;
            model.SelectedStatus = model.Project.Status.Id;
        }
        public void IncommingInvoiceFillInSelectList(ProjectIncommingInvoiceAddUpdateModel model)
        {
            var service2 = _projectService;
            var aresponse = service2.GetProjectContractsForSelect(model.ProjectId);
            if (aresponse.Success)
            {
                model.ProjectContracts = aresponse.Values;
            }
        }
        private List<IdNameBO> GetSiteManagersForCompany(int? companyId)
        {
            if (!companyId.HasValue || companyId.Value <= 0)
                return new List<IdNameBO>();

            return _db.CompanyContacts
                .Where(c => c.CompanyId == companyId.Value)
                .OrderBy(c => c.ContactNaam)
                .ThenBy(c => c.ContactVoornaam)
                .Select(c => new IdNameBO
                {
                    ID = c.ContactId,
                    Display = (c.ContactNaam ?? string.Empty) + (string.IsNullOrWhiteSpace(c.ContactVoornaam) ? string.Empty : $" {c.ContactVoornaam}")
                })
                .ToList();
        }

        public string GetCompanyName(int companyid)
        {
            var pservice = _companyService;
            var presponse = pservice.GetCompanyNameById(companyid);
            return presponse;
        }

        /// <summary>
        /// Aantal leveranciers voor het inner-menu "Leveranciers"-teller: zelfde telling als
        /// DetailContracts' eigen SupplierRows-opbouw (één rij per bedrijf met een contract, plus
        /// bedrijven die enkel facturen hebben zonder contract) — hier hertelt zonder de rijen zelf
        /// op te bouwen, enkel het aantal.
        /// </summary>
        private int GetProjectSupplierCount(int projectid, List<ContractBO> allContracts)
        {
            var invoiceSummaryResponse = _projectService.GetProjectIncommingInvoiceCompanySummaries(projectid);
            var invoiceSummaries = invoiceSummaryResponse.Success ? invoiceSummaryResponse.Values : new List<CompanyInvoiceSummaryBO>();
            var contractCompanyIds = (allContracts ?? new List<ContractBO>())
                .Where(c => c.Company != null)
                .Select(c => c.Company.ID)
                .ToHashSet();
            return contractCompanyIds.Count + invoiceSummaries.Count(s => s.Company != null && !contractCompanyIds.Contains(s.Company.ID));
        }

        /// <summary>
        /// Koppelt de leverancier van een contract aan het juiste facturatiebedrijf (IssuerCompany).
        /// Doelbedrijf = het facturatiebedrijf-bouwheer van het project (<see cref="Project.IssuerCompanyIdBuilder"/>),
        /// of bij coördinatieprojecten <see cref="Project.CoordinationIssuerCompanyId"/>. Is er geen,
        /// dan valt het terug op het als "externe standaard" gemarkeerde facturatiebedrijf.
        /// </summary>
        private void EnsureSupplierIssuerLink(int companyId, int projectId)
        {
            if (companyId <= 0 || projectId <= 0)
                return;

            try
            {
                var project = _db.Project
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => new
                    {
                        p.IssuerCompanyIdBuilder,
                        p.CoordinationIssuerCompanyId,
                        p.IsCoordinationProject,
                        p.IsOnlyCoordinationProject
                    })
                    .FirstOrDefault();
                if (project == null)
                    return;

                int? targetIssuerId = project.IssuerCompanyIdBuilder
                    ?? ((project.IsCoordinationProject || project.IsOnlyCoordinationProject) ? project.CoordinationIssuerCompanyId : null)
                    ?? _db.IssuerCompany
                        .Where(i => i.IsExternalCoordinationDefault && i.IsActive)
                        .Select(i => (int?)i.Id)
                        .FirstOrDefault();

                if (targetIssuerId is null or <= 0)
                {
                    _logger?.LogWarning(
                        "EnsureSupplierIssuerLink: geen doel-IssuerCompany voor project {ProjectId} (geen bouwheer-facturatiebedrijf en geen 'externe standaard' ingesteld).",
                        projectId);
                    return;
                }

                var alreadyLinked = _db.CompanyIssuerCompany
                    .Any(l => l.CompanyId == companyId && l.IssuerCompanyId == targetIssuerId.Value);
                if (alreadyLinked)
                    return;

                _db.CompanyIssuerCompany.Add(new CompanyIssuerCompany
                {
                    CompanyId = companyId,
                    IssuerCompanyId = targetIssuerId.Value
                });
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                // De contract-opslag zelf mag hier niet op vastlopen.
                _logger?.LogError(ex,
                    "EnsureSupplierIssuerLink mislukt voor leverancier {CompanyId} / project {ProjectId}",
                    companyId, projectId);
            }
        }

    }
}
