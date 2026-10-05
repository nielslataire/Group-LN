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
    /// <summary>Leveranciers/contracten van een project: contractoverzicht en -detail, toevoegen/bewerken, bijbestellingen, nacalculatie en printlijsten, calculatie-instellingen. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Contracts/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL CONTRACTEN ==========
        [HttpGet]
        //[Breadcrumb("Leveranciers")]
        [Breadcrumb("Leveranciers", FromAction = "Detail")]
        public ActionResult DetailContracts(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new DetailContractsModel();
            var service = _projectService;
            var response = service.GetProjectContracts(projectid);

            if (response.Success)
            {
                model.Contracts = response.Values;
            }

            var invoiceSummaryResponse = service.GetProjectIncommingInvoiceCompanySummaries(projectid);
            var invoiceSummaries = invoiceSummaryResponse.Success ? invoiceSummaryResponse.Values : new List<CompanyInvoiceSummaryBO>();
            var contractCompanyIds = model.Contracts
                .Where(c => c.Company != null)
                .Select(c => c.Company.ID)
                .ToHashSet();

            var rows = new List<ContractSupplierRowModel>();

            // Groepeer contracten per leverancier (één rij per bedrijf)
            var contractsByCompany = model.Contracts
                .Where(c => c.Company != null)
                .GroupBy(c => c.Company.ID)
                .ToList();

            foreach (var group in contractsByCompany)
            {
                var company = group.First().Company;
                var summary = invoiceSummaries.FirstOrDefault(s => s.Company?.ID == company.ID);
                rows.Add(new ContractSupplierRowModel
                {
                    Contracts = group.ToList(),
                    Company = company,
                    TotalInvoiced = summary?.TotalInvoiced ?? 0
                });
            }

            foreach (var summary in invoiceSummaries.Where(s => s.Company != null && !contractCompanyIds.Contains(s.Company.ID)))
            {
                rows.Add(new ContractSupplierRowModel
                {
                    Contracts = new List<ContractBO>(),
                    Company = summary.Company,
                    TotalInvoiced = summary.TotalInvoiced
                });
            }

            model.SupplierRows = rows.OrderBy(r => r.Company?.Display).ToList();


            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            // DESIGN.md regel 2: het kruimelpad mag deze pagina zelf nooit als laatste item vermelden
            // (dat zou de titel dupliceren) — stopt daarom bij het project, niet bij een "Leveranciers"-
            // blad. Diepere pagina's (DetailContract/DetailSupplier/EditContract) bouwen intern nog wel
            // hun eigen "Leveranciers"-tussenknoop (met deze Action/RouteValues) als ECHTE voorouder —
            // dat is geen fout, enkel deze actie s' eigen blad mag het niet meer zijn.
            ViewData["BreadcrumbNode"] = projectDetail;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectSuppliers = _ps.HasWrite(PermissionCodes.ProjectsSuppliers);
            ViewBag.CanDeleteProjectSuppliers = _ps.HasDelete(PermissionCodes.ProjectsSuppliers);
            SetPageHeader("ph ph-hard-hat", $"{model.ProjectName} - Leveranciers");
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailContractsV2" : "DetailContracts", model);
        }

        [HttpGet]
        [Breadcrumb("Leverancier detail", FromAction = "DetailContracts")]
        public ActionResult DetailContract(int projectid, int contractid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectContractDetailModel();
            var projectService = _projectService;
            var companyService = _companyService;

            model.ProjectId = projectid;
            model.ProjectName = projectService.GetProjectNameById(projectid);

            var contractResponse = projectService.GetContract(contractid);
            if (contractResponse.Success)
            {
                model.Contract = contractResponse.Value;
            }
            else
            {
                model.Contract = null;
            }

            // Alle contracten van dit project — nodig voor zowel deze leverancier se eigen
            // contractenlijst als de inner-menu "Leveranciers"-teller (GetProjectSupplierCount), zodat
            // dat laatste geen tweede round-trip naar dezelfde data vraagt.
            var allContractsResponse = projectService.GetProjectContracts(projectid);
            var allContracts = allContractsResponse.Success ? allContractsResponse.Values : new List<ContractBO>();

            var companyId = model.Contract?.Company?.ID ?? 0;
            if (companyId > 0)
            {
                var companyResponse = companyService.GetCompanyByID(companyId);
                if (companyResponse.Success)
                {
                    model.Company = companyResponse.Value;
                }

                var invoiceResponse = projectService.GetProjectIncommingInvoicesByCompany(projectid, companyId);
                if (invoiceResponse.Success)
                {
                    model.IncommingInvoices = invoiceResponse.Values
                        .OrderByDescending(m => m.IncommingInvoiceDate)
                        .ToList();
                }

                // Laad alle contracten van deze leverancier voor dit project
                model.Contracts = allContracts
                    .Where(c => c.Company?.ID == companyId)
                    .ToList();
            }

            model.HasContract = model.Contracts.Any();
            model.SupplierCount = GetProjectSupplierCount(projectid, allContracts);

            var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            // DESIGN.md regel 2: geen eigen blad hier — de titel (companyName) zou anders letterlijk
            // herhaald worden als laatste kruimel. Stopt bij "Leveranciers", een echte voorouder.
            ViewData["BreadcrumbNode"] = projectContracts;

            // gl-v2 (DetailContractV2): DetailContracts is de enige actie in deze groep die deze
            // permissievlaggen al zette — het legacy DetailContract.cshtml gebruikt ze wel (Bewerken/
            // Verwijderen-knoppen) maar kreeg ze hier nooit gevuld, dus die knoppen renderden op deze
            // pagina altijd als verborgen. Zelfde berekening als DetailContracts, nu ook hier.
            var _psDetail = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectSuppliers = _psDetail.HasWrite(PermissionCodes.ProjectsSuppliers);
            ViewBag.CanDeleteProjectSuppliers = _psDetail.HasDelete(PermissionCodes.ProjectsSuppliers);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - {model.Company?.Bedrijfsnaam ?? "Leverancier detail"}");
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailContractV2" : "DetailContract", model);
        }

        [HttpGet]
        [Breadcrumb("Leverancier detail", FromAction = "DetailContracts")]
        public ActionResult DetailSupplier(int projectid, int companyid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectContractDetailModel();
            var projectService = _projectService;
            var companyService = _companyService;

            model.ProjectId = projectid;
            model.ProjectName = projectService.GetProjectNameById(projectid);
            model.Contract = null;

            var companyResponse = companyService.GetCompanyByID(companyid);
            if (companyResponse.Success)
            {
                model.Company = companyResponse.Value;
            }

            var invoiceResponse = projectService.GetProjectIncommingInvoicesByCompany(projectid, companyid);
            if (invoiceResponse.Success)
            {
                model.IncommingInvoices = invoiceResponse.Values
                    .OrderByDescending(m => m.IncommingInvoiceDate)
                    .ToList();
            }

            // Laad alle contracten van dit project — voor deze leverancier se eigen contractenlijst
            // én (ongefilterd) voor de inner-menu "Leveranciers"-teller hieronder.
            var allContractsResponse = projectService.GetProjectContracts(projectid);
            var allContracts = allContractsResponse.Success ? allContractsResponse.Values : new List<ContractBO>();
            model.Contracts = allContracts
                .Where(c => c.Company?.ID == companyid)
                .ToList();
            model.HasContract = model.Contracts.Any();
            model.SupplierCount = GetProjectSupplierCount(projectid, allContracts);

            var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            // DESIGN.md regel 2: zelfde reden als DetailContract hierboven — geen eigen blad, dat zou
            // de titel (companyName) herhalen. Stopt bij "Leveranciers".
            ViewData["BreadcrumbNode"] = projectContracts;

            // gl-v2: zelfde ontbrekende-vlaggen-fix als DetailContract hierboven — deze actie deelt
            // exact dezelfde view (met of zonder een specifiek contractid binnengekomen).
            var _psSupplier = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectSuppliers = _psSupplier.HasWrite(PermissionCodes.ProjectsSuppliers);
            ViewBag.CanDeleteProjectSuppliers = _psSupplier.HasDelete(PermissionCodes.ProjectsSuppliers);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - {model.Company?.Bedrijfsnaam ?? "Leverancier detail"}");
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailContractV2" : "DetailContract", model);
        }

        [HttpGet]
        //[Breadcrumb("Nacalculatie")]
        [Breadcrumb("Nacalculatie", FromAction = "Detail")]
        public ActionResult Recalculation(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ProjectContractsModel model = new ProjectContractsModel();
            var service = _projectService;
            var aservice = _activityService;
            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);
            // Get Units
            var response = aservice.GetActivityGroups();
            model.ActivityGroups = response.Values;
            var response2 = service.GetProjectContracts(projectid);
            model.Contracts = response2.Values;
            var response3 = service.GetProjectBudget(projectid);
            model.BudgetActivities = response3.Values;
            var response4 = service.GetProjectIncommingInvoicesForRecalculation(projectid);
            model.IncommingInvoicesActivities = response4.Values;

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectRecalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectRecalc;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectCalculation = _ps.HasWrite(PermissionCodes.ProjectsPostCalculation);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Nacalculatie");
            return View(model);
        }
        [HttpGet]
        public IActionResult PrintRecalculation(int projectid)
        {
            // 1) Haal het model op zoals je Recalculation-view dat ook doet
            ProjectContractsModel model = new ProjectContractsModel();
            var service = _projectService;
            var aservice = _activityService;
            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);
            // Get Units
            var response = aservice.GetActivityGroups();
            model.ActivityGroups = response.Values;
            var response2 = service.GetProjectContracts(projectid);
            model.Contracts = response2.Values;
            var response3 = service.GetProjectBudget(projectid);
            model.BudgetActivities = response3.Values;
            var response4 = service.GetProjectIncommingInvoicesForRecalculation(projectid);
            model.IncommingInvoicesActivities = response4.Values;
            if (model == null)
                return NotFound();

            // 2) Logo laden
            byte[] logoBytes = null;
            var logoPath = Path.Combine(_env.WebRootPath, "Img", "logo.png");
            if (System.IO.File.Exists(logoPath))
                logoBytes = System.IO.File.ReadAllBytes(logoPath);

            // 3) Optioneel: Avenir registreren (indien TTF’s aanwezig)
            //    Plaats je TTF’s in: wwwroot/fonts/avenir/
            //    Pas bestandsnamen aan indien nodig.
            string fontFamily = null;

            var fontsRoot = Path.Combine(_env.WebRootPath, "fonts");
            var regular = Path.Combine(fontsRoot, "Avenir-Light.ttf");
            var bold = Path.Combine(fontsRoot, "Avenir-Heavy.ttf");
            var italic = Path.Combine(fontsRoot, "Avenir-LightOblique.ttf");
            var boldIt = Path.Combine(fontsRoot, "Avenir-HeavyOblique.ttf");


            try
            {
                if (System.IO.File.Exists(regular))
                    using (var stream = System.IO.File.OpenRead(regular))
                        FontManager.RegisterFont(stream);

                if (System.IO.File.Exists(bold))
                    using (var stream = System.IO.File.OpenRead(bold))
                        FontManager.RegisterFont(stream);

                if (System.IO.File.Exists(italic))
                    using (var stream = System.IO.File.OpenRead(italic))
                        FontManager.RegisterFont(stream);

                if (System.IO.File.Exists(boldIt))
                    using (var stream = System.IO.File.OpenRead(boldIt))
                        FontManager.RegisterFont(stream);

                fontFamily = "Avenir";   // moet exact overeenkomen met de internal name in het TTF
            }
            catch
            {
                // fallback naar default font
            }

            // 4) Document genereren (landscape + logo + fontFamily)
            var document = new RecalculationReportDocument(model, logoBytes, fontFamily);
            var pdfBytes = document.GeneratePdf();

            var safeProject = (model.ProjectName ?? "Project").Replace(Path.GetInvalidFileNameChars(), '_');
            var fileName = $"Nacalculatie_{safeProject}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        // Aannemerslijst (werf) als PDF in de Group LN-huisstijl: PROJECTFICHE + tabel met
        // aannemers/studiebureaus/nutspartijen per lot. De vijf statuskolommen worden via
        // query-parameters aan/uit gezet.
        [HttpGet]
        public IActionResult PrintSupplierList(int projectid, bool sent = true, bool signed = true,
            bool vgm = true, bool notification = true, bool pid = true)
        {
            var project = _db.Project
                .Include(p => p.Builder)
                .Include(p => p.Architect)
                .Include(p => p.Engineer)
                .Include(p => p.EpbReporter)
                .Include(p => p.SecurityCoordinator)
                .Include(p => p.AspNetUser)
                .Include(p => p.PostalCode)
                .Include(p => p.Units).ThenInclude(u => u.Type)
                .AsNoTracking()
                .FirstOrDefault(p => p.ProjectId == projectid);

            var projectName = project?.ProjectName ?? _projectService.GetProjectNameById(projectid);

            var contractsResponse = _projectService.GetProjectContracts(projectid);
            var contracts = contractsResponse.Success ? contractsResponse.Values : new List<ContractBO>();

            // Rauwe bedrijfs- en contactgegevens ophalen voor de betrokken contracten.
            var companyIds = contracts.Select(c => c.Company?.ID ?? 0).Where(id => id > 0).Distinct().ToList();
            var contactIds = contracts.Where(c => c.SiteManagerContactId.HasValue)
                .Select(c => c.SiteManagerContactId.Value).Distinct().ToList();
            var companies = _db.CompanyInfo.AsNoTracking()
                .Where(c => companyIds.Contains(c.CompanyId)).ToDictionary(c => c.CompanyId);
            var contacts = _db.CompanyContacts.AsNoTracking()
                .Where(c => contactIds.Contains(c.ContactId)).ToDictionary(c => c.ContactId);

            string FmtVat(string nr)
            {
                var d = new string((nr ?? "").Where(char.IsDigit).ToArray());
                return d.Length == 10 ? $"{d[..4]}.{d.Substring(4, 3)}.{d.Substring(7, 3)}" : (nr ?? "").Trim();
            }
            string CompanyAddress(DALCore.Models.CompanyInfo ci) => ci == null ? null : string.Join("\n",
                new[]
                {
                    string.Join(" ", new[] { ci.Straat, ci.Huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    string.Join(" ", new[] { ci.Postcode, ci.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)))
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var model = new SupplierListModel { ProjectId = projectid, ProjectName = projectName };

            foreach (var contract in contracts)
            {
                var ci = contract.Company != null && companies.TryGetValue(contract.Company.ID, out var c) ? c : null;
                var companyName = ci?.BedrijfsNaam ?? contract.Company?.Display ?? string.Empty;
                var vat = FmtVat(ci?.Ondernemingsnummer ?? ci?.VatNumber);
                var address = CompanyAddress(ci);

                string contactName = null, contactPhone = null, contactEmail = null;
                var isGeneral = true;
                if (contract.SiteManagerContactId.HasValue &&
                    contacts.TryGetValue(contract.SiteManagerContactId.Value, out var ct))
                {
                    contactName = string.Join(" ", new[] { ct.ContactVoornaam, ct.ContactNaam }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    contactPhone = string.IsNullOrWhiteSpace(ct.Gsm) ? ct.Telefoon : ct.Gsm;
                    contactEmail = ct.Email;
                    isGeneral = string.IsNullOrWhiteSpace(contactName) && string.IsNullOrWhiteSpace(contactPhone) && string.IsNullOrWhiteSpace(contactEmail);
                }
                if (isGeneral)
                {
                    contactPhone = string.IsNullOrWhiteSpace(ci?.Gsm) ? ci?.Telefoon1 : ci?.Gsm;
                    contactEmail = ci?.Email;
                }

                var activities = contract.Activities ?? new List<ContractActivityBO>();
                var buckets = activities.Count == 0
                    ? new List<(int Lot, string Name, int ActivityId, string Activity)> { (0, "ALGEMEEN", 0, "(geen lot gekoppeld)") }
                    : activities.Select(a => (
                        Lot: a.Activity?.Group?.Lot ?? 0,
                        Name: string.IsNullOrWhiteSpace(a.Activity?.Group?.Name) ? "ALGEMEEN" : a.Activity.Group.Name,
                        ActivityId: a.Activity?.ID ?? 0,
                        Activity: a.Activity?.Name ?? "(onbekende activiteit)")).ToList();

                foreach (var b in buckets)
                {
                    model.Rows.Add(new SupplierListRow
                    {
                        GroupLot = b.Lot,
                        GroupName = b.Name,
                        ActivityId = b.ActivityId,
                        ActivityName = b.Activity,
                        CompanyName = companyName,
                        Vat = vat,
                        Address = address,
                        ContactName = contactName,
                        ContactPhone = contactPhone,
                        ContactEmail = contactEmail,
                        ContactIsGeneral = isGeneral,
                        SentDate = contract.ContractSentDate,
                        SentNote = contract.ContractSentNote,
                        Signed = contract.ContractSigned,
                        Vgm = contract.VgmCharter,
                        Notification = contract.SiteNotification,
                        Pid = contract.PidAttest
                    });
                }
            }

            // Ontwerpteamrollen aanvullen uit de projectvelden wanneer er geen contractrij is.
            void AddTeamRow(string role, DALCore.Models.CompanyInfo ci)
            {
                if (ci == null) return;
                if (model.Rows.Any(r => string.Equals(r.ActivityName, role, StringComparison.OrdinalIgnoreCase))) return;
                model.Rows.Add(new SupplierListRow
                {
                    GroupLot = 0,
                    GroupName = "ALGEMEEN",
                    ActivityName = role,
                    CompanyName = ci.BedrijfsNaam,
                    Vat = FmtVat(ci.Ondernemingsnummer ?? ci.VatNumber),
                    Address = CompanyAddress(ci),
                    ContactPhone = string.IsNullOrWhiteSpace(ci.Gsm) ? ci.Telefoon1 : ci.Gsm,
                    ContactEmail = ci.Email,
                    ContactIsGeneral = true,
                    IsSynthesized = true
                });
            }
            AddTeamRow("Architect", project?.Architect);
            AddTeamRow("Ingenieur stabiliteit", project?.Engineer);
            AddTeamRow("EPB-verslaggever", project?.EpbReporter);
            AddTeamRow("Veiligheidscoördinatie", project?.SecurityCoordinator);

            model.Rows = model.Rows
                .OrderBy(r => r.GroupLot)
                .ThenBy(r => r.GroupName)
                .ThenBy(r => r.IsSynthesized)
                .ThenBy(r => r.ActivityName)
                .ThenBy(r => r.CompanyName)
                .ToList();

            // PROJECTFICHE
            string PostalLine(DALCore.Models.PostalCode pc) => pc == null ? null :
                string.Join(" ", new[] { pc.Postcode, pc.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var aard = string.Join(" · ", (project?.Units ?? new List<DALCore.Models.Units>())
                .Where(u => !u.IsOption)
                .GroupBy(u => u.Type?.Name ?? "eenheid")
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Count()}× {g.Key.ToLowerInvariant()}"));

            var pc2 = project?.PostalCode;
            var vc = project?.SecurityCoordinator;
            // Veiligheidscoördinator staat vaak enkel als contract/activiteit (Activiteit-ID 165) gekoppeld
            // en niet als apart projectveld ingevuld; val in dat geval terug op die contractrij voor de fiche.
            const int VeiligheidscoordinatieActivityId = 165;
            var vcRow = vc == null
                ? model.Rows.FirstOrDefault(r => !r.IsSynthesized && r.ActivityId == VeiligheidscoordinatieActivityId)
                : null;
            var bu = project?.Builder;
            var usr = project?.AspNetUser;
            model.Project = new SupplierListProjectInfo
            {
                ProjectName = projectName,
                AddressLine = string.Join(" ", new[] { project?.Street, project?.Number }.Where(s => !string.IsNullOrWhiteSpace(s))),
                CityLine = PostalLine(pc2),
                OpdrachtgeverName = bu?.BedrijfsNaam,
                OpdrachtgeverAddress = bu == null ? null : string.Join(" · ", new[]
                {
                    string.Join(", ", new[]
                    {
                        string.Join(" ", new[] { bu.Straat, bu.Huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        string.Join(" ", new[] { bu.Postcode, bu.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)))
                    }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    bu.Email
                }.Where(s => !string.IsNullOrWhiteSpace(s))),
                ProjectcoordinatieName = usr == null ? null : string.Join(" ", new[] { usr.Voornaam, usr.Familienaam }.Where(s => !string.IsNullOrWhiteSpace(s))),
                ProjectcoordinatiePhone = usr?.Gsm,
                ProjectcoordinatieEmail = usr?.Email,
                VeiligheidscoordinatorName = vc?.BedrijfsNaam ?? vcRow?.CompanyName,
                VeiligheidscoordinatorAddress = vc != null
                    ? string.Join(", ",
                        new[] { string.Join(" ", new[] { vc.Straat, vc.Huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s))),
                                string.Join(" ", new[] { vc.Postcode, vc.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s))) }
                        .Where(s => !string.IsNullOrWhiteSpace(s)))
                    : vcRow?.Address?.Replace("\n", ", "),
                VeiligheidscoordinatorEmail = vc?.Email ?? vcRow?.ContactEmail,
                AardVanDeWerken = string.IsNullOrWhiteSpace(aard) ? null : aard,
                StartDatumWerf = project?.StartDateConstruction,
                WerfmeldingDate = project?.WerfmeldingDate,
                WerfmeldingDossier = project?.WerfmeldingDossier,
                AantalPartijen = model.Rows.Select(r => r.CompanyName).Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                LaatstBijgewerktDoor = User.GetCpmDisplayName()
            };

            // Logo + Avenir-font (zoals PrintRecalculation)
            byte[] logoBytes = null;
            var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
            if (System.IO.File.Exists(logoPath))
                logoBytes = System.IO.File.ReadAllBytes(logoPath);

            string fontFamily = null;
            var fontsRoot = Path.Combine(_env.WebRootPath, "fonts");
            try
            {
                foreach (var f in new[]
                {
                    "Avenir-Roman.ttf", "Avenir-Medium.ttf", "Avenir-Heavy.ttf", "Avenir-Black.ttf",
                    "Avenir-Oblique.ttf", "Avenir-MediumOblique.ttf", "Avenir-HeavyOblique.ttf", "Avenir-BlackOblique.ttf"
                })
                {
                    var fp = Path.Combine(fontsRoot, f);
                    if (System.IO.File.Exists(fp))
                        using (var stream = System.IO.File.OpenRead(fp))
                            FontManager.RegisterFont(stream);
                }
                fontFamily = "Avenir";
            }
            catch { /* fallback naar default font */ }

            var columns = new SupplierListColumns(sent, signed, vgm, notification, pid);
            var document = new SupplierListDocument(model, columns, logoBytes, fontFamily);

            byte[] pdfBytes;
            try
            {
                pdfBytes = document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Aannemerslijst-PDF genereren mislukt voor project {ProjectId}", projectid);
                return StatusCode(500, "De aannemerslijst kon niet worden opgemaakt: " + ex.Message);
            }

            var safeProject = (projectName ?? "Project").Replace(Path.GetInvalidFileNameChars(), '_');
            var fileName = $"Aannemerslijst_{safeProject}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        public IActionResult PrintClientList(int projectid)
        {
            var project = _db.Project
                .Include(p => p.PostalCode)
                .Include(p => p.Builder)
                .Include(p => p.Units).ThenInclude(u => u.Type)
                .AsNoTracking()
                .FirstOrDefault(p => p.ProjectId == projectid);

            var projectName = project?.ProjectName ?? _projectService.GetProjectNameById(projectid);

            var clientsResponse = _clientService.GetClientAccountsByProjectIdWithUnits(projectid);
            var clientAccounts = clientsResponse.Success ? clientsResponse.Values : new List<ClientAccountWithUnitsBO>();

            string FormatAddress(string street, string housenumber, string busnumber, PostalCodeBO pc)
            {
                var line1 = string.Join(" ", new[] { street, housenumber }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(busnumber)) line1 = string.IsNullOrWhiteSpace(line1) ? $"bus {busnumber}" : $"{line1} bus {busnumber}";
                var line2 = pc == null ? null : string.Join(" ", new[] { pc.Postcode, pc.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return string.Join(", ", new[] { line1, line2 }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            // Zelfde volgorde als de eenheden-kolommen op Projecten/DetailClients: Wooneenheid, Commerciële
            // ruimte, Parkeergelegenheid, Berging, en dan de rest.
            var unitGroupOrder = new Dictionary<int, int> { [1] = 0, [4] = 1, [3] = 2, [2] = 3 };
            string FormatUnit(UnitBO u) => string.Join(" ", new[] { u.Type?.Name, u.Name?.ToUpperInvariant() }.Where(s => !string.IsNullOrWhiteSpace(s)));

            string ClientDisplayName(ClientAccountBO cl) => cl == null ? "—"
                : string.Join(" ", new[] { cl.Salutation.GetDisplayName(), cl.DisplayName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            string PersonDisplayName(ClientContactBO p) =>
                string.Join(" ", new[] { p.Salutation.GetDisplayName(), p.Firstname, p.Name }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var model = new ClientListModel { ProjectId = projectid, ProjectName = projectName };

            // Klanten sorteren zoals op Projecten/DetailClients: op naam van hun wooneenheid (lot).
            var sortedClientAccounts = clientAccounts.OrderBy(
                m => m.Units.Where(a => a.Type.GroupId == 1).Count() > 0 ? m.Units.Where(a => a.Type.GroupId == 1).FirstOrDefault().Name : "",
                new ServiceCore.Helpers.AlphanumComparator());

            foreach (var ca in sortedClientAccounts)
            {
                var client = ca.Client;
                var row = new ClientListRow
                {
                    ClientName = ClientDisplayName(client),
                    Address = FormatAddress(client?.Street, client?.Housenumber, client?.Busnumber, client?.Postalcode),
                    Units = (ca.Units ?? new List<UnitBO>())
                        .Where(u => !u.IsOption)
                        .OrderBy(u => unitGroupOrder.TryGetValue(u.Type?.GroupId ?? -1, out var ord) ? ord : int.MaxValue)
                        .Select(FormatUnit)
                        .ToList()
                };

                foreach (var contact in client?.Contacts ?? new List<ClientContactBO>())
                {
                    row.Contacts.Add(new ClientListPerson
                    {
                        Name = PersonDisplayName(contact),
                        Phone = contact.Phone,
                        Cellphone = contact.Cellphone,
                        Email = contact.Email
                    });
                }

                foreach (var owner in client?.CoOwners ?? new List<ClientContactBO>())
                {
                    row.CoOwners.Add(new ClientListPerson
                    {
                        Name = PersonDisplayName(owner),
                        Address = FormatAddress(owner.Street, owner.Housenumber, owner.Busnumber, owner.Postalcode),
                        Phone = owner.Phone,
                        Email = owner.Email
                    });
                }

                model.Rows.Add(row);
            }

            var bu = project?.Builder;
            model.Project = new ClientListProjectInfo
            {
                ProjectName = projectName,
                AddressLine = string.Join(" ", new[] { project?.Street, project?.Number }.Where(s => !string.IsNullOrWhiteSpace(s))),
                CityLine = project?.PostalCode == null ? null : string.Join(" ", new[] { project.PostalCode.Postcode, project.PostalCode.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s))),
                OpdrachtgeverName = bu?.BedrijfsNaam,
                OpdrachtgeverAddress = bu == null ? null : string.Join(", ", new[]
                {
                    string.Join(" ", new[] { bu.Straat, bu.Huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    string.Join(" ", new[] { bu.Postcode, bu.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)))
                }.Where(s => !string.IsNullOrWhiteSpace(s))),
                AantalKlanten = model.Rows.Count,
                AantalEenhedenTotaal = (project?.Units ?? new List<DALCore.Models.Units>()).Count(u => !u.IsOption),
                AantalEenhedenVerkocht = model.Rows.Sum(r => r.Units.Count),
                LaatstBijgewerktDoor = User.GetCpmDisplayName()
            };

            // Logo + Avenir-font (zoals PrintSupplierList)
            byte[] logoBytes = null;
            var logoPath = Path.Combine(_env.WebRootPath, "Img", "groupln-logo.png");
            if (System.IO.File.Exists(logoPath))
                logoBytes = System.IO.File.ReadAllBytes(logoPath);

            string fontFamily = null;
            var fontsRoot = Path.Combine(_env.WebRootPath, "fonts");
            try
            {
                foreach (var f in new[]
                {
                    "Avenir-Roman.ttf", "Avenir-Medium.ttf", "Avenir-Heavy.ttf", "Avenir-Black.ttf",
                    "Avenir-Oblique.ttf", "Avenir-MediumOblique.ttf", "Avenir-HeavyOblique.ttf", "Avenir-BlackOblique.ttf"
                })
                {
                    var fp = Path.Combine(fontsRoot, f);
                    if (System.IO.File.Exists(fp))
                        using (var stream = System.IO.File.OpenRead(fp))
                            FontManager.RegisterFont(stream);
                }
                fontFamily = "Avenir";
            }
            catch { /* fallback naar default font */ }

            var document = new ClientListDocument(model, logoBytes, fontFamily);

            byte[] pdfBytes;
            try
            {
                pdfBytes = document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Klantenlijst-PDF genereren mislukt voor project {ProjectId}", projectid);
                return StatusCode(500, "De klantenlijst kon niet worden opgemaakt: " + ex.Message);
            }

            var safeClientProject = (projectName ?? "Project").Replace(Path.GetInvalidFileNameChars(), '_');
            var clientFileName = $"Klantenlijst_{safeClientProject}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", clientFileName);
        }

        [HttpGet]
        public IActionResult ExportInvoicesExcel(int projectid)
        {
            var invoices = _projectService
                .GetProjectIncommingInvoicesForRecalculation(projectid)
                .Values;

            // Groepeer op InvoiceId → één rij per factuur, gesorteerd op datum (oud → nieuw)
            var rows = invoices
                .GroupBy(x => x.InvoiceId)
                .Select(g =>
                {
                    var first = g.First();
                    var omschrijvingen = g
                        .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                        .Select(x => x.Description.Trim())
                        .Distinct()
                        .ToList();
                    return new
                    {
                        Datum        = first.Invoicedate,
                        Leverancier  = first.Company?.Display ?? "",
                        TotaalPrijs  = g.Sum(x => x.Price),
                        Omschrijving = omschrijvingen.Any() ? string.Join(" / ", omschrijvingen) : "",
                        ExterneRef   = first.ExternalInvoiceId ?? "",
                    };
                })
                .OrderBy(x => x.Datum)
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Inkomende facturen");

            // Koptekst
            ws.Cell(1, 1).Value = "Factuurdatum";
            ws.Cell(1, 2).Value = "Leverancier";
            ws.Cell(1, 3).Value = "Totaalprijs factuur";
            ws.Cell(1, 4).Value = "Omschrijving";
            ws.Cell(1, 5).Value = "Externe ref.";

            var headerRange = ws.Range("A1:E1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // Data
            int rij = 2;
            foreach (var row in rows)
            {
                ws.Cell(rij, 1).Value = row.Datum.ToDateTime(TimeOnly.MinValue);
                ws.Cell(rij, 1).Style.NumberFormat.Format = "dd/MM/yyyy";
                ws.Cell(rij, 2).Value = row.Leverancier;
                ws.Cell(rij, 3).Value = (double)row.TotaalPrijs;
                ws.Cell(rij, 3).Style.NumberFormat.Format = "€ #,##0.00";
                ws.Cell(rij, 4).Value = row.Omschrijving;
                ws.Cell(rij, 5).Value = row.ExterneRef;
                rij++;
            }

            // Kolombreedte automatisch + minimum voor datum en bedrag
            ws.Columns().AdjustToContents();
            if (ws.Column(1).Width < 14) ws.Column(1).Width = 14;
            if (ws.Column(3).Width < 20) ws.Column(3).Width = 20;

            // Randen rond het datagebied
            if (rows.Any())
            {
                ws.Range(1, 1, rij - 1, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(1, 1, rij - 1, 5).Style.Border.InsideBorder  = XLBorderStyleValues.Hair;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            var projectName = _projectService.GetProjectNameById(projectid) ?? "Project";
            var safeName    = projectName.Replace(Path.GetInvalidFileNameChars(), '_');
            var fileName    = $"Inkomende_facturen_{safeName}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet]
        [Breadcrumb("Nacalculatie Detail", FromAction = "Recalculation")]
        //[Breadcrumb("Nacalculatie detail")]
        public ActionResult RecalculationDetail(int projectId, int activityId, int groupid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectRecalculationDetailModel();
            var projectService = _projectService;
            var activityService = _activityService;

            model.ProjectId = projectId;
            model.ActivityID = activityId;
            model.GroupID = groupid;
            model.ProjectName = projectService.GetProjectNameById(projectId);

            //var node = SiteMaps.Current.CurrentNode;
            //if (node?.ParentNode?.ParentNode?.ParentNode != null)
            //{
            //    node.ParentNode.ParentNode.Title = projectService.GetProjectNameById(model.ProjectId);
            //}

            var activityResponse = activityService.GetActivitybyId(activityId);
            model.Activity = activityResponse.Value;

            var groupResponse = activityService.GetActivityGroups();
            model.ActivityGroups = groupResponse.Values;
            //if (node != null)
            //{
            //    node.Title = model.Activity.Name;
            //}

            var invoicesResponse = projectService.GetProjectIncommingInvoicesByGroup(projectId, groupid);
            model.IncommingInvoicesActivities = invoicesResponse.Values;
            var response2 = projectService.GetProjectContracts(projectId);
            model.Contracts = response2.Values;
            var response3 = projectService.GetProjectBudget(projectId);
            model.BudgetActivities = response3.Values;

            var contractsResponse = projectService.GetProjectContractsWithoutInvoices(projectId, activityId);
            model.ContractsWithoutInvoices = contractsResponse.Values;

            var contractActivitiesResponse = projectService.GetProjectContractActivitiesByActivityId(projectId, activityId);
            if (contractActivitiesResponse.Success)
            {
                model.ContractActivities = contractActivitiesResponse.Values;
            }

            string breadcrumbTitle;
                var group = model.ActivityGroups?.FirstOrDefault(g => g.ID == groupid);
                breadcrumbTitle = group != null
                    ? group.Name
                    : "Groep";

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectId }
            };
            var projectRecalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectId }
            };
            var projectRecalcAct = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("RecalculationDetail", "Projecten", breadcrumbTitle)
            {
                Parent = projectRecalc,
                RouteValues = new {
                    projectId = projectId,
                    activityId = activityId,
                    groupid = groupid
                }
            };
            ViewData["BreadcrumbNode"] = projectRecalcAct;

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Nacalculatie - {breadcrumbTitle}");
            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Contract toevoegen", FromAction = "DetailContracts")]
        //[Breadcrumb("Contract toevoegen")]
        public ActionResult AddContract(int projectid, int contractid = 0, int companyid = 0)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            //Sidebar collapse
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            //Fill model
            ProjectAddContractModel model = new ProjectAddContractModel();
            var service = _projectService;
            model.ProjectId = projectid;

            if (contractid == 0)
            {
                model.Contract.ProjectId = projectid;
                model.Contract.GuaranteeType = ContractGuaranteeType.NoGuarantee;

                // Vanuit de leverancierslijst (+ "Contract toevoegen" bij een bedrijf)
                // komt companyid mee: het nieuwe contract meteen aan diezelfde
                // leverancier koppelen zodat er niet opnieuw gezocht moet worden.
                if (companyid > 0)
                {
                    model.Contract.Company.ID = companyid;
                    model.Contract.Company.Display = _companyService.GetCompanyNameById(companyid);
                }
            }
            else
            {
                var cresponse = service.GetContract(contractid);
                if (cresponse.Success)
                    model.Contract = cresponse.Value;
            }
            model.ProjectName = service.GetProjectNameById(projectid);

            model.Insurance.Startdate = DateOnly.FromDateTime(DateTime.Now);
            var iservice = _insuranceService;
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;

            PopulateAddContractLookups(model);

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var projectContractsAdd = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddContract", "Projecten", "Toevoegen")
            {
                Parent = projectContracts
            };
            if (ViewData["UseGlV2Layout"] as bool? == true)
            {
                // DESIGN.md (One-Line Topbar Rule + Clickable-Crumb Rule): de titel "Contract toevoegen" wordt niet
                // herhaald in het kruimelpad; het pad stopt bij "Leveranciers" en die laatste kruimel blijft een link.
                ViewData["BreadcrumbNode"] = projectContracts;
                ViewData["BreadcrumbLastIsLink"] = true;
            }
            else ViewData["BreadcrumbNode"] = projectContractsAdd;

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Contract toevoegen");
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "AddContractV2" : "AddContract", model);
        }
        [HttpPost]
        public async Task<ActionResult> AddContract(ProjectAddContractModel model, List<ContractActivityBO> activities, List<ContractAdditionalOrderBO> additionalorders, IFormFile? guaranteeDoc)
        {
            model.SiteManagers = GetSiteManagersForCompany(model.Contract.Company.ID);
            var addAnother = Request.Form["saveAction"] == "addAnother";

            // model.ProjectId werd vroeger niet altijd mee gepost; val terug op het contract zelf.
            var projectId = model.ProjectId > 0 ? model.ProjectId : (model.Contract?.ProjectId ?? 0);
            model.ProjectId = projectId;
            if (model.Contract != null && model.Contract.ProjectId <= 0)
                model.Contract.ProjectId = projectId;

            // Bankwaarborg-document (pdf/jpg/jpeg) — optioneel, maar indien meegestuurd valideren.
            if (guaranteeDoc is { Length: > 0 } && !ValidateGuaranteeDoc(guaranteeDoc, out var guaranteeDocError))
                ModelState.AddModelError("guaranteeDoc", guaranteeDocError);

            // Server-vangnet voor het Foutoverzicht (DESIGN.md punt 24): dezelfde regels als de pagina-JS.
            if (model.Contract?.Company == null || model.Contract.Company.ID <= 0)
                ModelState.AddModelError("Contract.Company.ID", "Kies een leverancier.");
            if (!(activities?.Any() ?? false))
                ModelState.AddModelError("Activities", "Voeg minstens één lot toe.");

            if (!ModelState.IsValid)
            {
                ViewData["GlV2ErrorLocations"] = new Dictionary<string, string>
                {
                    ["Contract.Company.ID"] = "Algemeen · Leverancier",
                    ["Activities"] = "Loten & bijbestellingen",
                };
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();
                AddMessage("error", firstError ?? "Controleer de ingevulde gegevens.", "Validatiefout");
                SetPageHeader("bx bx-building-house", $"{(string.IsNullOrWhiteSpace(model.ProjectName) ? _projectService.GetProjectNameById(projectId) : model.ProjectName)} - Contract toevoegen");
                PopulateAddContractLookups(model);
                return View(ViewData["UseGlV2Layout"] as bool? == true ? "AddContractV2" : "AddContract", model);
            }

            if (guaranteeDoc is { Length: > 0 })
            {
                var storedName = await UploadAssetToStorageAsync(guaranteeDoc, "guarantees");
                if (string.IsNullOrWhiteSpace(storedName))
                {
                    AddMessage("error", "Het waarborgdocument kon niet naar de storage geüpload worden.", "Fout!");
                    SetPageHeader("bx bx-building-house", $"{(string.IsNullOrWhiteSpace(model.ProjectName) ? _projectService.GetProjectNameById(projectId) : model.ProjectName)} - Contract toevoegen");
                    PopulateAddContractLookups(model);
                    return View(ViewData["UseGlV2Layout"] as bool? == true ? "AddContractV2" : "AddContract", model);
                }
                model.Contract.GuaranteeDocFilename = storedName;
                model.Contract.GuaranteeDocUploadedAt = DateTime.Now;
            }
            else if (model.Contract.Id > 0)
            {
                // Geen nieuw bestand: bestaande waarborgdoc-gegevens behouden (die staan niet in het formulier).
                var existing = _projectService.GetContract(model.Contract.Id);
                if (existing.Success && existing.Value != null)
                {
                    model.Contract.GuaranteeDocFilename = existing.Value.GuaranteeDocFilename;
                    model.Contract.GuaranteeDocUploadedAt = existing.Value.GuaranteeDocUploadedAt;
                }
            }

            // Na opslaan altijd terug naar de leverancierslijst van het juiste project
            // (de oude "ga terug naar Referer"-logica landde bij een verloren project op een lege lijst).
            var backToList = Url.Action("DetailContracts", "Projecten", new { projectid = projectId });

            foreach (var contractactivity in activities ?? Enumerable.Empty<ContractActivityBO>())
            {
                if (contractactivity.Activity.ID == 142 && contractactivity.ContractId == 0)
                {
                    InsuranceBO i = new InsuranceBO();
                    i.Startdate = DateOnly.FromDateTime(DateTime.Now);
                    contractactivity.InsuranceData = i;
                }
                model.Contract.Activities.Add(contractactivity);
            }

            var service = _projectService;
            var response = service.InsertUpdateProjectContract(model.Contract);
            if (response.Success)
            {
                EnsureSupplierIssuerLink(model.Contract.Company.ID, projectId);
                AddMessage("success", "Het contract is toegevoegd aan het project " + model.ProjectName, "Geslaagd!");
                if (addAnother)
                {
                    return RedirectToAction(nameof(AddContract), new { projectid = projectId });
                }
                return Redirect(backToList);
            }
            else
            {
                var serviceError = response.Messages?.FirstOrDefault(m => m.Type == MessageType.Error)?.Message
                    ?? "Het contract is NIET toegevoegd aan het project " + model.ProjectName;
                AddMessage("error", serviceError, "Fout!");
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Contract toevoegen");
                PopulateAddContractLookups(model);
                return View(ViewData["UseGlV2Layout"] as bool? == true ? "AddContractV2" : "AddContract", model);
            }
        }

        private void PopulateAddContractLookups(ProjectAddContractModel model)
        {
            model.AllActivities = _db.Activity
                .Select(a => new ActivityFilterItemViewModel
                {
                    Id = a.ActivityId,
                    Name = a.Omschrijving,
                    GroupName = a.Group != null ? a.Group.Name : null
                })
                .OrderBy(a => a.GroupName)
                .ThenBy(a => a.Name)
                .ToList();

            model.LegalForms = _db.CompanyLegalForm
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .Select(l => new IdNameBO { ID = l.Id, Display = l.Name })
                .ToList();

            var countriesResponse = _countryService.GetVisibleCountriesForSelect();
            if (countriesResponse.Success)
                model.Countries = countriesResponse.Values;

            // Lotenkiezer: de activiteiten van de gekozen leverancier (leeg zolang er geen leverancier is).
            var companyId = model.Contract?.Company?.ID ?? 0;
            var companyActivities = new List<IdNameBO>();
            if (companyId > 0)
            {
                var activitiesResponse = _companyService.GetCompanyActivities(companyId);
                if (activitiesResponse.Success)
                    companyActivities = activitiesResponse.Values.Select(a => new IdNameBO { ID = a.ID, Display = a.Name, Group = "-Bedrijfsactiviteit-" }).ToList();
            }
            model.Activities = companyActivities;
            model.SiteManagers = GetSiteManagersForCompany(companyId);

            // Inner-menu "Leveranciers"-teller — op elk redisplay-pad opnieuw gevuld (zelfde
            // discipline als Klanten/EditProject se ProjectClientCount via FillInAddSelectListsEdit).
            var contractsResponse = _projectService.GetProjectContracts(model.ProjectId);
            model.SupplierCount = GetProjectSupplierCount(model.ProjectId, contractsResponse.Success ? contractsResponse.Values : new List<ContractBO>());
        }
        [HttpGet]
        //[Breadcrumb("Contract bewerken")]
        [Breadcrumb("Contract bewerken", FromAction = "DetailContracts")]
        public ActionResult EditContract(int projectid, int contractid = 0)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            //Sidebar collapse
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            //Fill model
            ProjectAddContractModel model = new ProjectAddContractModel();
            var service = _projectService;
            model.ProjectId = projectid;

            if (contractid == 0)
            {
                model.Contract.ProjectId = projectid;
                model.Contract.GuaranteeType = ContractGuaranteeType.NoGuarantee;
            }
            else
            {
                var cresponse = service.GetContract(contractid);
                if (cresponse.Success)
                    model.Contract = cresponse.Value;
            }
            model.ProjectName = service.GetProjectNameById(projectid);
            var pservice = _companyService;
            var presponse = pservice.GetCompanyActivities(model.Contract.Company.ID);
            var activitiesList = new List<IdNameBO>();

            if (presponse.Success)
            {
                foreach (var selectedActivity in presponse.Values)
                {
                    var singleActivity = new IdNameBO
                    {
                        ID = selectedActivity.ID,
                        Display = selectedActivity.Name,
                        Group = "-Bedrijfsactiviteit-"
                    };
                    activitiesList.Add(singleActivity);
                }
            }
            model.Activities = activitiesList;
            model.Insurance.Startdate = DateOnly.FromDateTime(DateTime.Now);
            model.SiteManagers = GetSiteManagersForCompany(model.Contract.Company.ID);
            var iservice = _insuranceService;
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;

            // Inner-menu "Leveranciers"-teller — zelfde telling als DetailContracts/DetailContractV2.
            var allContractsResponse = service.GetProjectContracts(projectid);
            model.SupplierCount = GetProjectSupplierCount(projectid, allContractsResponse.Success ? allContractsResponse.Values : new List<ContractBO>());

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var supplierName = string.IsNullOrWhiteSpace(model.Contract.Company?.Display) ? "Leverancier" : model.Contract.Company.Display;
            var supplierDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContract", "Projecten", supplierName)
            {
                Parent = projectContracts,
                RouteValues = new { projectid = projectid, contractid = contractid }
            };
            // DESIGN.md regel 2: geen eigen "Contract bewerken"-blad — dat zou de titel
            // ("Contract bewerken — " + companyName) herhalen. Stopt bij de leverancier zelf.
            ViewData["BreadcrumbNode"] = supplierDetail;

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Contract bewerken");
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditContractV2" : "EditContract", model);
        }
        [HttpPost]
        public async Task<ActionResult> EditContract(ProjectAddContractModel model, List<ContractActivityBO> activities, List<ContractAdditionalOrderBO> additionalorders, IFormFile? guaranteeDoc)
        {
            model.SiteManagers = GetSiteManagersForCompany(model.Contract.Company.ID);

            var projectId = model.ProjectId > 0 ? model.ProjectId : (model.Contract?.ProjectId ?? 0);
            model.ProjectId = projectId;
            if (model.Contract != null && model.Contract.ProjectId <= 0)
                model.Contract.ProjectId = projectId;

            // Bankwaarborg-document (pdf/jpg/jpeg) — optioneel, maar indien meegestuurd valideren.
            if (guaranteeDoc is { Length: > 0 } && !ValidateGuaranteeDoc(guaranteeDoc, out var guaranteeDocError))
                ModelState.AddModelError("guaranteeDoc", guaranteeDocError);

            if (!(activities?.Any() ?? false))
                ModelState.AddModelError("Activities", "Voeg minstens één lot toe.");

            if (!ModelState.IsValid)
            {
                ViewData["GlV2ErrorLocations"] = new Dictionary<string, string> { ["Activities"] = "Loten & bijbestellingen" };
                var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
                AddMessage("error", firstError ?? "Controleer de ingevulde gegevens.", "Validatiefout");
                SetPageHeader("bx bx-building-house", $"{(string.IsNullOrWhiteSpace(model.ProjectName) ? _projectService.GetProjectNameById(projectId) : model.ProjectName)} - Contract bewerken");
                PopulateAddContractLookups(model);
                return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditContractV2" : "EditContract", model);
            }

            // Bestaande waarborgdoc-gegevens (staan niet in het formulier) — nodig om te
            // behouden bij geen upload, en om het oude bestand op te ruimen bij een vervanging.
            string? oldGuaranteeFile = null;
            if (model.Contract.Id > 0)
            {
                var existing = _projectService.GetContract(model.Contract.Id);
                if (existing.Success && existing.Value != null)
                {
                    oldGuaranteeFile = existing.Value.GuaranteeDocFilename;
                    model.Contract.GuaranteeDocFilename = existing.Value.GuaranteeDocFilename;
                    model.Contract.GuaranteeDocUploadedAt = existing.Value.GuaranteeDocUploadedAt;
                }
            }

            if (guaranteeDoc is { Length: > 0 })
            {
                var storedName = await UploadAssetToStorageAsync(guaranteeDoc, "guarantees");
                if (string.IsNullOrWhiteSpace(storedName))
                {
                    AddMessage("error", "Het waarborgdocument kon niet naar de storage geüpload worden.", "Fout!");
                    SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Contract bewerken");
                    PopulateAddContractLookups(model);
                    return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditContractV2" : "EditContract", model);
                }
                model.Contract.GuaranteeDocFilename = storedName;
                model.Contract.GuaranteeDocUploadedAt = DateTime.Now;
            }

            var backToList = Url.Action("DetailContracts", "Projecten", new { projectid = projectId });
            foreach (var contractactivity in activities ?? Enumerable.Empty<ContractActivityBO>())
            {
                if (contractactivity.Activity.ID == 142 && contractactivity.ContractId == 0)
                {
                    InsuranceBO i = new InsuranceBO();
                    i.Startdate = DateOnly.FromDateTime(DateTime.Now);
                    contractactivity.InsuranceData = i;
                }
                model.Contract.Activities.Add(contractactivity);
            }

            var service = _projectService;
            var response = service.InsertUpdateProjectContract(model.Contract);
            if (response.Success)
            {
                // Vervangen waarborgdocument: het oude bestand uit de storage opruimen.
                if (!string.IsNullOrWhiteSpace(oldGuaranteeFile)
                    && !string.Equals(oldGuaranteeFile, model.Contract.GuaranteeDocFilename, StringComparison.OrdinalIgnoreCase))
                {
                    await DeleteGuaranteeDocFromStorageAsync(oldGuaranteeFile);
                }

                EnsureSupplierIssuerLink(model.Contract.Company.ID, projectId);
                AddMessage("success", "Het contract is bijgewerkt voor project " + model.ProjectName, "Geslaagd!");
                return Redirect(backToList);
            }

            AddMessage("error", "Het contract is NIET bijgewerkt voor project " + model.ProjectName, "Fout!");
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Contract bewerken");
            PopulateAddContractLookups(model);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditContractV2" : "EditContract", model);
        }
        [HttpGet]
        public ActionResult ModalDeleteContract(int id)
        {
            var viewModel = new ContractBO();

            if (id != 0)
            {
                var dservice = _projectService;
                var response = dservice.GetContract(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                    ViewBag.CompanyName = GetCompanyName(viewModel.Company.ID);
                }
            }

            ViewBag.CanDelete = id == 0 || !_projectService.ContractHasLinkedData(id);
            return PartialView("_ModalDeleteContract", viewModel);
        }
        [HttpGet]
        public ActionResult ModalDeleteContractV2(int id)
        {
            var viewModel = new ContractBO();

            if (id != 0)
            {
                var dservice = _projectService;
                var response = dservice.GetContract(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                    ViewBag.CompanyName = GetCompanyName(viewModel.Company.ID);
                }
            }

            ViewBag.CanDelete = id == 0 || !_projectService.ContractHasLinkedData(id);
            return PartialView("Modals/_ModalDeleteContractV2", viewModel);
        }
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsSuppliers)]
        public ActionResult DeleteContract(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = _projectService;
                var ids = new List<int> { id };
                var response = service.DeleteContracts(ids);

                if (response.Success)
                {
                    AddMessage("success", "Het contract is verwijderd", "Geslaagd!");
                    return RedirectToAction("DetailContracts", "Projecten", new { projectid = projectid });
                }
                else
                {
                    AddMessage("error", "Het contract is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                    return RedirectToAction("DetailContracts", "Projecten", new { projectid = projectid });
                }
            }

            return RedirectToAction("DetailContracts", "Projecten", new { projectid = projectid });
        }

        public ActionResult GetSubType(int id)
        {
            List<SelectListItem> items = new List<SelectListItem>();

            var service2 = _unitService;

            var responselevels = service2.GetUnitTypesByGroupId(id);
            List<IdNameBO> iList = new List<IdNameBO>();
            IdNameBO bo = new IdNameBO();
            if ((responselevels.Success))
            {
                foreach (var type in responselevels.Values)
                {
                    bo = new IdNameBO();
                    bo.ID = type.Id;
                    bo.Display = type.Name;
                    iList.Add(bo);
                }
            }
            return Json(iList);
        }
        [HttpPost]
        public JsonResult GetCompanyActivities(int companyid)
        {
            var pservice = _companyService;
            var presponse = pservice.GetCompanyActivities(companyid);

            var activitiesList = new List<Select2DTO>();

            if (presponse.Success)
            {
                foreach (var selectedActivity in presponse.Values)
                {
                    var singleActivity = new Select2DTO
                    {
                        id = selectedActivity.ID,
                        text = selectedActivity.Name,
                        group = "-Bedrijfsactiviteit-"
                    };
                    activitiesList.Add(singleActivity);
                }
            }

            return Json(activitiesList);
        }

        [HttpPost]
        public JsonResult GetContractActivities(int contractid)
        {
            var pservice = _projectService;
            var presponse = pservice.GetContract(contractid);

            var activitiesList = new List<Select2DTO>();

            if (presponse.Success)
            {
                foreach (var selectedActivity in presponse.Value.Activities)
                {
                    var singleActivity = new Select2DTO
                    {
                        id = selectedActivity.ContractActivityId,
                        text = selectedActivity.Activity.Name
                    };
                    activitiesList.Add(singleActivity);
                }
            }

            return Json(activitiesList);
        }
        [HttpPost]
        public PartialViewResult AddSelectedActivities(int ActivityId, string ActivityName)
        {
            var nContractActivity = new ContractActivityBO();
            var nActivity = new ActivityBO
            {
                ID = ActivityId,
                Name = ActivityName
            };
            nContractActivity.Activity = nActivity;

            ViewData["mode"] = "add";
            return PartialView(ViewData["UseGlV2Layout"] as bool? == true ? "_ActivityRowV2" : "_ActivityRow", nContractActivity);
        }
        [HttpPost]
        public PartialViewResult AddAdditionalOrders(int contractActivityId, string activityName)
        {
            var nAdditionalOrder = new ContractAdditionalOrderBO
            {
                ContractActivityId = contractActivityId,
                ActivityName = activityName
            };

            ViewData["mode"] = "add";
            return PartialView("_AdditionalOrderRow", nAdditionalOrder);
        }

        // ===== Bijbestellingen op een lot — directe AJAX-CRUD vanaf de contractdetailpagina =====
        // Prijs komt als invariante string (JS Number.toString, punt als decimaalteken)
        // binnen: de request-cultuur is nl-BE, dus decimal-modelbinding zou "1234.5"
        // fout lezen. Daarom hier expliciet invariant parsen.
        private static decimal ParseInvariantAmount(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0m;
            return decimal.TryParse(raw.Trim(), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsSuppliers)]
        public IActionResult AddContractAdditionalOrder(int contractActivityId, int contractId, int activityId, string description, string price)
        {
            // Geen bestaand lot gekozen maar wel contract + activiteit: het lot bestaat
            // (nog) niet op het contract -> aanmaken en daarop de bijbestelling zetten.
            if (contractActivityId <= 0 && contractId > 0 && activityId > 0)
                contractActivityId = _projectService.GetOrCreateContractActivity(contractId, activityId);

            var response = _projectService.AddContractAdditionalOrder(contractActivityId, description, ParseInvariantAmount(price));
            return Json(new { success = response.Success, error = response.Messages.FirstOrDefault(m => m.Type == MessageType.Error)?.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsSuppliers)]
        public IActionResult UpdateContractAdditionalOrder(int id, string description, string price)
        {
            var response = _projectService.UpdateContractAdditionalOrder(id, description, ParseInvariantAmount(price));
            return Json(new { success = response.Success, error = response.Messages.FirstOrDefault(m => m.Type == MessageType.Error)?.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsSuppliers)]
        public IActionResult DeleteContractAdditionalOrder(int id)
        {
            var response = _projectService.DeleteContractAdditionalOrder(id);
            return Json(new { success = response.Success, error = response.Messages.FirstOrDefault(m => m.Type == MessageType.Error)?.Message });
        }
        [Breadcrumb("Budget instellen", FromAction = "DetailContracts")]
        [HttpGet]
        public IActionResult CalculationSettings(int projectid)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            //Sidebar collapse
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var model = new ProjectCalculationSettings
            {
                ProjectId = projectid,
                ProjectName = _projectService.GetProjectNameById(projectid)
            };

            // Groups
            var aservice = _activityService;
            var groupsResp = aservice.GetActivityGroups();
            model.ActivityGroups = groupsResp.Values?.ToList() ?? new();

            // Reeds ingestelde budgetregels
            var pservice = _projectService;
            var budgetResp = pservice.GetProjectBudget(projectid);
            model.BudgetActivities = budgetResp.Values?.ToList() ?? new();

            // Select2-lijst (als optgroups)
            var listResp = aservice.GetActivitiesForSelect();
            if (listResp.Success)
            {
                model.ListActivities = listResp.Values.Select(x => new IdNameBO
                {
                    ID = x.ID,
                    Display = x.Display,
                    Group = x.Group,
                    GroupId = x.GroupId
                }).ToList();
            }

            // Preselecteer wat al op budget staat
            if (model.BudgetActivities.Any())
                model.SelectedActivities = model.BudgetActivities.Select(b => b.Activity.ID).Distinct().ToList();

            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectRecalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var projectRecalcAct = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("CalculationSettings", "Projecten", "Budget instellen")
            {
                Parent = projectRecalc,
                RouteValues = new
                {
                    projectId = projectid
                }
            };
            ViewData["BreadcrumbNode"] = projectRecalcAct;

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Budget instellen");
            return View(model);
        }

        [HttpPost]
        public IActionResult CalculationSettings(ProjectCalculationSettings model, List<BudgetActivityBO> budgetactivities)
        {
            if (!ModelState.IsValid)
            {
                SetPageHeader("bx bx-building-house", $"{(model.ProjectName ?? _projectService.GetProjectNameById(model.ProjectId))} - Budget instellen");
                return View(model);
            }

            // ProjectId meegeven aan alle regels
            if (budgetactivities != null)
            {
                // verwijder null items
                budgetactivities = budgetactivities
                    .Where(b => b != null)
                    .ToList();

                // projectId invullen
                budgetactivities.ForEach(b => b.ProjectId = model.ProjectId);
            }

            var service = _projectService;
            var resp = service.InsertUpdateProjectBudgetActivities(budgetactivities ?? new(), model.ProjectId);

            if (resp.Success)
            {
                TempData["MessageType"] = "success";
                TempData["MessageTitle"] = "Geslaagd!";
                TempData["Message"] = "De activiteiten zijn aan het budget toegevoegd.";
                return RedirectToAction("Recalculation", "Projecten", new { projectid = model.ProjectId });
            }

            TempData["MessageType"] = "error";
            TempData["MessageTitle"] = "Fout!";
            TempData["Message"] = "De activiteiten zijn NIET aan het budget toegevoegd.";
            SetPageHeader("bx bx-building-house", $"{(model.ProjectName ?? _projectService.GetProjectNameById(model.ProjectId))} - Budget instellen");
            return View(model);
        }

        [HttpPost]
        public IActionResult AddBudgetActivity(int actId)
        {
            var aservice = _activityService;
            var response = aservice.GetActivitybyId(actId);
            var act = response.Value;

            var budget = new BudgetActivityBO
            {
                Activity = act
            };

            return PartialView("_BudgetActivityRow", budget);
        }

    }
}
