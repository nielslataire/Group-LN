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
    /// <summary>Budget-wizard (BudgetIndex t/m DownloadBudgetExcel). Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Budget/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ── Budget Wizard ────────────────────────────────────────────────────

        /// <summary>
        /// Zet topbar-titel + volledige broodkruimel voor een budgetpagina.
        /// Zonder versie: Home / Projecten / {projectnaam} / Budgetten / {stap}.
        /// Met versie:    … / Budgetten / {versie} / {stap}.
        /// </summary>
        private void SetBudgetPageContext(int projectId, string projectName, string stepAction, string stepLabel,
            object stepRoute = null, int? versieId = null, string versieLabel = null)
        {
            var naam = string.IsNullOrWhiteSpace(projectName) ? "Project" : projectName;

            var bcHome = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var bcProjecten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = bcHome };
            var bcDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", naam)
            {
                Parent = bcProjecten,
                RouteValues = new { projectid = projectId }
            };

            SmartBreadcrumbs.Nodes.MvcBreadcrumbNode current;
            if (stepAction == nameof(BudgetIndex))
            {
                current = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("BudgetIndex", "Projecten", "Budgetten")
                {
                    Parent = bcDetail,
                    RouteValues = new { projectId }
                };
            }
            else
            {
                SmartBreadcrumbs.Nodes.MvcBreadcrumbNode parent = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("BudgetIndex", "Projecten", "Budgetten")
                {
                    Parent = bcDetail,
                    RouteValues = new { projectId }
                };

                if (versieId.HasValue)
                {
                    parent = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("BudgetGegevens", "Projecten",
                        string.IsNullOrWhiteSpace(versieLabel) ? $"Versie {versieId.Value}" : versieLabel)
                    {
                        Parent = parent,
                        RouteValues = new { versieId = versieId.Value }
                    };
                }

                current = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode(stepAction, "Projecten", stepLabel)
                {
                    Parent = parent,
                    RouteValues = stepRoute
                };
            }

            ViewData["BreadcrumbNode"] = current;
            SetPageHeader("bx bx-euro", string.IsNullOrWhiteSpace(projectName) ? stepLabel : $"{projectName} — {stepLabel}");
        }

        [HttpGet]
        public async Task<IActionResult> BudgetIndex(int projectId)
        {
            var projectNaamLite = _projectService.GetProjectNameById(projectId);
            if (string.IsNullOrEmpty(projectNaamLite)) return NotFound();

            var mastersResponse = _budgetService.GetBudgetMasters(projectId);

            var model = new BudgetIndexModel
            {
                ProjectId    = projectId,
                ProjectName  = projectNaamLite,
                BudgetMasters = mastersResponse.Success ? mastersResponse.Values : new List<BudgetMasterBO>()
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetIndex), "Budgetten");
            if (GebruikGlV2)
            {
                await VulBudgetIndexV2Async(model);
                return View("BudgetIndexV2", model);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult BudgetMasterAanmaken(int projectId)
        {
            var projectNaamLite = _projectService.GetProjectNameById(projectId);
            if (string.IsNullOrEmpty(projectNaamLite)) return NotFound();

            var model = new BudgetMasterAanmakenModel
            {
                ProjectId   = projectId,
                ProjectName = projectNaamLite
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetMasterAanmaken), "Nieuw budget", new { projectId = model.ProjectId });
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BudgetMasterAanmaken(BudgetMasterAanmakenModel model)
        {
            if (!ModelState.IsValid)
            {
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Budgetmaster aanmaken");
                return View(model);
            }

            var userId = _db.Users.FirstOrDefault(u => u.Email == User.Identity.Name)?.Id ?? 0;

            var bo = new BudgetMasterBO
            {
                ProjectId   = model.ProjectId,
                Naam        = model.Naam,
                Omschrijving = model.Omschrijving
            };

            var response = _budgetService.CreateBudgetMaster(bo, userId);
            if (!response.Success)
            {
                foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                    ModelState.AddModelError(string.Empty, msg.Message);
                if (GebruikGlV2) { TempData["Error"] = string.Join(" ", response.Messages.Select(m => m.Message)); return RedirectToAction(nameof(BudgetIndex), new { projectId = model.ProjectId }); }
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Budgetmaster aanmaken");
                return View(model);
            }

            // 39a "Nieuw budget — leeg of als kopie van een bestaand budget": versie 1 krijgt de volledige inhoud van de gekozen versie.
            if (model.KopieVanVersieId is > 0)
            {
                var kopie = _budgetService.KopieerVersieInhoud(model.KopieVanVersieId.Value, response.InsertedId);
                if (!kopie.Success) TempData["Error"] = "Budget aangemaakt, maar de kopie mislukte: " + kopie.Messages.FirstOrDefault()?.Message;
            }

            return RedirectToAction(nameof(BudgetGegevens), new { versieId = response.InsertedId });
        }

        [HttpGet]
        public async Task<IActionResult> BudgetGegevens(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null)
                return NotFound();

            var projectNaamLite = _projectService.GetProjectNameById(versieEntity.ProjectId);
            if (string.IsNullOrEmpty(projectNaamLite)) return NotFound();

            var gegevensResponse = _budgetService.GetBudgetGegevens(versieId);
            var gegevens = gegevensResponse.Success ? gegevensResponse.Value : new BudgetGegevensBO();

            if (gegevens.SIndexHuidig == null || gegevens.SIndexHuidig == 0)
                gegevens.SIndexHuidig = await _bouwIndex.GetActieveIndexAsync("S");
            if (gegevens.IIndexHuidig == null || gegevens.IIndexHuidig == 0)
                gegevens.IIndexHuidig = await _bouwIndex.GetActieveIndexAsync("I2021");

            var bouwheerOptions = new List<SelectListItem>
            {
                new SelectListItem("— geen —", "")
            };
            var companyList = _uow.CompanyInfo.GetNoTracking()
                .OrderBy(c => c.BedrijfsNaam)
                .Select(c => new { c.CompanyId, c.BedrijfsNaam })
                .ToList();
            bouwheerOptions.AddRange(companyList.Select(c => new SelectListItem(c.BedrijfsNaam, c.CompanyId.ToString())));

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                BudgetMasterId = versieEntity.BudgetMasterId,
                ProjectId    = versieEntity.ProjectId,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam,
                Status       = versieEntity.Status,
                IsHuidig     = versieEntity.IsHuidig,
                CreatedAt    = versieEntity.CreatedAt
            };

            var formulaCtx = await _formulaService.BuildContextAsync(versieId, gegevens);
            var formulaVoorstellingen = _formulaService.BerekenAlle(formulaCtx);

            var model = new BudgetGegevensModel
            {
                VersieId      = versieId,
                MasterId      = versieEntity.BudgetMasterId,
                ProjectId     = versieEntity.ProjectId,
                ProjectName   = projectNaamLite,
                VersieLabel   = versieBO.VersieLabel,
                VersieStatus  = versieEntity.Status,
                MasterNaam    = versieEntity.BudgetMaster?.Naam,
                VersieCreatedAt = versieEntity.CreatedAt,
                Gegevens      = gegevens,
                BouwheerOptions = bouwheerOptions,
                FormulaVoorstellingen = formulaVoorstellingen
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",      nameof(HomeController.Index),        "Home",       true),
                new Breadcrumb("Projecten", nameof(ProjectenController.Index),   "Projecten",  true),
                new Breadcrumb("Detail",    nameof(ProjectenController.Detail),  "Projecten",  true),
                new Breadcrumb("Budgetten", nameof(BudgetIndex),                 "Projecten",  true),
                new Breadcrumb("Gegevens",  nameof(BudgetGegevens),              "Projecten",  false),
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetGegevens), "Gegevens", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2)
            {
                await PrepareWizardV2(versieId, 1);
                // Peildatum + bron van de gekozen indexwaarde (39b: "peildatum 01/09/2026 · bron FOD Economie")
                string IndexInfo(string type, decimal? waarde)
                {
                    if (!waarde.HasValue) return "";
                    var rij = _uow.BouwIndex.GetNoTracking().Where(x => x.IndexType == type && x.IndexWaarde == waarde.Value)
                        .OrderByDescending(x => x.Jaar).ThenByDescending(x => x.Maand).FirstOrDefault();
                    if (rij == null) return "handmatig ingevuld";
                    var peil = rij.GeldigVanaf?.ToString("dd/MM/yyyy") ?? (rij.Jaar.HasValue ? (rij.Maand.HasValue ? $"{rij.Maand:00}/{rij.Jaar}" : rij.Jaar.ToString()) : null);
                    return string.Join(" · ", new[] { peil != null ? "peildatum " + peil : null, string.IsNullOrWhiteSpace(rij.Bron) ? null : "bron " + rij.Bron }.Where(x => x != null));
                }
                ViewData["SIndexInfo"] = IndexInfo("S", gegevens.SIndexHuidig);
                ViewData["IIndexInfo"] = IndexInfo("I2021", gegevens.IIndexHuidig);
                ViewData["PoortWaarschuwing"] = await HttpContext.RequestServices.GetRequiredService<ServiceCore.Budget.BudgetActivityFormuleService>().IsPoortWaarschuwingAsync(versieId);
                return View("BudgetGegevensV2", model);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BudgetGegevens(BudgetGegevensModel model, string submitAction)
        {
            if (!ModelState.IsValid)
            {
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Budget gegevens");
                return View(model);
            }

            var response = _budgetService.SaveBudgetGegevens(model.Gegevens, model.VersieId);
            if (!response.Success)
            {
                foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                    ModelState.AddModelError(string.Empty, msg.Message);
                SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Budget gegevens");
                return View(model);
            }

            if (submitAction == "next")
                return RedirectToAction(nameof(BudgetOppervlaktes), new { versieId = model.VersieId });

            return RedirectToAction(nameof(BudgetGegevens), new { versieId = model.VersieId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BudgetGegevensOpslaan(BudgetGegevensModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false });

            var response = _budgetService.SaveBudgetGegevens(model.Gegevens, model.VersieId);
            return Json(new { success = response.Success });
        }

        // ── BudgetOppervlaktes ────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetOppervlaktes(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectNaamLite = _projectService.GetProjectNameById(versieEntity.ProjectId);
            if (string.IsNullOrEmpty(projectNaamLite)) return NotFound();

            var rijResp     = _budgetService.GetBudgetOppervlaktes(versieId);
            var totaalResp  = _budgetService.GetBudgetOppervlaktesTotaal(versieId);

            var groupOptions = new List<SelectListItem> { new SelectListItem("— kies type —", "") };
            var dbGroupTypes = _uow.UnitGroupTypes.GetNoTracking()
                .Where(g => g.Selectable)
                .OrderBy(g => g.Name == "Wooneenheid" ? 0 : 1)
                .ThenBy(g => g.Name)
                .Select(g => new { g.Id, g.Name })
                .ToList();
            groupOptions.AddRange(dbGroupTypes.Select(g => new SelectListItem(g.Name, g.Id.ToString())));

            var allTypesBos = _uow.UnitTypes.GetNoTracking()
                .Where(t => t.Selectable != false)
                .Select(t => new UnitTypeBO { Id = t.Id, Name = t.Name, Shortcode = t.Shortcode, GroupId = t.GroupId })
                .ToList();

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                BudgetMasterId = versieEntity.BudgetMasterId,
                ProjectId    = versieEntity.ProjectId,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam,
                Status       = versieEntity.Status,
                IsHuidig     = versieEntity.IsHuidig,
                CreatedAt    = versieEntity.CreatedAt
            };

            var model = new BudgetOppervlaktesModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectNaamLite,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Rijen       = rijResp.Success ? rijResp.Values : new List<BudgetOppervlaktesBO>(),
                Totalen     = totaalResp.Success ? totaalResp.Value : new BudgetOppervlaktesTotaalBO(),
                GroupTypes  = groupOptions,
                AllTypes    = allTypesBos
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",          nameof(HomeController.Index),       "Home",       true),
                new Breadcrumb("Projecten",     nameof(ProjectenController.Index),  "Projecten",  true),
                new Breadcrumb("Detail",        nameof(ProjectenController.Detail), "Projecten",  true),
                new Breadcrumb("Budgetten",     nameof(BudgetIndex),                "Projecten",  true),
                new Breadcrumb("Oppervlaktes",  nameof(BudgetOppervlaktes),         "Projecten",  false),
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetOppervlaktes), "Oppervlaktes", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2)
            {
                await PrepareWizardV2(versieId, 2);
                ViewData["Factoren"] = _budgetService.GetVmswFactoren(versieId);
                return View("BudgetOppervlaktesV2", model);
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesRijToevoegen(int versieId, string eenheidNaam, int? groupTypeId, int? typeId, bool v2 = false)
        {
            var rij = new BudgetOppervlaktesBO
            {
                BudgetVersieId  = versieId,
                EenheidNaam     = eenheidNaam ?? "",
                UnitGroupTypeId = groupTypeId,
                UnitTypeId      = typeId
            };

            if (groupTypeId.HasValue)
            {
                var grp = _uow.UnitGroupTypes.GetNoTracking().SingleOrDefault(g => g.Id == groupTypeId.Value);
                rij.GroupTypeName = grp?.Name;
            }
            if (typeId.HasValue)
            {
                var tp = _uow.UnitTypes.GetNoTracking().SingleOrDefault(t => t.Id == typeId.Value);
                rij.TypeName      = tp?.Name;
                rij.TypeShortcode = tp?.Shortcode;
            }

            var response = _budgetService.AddBudgetOppervlaktesRij(rij, versieId);
            if (!response.Success)
                return BadRequest(response.Messages.FirstOrDefault()?.Message);

            rij.Id = response.InsertedId;
            if (v2)
            {
                rij.Factoren = _budgetService.GetVmswFactoren(versieId);
                ViewData["GroupTypes"] = _uow.UnitGroupTypes.GetNoTracking().Where(g => g.Selectable).OrderBy(g => g.Name == "Wooneenheid" ? 0 : 1).ThenBy(g => g.Name).Select(g => new SelectListItem(g.Name, g.Id.ToString())).ToList();
                ViewData["AllTypes"] = _uow.UnitTypes.GetNoTracking().Where(t => t.Selectable != false).Select(t => new UnitTypeBO { Id = t.Id, Name = t.Name, Shortcode = t.Shortcode, GroupId = t.GroupId }).ToList();
                return PartialView("~/Views/Projecten/Budget/V2/_BwOppRij.cshtml", rij);
            }
            return PartialView("Partials/_BudgetOppervlaktesRij", rij);
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesRijOpslaan([FromBody] BudgetOppervlaktesRijModel model)
        {
            var bo = new BudgetOppervlaktesBO
            {
                Id                      = model.RijId,
                BudgetVersieId          = model.VersieId,
                EenheidNaam             = model.EenheidNaam ?? "",
                UnitGroupTypeId         = model.UnitGroupTypeId,
                UnitTypeId              = model.UnitTypeId,
                BewoonbareOpp           = model.BewoonbareOpp,
                Tuin                    = model.Tuin,
                TerrasPrefab            = model.TerrasPrefab,
                TerrasGelijkvloers      = model.TerrasGelijkvloers,
                Dakterras               = model.Dakterras,
                GaragesParkingsBovenGr  = model.GaragesParkingsBovenGr,
                GarBergOndergronds      = model.GarBergOndergronds,
                BergGelijkvloers        = model.BergGelijkvloers,
                Carports                = model.Carports,
                DoorritGVL              = model.DoorritGVL,
                Zolder                  = model.Zolder,
                GemeenschappelijkeDelen = model.GemeenschappelijkeDelen,
                Wegenis                 = model.Wegenis,
                Grondopp                = model.Grondopp
            };

            var response = _budgetService.UpdateBudgetOppervlaktesRij(bo);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totalen = _budgetService.GetBudgetOppervlaktesTotaal(model.VersieId);
            var t = totalen.Value ?? new BudgetOppervlaktesTotaalBO();

            return Json(new
            {
                success           = true,
                oppGereduceerd    = bo.OppGereduceerd,
                formula           = bo.FormulaOppGereduceerd,
                totalen = new
                {
                    aantalWooneenheden    = t.AantalWooneenheden,
                    aantalParkeerplaatsen = t.AantalParkeerplaatsen,
                    aantalCommercieel     = t.AantalCommercieel,
                    aantalTotaal          = t.AantalTotaal,
                    totaalBewoonbaar      = t.TotaalBewoonbaar,
                    totaalGereduceerd     = t.TotaalGereduceerd,
                    gemiddeldeM2PerEenheid = t.GemiddeldeM2PerEenheid,
                    totaalGrondopp        = t.TotaalGrondopp
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesRijVerwijderen(int rijId, int versieId)
        {
            var response = _budgetService.DeleteBudgetOppervlaktesRij(rijId, versieId);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totalen = _budgetService.GetBudgetOppervlaktesTotaal(versieId);
            var t = totalen.Value ?? new BudgetOppervlaktesTotaalBO();

            return Json(new
            {
                success = true,
                totalen = new
                {
                    aantalWooneenheden    = t.AantalWooneenheden,
                    aantalParkeerplaatsen = t.AantalParkeerplaatsen,
                    aantalCommercieel     = t.AantalCommercieel,
                    aantalTotaal          = t.AantalTotaal,
                    totaalBewoonbaar      = t.TotaalBewoonbaar,
                    totaalGereduceerd     = t.TotaalGereduceerd,
                    gemiddeldeM2PerEenheid = t.GemiddeldeM2PerEenheid,
                    totaalGrondopp        = t.TotaalGrondopp
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetOppervlaktesVolgorde(int versieId, [FromBody] int[] orderedIds)
        {
            var response = _budgetService.ReorderBudgetOppervlaktes(orderedIds?.ToList() ?? new List<int>(), versieId);
            return Json(new { success = response.Success });
        }

        [HttpPost]
        public IActionResult BudgetNieuweVersie(int masterId, string versieNaam, string notitie)
        {
            var userId = _db.Users.FirstOrDefault(u => u.Email == User.Identity.Name)?.Id ?? 0;
            var response = _budgetService.CreateNieuweVersie(masterId, versieNaam, notitie, userId);

            var versienummer = 0;
            if (response.Success)
            {
                var versie = _uow.BudgetVersies.GetNoTracking()
                    .SingleOrDefault(v => v.Id == response.InsertedId);
                versienummer = versie?.Versienummer ?? 0;
            }

            return Json(new { success = response.Success, newVersieId = response.InsertedId, versienummer });
        }

        [HttpPost]
        public IActionResult BudgetVersieActiveren(int versieId)
        {
            var response = _budgetService.ActiveerVersie(versieId);
            return Json(new { success = response.Success });
        }

        // ── BudgetSanitair ────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetSanitair(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectNaamLite = _projectService.GetProjectNameById(versieEntity.ProjectId);
            if (string.IsNullOrEmpty(projectNaamLite)) return NotFound();

            _budgetService.SyncSanitairVanOppervlaktes(versieId);

            var rijResp    = _budgetService.GetBudgetSanitair(versieId);
            var totaalResp = _budgetService.GetBudgetSanitairTotaal(versieId);

            var versieBO = new BudgetVersieBO
            {
                Id             = versieEntity.Id,
                BudgetMasterId = versieEntity.BudgetMasterId,
                ProjectId      = versieEntity.ProjectId,
                Versienummer   = versieEntity.Versienummer,
                VersieNaam     = versieEntity.VersieNaam,
                Status         = versieEntity.Status,
                IsHuidig       = versieEntity.IsHuidig,
                CreatedAt      = versieEntity.CreatedAt
            };

            var model = new BudgetSanitairModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectNaamLite,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Rijen       = rijResp.Success ? rijResp.Values : new List<BudgetSanitairBO>(),
                Totaal      = totaalResp.Success ? totaalResp.Value : new BudgetSanitairTotaalBO()
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",       nameof(HomeController.Index),      "Home",       true),
                new Breadcrumb("Projecten",  nameof(ProjectenController.Index), "Projecten",  true),
                new Breadcrumb("Detail",     nameof(ProjectenController.Detail),"Projecten",  true),
                new Breadcrumb("Budgetten",  nameof(BudgetIndex),               "Projecten",  true),
                new Breadcrumb("Sanitair",   nameof(BudgetSanitair),            "Projecten",  false),
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetSanitair), "Sanitair", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2) { await PrepareWizardV2(versieId, 3); return View("BudgetSanitairV2", model); }
            return View(model);
        }

        [HttpPost]
        public IActionResult BudgetSanitairRijToevoegen(int versieId, string eenheidNaam, int? unitTypeId)
        {
            var rij = new BudgetSanitairBO
            {
                BudgetVersieId = versieId,
                EenheidNaam    = eenheidNaam ?? "",
                UnitTypeId     = unitTypeId
            };

            var response = _budgetService.AddBudgetSanitairRij(rij, versieId);
            if (!response.Success)
                return BadRequest(response.Messages.FirstOrDefault()?.Message);

            rij.Id = response.InsertedId;
            return PartialView("Partials/_BudgetSanitairRij", rij);
        }

        [HttpPost]
        public IActionResult BudgetSanitairRijOpslaan([FromBody] BudgetSanitairRijModel model)
        {
            var bo = new BudgetSanitairBO
            {
                Id                  = model.RijId,
                BudgetVersieId      = model.VersieId,
                EenheidNaam         = model.EenheidNaam,
                UnitTypeId          = model.UnitTypeId,
                Badkamer            = model.Badkamer,
                ToiletInBadkamer    = model.ToiletInBadkamer,
                AfzonderlijkToilet  = model.AfzonderlijkToilet,
                DoucheInBadkamer    = model.DoucheInBadkamer,
                Douchekamer         = model.Douchekamer
            };

            var response = _budgetService.UpdateBudgetSanitairRij(bo);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaalResp = _budgetService.GetBudgetSanitairTotaal(model.VersieId);
            var t = totaalResp.Value ?? new BudgetSanitairTotaalBO();

            return Json(new
            {
                success = true,
                totaal = new
                {
                    totaalBadkamers             = t.TotaalBadkamers,
                    totaalToilettenInBadkamer   = t.TotaalToilettenInBadkamer,
                    totaalAfzonderlijkeToiletten = t.TotaalAfzonderlijkeToiletten,
                    totaalDouchesInBadkamer     = t.TotaalDouchesInBadkamer,
                    totaalDouchekamers          = t.TotaalDouchekamers,
                    aantalEenheden              = t.AantalEenheden
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetSanitairRijVerwijderen(int rijId, int versieId)
        {
            var response = _budgetService.DeleteBudgetSanitairRij(rijId, versieId);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaalResp = _budgetService.GetBudgetSanitairTotaal(versieId);
            var t = totaalResp.Value ?? new BudgetSanitairTotaalBO();

            return Json(new
            {
                success = true,
                totaal = new
                {
                    totaalBadkamers             = t.TotaalBadkamers,
                    totaalToilettenInBadkamer   = t.TotaalToilettenInBadkamer,
                    totaalAfzonderlijkeToiletten = t.TotaalAfzonderlijkeToiletten,
                    totaalDouchesInBadkamer     = t.TotaalDouchesInBadkamer,
                    totaalDouchekamers          = t.TotaalDouchekamers,
                    aantalEenheden              = t.AantalEenheden
                }
            });
        }

        // ── BudgetGevels ─────────────────────────────────────────────────────

        private static readonly string[] GevelTypes = { "GevelNieuwbouw", "GevelBestaand", "RaamNieuwbouw", "RaamBestaand", "Ballustrade", "Zichtscherm", "Leien" };

        [HttpGet]
        public async Task<IActionResult> BudgetGevels(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectNaamLite = _projectService.GetProjectNameById(versieEntity.ProjectId);
            if (string.IsNullOrEmpty(projectNaamLite)) return NotFound();

            var elementenResp = _budgetService.GetBudgetGevelElementen(versieId);
            var totaalResp    = _budgetService.GetBudgetGevelTotaal(versieId);

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam
            };

            var elementen = (elementenResp.Success ? elementenResp.Values : new List<BudgetGevelElementBO>())
                .Where(e => GevelTypes.Contains(e.ElementType))
                .GroupBy(e => e.ElementType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var model = new BudgetGevelsDakModel
            {
                VersieId    = versieId,
                MasterId    = versieEntity.BudgetMasterId,
                ProjectId   = versieEntity.ProjectId,
                ProjectName = projectNaamLite,
                VersieLabel = versieBO.VersieLabel,
                MasterNaam  = versieEntity.BudgetMaster?.Naam,
                Elementen   = elementen,
                Totaal      = totaalResp.Success ? totaalResp.Value : new BudgetGevelTotaalBO()
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",       nameof(HomeController.Index),      "Home",       true),
                new Breadcrumb("Projecten",  nameof(ProjectenController.Index), "Projecten",  true),
                new Breadcrumb("Detail",     nameof(ProjectenController.Detail),"Projecten",  true),
                new Breadcrumb("Budgetten",  nameof(BudgetIndex),               "Projecten",  true),
                new Breadcrumb("Gevels",     nameof(BudgetGevels),              "Projecten",  false),
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetGevels), "Gevels & ramen", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2) { await PrepareWizardV2(versieId, 4); return View("BudgetGevelsV2", model); }
            return View(model);
        }

        // ── BudgetDakAfbraak ──────────────────────────────────────────────────

        private static readonly string[] DakAfbraakTypes = { "PlatDak", "HellendDak", "GroenDak", "Dakoversteken", "OnderkantDoorrit", "Afbraak" };

        [HttpGet]
        public async Task<IActionResult> BudgetDakAfbraak(int versieId)
        {
            var versieEntity = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .SingleOrDefault();

            if (versieEntity == null) return NotFound();

            var projectNaamLite = _projectService.GetProjectNameById(versieEntity.ProjectId);
            if (string.IsNullOrEmpty(projectNaamLite)) return NotFound();

            var elementenResp = _budgetService.GetBudgetGevelElementen(versieId);
            var totaalResp    = _budgetService.GetBudgetGevelTotaal(versieId);

            var versieBO = new BudgetVersieBO
            {
                Id           = versieEntity.Id,
                Versienummer = versieEntity.Versienummer,
                VersieNaam   = versieEntity.VersieNaam
            };

            var elementen = (elementenResp.Success ? elementenResp.Values : new List<BudgetGevelElementBO>())
                .Where(e => DakAfbraakTypes.Contains(e.ElementType))
                .GroupBy(e => e.ElementType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var gegevens = _uow.BudgetGegevens.GetNoTracking()
                .FirstOrDefault(g => g.BudgetVersieId == versieId);

            var model = new BudgetGevelsDakModel
            {
                VersieId      = versieId,
                MasterId      = versieEntity.BudgetMasterId,
                ProjectId     = versieEntity.ProjectId,
                ProjectName   = projectNaamLite,
                VersieLabel   = versieBO.VersieLabel,
                MasterNaam    = versieEntity.BudgetMaster?.Naam,
                Elementen     = elementen,
                Totaal        = totaalResp.Success ? totaalResp.Value : new BudgetGevelTotaalBO(),
                AantalVeluxen = gegevens?.AantalVeluxen ?? 0
            };

            ViewBag.Breadcrumbs = new List<Breadcrumb>
            {
                new Breadcrumb("Home",         nameof(HomeController.Index),        "Home",       true),
                new Breadcrumb("Projecten",    nameof(ProjectenController.Index),   "Projecten",  true),
                new Breadcrumb("Detail",       nameof(ProjectenController.Detail),  "Projecten",  true),
                new Breadcrumb("Budgetten",    nameof(BudgetIndex),                 "Projecten",  true),
                new Breadcrumb("Dak & Afbraak",nameof(BudgetDakAfbraak),            "Projecten",  false),
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetDakAfbraak), "Dak & afbraak", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2) { await PrepareWizardV2(versieId, 5); return View("BudgetDakAfbraakV2", model); }
            return View(model);
        }

        [HttpPost]
        public IActionResult BudgetGevelElementToevoegen(int versieId, string elementType,
            string eenheidNaam, string beschrijving, bool v2 = false)
        {
            var bo = new BudgetGevelElementBO
            {
                BudgetVersieId = versieId,
                ElementType    = elementType ?? "GevelNieuwbouw",
                EenheidNaam    = eenheidNaam,
                Beschrijving   = beschrijving,
                Aantal         = 1m
            };

            var response = _budgetService.AddBudgetGevelElement(bo, versieId);
            if (!response.Success)
                return BadRequest(response.Messages.FirstOrDefault()?.Message);

            bo.Id = response.InsertedId;
            ViewBag.VersieId = versieId;
            if (v2) return PartialView("~/Views/Projecten/Budget/V2/_BwGevelRij.cshtml", bo);
            return PartialView("Partials/_BudgetGevelRij", bo);
        }

        [HttpPost]
        public IActionResult BudgetGevelElementOpslaan([FromBody] BudgetGevelElementModel model)
        {
            var bo = new BudgetGevelElementBO
            {
                Id           = model.ElementId,
                BudgetVersieId = model.VersieId,
                ElementType  = model.ElementType,
                EenheidNaam  = model.EenheidNaam,
                Beschrijving = model.Beschrijving,
                Aantal       = model.Aantal,
                Breedte      = model.Breedte,
                Hoogte       = model.Hoogte,
                Lengte       = model.Lengte
            };

            var response = _budgetService.UpdateBudgetGevelElement(bo);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaal = _budgetService.GetBudgetGevelTotaal(model.VersieId);
            var t = totaal.Value ?? new BudgetGevelTotaalBO();

            return Json(new
            {
                success      = true,
                resultaatM2  = bo.ResultaatM2,
                resultaatLm  = bo.ResultaatLm,
                formula      = bo.FormulaResultaat,
                totaal = new
                {
                    totaalGevelNieuwbouw   = t.TotaalGevelNieuwbouw,
                    totaalGevelBestaand    = t.TotaalGevelBestaand,
                    totaalRaamNieuwbouw    = t.TotaalRaamNieuwbouw,
                    totaalRaamBestaand     = t.TotaalRaamBestaand,
                    totaalBallustrade      = t.TotaalBallustrade,
                    totaalZichtscherm      = t.TotaalZichtscherm,
                    totaalLeien            = t.TotaalLeien,
                    totaalPlatDak          = t.TotaalPlatDak,
                    totaalHellendDak       = t.TotaalHellendDak,
                    totaalGroenDak         = t.TotaalGroenDak,
                    totaalDakoversteken    = t.TotaalDakoversteken,
                    totaalOnderkantDoorrit = t.TotaalOnderkantDoorrit,
                    totaalAfbraak          = t.TotaalAfbraak,
                    totaalGevelCombineerd  = t.TotaalGevelCombineerd,
                    totaalRaamCombineerd   = t.TotaalRaamCombineerd,
                    totaalDakCombineerd    = t.TotaalDakCombineerd
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetGevelElementVerwijderen(int elementId, int versieId)
        {
            var response = _budgetService.DeleteBudgetGevelElement(elementId, versieId);
            if (!response.Success)
                return Json(new { success = false, error = response.Messages.FirstOrDefault()?.Message });

            var totaal = _budgetService.GetBudgetGevelTotaal(versieId);
            var t = totaal.Value ?? new BudgetGevelTotaalBO();

            return Json(new
            {
                success = true,
                totaal = new
                {
                    totaalGevelNieuwbouw   = t.TotaalGevelNieuwbouw,
                    totaalGevelBestaand    = t.TotaalGevelBestaand,
                    totaalRaamNieuwbouw    = t.TotaalRaamNieuwbouw,
                    totaalRaamBestaand     = t.TotaalRaamBestaand,
                    totaalBallustrade      = t.TotaalBallustrade,
                    totaalZichtscherm      = t.TotaalZichtscherm,
                    totaalLeien            = t.TotaalLeien,
                    totaalPlatDak          = t.TotaalPlatDak,
                    totaalHellendDak       = t.TotaalHellendDak,
                    totaalGroenDak         = t.TotaalGroenDak,
                    totaalDakoversteken    = t.TotaalDakoversteken,
                    totaalOnderkantDoorrit = t.TotaalOnderkantDoorrit,
                    totaalAfbraak          = t.TotaalAfbraak,
                    totaalGevelCombineerd  = t.TotaalGevelCombineerd,
                    totaalRaamCombineerd   = t.TotaalRaamCombineerd,
                    totaalDakCombineerd    = t.TotaalDakCombineerd
                }
            });
        }

        [HttpPost]
        public IActionResult BudgetAantalVeluxenOpslaan(int versieId, int aantalVeluxen)
        {
            var entity = _uow.BudgetGegevens.GetNormal()
                .FirstOrDefault(g => g.BudgetVersieId == versieId);
            if (entity == null)
                return Json(new { success = false });

            entity.AantalVeluxen = aantalVeluxen;
            _uow.SaveChanges();
            return Json(new { success = true });
        }

        // ── BudgetActivityLijnen ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetActivityLijnen(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId)
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .SingleOrDefault();

            if (versie == null) return NotFound();

            var projectNaamLite = _projectService.GetProjectNameById(versie.BudgetMaster.ProjectId);

            var lotGroepen = await _budgetActivityService.GetLotGroepenAsync(versieId);

            // Totalen rekenen per woon-/commerciële eenheid
            var aantalEenheden = await _budgetActivityService.GetAantalWoonCommEenhedenAsync(versieId);

            var totaalGBA = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId)
                .Sum(o => (decimal?)o.BewoonbareOpp) ?? 0m;

            // Nacalc: referentieprijs per activiteit uit de gekozen referentieprojecten (geïndexeerd naar de huidige index van de versie)
            var referentieIds = await _referentieProjectService.GetReferentieIdsVoorVersieAsync(versieId);
            var referentie    = await _referentieProjectService.BerekenReferentieAsync(versieId);
            foreach (var lijn in lotGroepen.SelectMany(g => g.Lijnen))
            {
                if (!referentie.TryGetValue(lijn.ActivityId, out var r)) continue;
                lijn.ReferentiePrijsPerEenheid = r.PrijsPerEenheid;
                lijn.ReferentiePrijsPerM2      = r.PrijsPerM2;
                lijn.ReferentieAantalProjecten = r.AantalProjecten;
                lijn.ReferentieMinPerEenheid   = r.MinPerEenheid;
                lijn.ReferentieMaxPerEenheid   = r.MaxPerEenheid;
            }

            var model = new BudgetActivityLijnenModel
            {
                BudgetVersieId       = versieId,
                ProjectId            = versie.BudgetMaster.ProjectId,
                ProjectName          = projectNaamLite,
                BudgetNaam           = versie.BudgetMaster.Naam,
                Versienummer         = versie.Versienummer,
                VersieLabel          = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                           ? $"v{versie.Versienummer}"
                                           : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus         = versie.Status,
                LotGroepen           = lotGroepen,
                AantalEenheden       = aantalEenheden,
                OppervlakteGBA       = totaalGBA,
                SIndexStart          = versie.BudgetGegevens?.SIndexStart  ?? 0m,
                SIndexHuidig         = versie.BudgetGegevens?.SIndexHuidig ?? 0m,
                IIndexStart          = versie.BudgetGegevens?.IIndexStart  ?? 0m,
                IIndexHuidig         = versie.BudgetGegevens?.IIndexHuidig ?? 0m,
                Referenties          = await _referentieProjectService.GetAlleAsync(),
                GeselecteerdeReferentieIds = referentieIds
            };

            ViewData["Referrer"] = Request.Headers["Referer"].ToString();

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetActivityLijnen), "Activiteiten", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2) { await PrepareWizardV2(versieId, 6); return View("BudgetActivityLijnenV2", model); }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveActivityLijnen([FromBody] SaveActivityLijnenRequest request)
        {
            if (request == null)
                return Json(new { success = false, message = "Ongeldig verzoek." });

            var lijnen = request.Lijnen?.Select(dto => new BudgetActivityLijnBO
            {
                ActivityId                  = dto.ActivityId,
                AlternatievePrijsPerEenheid = dto.AlternatievePrijsPerEenheid ?? 0m,
                NacalcPrijsPerEenheid       = dto.NacalcPrijsPerEenheid       ?? 0m,
                Correctiefactor             = dto.Correctiefactor,
                IsManueel                   = dto.IsManueel
            }).ToList() ?? new List<BudgetActivityLijnBO>();

            var response = await _budgetActivityService.SaveLijnenAsync(request.BudgetVersieId, lijnen);

            return Json(new { success = response.Success, message = response.Messages.FirstOrDefault()?.Message });
        }

        // POST /Projecten/SetNacalcReferenties — welke referentieprojecten deze versie vergelijkt op stap 6.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNacalcReferenties(int versieId, int[] referentieIds)
        {
            if (!_uow.BudgetVersies.GetNoTracking().Any(v => v.Id == versieId)) return NotFound();
            var r = await _referentieProjectService.SetReferentiesVoorVersieAsync(versieId, referentieIds ?? Array.Empty<int>());
            TempData[r.Success ? "Message" : "Error"] = string.Join(" ", r.Messages.Select(m => m.Message));
            return RedirectToAction(nameof(BudgetActivityLijnen), new { versieId });
        }

        [HttpGet]
        public async Task<IActionResult> GetBouwIndexen(string type)
        {
            var lijst = await _bouwIndex.GetAlleIndexenAsync(type);
            return Json(lijst.Select(x => new {
                id = x.Id,
                jaar = x.Jaar,
                maand = x.Maand,
                indexWaarde = x.IndexWaarde,
                isActief = x.IsActief
            }));
        }

        // ── BudgetParams (stap 7) ─────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetParams(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var budgetParams = await _berekeningService.GetOrCreateParamsAsync(versieId);

            var aantalEenh = _uow.BudgetOppervlaktes.GetNoTracking()
                .Count(o => o.BudgetVersieId == versieId);

            // Zelfde bouwkost als stap 6: effectief bedrag per activiteit (opgeslagen alt.prijs
            // óf het voorstel/formule-bedrag), per woon-/comm. eenheid, met de correctie-%.
            var totaalBouw = await _budgetActivityService.GetTotaalGecorrigeerdeBouwAsync(versieId);

            var projectNaam = _projectService.GetProjectNameById(versie.ProjectId);

            var model = new BudgetParamsModel
            {
                BudgetVersieId = versieId,
                ProjectId      = versie.ProjectId,
                ProjectName    = projectNaam,
                BudgetNaam     = versie.BudgetMaster?.Naam ?? string.Empty,
                Versienummer   = versie.Versienummer,
                VersieLabel    = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                     ? $"v{versie.Versienummer}"
                                     : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus   = versie.Status,
                Params         = budgetParams,
                TotaalBouw     = totaalBouw,
                AantalEenheden = aantalEenh,
                AantalLiften   = versie.BudgetGegevens?.AantalLiften ?? 0
            };

            SetBudgetPageContext(model.ProjectId, projectNaam, nameof(BudgetParams), "Parameters", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2)
            {
                await PrepareWizardV2(versieId, 7);
                ViewData["DecennaleBevestigd"] = (versie.WaarschuwingenBevestigd ?? "").Split(',').Contains("decennale");
                return View("BudgetParamsV2", model);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BudgetParams(BudgetParamsModel model,
            decimal? pctProjectcoord, decimal? pctArchitect, decimal? pctIngenieur,
            decimal? pctDoelmarge, decimal? pctGrondmarge,
            decimal? pctVeiligheid, decimal? pctDecennale, decimal? pctABR, decimal? pctOnvoorzien,
            decimal? pctWetBreyne, decimal? pctSlGebouw, decimal? pctSlGrond)
        {
            // Alle percentages worden in procentpunten ingevoerd (2,00) en als fractie bewaard (0,02) — voordien stond de ene helft
            // in procentpunten en de andere helft als fractie, wat tot tienvoudige fouten leidde (okt. 2026).
            static decimal Fractie(decimal? pct) => Math.Round((pct ?? 0m) / 100m, 6);
            static decimal? FractieOfNull(decimal? pct) => pct.GetValueOrDefault() == 0m ? (decimal?)null : Fractie(pct);
            model.Params.VeiligheidscoordEPBPerc  = FractieOfNull(pctVeiligheid);
            model.Params.DecennaleGeslRuwbouwPerc = FractieOfNull(pctDecennale);
            model.Params.ABRPlaatsbeschrPerc      = FractieOfNull(pctABR);
            model.Params.OnvoorzienPerc           = Fractie(pctOnvoorzien);
            model.Params.WetBreynePerc            = Fractie(pctWetBreyne);
            model.Params.StraightloanGebouwPerc   = Fractie(pctSlGebouw);
            model.Params.StraightloanGrondPerc    = Fractie(pctSlGrond);

            // Projectcoördinatie / Architect / Ingenieur worden in procentpunten (5,25)
            // ingevoerd maar als fractie bewaard. Leeg of 0 bij architect/ingenieur =
            // "niet overschreven" (null) → blijft de standaard uit Instellingen volgen.
            model.Params.ProjectcoordinatiePerc = Math.Round((pctProjectcoord ?? 0m) / 100m, 6);
            model.Params.ArchitectPerc = pctArchitect.GetValueOrDefault() == 0m ? (decimal?)null : Math.Round(pctArchitect.Value / 100m, 6);
            model.Params.StudieIRPerc  = pctIngenieur.GetValueOrDefault() == 0m ? (decimal?)null : Math.Round(pctIngenieur.Value / 100m, 6);
            // Marges voor het verkoopvoorstel: zelfde conventie (procentpunten in, fractie opgeslagen, leeg = standaard).
            model.Params.DoelMargePerc  = pctDoelmarge.GetValueOrDefault()  == 0m ? (decimal?)null : Math.Round(pctDoelmarge.Value  / 100m, 6);
            model.Params.GrondMargePerc = pctGrondmarge.GetValueOrDefault() == 0m ? (decimal?)null : Math.Round(pctGrondmarge.Value / 100m, 6);

            var bestaand = await _db.BudgetParams
                .FirstOrDefaultAsync(p => p.BudgetVersieId == model.BudgetVersieId);

            if (bestaand == null)
            {
                model.Params.BudgetVersieId = model.BudgetVersieId;
                _db.BudgetParams.Add(model.Params);
            }
            else
            {
                bestaand.ProjectcoordinatiePerc  = model.Params.ProjectcoordinatiePerc;
                bestaand.ArchitectPerc           = model.Params.ArchitectPerc;
                bestaand.VeiligheidscoordEPBPerc = model.Params.VeiligheidscoordEPBPerc;
                bestaand.VentVerslaggeverForfait  = model.Params.VentVerslaggeverForfait;
                bestaand.StudieIRPerc             = model.Params.StudieIRPerc;
                bestaand.OpmetingSonderingForfait = model.Params.OpmetingSonderingForfait;
                bestaand.DecennaleGeslRuwbouwPerc = model.Params.DecennaleGeslRuwbouwPerc;
                bestaand.ABRPlaatsbeschrPerc      = model.Params.ABRPlaatsbeschrPerc;
                bestaand.InfrastructuurForfait    = model.Params.InfrastructuurForfait;
                bestaand.LiftPrijsPerStuk         = model.Params.LiftPrijsPerStuk;
                bestaand.WetBreynePerc            = model.Params.WetBreynePerc;
                bestaand.WetBreyneMaanden         = model.Params.WetBreyneMaanden;
                bestaand.StraightloanGebouwPerc   = model.Params.StraightloanGebouwPerc;
                bestaand.StraightloanGebouwMaanden= model.Params.StraightloanGebouwMaanden;
                bestaand.StraightloanGrondPerc    = model.Params.StraightloanGrondPerc;
                bestaand.StraightloanGrondMaanden = model.Params.StraightloanGrondMaanden;
                bestaand.AankoopprijsGrond        = model.Params.AankoopprijsGrond;
                bestaand.OnvoorzienPerc           = model.Params.OnvoorzienPerc;
                bestaand.PubliciteitForfait       = model.Params.PubliciteitForfait;
                bestaand.DoelMargePerc            = model.Params.DoelMargePerc;
                bestaand.GrondMargePerc           = model.Params.GrondMargePerc;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(BudgetVerkoop), new { versieId = model.BudgetVersieId });
        }

        // ── BudgetVerkoop (stap 8) ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetVerkoop(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var lijnen = await _db.BudgetVerkoopLijn
                .Where(l => l.BudgetVersieId == versieId)
                .OrderBy(l => l.SortOrder)
                .ToListAsync();

            var eenheden = await _db.BudgetOppervlaktes
                .Where(o => o.BudgetVersieId == versieId)
                .Select(o => o.EenheidNaam)
                .Distinct()
                .ToListAsync();

            var projectNaam = _projectService.GetProjectNameById(versie.ProjectId);

            var voorstel = await _verkoopVoorstelService.BerekenAsync(versieId);
            var unitOptions = BuildVerkoopUnitOptions(versie.ProjectId);
            ViewData["UnitOptions"] = unitOptions;

            var refBouw = await _db.BudgetPrijsReferentie
                .Where(p => p.PrijsType == "Bouw" && !p.Gearchiveerd && (p.ProjectId == null || p.ProjectId == versie.ProjectId))
                .OrderBy(p => p.Code).ThenBy(p => p.ProjectId.HasValue).ToListAsync();
            var refGrond = await _db.BudgetPrijsReferentie
                .Where(p => p.PrijsType == "Grond" && !p.Gearchiveerd && (p.ProjectId == null || p.ProjectId == versie.ProjectId))
                .OrderBy(p => p.Code).ThenBy(p => p.ProjectId.HasValue).ToListAsync();
            ViewData["RefBouw"]  = refBouw;
            ViewData["RefGrond"] = refGrond;

            var model = new BudgetVerkoopModel
            {
                Voorstel              = voorstel,
                UnitOptions           = unitOptions,
                EenhedenInfo          = BuildVerkoopEenhedenInfo(voorstel),
                BudgetVersieId        = versieId,
                ProjectId             = versie.ProjectId,
                ProjectName           = projectNaam,
                BudgetNaam            = versie.BudgetMaster?.Naam ?? string.Empty,
                Versienummer          = versie.Versienummer,
                VersieLabel           = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                            ? $"v{versie.Versienummer}"
                                            : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus          = versie.Status,
                Lijnen                = lijnen,
                PrijsReferentiesBouw  = refBouw,
                PrijsReferentiesGrond = refGrond,
                BeschikbareEenheden   = eenheden
            };

            SetBudgetPageContext(model.ProjectId, projectNaam, nameof(BudgetVerkoop), "Verkoop", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2)
            {
                // Waarschuwing stap 8 (vraagprijs onder minimum) kent enkel deze pagina
                var minima = (voorstel?.Eenheden ?? new List<BOCore.Budget.BudgetVerkoopVoorstelEenheidBO>()).ToDictionary(e => (e.EenheidNaam ?? "").Trim(), e => Math.Round(e.MinimumVerkoopprijs, 0), StringComparer.OrdinalIgnoreCase);
                var onder = lijnen.Count(l => l.Vraagprijs is > 0m && minima.TryGetValue((l.EenheidNaam ?? "").Trim(), out var mn) && l.Vraagprijs < mn);
                await PrepareWizardV2(versieId, 8, new Dictionary<int, (int, int)> { [8] = (0, onder) });
                return View("BudgetVerkoopV2", model);
            }
            return View(model);
        }

        /// <summary>Oppervlaktes en voorstelbedragen per eenheid (sleutel = naam, hoofdletterongevoelig) voor de rekenregels op stap 8.</summary>
        private static Dictionary<string, VerkoopEenheidInfo> BuildVerkoopEenhedenInfo(BOCore.Budget.BudgetVerkoopVoorstelBO voorstel)
        {
            var info = new Dictionary<string, VerkoopEenheidInfo>(StringComparer.OrdinalIgnoreCase);
            if (voorstel?.Eenheden == null) return info;
            foreach (var e in voorstel.Eenheden)
            {
                if (string.IsNullOrWhiteSpace(e.EenheidNaam)) continue;
                info[e.EenheidNaam.Trim()] = new VerkoopEenheidInfo
                {
                    OppGereduceerd = e.OppGereduceerd,
                    Grondopp       = e.Grondopp,
                    BewoonbareOpp  = e.BewoonbareOpp,
                    VoorstelGrond  = Math.Round(e.Grondwaarde, 0),
                    VoorstelBouw   = Math.Round(e.Bouwwaarde, 0),
                    Minimum        = Math.Round(e.MinimumVerkoopprijs, 0),
                    Markt          = e.MarktPrijs,
                    MarktPerM2     = e.MarktMediaanPerM2
                };
            }
            return info;
        }

        [HttpPost]
        public async Task<IActionResult> SaveBudgetVerkoop([FromBody] SaveVerkoopRequest req)
        {
            try
            {
                var bestaand = await _db.BudgetVerkoopLijn
                    .Where(l => l.BudgetVersieId == req.BudgetVersieId).ToListAsync();
                _db.BudgetVerkoopLijn.RemoveRange(bestaand);

                // Server-kant van de rekenregels (zelfde als het JS op de pagina): €/m² × oppervlakte of omgekeerd, vraagprijs = som.
                var oppResp = _budgetService.GetBudgetOppervlaktes(req.BudgetVersieId);
                var oppervlaktes = (oppResp.Success && oppResp.Values != null ? oppResp.Values : new List<BOCore.BudgetOppervlaktesBO>())
                    .Where(o => !string.IsNullOrWhiteSpace(o.EenheidNaam))
                    .GroupBy(o => o.EenheidNaam.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var nieuw = new List<BudgetVerkoopLijn>();
                for (int i = 0; i < req.Lijnen.Count; i++)
                {
                    var lijn = req.Lijnen[i].NaarEntiteit(req.BudgetVersieId, i);
                    var naam = (lijn.EenheidNaam ?? "").Trim();
                    if (oppervlaktes.TryGetValue(naam, out var opp))
                        ServiceCore.Budget.VerkoopLijnRekenregel.Herbereken(lijn, opp.OppGereduceerd, opp.Grondopp);
                    else
                        ServiceCore.Budget.VerkoopLijnRekenregel.Herbereken(lijn, 0m, 0m);
                    nieuw.Add(lijn);
                }
                _db.BudgetVerkoopLijn.AddRange(nieuw);
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult BlankVerkoopRij(int versieId, string eenheidNaam)
        {
            var projectId = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.Id == versieId).Select(v => (int?)v.ProjectId).FirstOrDefault();
            ViewData["UnitOptions"] = projectId.HasValue ? BuildVerkoopUnitOptions(projectId.Value) : new List<SelectListItem>();
            ViewData["RefBouw"]  = _db.BudgetPrijsReferentie.AsNoTracking()
                .Where(p => p.PrijsType == "Bouw" && !p.Gearchiveerd && (p.ProjectId == null || p.ProjectId == projectId)).OrderBy(p => p.Code).ToList();
            ViewData["RefGrond"] = _db.BudgetPrijsReferentie.AsNoTracking()
                .Where(p => p.PrijsType == "Grond" && !p.Gearchiveerd && (p.ProjectId == null || p.ProjectId == projectId)).OrderBy(p => p.Code).ToList();

            var lijn = new BudgetVerkoopLijn
            {
                BudgetVersieId = versieId,
                EenheidNaam    = eenheidNaam
            };
            return PartialView("Partials/_VerkoopRij", lijn);
        }

        // POST /Projecten/BudgetPrijsReferentieToevoegen — projectspecifieke prijscode (€/m²) vanaf stap 8; algemene codes via Instellingen › Budget.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BudgetPrijsReferentieToevoegen(int versieId, string prijsType, int code, decimal prijsPerM2, string omschrijving, DateTime? datum, string bron)
        {
            var versie = _uow.BudgetVersies.GetNoTracking().FirstOrDefault(v => v.Id == versieId);
            if (versie == null) return NotFound();

            var r = _prijsReferentieService.InsertUpdate(new BOCore.Budget.BudgetPrijsReferentieBO
            {
                ProjectId = versie.ProjectId, PrijsType = prijsType, Code = code, PrijsPerM2 = prijsPerM2,
                Omschrijving = omschrijving, Datum = datum, Bron = bron
            });
            TempData[r.Success ? "Message" : "Error"] = string.Join(" ", r.Messages.Select(m => m.Message));
            return RedirectToAction(nameof(BudgetVerkoop), new { versieId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BudgetPrijsReferentieVerwijderen(int versieId, int id)
        {
            var versie = _uow.BudgetVersies.GetNoTracking().FirstOrDefault(v => v.Id == versieId);
            if (versie == null) return NotFound();
            // Enkel codes van dit project; algemene codes worden in Instellingen beheerd.
            var eigen = _db.BudgetPrijsReferentie.AsNoTracking().Any(p => p.Id == id && p.ProjectId == versie.ProjectId);
            var r = eigen ? _prijsReferentieService.Delete(id) : new Response();
            if (!eigen) r.AddError("Alleen projectspecifieke codes kunnen hier verwijderd worden.");
            TempData[r.Success ? "Message" : "Error"] = string.Join(" ", r.Messages.Select(m => m.Message));
            return RedirectToAction(nameof(BudgetVerkoop), new { versieId });
        }

        private List<SelectListItem> BuildVerkoopUnitOptions(int projectId)
        {
            var resp = _unitService.GetUnitsByProjectIdForSelect(projectId, false);
            var items = new List<SelectListItem> { new SelectListItem("— geen eenheid —", "") };
            if (resp.Success && resp.Values != null)
                items.AddRange(resp.Values.Select(u => new SelectListItem(u.Display, u.ID.ToString())));
            return items;
        }

        // POST /Projecten/DoorzettenVerkoopNaarUnits — grond-/bouwwaarde van de verkooplijnen naar de Units.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoorzettenVerkoopNaarUnits(int versieId)
        {
            var response = await _verkoopVoorstelService.DoorzettenNaarUnitsAsync(versieId);
            return Json(new
            {
                success  = response.Success,
                messages = response.Messages.Select(m => new { type = m.Type.ToString(), text = m.Message }).ToList()
            });
        }

        // ── BudgetResultaat (stap 9) ──────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetResultaat(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var resultaat = await _berekeningService.BerekenAsync(versieId);
            resultaat.Verkoop = await _verkoopVoorstelService.SamenvattingAsync(versieId);
            resultaat.VersieNaam   = versie.VersieNaam   ?? string.Empty;
            resultaat.Versienummer = versie.Versienummer;

            // Lijnprijzen zijn per woon-/commerciële eenheid
            var aantalEenh = await _budgetActivityService.GetAantalWoonCommEenhedenAsync(versieId);

            // Dezelfde bouwkost als in de kostprijs (gecorrigeerde bedragen, incl. voorstellen van nog niet opgeslagen lijnen) —
            // voordien telde deze KPI de ruwe alt.-prijzen zonder correctie, waardoor twee verschillende "bouwkosten" op één pagina stonden.
            var altBouw = resultaat.TotaalBouw;

            var nacBouw = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId)
                .AsEnumerable()
                .Sum(l => (l.NacalcPrijsPerEenheid ?? 0m) * aantalEenh);

            var sStart  = versie.BudgetGegevens?.SIndexStart  ?? 100m;
            var sHuidig = versie.BudgetGegevens?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
            var iStart  = versie.BudgetGegevens?.IIndexStart  ?? 100m;
            var iHuidig = versie.BudgetGegevens?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I2021");
            var gewogen = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            var andereVersies = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.BudgetMasterId == versie.BudgetMasterId && v.Id != versieId)
                .OrderByDescending(v => v.Versienummer)
                .ToList();

            var projectNaam = _projectService.GetProjectNameById(versie.ProjectId);

            var voorstel = await _verkoopVoorstelService.BerekenAsync(versieId);

            var model = new BudgetResultaatModel
            {
                Voorstel             = voorstel,
                BudgetVersieId       = versieId,
                ProjectId            = versie.ProjectId,
                ProjectName          = projectNaam,
                BudgetNaam           = versie.BudgetMaster?.Naam ?? string.Empty,
                Versienummer         = versie.Versienummer,
                VersieLabel          = string.IsNullOrWhiteSpace(versie.VersieNaam)
                                           ? $"v{versie.Versienummer}"
                                           : $"v{versie.Versienummer} • {versie.VersieNaam}",
                VersieStatus         = versie.Status,
                BudgetMasterId       = versie.BudgetMasterId,
                Resultaat            = resultaat,
                TotaalBouwAlternatief = altBouw,
                // De bewaarde nacalc per lijn is al geïndexeerd naar de huidige index (stap 6, referentieprojecten): niet nog eens indexeren.
                TotaalBouwNacalc     = nacBouw,
                GewogenFactor        = gewogen,
                AndereVersies        = andereVersies
            };

            SetBudgetPageContext(model.ProjectId, projectNaam, nameof(BudgetResultaat), "Resultaat", new { versieId }, versieId: versieId, versieLabel: model.VersieLabel);
            if (GebruikGlV2)
            {
                var vkLijnen = _uow.BudgetVerkoopLijn.GetNoTracking().Where(l => l.BudgetVersieId == versieId).ToList();
                var minima = (voorstel?.Eenheden ?? new List<BOCore.Budget.BudgetVerkoopVoorstelEenheidBO>()).ToDictionary(e => (e.EenheidNaam ?? "").Trim(), e => Math.Round(e.MinimumVerkoopprijs, 0), StringComparer.OrdinalIgnoreCase);
                var onder = vkLijnen.Count(l => l.Vraagprijs is > 0m && minima.TryGetValue((l.EenheidNaam ?? "").Trim(), out var mn) && l.Vraagprijs < mn);
                await PrepareWizardV2(versieId, 9, new Dictionary<int, (int, int)> { [8] = (0, onder) });
                return View("BudgetResultaatV2", model);
            }
            return View(model);
        }

        // ── BudgetVergelijken ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BudgetVergelijken(int masterId)
        {
            var master = await _db.BudgetMaster
                .Include(m => m.BudgetVersies)
                .FirstOrDefaultAsync(m => m.Id == masterId);

            if (master == null) return NotFound();

            var project = await _db.Project.FindAsync(master.ProjectId);

            var model = new BudgetVergelijkenModel
            {
                BudgetMasterId = masterId,
                ProjectId      = master.ProjectId,
                ProjectName    = project?.ProjectName ?? string.Empty,
                BudgetNaam     = master.Naam,
                AlleVersies    = master.BudgetVersies
                                    .OrderByDescending(v => v.Versienummer)
                                    .ToList()
            };

            SetBudgetPageContext(model.ProjectId, model.ProjectName, nameof(BudgetVergelijken), "Versies vergelijken", new { masterId });
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> VergelijkenResultaat([FromBody] VergelijkRequest req)
        {
            if (req?.VersieIds == null || !req.VersieIds.Any())
                return Json(new { success = false, message = "Geen versies geselecteerd." });

            var resultaten = await _berekeningService.GetVergelijkingAsync(req.VersieIds);
            foreach (var r in resultaten)
            {
                try { r.Verkoop = await _verkoopVoorstelService.SamenvattingAsync(r.BudgetVersieId); }
                catch { r.Verkoop = null; }
            }
            return Json(new { success = true, resultaten });
        }

        [HttpPost]
        public async Task<IActionResult> HerstelVersie(int versieId)
        {
            var bron = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (bron == null)
                return Json(new { success = false, message = "Versie niet gevonden." });

            var maxVersie = _uow.BudgetVersies.GetNoTracking()
                .Where(v => v.BudgetMasterId == bron.BudgetMasterId)
                .Max(v => (int?)v.Versienummer) ?? 0;

            var nieuw = new BudgetVersie
            {
                BudgetMasterId = bron.BudgetMasterId,
                ProjectId      = bron.ProjectId,
                Versienummer   = maxVersie + 1,
                VersieNaam     = $"Herstel van v{bron.Versienummer}",
                Status         = "Concept",
                IsHuidig       = false,
                CreatedAt      = DateTime.Now
            };
            _db.BudgetVersie.Add(nieuw);
            await _db.SaveChangesAsync();

            // Volledige kopie via dezelfde helper als "Nieuwe versie" (okt. 2026): elk veld van elke budgettabel.
            var kopie = _budgetService.KopieerVersieInhoud(versieId, nieuw.Id);
            if (!kopie.Success)
                return Json(new { success = false, message = kopie.Messages.FirstOrDefault()?.Message ?? "Kopiëren mislukt." });

            return Json(new { success = true, nieuweVersieId = nieuw.Id, versienummer = nieuw.Versienummer });
        }

        // ── DownloadBudgetPDF ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DownloadBudgetPDF(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var resultaat = await _berekeningService.BerekenAsync(versieId);
            resultaat.Verkoop = await _verkoopVoorstelService.SamenvattingAsync(versieId);

            var sStart  = versie.BudgetGegevens?.SIndexStart  ?? 100m;
            var sHuidig = versie.BudgetGegevens?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
            var iStart  = versie.BudgetGegevens?.IIndexStart  ?? 100m;
            var iHuidig = versie.BudgetGegevens?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I2021");
            var gewogen = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            var activiteitLijnen = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId).ToList();

            var activiteiten = _uow.Activities.GetNoTracking().ToList();
            var groepen      = _uow.ActivityGroups.GetNoTracking().OrderBy(g => g.Lot).ToList();
            var oppervlaktes = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId).OrderBy(o => o.SortOrder).ToList();

            var document = new BudgetDocument(resultaat, versie, activiteitLijnen, activiteiten, groepen, oppervlaktes, gewogen);
            var pdfBytes = document.GeneratePdf();

            var bestandsnaam = $"Budget_{versie.VersieNaam}_v{versie.Versienummer}_{DateTime.Now:yyyyMMdd}.pdf"
                .Replace(Path.GetInvalidFileNameChars(), '_');

            return File(pdfBytes, "application/pdf", bestandsnaam);
        }

        // ── DownloadBudgetExcel ───────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DownloadBudgetExcel(int versieId)
        {
            var versie = _uow.BudgetVersies.GetNoTracking()
                .Include(v => v.BudgetMaster)
                .Include(v => v.BudgetGegevens)
                .FirstOrDefault(v => v.Id == versieId);

            if (versie == null) return NotFound();

            var resultaat = await _berekeningService.BerekenAsync(versieId);
            resultaat.Verkoop = await _verkoopVoorstelService.SamenvattingAsync(versieId);

            var sStart  = versie.BudgetGegevens?.SIndexStart  ?? 100m;
            var sHuidig = versie.BudgetGegevens?.SIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("S");
            var iStart  = versie.BudgetGegevens?.IIndexStart  ?? 100m;
            var iHuidig = versie.BudgetGegevens?.IIndexHuidig ?? await _bouwIndex.GetActieveIndexAsync("I2021");
            var gewogen = _bouwIndex.BerekenGewogenFactor(sStart, sHuidig, iStart, iHuidig);

            var activiteitLijnen = _uow.BudgetActivityLijnen.GetNoTracking()
                .Where(l => l.BudgetVersieId == versieId).ToList();

            var activiteiten = _uow.Activities.GetNoTracking().ToList();
            var groepen      = _uow.ActivityGroups.GetNoTracking().OrderBy(g => g.Lot).ToList();
            var oppervlaktes = _uow.BudgetOppervlaktes.GetNoTracking()
                .Where(o => o.BudgetVersieId == versieId).OrderBy(o => o.SortOrder).ToList();

            var excelBytes = _excelService.GenereerBudgetExcel(
                resultaat, versie, activiteitLijnen, activiteiten, groepen, oppervlaktes, gewogen);

            var bestandsnaam = $"Budget_{versie.VersieNaam}_v{versie.Versienummer}_{DateTime.Now:yyyyMMdd}.xlsx"
                .Replace(Path.GetInvalidFileNameChars(), '_');

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                bestandsnaam);
        }
    }
}
