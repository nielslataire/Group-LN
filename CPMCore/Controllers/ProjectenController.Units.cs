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
    /// <summary>Eenheden van een project: lijst (legacy + gl-v2), toevoegen/bewerken, koppelen, afwerkingen, export/print, aandelen basisakte (het gl-v2-formulier staat in ProjectenController.UnitFormV2.cs). Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Units/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL EENHEDEN ==========

        [HttpGet]
        //[Breadcrumb("Eenheden")]
        [Breadcrumb("Eenheden", FromAction = "Detail")]
        public ActionResult DetailUnits(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            DetailUnitsModel model = new DetailUnitsModel();
            model = FillDetailUnitModel(projectid);

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
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            // gl-v2 (DetailUnitsV2, design-handoff punt 16a/16d): het kruimelpad stopt bij de projectnaam
            // i.p.v. nog een "Eenheden"-knoop toe te voegen die de paginatitel herhaalt — punt 13,
            // regel 2, zelfde fix als op DetailClients. Enkel in de gl-v2-tak: de legacy view rendert
            // datzelfde SmartBreadcrumbs-pad en houdt daar zijn eigen laatste "Eenheden"-knoop.
            var useGlV2Units = ViewData["UseGlV2Layout"] as bool? == true;
            ViewData["BreadcrumbNode"] = useGlV2Units ? projectDetail : projectUnits;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectUnits = _ps.HasWrite(PermissionCodes.ProjectsUnits);
            ViewBag.CanDeleteProjectUnits = _ps.HasDelete(PermissionCodes.ProjectsUnits);
            if (useGlV2Units)
            {
                model.GlV2 = BuildDetailUnitsV2Vm(
                    projectid,
                    model.ProjectName,
                    model.ProjectLandShare,
                    ViewBag.CanWriteProjectUnits as bool? ?? false);
            }
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Eenheden");
            return View(useGlV2Units ? "DetailUnitsV2" : "DetailUnits", model);
        }

        [HttpGet]
        //[Breadcrumb("Eenheid toevoegen")]
        [Breadcrumb("Eenheid toevoegen", FromAction = "DetailUnits")]
        public ActionResult AddUnit(int projectid, int copyFrom = 0, string? returnUrl = null)
        {
            // gl-v2 (design-handoff 16c): gedeeld formulier, zie ProjectenController.UnitFormV2.cs.
            if (ViewData["UseGlV2Layout"] as bool? == true)
                return UnitFormV2Get(projectid, 0, copyFrom, returnUrl)!;
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            ViewData["Referrer"] = referrer;
            var model = new AddUnitModel();
            var service = _unitService;
            var service2 = _projectService;

            // Get Units for attached unit select
            var u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(projectid);
            if (u2response.Success) model.AttachableUnits = u2response.Values;

            // Get GroupTypes
            var responsegroup = service.GetUnitGroupTypes();
            if (responsegroup.Success) model.GroupTypes = responsegroup.Values;

            // Get Subtypes
            var responsetypes = service.GetUnitTypesByGroupId(model.SelectedGroupType);
            if (responsetypes.Success) model.Types = responsetypes.Values;

            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);
            model.ProjectLandShare = (int)service2.GetProjectLandshareById(projectid);

            var constval = new UnitConstructionValueBO();
            constval.PaymentGroupId = 0;
            model.ConstructionValues.Add(constval);

            var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid);
            if (responsepaymentgroups.Success) model.PaymentGroups = responsepaymentgroups.Values;
            ViewBag.paymentgroups = model.PaymentGroups;


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
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddUnit", "Projecten", "Eenheden toevoegen")
            {
                Parent = projectUnits,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Eenheid toevoegen");
            return View(model);
        }
        [HttpPost]
        public ActionResult AddUnit(DetailUnitsModel Model)
        {
            if (Model.SelectedType == 0)
                return RedirectToAction("DetailUnits", Model);
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            Model.AddUnit.ProjectId = Model.ProjectId;
            Model.AddUnit.Type.Id = Model.SelectedType;

            if (Model.AddUnit.AttachedUnitsId == 0)
                Model.AddUnit.AttachedUnitsId = null;
            var service = _unitService;
            var service2 = _projectService;
            var response = service.InsertUpdateUnit(Model.AddUnit);
            if (response.Success == true)
            {
                foreach (var item in Model.ConstructionValues)
                {
                    item.UnitId = response.InsertedId;
                    var responseConst = service.InsertUpdateConstructionValue(item);
                }
                Model.AddUnit.Name = "";
                var response2 = service.GetGroupedUnitsByProjectId(Model.ProjectId);
                Model.UnitsGrouped = response2.Values;
                // Get GroupTypes
                var responsegroup = service.GetUnitGroupTypes();
                if ((responsegroup.Success))
                    Model.GroupTypes = responsegroup.Values;
                // Get Subtypes
                var responsetypes = service.GetUnitTypesByGroupId(Model.SelectedGroupType);
                if ((responsetypes.Success))
                    Model.Types = responsetypes.Values;

                Model.ProjectName = service2.GetProjectNameById(Model.ProjectId);

                AddMessage("success", "De eenheid is aan het project toegevoegd", "Geslaagd!");
                return RedirectToAction("DetailUnits", new { projectid = Model.ProjectId });
            }
            else
            {
                AddMessage("error", "De eenheid is NIET toegevoegd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                return RedirectToAction("DetailUnits", new { projectid = Model.ProjectId });
            }
        }
        [HttpGet]
        [Breadcrumb("Eenheid bewerken", FromAction = "DetailUnits")]
        //[Breadcrumb("Eenheid bewerken")]
        public async Task<ActionResult> EditUnit(int projectid, int unitid, string? returnUrl = null)
        {
            // gl-v2 (design-handoff 16b): gedeeld formulier, zie ProjectenController.UnitFormV2.cs.
            // null = een KOPPELING-pseudo-eenheid (IsLink) — die houdt haar legacy formulier.
            if (ViewData["UseGlV2Layout"] as bool? == true)
            {
                var v2 = UnitFormV2Get(projectid, unitid, 0, returnUrl);
                if (v2 != null) return v2;
            }
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            TempData["Referrer"] = referrer;
            EditUnitModel model = new EditUnitModel();
            var service = _unitService;
            var service2 = _projectService;

            // Get Unit
            var response = service.GetUnitById(unitid);
            if ((response.Success))
                model.Unit = response.Value;
            // linkedunits
            foreach (var u in model.Unit.LinkedUnits)
                model.SelectedUnits.Add(u.Id);
            if (model.Unit.IsLink == true)
                model.Type = EditUnitModel.EnumType.Koppeling;
            else
                model.Type = EditUnitModel.EnumType.Eenheid;
            // Get Units for select
            var uresponse = service.GetUnitsByProjectIdForSelect(model.Unit.ProjectId, model.Unit.Type.Id);
            if ((uresponse.Success))
                model.Units = uresponse.Values;
            // Get Units for attached unit select
            var u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(model.Unit.ProjectId, unitid);
            if ((u2response.Success))
                model.AttachableUnits = u2response.Values;
            // Get GroupTypes
            var responsegroup = service.GetUnitGroupTypes();
            if ((responsegroup.Success))
                model.GroupTypes = responsegroup.Values;
            model.SelectedGroupType = model.Unit.Type.GroupId;
            // Get Rooms
            var responserooms = service.GetRooms(unitid);
            if ((responserooms.Success))
                model.Rooms = responserooms.Values;
            model.Rooms = model.Rooms.OrderBy(m => m.Type).ToList();
            // Get Constructionvalues
            var responseconstructionvalues = service.GetConstructionValues(unitid);
            if ((responseconstructionvalues.Success))
                model.ConstructionValues = responseconstructionvalues.Values;

            // Get FinishingOptions (auto-migrate legacy CVs on first open)
            service.EnsureDefaultFinishingOption(unitid);
            var responseFinishing = service.GetFinishingOptions(unitid);
            model.FinishingOptions = responseFinishing.Success ? responseFinishing.Values : new List<UnitFinishingOptionBO>();

            // Get Subtypes
            var responsetypes = service.GetUnitTypesByGroupId(model.Unit.Type.GroupId);
            if ((responsetypes.Success))
                model.Types = responsetypes.Values;
            model.SelectedType = model.Unit.Type.Id;

            // Get PaymentGroups
            var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid);
            if ((responsepaymentgroups.Success))
                model.PaymentGroups = responsepaymentgroups.Values;

            ViewBag.PaymentGroups = model.PaymentGroups
            .Select(pg => new SelectListItem { Value = pg.ID.ToString(), Text = pg.Display })
            .ToList();

            if (model.Unit.PaymentGroupId is not null)
                model.SelectedPaymentGroup = model.Unit.PaymentGroupId;
            else
                model.SelectedPaymentGroup = 0;

            model.ProjectId = model.Unit.ProjectId;
            model.ProjectName = service2.GetProjectNameById(model.Unit.ProjectId);
            model.ExecutionPlans = await BuildUnitExecutionPlansVm(model.Unit.Id);


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
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddUnit", "Projecten", model.Unit.Name + " - Bewerken")
            {
                Parent = projectUnits,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - {model.Unit.Name} - Bewerken");
            return View(model);
        }
        [HttpPost]
        public async Task<ActionResult> EditUnit(EditUnitModel Model, IFormFile? file, List<IFormFile>? executionPlanFiles, List<string>? executionPlanNames, List<int>? deleteExecutionPlanIds)
        {
            //Referrer
            var Referrer = TempData["Referrer"];

            StringCollection validtypes = new StringCollection();
            validtypes.Add("application/pdf");
            string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".pdf";
            if (file != null && file.Length > 0)
            {
                if ((!validtypes.Contains(file.ContentType)))
                    ModelState.AddModelError("PdfUpload", "Verkeerd type gekozen, kies een pdf");
            }
            if (executionPlanFiles != null)
            {
                foreach (var planFile in executionPlanFiles.Where(f => f != null && f.Length > 0))
                {
                    if (!validtypes.Contains(planFile.ContentType))
                    {
                        ModelState.AddModelError("ExecutionPlansUpload", "Alle uitvoeringsplannen moeten PDF bestanden zijn.");
                        break;
                    }
                }
            }
            // De tweede (en laatste) plek waar Units.AttachedUnitId gezet kan worden — de dropdown
            // "Gekoppelde eenheid" hieronder. GetUnitsByProjectIdForSelectAttachedUnit houdt leden van
            // een KOPPELING al buiten de OUDER-keuze, maar belet niet dat de eenheid die je hier bewerkt
            // zelf een lid IS. Zou je dan toch een ouder kiezen, dan hangt die eenheid via beide
            // koppelmechanismen vast en telt ze dubbel — zie DESIGN.md, "De twee koppelmechanismen op
            // Units". Dat is de spiegelkant van de filter op de koppeldialoog en het laatste gat.
            // Een lid dat hier bewaard wordt ZONDER een ouder te kiezen, blijft gewoon werken.
            // Geen ModelState-fout maar een blokkade met omleiding: deze legacy view toont geen
            // ValidationSummary en vult bij een ongeldige POST haar keuzelijsten niet opnieuw, dus een
            // ModelState-fout zou hier als een stille, half lege pagina landen. Omleiden naar de GET
            // geeft een schoon formulier mét uitleg. Kost de gebruiker de andere wijzigingen uit deze
            // POST — aanvaardbaar voor een handeling die sowieso geweigerd wordt.
            if (Model.Unit is not null && Model.Unit.AttachedUnitsId is not null and not 0)
            {
                var edited = _unitService.GetUnitById(Model.Unit.Id).Value;
                if (edited?.LinkedUnitId is not null and not 0)
                {
                    AddMessage("error",
                        $"{edited.Name} is al lid van een koppeling en kan daarom niet ook onder een andere eenheid gehangen worden. Haal ze eerst uit die koppeling.",
                        "Fout!");
                    return RedirectToAction("EditUnit", "Projecten", new { projectid = Model.ProjectId, unitid = Model.Unit.Id });
                }
            }

            if (ModelState.IsValid)
            {
                if ((file != null && file.Length > 0))
                {
                    var uploadedFileName = await UploadAssetToStorageAsync(file, "plans");
                    if (string.IsNullOrWhiteSpace(uploadedFileName))
                    {
                        ModelState.AddModelError("PdfUpload", "Plan upload naar storage API mislukt.");
                        Model.ExecutionPlans = await BuildUnitExecutionPlansVm(Model.Unit.Id);
                        SetPageHeader("bx bx-building-house", $"{_projectService.GetProjectNameById(Model.ProjectId)} - {Model.Unit.Name} - Bewerken");
                        return View(Model);
                    }

                    Model.Unit.Plan = uploadedFileName;
                }



                ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
                Model.Unit.ProjectId = Model.ProjectId;
                Model.Unit.Type.Id = Model.SelectedType;
                if (Model.SelectedPaymentGroup != 0 && Model.SelectedPaymentGroup is not null)
                    Model.Unit.PaymentGroupId = Model.SelectedPaymentGroup;
                if ((Model.Unit.IsLink))
                {
                    foreach (var i in Model.SelectedUnits)
                    {
                        UnitBO bo = new UnitBO();
                        bo.Id = i;
                        Model.Unit.LinkedUnits.Add(bo);
                    }
                    Model.Unit.Name = "KOPPELING";
                }
                var service = _unitService;
                var service2 = _projectService;
                try
                {
                    var response = service.InsertUpdateUnit(Model.Unit);
                    if (response.Success == false)
                        throw new ApplicationException(response.Messages.SingleOrDefault().Message);
                    Response response2 = new Response();
                    foreach (var room in Model.Rooms)
                        response2 = service.InsertUpdateRoom(room);
                    if (response2.Success == false)
                    {
                        foreach (var message in response2.Messages)
                            throw new ApplicationException(message.Message);
                    }
                    Response response3 = new Response();
                    foreach (var constructionvalue in Model.ConstructionValues)
                    {
                        response3 = service.InsertUpdateConstructionValue(constructionvalue);
                        if (response3.Success)
                            constructionvalue.Id = response3.InsertedId;
                    }
                    if (response3.Success == false)
                    {
                        foreach (var message2 in response3.Messages)
                            throw new ApplicationException(message2.Message);
                    }
                    List<UnitConstructionValueBO> tableresult = new List<UnitConstructionValueBO>();
                    var responsetable = service.GetConstructionValues(Model.Unit.Id);
                    if ((responsetable.Success))
                        tableresult = responsetable.Values;
                    List<int> deleteids = new List<int>();
                    foreach (var result in tableresult)
                    {
                        if (Model.ConstructionValues.Exists(m => m.Id == result.Id))
                        {
                        }
                        else
                            deleteids.Add(result.Id);
                    }
                    var response4 = service.DeleteConstructionValues(deleteids);
                    if (response4.Success == false)
                        throw new ApplicationException(response4.Messages.SingleOrDefault().Message);
                    if (deleteExecutionPlanIds != null && deleteExecutionPlanIds.Count > 0)
                    {
                        var plansToDelete = await _db.UnitExecutionPlan
                            .Where(x => x.UnitId == Model.Unit.Id && deleteExecutionPlanIds.Contains(x.Id))
                            .ToListAsync();

                        if (plansToDelete.Count > 0)
                        {
                            _db.UnitExecutionPlan.RemoveRange(plansToDelete);
                        }
                    }

                    if (executionPlanFiles != null && executionPlanFiles.Count > 0)
                    {
                        var names = executionPlanNames ?? new List<string>();
                        var index = 0;
                        foreach (var executionPlanFile in executionPlanFiles.Where(f => f != null && f.Length > 0))
                        {
                            var uploadedPlanFileName = await UploadAssetToStorageAsync(executionPlanFile, "plans");
                            if (string.IsNullOrWhiteSpace(uploadedPlanFileName))
                                throw new ApplicationException("Uitvoeringsplan upload naar storage API mislukt.");

                            var enteredName = index < names.Count ? names[index] : null;
                            var planName = string.IsNullOrWhiteSpace(enteredName)
                                ? Path.GetFileNameWithoutExtension(executionPlanFile.FileName)
                                : enteredName.Trim();

                            _db.UnitExecutionPlan.Add(new UnitExecutionPlan
                            {
                                UnitId = Model.Unit.Id,
                                Name = planName,
                                FileId = uploadedPlanFileName,
                                CreatedDate = DateTime.UtcNow,
                                CreatedByUserId = User.FindFirst(CpmClaims.UserId)?.Value
                            });
                            index++;
                        }
                    }

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    AddMessage("error", "De eenheid is NIET volledig bijgewerkt, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                }
                finally
                {
                    AddMessage("success", "De eenheid is met succes bijgewerkt", "Geslaagd!");
                }
                if (Referrer != null)
                {
                    return Redirect(Referrer.ToString());
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            Model.ExecutionPlans = await BuildUnitExecutionPlansVm(Model.Unit.Id);
            SetPageHeader("bx bx-building-house", $"{_projectService.GetProjectNameById(Model.ProjectId)} - {Model.Unit.Name} - Bewerken");
            return View(Model);
        }

        private async Task<List<UnitExecutionPlanVm>> BuildUnitExecutionPlansVm(int unitId)
        {
            var plans = await _db.UnitExecutionPlan
                .Where(x => x.UnitId == unitId && x.DeletedDate == null)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return plans.Select(x => new UnitExecutionPlanVm
            {
                Id = x.Id,
                Name = x.Name,
                FileId = x.FileId,
                Url = GetSignedAssetUrlByFileName(x.FileId, "plans")
            }).ToList();
        }

        public DetailUnitsModel FillDetailUnitModel(int id)
        {
            var model = new DetailUnitsModel();
            var service = _unitService;
            var service2 = _projectService;



            //Get Units
            var responseu = service.GetUnitsByProjectId(id);
            if (responseu.Success) model.ProjectUnits = responseu.Values;


            //// Get Units for attached unit select
            //var u2response = service.GetUnitsByProjectIdForSelectAttachedUnit(id);
            //if (u2response.Success) model.AttachableUnits = u2response.Values;

            //// Get Units
            //var response = service.GetGroupedUnitsByProjectId(id);
            //model.UnitsGrouped = response.Values;

            //// Get GroupTypes
            //var responsegroup = service.GetUnitGroupTypes();
            //if (responsegroup.Success) model.GroupTypes = responsegroup.Values;
            //model.SelectedGroupType = 1;

            //// Get Subtypes
            //var responsetypes = service.GetUnitTypesByGroupId(model.SelectedGroupType);
            //if (responsetypes.Success) model.Types = responsetypes.Values;

            model.ProjectId = id;
            model.ProjectName = service2.GetProjectNameById(id);
            model.ProjectLandShare = (int)service2.GetProjectLandshareById(id);

            //var constval = new UnitConstructionValueBO();
            //constval.PaymentGroupId = 0;
            //model.ConstructionValues.Add(constval);

            //var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(id);
            //if (responsepaymentgroups.Success) model.PaymentGroups = responsepaymentgroups.Values;
            //ViewBag.paymentgroups = model.PaymentGroups;

            return model;
        }
        [HttpPost]
        public PartialViewResult BlankConstructionValueRow(int unitid, int projectid, int? finishingOptionId = null)
        {
            UnitConstructionValueBO bo = new UnitConstructionValueBO();
            bo.UnitId = unitid;
            bo.PaymentGroupId = 0;
            bo.FinishingOptionId = finishingOptionId;
            var service2 = _projectService;
            var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid);
            ViewBag.paymentgroups = responsepaymentgroups.Values;
            return PartialView("_ConstructionValueRow", bo);
        }

        [HttpPost]
        public JsonResult AddFinishingOption(int unitId, string name)
        {
            var bo = new UnitFinishingOptionBO { UnitId = unitId, Name = name, SortOrder = 0 };
            var resp = _unitService.InsertUpdateFinishingOption(bo);
            if (!resp.Success)
                return Json(new { error = "Kon optie niet opslaan" });
            return Json(new { id = resp.InsertedId, name });
        }

        [HttpPost]
        public JsonResult RenameFinishingOption(int id, string name)
        {
            var resp = _unitService.GetFinishingOptionById(id);
            if (!resp.Success) return Json(new { error = "Niet gevonden" });
            resp.Value.Name = name;
            _unitService.InsertUpdateFinishingOption(resp.Value);
            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult RemoveFinishingOption(int id)
        {
            _unitService.DeleteFinishingOption(id);
            return Json(new { ok = true });
        }

        [HttpPost]
        public PartialViewResult BlankFinishingOptionSection(int optionId, int unitId, int projectId)
        {
            var resp = _unitService.GetFinishingOptionById(optionId);
            var bo = resp.Success ? resp.Value : new UnitFinishingOptionBO { Id = optionId, UnitId = unitId };
            var pgResp = _projectService.GetProjectPaymentGroupsForSelect(projectId);
            ViewBag.PaymentGroups = pgResp.Values.Select(pg => new SelectListItem { Value = pg.ID.ToString(), Text = pg.Display }).ToList();
            ViewBag.UnitId = unitId;
            ViewBag.ProjectId = projectId;
            return PartialView("_FinishingOptionSection", bo);
        }
        [HttpPost]
        public PartialViewResult BlankRoomRow(int unitid)
        {
            RoomBO bo = new RoomBO();
            bo.UnitId = unitid;
            bo.Number = 1;
            return PartialView("_RoomEditorRow", bo);
        }
        [HttpGet]
        public ActionResult ModalDeleteUnit(int id)
        {
            var viewModel = new UnitBO();
            if (id != 0)
            {
                var dservice = _unitService;
                viewModel = dservice.GetUnitById(id).Value;
            }
            return PartialView("_ModalDeleteUnit", viewModel);
        }
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsUnits)]
        public ActionResult DeleteUnit(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = _unitService;
                List<int> ids = new List<int>();
                ids.Add(id);
                var response = service.DeleteUnit(ids);
                if (response.Success == true)
                {
                    AddMessage("success", "De eenheid is verwijderd", "Geslaagd!");
                    return RedirectToAction("DetailUnits", "Projecten", new { projectid = projectid });
                }
                else
                {
                    AddMessage("error", "De eenheid is niet verwijderd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                    return RedirectToAction("DetailUnits", "Projecten", new { projectid = projectid });
                }
            }
            return RedirectToAction("DetailUnits", "Projecten", new { projectid = projectid });
        }
        /// <summary>Waarom deze eenheid niet in een samengestelde KOPPELING (Units.IsLink) mag — of null
        /// wanneer er niets in de weg staat. De vier redenen komen uit de conflictanalyse in DESIGN.md
        /// ("De twee koppelmechanismen op Units"): een eenheid die al onder een lot hangt of al lid is van
        /// een koppeling zou via beide mechanismen tegelijk geteld worden, en een KOPPELING-rij zelf in een
        /// nieuwe koppeling stoppen laat de translator haar naam en aandeel uit de leden herschrijven.</summary>
        private static string? UnitLinkBlockedReason(UnitBO? unit)
        {
            if (unit is null || unit.Id == 0) return "de eenheid werd niet gevonden.";
            if (unit.IsLink) return "dit is zelf al een samengestelde koppeling.";
            if (unit.AttachedUnitsId is not null and not 0)
                return "ze hangt al als gekoppelde eenheid onder een andere eenheid. Maak die koppeling eerst los op het eenheidsformulier.";
            if (unit.LinkedUnitId is not null and not 0)
                return "ze is al lid van een andere koppeling.";
            return null;
        }

        [HttpGet]
        public ActionResult ModalAddUnitLink(int id)
        {
            var viewModel = new AddUnitLinkModel();
            var service = _unitService;
            // Get Unit
            var response = service.GetUnitById(id);
            if ((response.Success))
                viewModel.SelectedUnit = response.Value;

            // Deze dialoog maakt een samengestelde KOPPELING-eenheid aan (Units.IsLink). Dat is een
            // ander mechanisme dan Units.AttachedUnitId, waarop de eenhedenboom van
            // Projecten/DetailUnitsV2 en Projecten/DetailV2 rusten. Beide op dezelfde eenheid toepassen
            // laat haar prijs en aandeel twee keer meetellen (één keer in het lot waaronder ze hangt,
            // één keer in de pseudo-eenheid) — zie DESIGN.md, "De twee koppelmechanismen op Units".
            // GetUnitsForLinkSelect houdt daarom alleen eenheden over die nog aan niets vasthangen; de
            // aangeklikte eenheid valt vanzelf weg (excludeUnitId), wat de losse Units.Remove-regel
            // hieronder vroeger met de hand deed.
            var response2 = service.GetUnitsForLinkSelect(
                viewModel.SelectedUnit.ProjectId,
                viewModel.SelectedUnit.Type.Id,
                id);
            if ((response2.Success))
                viewModel.Units = response2.Values;

            return PartialView("_ModalAddLink", viewModel);
        }
        [HttpPost]
        public ActionResult AddUnitLink(AddUnitLinkModel model)
        {
            Response response = new Response();

            // Dezelfde filter als op de keuzelijst in ModalAddUnitLink, nu ook aan de serverkant: een
            // oud of opengebleven tabblad mag geen eenheid tot lid van een koppeling maken die inmiddels
            // al onder een lot hangt of al lid is van een andere koppeling. Zie DESIGN.md, "De twee
            // koppelmechanismen op Units", voor waarom dat dubbel tellen oplevert.
            // Alleen de GEKOZEN eenheden worden gecontroleerd: model.SelectedUnit.Id komt niet mee uit
            // het formulier (er staat geen hidden field voor) en is hier dus 0 — HandleLinkedUnits
            // negeert die 0, dus de aangeklikte eenheid wordt zelf géén lid van de koppeling. Dat gedrag
            // blijft bewust ongewijzigd; het hier "repareren" zou veranderen wát een koppeling bevat.
            foreach (var candidateId in (model.SelectedUnits ?? new List<int>()).Where(x => x != 0).Distinct())
            {
                var candidate = _unitService.GetUnitById(candidateId).Value;
                var blocked = UnitLinkBlockedReason(candidate);
                if (blocked is not null)
                {
                    AddMessage("error", $"{candidate?.Name ?? "Deze eenheid"} kan niet gekoppeld worden: {blocked}", "Fout!");
                    return RedirectToAction("DetailUnits", "Projecten", new { projectid = model.SelectedUnit?.ProjectId ?? 0 });
                }
            }

            if (ModelState.IsValid)
            {
                var service = _unitService;
                model.SelectedUnits.Add(model.SelectedUnit.Id);
                foreach (var i in model.SelectedUnits)
                {
                    UnitBO bo = new UnitBO();
                    bo.Id = i;
                    model.Unit.LinkedUnits.Add(bo);
                }
                model.Unit.Name = "KOPPELING";
                model.Unit.ProjectId = model.SelectedUnit.ProjectId;
                model.Unit.IsLink = true;

                response = service.InsertUpdateUnit(model.Unit);
            }
            if (response.Success == true)
            {
                AddMessage("success", "De koppeling is geslaagd", "Geslaagd!");
                return RedirectToAction("DetailUnits", "Projecten", new { projectid = model.SelectedUnit.ProjectId });
            }
            else
            {
                AddMessage("error", "De koppeling is NIET geslaagd, gelieve opnieuw tot proberen of contact op te nemen met de administrator", "Fout!");
                return RedirectToAction("DetailUnits", "Projecten", new { projectid = model.SelectedUnit.ProjectId });
            }
        }

        // ========== PROJECT DETAIL EENHEDEN — gl-v2 (design-handoff punt 16) ==========
        // Eén opbouw voor 16a ("Eenhedenlijst — hoofdeenheden met hun berging en parking eronder") en
        // 16d ("Eenhedenlijst zonder basisakte — elk lot een eigen perceel, koper in plaats van
        // aandeel"): DetailUnitsV2Vm.HasBasisakte bepaalt welke kolom/KPI/projectregel de view kiest,
        // niet de gebruiker (16d, regel 1: "Het projecttype bepaalt de kolommen").

        /// <summary>Groepslabel in de TYPE-kolom — bergingen en parkeergelegenheden vallen samen onder
        /// "Nevenruimte" (16a's eigen kolomtekst), het echte subtype staat eronder.</summary>
        private static string UnitGroupLabelV2(int groupId) => groupId switch
        {
            1 => "Wooneenheid",
            4 => "Commerciële ruimte",
            2 or 3 => "Nevenruimte",
            _ => "Eenheid"
        };

        /// <summary>Filterwaarde achter de type-chips (16a §6) — een eigen sleutel per chip i.p.v. het
        /// ruwe GroupId, zodat view en JS niet met magische getallen werken.</summary>
        private static string UnitTypeFilterKeyV2(int groupId) => groupId switch
        {
            1 => "woning",
            4 => "commercieel",
            2 => "berging",
            3 => "parking",
            _ => "overig"
        };

        /// <summary>Verdiepingslabel uit Units.Level (een getal). Een nevenruimte (berging/parking) onder
        /// het gelijkvloers leest als "Ondergronds" i.p.v. "Kelder n": dat is het woord waarmee een
        /// berging of staanplaats in de praktijk beschreven wordt, terwijl "Kelder" bij een wooneenheid
        /// wél de juiste term is. Meer dan dit valt er niet uit de data te halen — "buiten" of
        /// "bovengronds" staat nergens als apart veld: Units.LevelId/ProjectLevels (een vrije
        /// verdiepingslijst per project) wordt door geen enkel scherm gevuld of gelezen, dus dat
        /// onderscheid zit vandaag in de naam van het eenheidstype (UnitTypes), die deze pagina al als
        /// tweede regel in de TYPE-kolom toont.</summary>
        private static string UnitLevelLabelV2(int? level, int groupId = 0)
        {
            if (level is null) return "—";
            if (level == 0) return "Gelijkvloers";
            if (level > 0) return $"Verdieping {level.Value}";
            var isSecondary = groupId == 2 || groupId == 3;
            if (!isSecondary) return $"Kelder {-level.Value}";
            return level == -1 ? "Ondergronds" : $"Ondergronds -{-level.Value}";
        }

        /// <summary>Adres + kadaster onder de naam van een hoofdeenheid (16a §3). Leeg wanneer geen van
        /// beide bekend is — dan rendert de view gewoon geen tweede regel i.p.v. een streepje.</summary>
        private static string UnitAddressLineV2(UnitBO u)
        {
            var street = string.Join(" ", new[] { u.Street, u.HouseNumber }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(u.BusNumber))
                street = string.IsNullOrWhiteSpace(street) ? $"bus {u.BusNumber}" : $"{street} bus {u.BusNumber}";
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
            if (!string.IsNullOrWhiteSpace(u.PreKad)) parts.Add($"kadaster {u.PreKad.Trim()}");
            return string.Join(" · ", parts);
        }

        /// <summary>Bedragen tolerant inlezen ("1.234,56" én "1234.56"). De gl-v2-geldvelden posten via
        /// AutoNumeric (unformatOnSubmit) een punt als decimaalteken, terwijl de request-cultuur nl-BE de
        /// punt als duizendscheiding leest — daarom niet op één cultuur vertrouwen maar beide vormen
        /// expliciet proberen, met de nl-BE-lezing eerst voor wat een gebruiker zelf typt.</summary>
        private static decimal? ParseAmountV2(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var text = raw.Trim();
            var be = CultureInfo.GetCultureInfo("nl-BE");
            if (text.Contains(',') && decimal.TryParse(text, NumberStyles.Number, be, out var beValue))
                return beValue;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invValue))
                return invValue;
            return decimal.TryParse(text, NumberStyles.Number, be, out var fallback) ? fallback : null;
        }

        /// <summary>De prijs van één eenheid, met dezelfde definitie als de Eenheden-tabel op
        /// Projecten/DetailV2: verkocht = grondwaarde verkocht + som van de ValueSold-bouwwaarden,
        /// anders grondwaarde + constructieprijzen volgens ServiceCore.Helpers.UnitPricing (dezelfde regel als
        /// de publieke site): zonder afwerkingen alle constructieprijzen samen, met afwerkingen elke
        /// afwerking als volledig alternatief — Price/From = de goedkoopste, Finished = de duurste (de
        /// "vanaf …"/"afgewerkt …"-regels uit 16a).</summary>
        private (decimal Price, decimal? From, decimal? Finished) UnitPriceV2(UnitBO money, bool isSold)
        {
            var values = money.ConstructionValues ?? new List<UnitConstructionValueBO>();
            var land = money.LandValue ?? 0m;
            var pricing = ServiceCore.Helpers.UnitPricing.Compute(
                values.Select(cv => (cv.FinishingOptionId, cv.Value ?? 0m)), DefaultOptionIdOf(money.Id));
            // Price = de STANDAARDafwerking (zo rekenen ook de budgetten); "vanaf" blijft de goedkoopste.
            var basePrice = land + pricing.Standard;

            if (isSold)
            {
                // Bij een verkochte eenheid zit de gekozen afwerking al in de ValueSold-bedragen
                // verrekend, dus geen aparte vanaf/afgewerkt-regels. Terugval op de vraagprijs wanneer
                // er nog geen verkoopbedragen ingevuld zijn — anders zou een net verkochte eenheid
                // € 0 tonen, wat als een fout leest i.p.v. als "nog niet ingevuld".
                var sold = (money.LandValueSold ?? 0m) + values.Sum(cv => cv.ValueSold ?? 0m);
                return (sold > 0m ? sold : basePrice, null, null);
            }

            if (!pricing.HasOptions) return (basePrice, null, null);
            return (basePrice, land + pricing.From, land + pricing.To);
        }

        // Standaardafwerking per eenheid (UnitFinishingOption.IsDefault), per request in één query geladen
        // voor de eenheden die een pagina toont; een niet voorgeladen eenheid wordt apart opgevraagd.
        private readonly Dictionary<int, int?> _defaultOptionCache = new();
        private void PreloadDefaultOptionIds(IEnumerable<int> unitIds)
        {
            var missing = unitIds.Where(id => !_defaultOptionCache.ContainsKey(id)).Distinct().ToList();
            if (missing.Count == 0) return;
            var found = _db.UnitFinishingOption.AsNoTracking()
                .Where(o => o.IsDefault && missing.Contains(o.UnitId))
                .Select(o => new { o.UnitId, o.Id, o.SortOrder })
                .ToList()
                .GroupBy(o => o.UnitId)
                .ToDictionary(g => g.Key, g => g.OrderBy(o => o.SortOrder).ThenBy(o => o.Id).First().Id);
            foreach (var id in missing) _defaultOptionCache[id] = found.TryGetValue(id, out var v) ? v : null;
        }
        private int? DefaultOptionIdOf(int unitId)
        {
            if (!_defaultOptionCache.TryGetValue(unitId, out var id))
            {
                PreloadDefaultOptionIds(new[] { unitId });
                id = _defaultOptionCache[unitId];
            }
            return id;
        }

        /// <summary>Bouwt de volledige eenhedenboom + KPI-cijfers voor Projecten/DetailUnitsV2.</summary>
        private DetailUnitsV2Vm BuildDetailUnitsV2Vm(int projectid, string projectName, decimal landshareTotal, bool canWrite)
        {
            var vm = new DetailUnitsV2Vm
            {
                ProjectId = projectid,
                ProjectName = projectName,
                LandshareTotal = landshareTotal
            };

            // Boomstructuur: dezelfde dienst als de Eenheden-tabel op Projecten/DetailV2 en de
            // "Nog geen klant"-rijen op Projecten/DetailClientsV2 al gebruiken — geeft enkel de
            // hoofdeenheden terug, met hun gekoppelde eenheden genest in AttachedUnits.
            var treeResp = _unitService.GetUnitsWithAttachedByProjectId(projectid);
            var tree = treeResp.Success && treeResp.Values is not null
                ? treeResp.Values
                : new List<UnitWithAttachedUnitsBO>();

            // Bedragen komen NIET uit die boom: GetUnitsWithAttachedByProjectId laadt
            // UnitConstructionValue enkel voor de hoofdeenheden, niet voor de gekoppelde eenheden — een
            // berging zou daar dus altijd € 0 bouwwaarde tonen. GetUnitsById laadt die regels voor élke
            // opgevraagde eenheid, dus prijzen lezen we daaruit (tweede query, exact de id's die we
            // renderen) i.p.v. uit de boom of uit het legacy ConstructionValue-veld, dat het huidige
            // eenheidsformulier niet meer bijwerkt.
            var moneyIds = new List<int>();
            foreach (var node in tree)
            {
                moneyIds.Add(node.Unit.Id);
                foreach (var child in node.AttachedUnits) moneyIds.Add(child.Id);
            }
            var moneyById = new Dictionary<int, UnitBO>();
            if (moneyIds.Count > 0)
            {
                var moneyResp = _unitService.GetUnitsById(moneyIds.Distinct().ToList());
                if (moneyResp.Success && moneyResp.Values is not null)
                    foreach (var u in moneyResp.Values) moneyById[u.Id] = u;
                PreloadDefaultOptionIds(moneyById.Keys);
            }
            UnitBO Money(UnitBO fallback) => moneyById.TryGetValue(fallback.Id, out var m) ? m : fallback;

            // Koper per eenheid — zelfde omgekeerde eenheid→klant-lookup als Projecten/DetailV2
            // (er bestaat geen kant-en-klare eenheid→klant-join).
            var clientByUnitId = new Dictionary<int, ClientAccountBO>();
            var clientsResp = _clientService.GetClientAccountsByProjectIdWithUnits(projectid);
            if (clientsResp.Success && clientsResp.Values is not null)
            {
                foreach (var cwu in clientsResp.Values)
                    foreach (var u in cwu.Units ?? new List<UnitBO>())
                        if (cwu.Client is not null) clientByUnitId[u.Id] = cwu.Client;
            }

            // Zelfde vlag als Projecten/DetailV2's eigen model.Project.IsOnlyCoordinationProject — het
            // inner menu heeft die op élke projectpagina nodig, niet enkel op de hub.
            var projectResp = _projectService.GetProjectByID(projectid);
            vm.IsCoordinationProject = projectResp.Success && projectResp.Value?.IsOnlyCoordinationProject == true;

            var comparator = new ServiceCore.Helpers.AlphanumComparator();

            DetailUnitsV2Row BuildChildRow(UnitBO child, UnitBO parent)
            {
                var money = Money(child);
                var (price, from, finished) = UnitPriceV2(money, isSold: false);
                var groupId = child.Type?.GroupId ?? 0;
                return new DetailUnitsV2Row
                {
                    UnitId = child.Id,
                    Name = child.Name ?? "",
                    SubLine = $"gekoppeld aan {parent.Name}",
                    TypeGroupLabel = UnitGroupLabelV2(groupId),
                    TypeName = child.Type?.Name ?? "",
                    TypeGroupId = groupId,
                    LevelLabel = UnitLevelLabelV2(child.Level, groupId),
                    Surface = child.Surface,
                    GroundSurface = child.GroundSurface,
                    Price = price,
                    OwnPrice = price,
                    PriceFrom = from,
                    PriceFinished = finished,
                    Landshare = child.Landshare,
                    Status = $"Met {parent.Name}",
                    StatusTone = "is-neutral",
                    IsAttached = true,
                    ParentName = parent.Name,
                    ParentUnitId = parent.Id,
                    IsLink = child.IsLink,
                    CanAttach = false,
                    TypeFilterKey = UnitTypeFilterKeyV2(groupId),
                    StatusFilterKey = "gekoppeld",
                    SearchText = $"{child.Name} {child.Type?.Name} {UnitAddressLineV2(child)} {parent.Name}".ToLowerInvariant()
                };
            }

            var topRows = new List<DetailUnitsV2Row>();
            foreach (var node in tree)
            {
                var unit = node.Unit;
                var money = Money(unit);
                var groupId = unit.Type?.GroupId ?? 0;
                var isSecondary = groupId == 2 || groupId == 3;
                clientByUnitId.TryGetValue(unit.Id, out var client);
                var isSold = unit.ClientAccountId is not null || client is not null;
                var (ownPrice, from, finished) = UnitPriceV2(money, isSold);

                var children = node.AttachedUnits
                    .OrderBy(c => c.Name, comparator)
                    .Select(c => BuildChildRow(c, unit))
                    .ToList();
                var attachedSum = children.Sum(c => c.Price);

                string status; string tone;
                if (client?.DateDeedOfSale is not null) { status = "Akte verleden"; tone = "is-neutral"; }
                else if (isSold) { status = "Verkocht"; tone = "is-positive"; }
                else if (unit.IsOption) { status = "In optie"; tone = "is-attention"; }
                else if (isSecondary) { status = "Los te koop"; tone = "is-info"; }
                else { status = "Beschikbaar"; tone = "is-neutral"; }

                topRows.Add(new DetailUnitsV2Row
                {
                    UnitId = unit.Id,
                    Name = unit.Name ?? "",
                    SubLine = UnitAddressLineV2(unit),
                    TypeGroupLabel = UnitGroupLabelV2(groupId),
                    TypeName = unit.Type?.Name ?? "",
                    TypeGroupId = groupId,
                    LevelLabel = UnitLevelLabelV2(unit.Level, groupId),
                    Surface = unit.Surface,
                    GroundSurface = unit.GroundSurface,
                    Price = ownPrice + attachedSum,
                    OwnPrice = ownPrice,
                    AttachedCount = children.Count,
                    PriceFrom = from is null ? null : from + attachedSum,
                    PriceFinished = finished is null ? null : finished + attachedSum,
                    Landshare = unit.Landshare,
                    Status = status,
                    StatusTone = tone,
                    ClientId = client?.Id,
                    ClientName = client is null
                        ? null
                        : string.Join(" ", new[] { client.Salutation.GetDisplayName(), client.DisplayName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    SalesAgreementDate = client?.DateSalesAgreement,
                    IsAttached = false,
                    IsLink = unit.IsLink,
                    CanAttach = !isSecondary && !unit.IsLink && canWrite,
                    Children = children,
                    TypeFilterKey = UnitTypeFilterKeyV2(groupId),
                    StatusFilterKey = isSold ? "verkocht" : "beschikbaar",
                    SearchText = $"{unit.Name} {unit.Type?.Name} {UnitAddressLineV2(unit)} {client?.DisplayName}".ToLowerInvariant()
                });
            }

            // Groepen in de volgorde van 16a: hoofdeenheden eerst, dan wat nog los te koop staat.
            // "Los te koop" = nevenruimtes die aan geen enkel lot hangen — 16a §6: "dan zie je meteen
            // wat nog een koper of een lot zoekt".
            void AddGroup(string key, string label, string icon, Func<DetailUnitsV2Row, bool> match, bool summarize = false)
            {
                var rows = topRows.Where(match).OrderBy(r => r.Name, comparator).ToList();
                if (rows.Count == 0) return;
                var group = new DetailUnitsV2Group { Key = key, Label = label, IconClass = icon, Rows = rows };
                if (summarize)
                {
                    // Losse nevenruimtes per type samengevat in de groepskop, dicht tot je ze opent:
                    // twintig bergingen onder elkaar zeggen je niets dat "20 bergingen · 96 m² · € 180.000"
                    // niet ook zegt. Alleen wat nog los staat: een gekoppelde eenheid hangt in de boom
                    // onder haar lot en is hier al niet meer bij.
                    var be = CultureInfo.GetCultureInfo("nl-BE");
                    var surface = rows.Sum(r => r.Surface ?? 0m);
                    var value = rows.Sum(r => r.PriceFinished ?? r.Price);
                    var sold = rows.Count(r => r.StatusFilterKey == "verkocht");
                    var parts = new List<string>();
                    if (sold > 0) parts.Add($"{rows.Count - sold} beschikbaar · {sold} verkocht");
                    if (surface > 0m) parts.Add(surface.ToString("#,##0.##", be) + " m²");
                    if (value > 0m) parts.Add(value.ToString("C0", be));
                    group.Summary = string.Join(" · ", parts);
                    group.CollapsedByDefault = true;
                }
                vm.Groups.Add(group);
            }
            AddGroup("woningen", "Woningen", "ph-house", r => r.TypeGroupId == 1);
            AddGroup("commercieel", "Commerciële ruimtes", "ph-storefront", r => r.TypeGroupId == 4);
            AddGroup("los-berging", "Losse bergingen", "ph-package", r => r.TypeGroupId == 2, summarize: true);
            AddGroup("los-parking", "Losse parkings", "ph-car", r => r.TypeGroupId == 3, summarize: true);
            AddGroup("overig", "Overige eenheden", "ph-squares-four", r => r.TypeGroupId is not (1 or 2 or 3 or 4));

            foreach (var group in vm.Groups)
            {
                foreach (var row in group.Rows)
                {
                    vm.AllRows.Add(row);
                    vm.AllRows.AddRange(row.Children);
                }
            }

            // ── KPI's en chiptellers ──────────────────────────────────────────────────────────────
            var mainRows = vm.AllRows.Where(r => r.TypeGroupId is 1 or 4).ToList();
            vm.MainCount = mainRows.Count;
            vm.SecondaryCount = vm.AllRows.Count(r => r.TypeGroupId is 2 or 3);
            vm.SoldMainCount = mainRows.Count(r => r.StatusFilterKey == "verkocht");
            // Totale verkoopwaarde over de hoofdrijen: hun prijs bevat de gekoppelde eenheden al, dus
            // over álle rijen tellen zou die dubbel rekenen.
            vm.SalesValueTotal = topRows.Sum(r => r.PriceFinished ?? r.Price);
            vm.LivingSurfaceTotal = mainRows.Sum(r => r.Surface ?? 0m);
            vm.GroundSurfaceTotal = vm.AllRows.Sum(r => r.GroundSurface ?? 0m);
            vm.ParcelCount = vm.AllRows.Count(r => (r.GroundSurface ?? 0m) > 0m);
            vm.LandshareAssigned = vm.AllRows.Sum(r => r.Landshare ?? 0m);
            vm.HasBasisakte = landshareTotal > 0m || vm.LandshareAssigned > 0m;

            vm.WoningCount = vm.AllRows.Count(r => r.TypeGroupId == 1);
            vm.CommercialCount = vm.AllRows.Count(r => r.TypeGroupId == 4);
            vm.StorageCount = vm.AllRows.Count(r => r.TypeGroupId == 2);
            vm.ParkingCount = vm.AllRows.Count(r => r.TypeGroupId == 3);
            vm.AvailableCount = vm.AllRows.Count(r => r.StatusFilterKey == "beschikbaar");
            vm.SoldCount = vm.AllRows.Count(r => r.StatusFilterKey == "verkocht");

            // De koppelregel onder een lot ("Berging of parking koppelen aan Lot 2", 16a §2) heeft enkel
            // zin zolang er nog iets te koppelen valt.
            vm.HasAttachableUnits = topRows.Any(r => (r.TypeGroupId == 2 || r.TypeGroupId == 3) && !r.IsLink);

            return vm;
        }

        /// <summary>Koppeldialoog uit 16c: eerst wát je koppelt, dan de bedragen die daaruit volgen.</summary>
        [HttpGet]
        public IActionResult ModalAttachUnitV2(int unitid, string? returnUrl = null)
        {
            var lotResp = _unitService.GetUnitById(unitid);
            if (!lotResp.Success || lotResp.Value is null) return NotFound();
            var lot = lotResp.Value;

            var vm = new AttachUnitV2Vm
            {
                ProjectId = lot.ProjectId,
                LotUnitId = lot.Id,
                LotName = lot.Name ?? "",
                ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null
            };

            var treeResp = _unitService.GetUnitsWithAttachedByProjectId(lot.ProjectId);
            var tree = treeResp.Success && treeResp.Values is not null
                ? treeResp.Values
                : new List<UnitWithAttachedUnitsBO>();

            var candidateIds = new List<int> { lot.Id };
            foreach (var node in tree)
            {
                if (node.Unit.Type?.GroupId is 2 or 3) candidateIds.Add(node.Unit.Id);
                foreach (var child in node.AttachedUnits)
                    if (child.Type?.GroupId is 2 or 3) candidateIds.Add(child.Id);
            }
            var moneyById = new Dictionary<int, UnitBO>();
            var moneyResp = _unitService.GetUnitsById(candidateIds.Distinct().ToList());
            if (moneyResp.Success && moneyResp.Values is not null)
                foreach (var u in moneyResp.Values) moneyById[u.Id] = u;
            PreloadDefaultOptionIds(moneyById.Keys);
            UnitBO Money(UnitBO fallback) => moneyById.TryGetValue(fallback.Id, out var m) ? m : fallback;

            // "Nieuwe prijs <lot>, casco" vertrekt van wat de lijst nu toont: eigen prijs + wat er al
            // aan hangt.
            var lotNode = tree.FirstOrDefault(n => n.Unit.Id == lot.Id);
            vm.LotPrice = UnitPriceV2(Money(lot), lot.ClientAccountId is not null).Price
                + (lotNode?.AttachedUnits.Sum(c => UnitPriceV2(Money(c), false).Price) ?? 0m);

            AttachUnitV2Candidate ToCandidate(UnitBO u, string? attachedTo)
            {
                var money = Money(u);
                var levelLabel = UnitLevelLabelV2(u.Level, u.Type?.GroupId ?? 0);
                var typeLine = string.Join(" · ", new[] { u.Type?.Name, levelLabel == "—" ? null : levelLabel }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.ToLowerInvariant()));
                return new AttachUnitV2Candidate
                {
                    UnitId = u.Id,
                    Name = u.Name ?? "",
                    TypeLine = typeLine,
                    LandValue = money.LandValue ?? 0m,
                    // Constructieprijs volgens dezelfde regel als de lijst (Price minus grond) — vroeger
                    // enkel de regels zonder afwerking, dus € 0 voor een eenheid met afwerkingen.
                    ConstructionValue = UnitPriceV2(money, false).Price - (money.LandValue ?? 0m),
                    Price = UnitPriceV2(money, false).Price,
                    AttachedToName = attachedTo
                };
            }

            var comparator = new ServiceCore.Helpers.AlphanumComparator();
            vm.Available = tree
                .Where(n => n.Unit.Id != lot.Id && (n.Unit.Type?.GroupId is 2 or 3) && !n.Unit.IsLink)
                .OrderBy(n => n.Unit.Name, comparator)
                .Select(n => ToCandidate(n.Unit, null))
                .ToList();
            vm.AlreadyAttached = tree
                .SelectMany(n => n.AttachedUnits
                    .Where(c => c.Type?.GroupId is 2 or 3)
                    .Select(c => new { Child = c, Parent = n.Unit }))
                .OrderBy(p => p.Child.Name, comparator)
                .Select(p => ToCandidate(p.Child, p.Parent.Name))
                .ToList();

            return PartialView("Modals/_ModalAttachUnitV2", vm);
        }

        /// <summary>Koppelen = de gekozen eenheid onder dit lot hangen (Units.AttachedUnitId, hetzelfde
        /// veld dat de "Gekoppelde eenheid"-dropdown op AddUnit/EditUnit al zet en waarop de boom van 16a
        /// gebouwd is). Bewust een eigen actie naast AddUnitLink hierboven: die maakt een samengestelde
        /// KOPPELING-pseudo-eenheid aan (Units.IsLink) en laat de leden uit de lijst verdwijnen — de
        /// legacy pagina blijft dat gebruiken, deze niet.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsUnits)]
        public IActionResult AttachUnitV2(int projectid, int lotUnitId, int unitId, string? landValue, string? returnUrl = null)
        {
            IActionResult Back() => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("DetailUnits", "Projecten", new { projectid });

            if (lotUnitId == 0 || unitId == 0 || lotUnitId == unitId)
            {
                AddMessage("error", "Er is geen eenheid gekozen om te koppelen.", "Fout!");
                return Back();
            }

            var unitResp = _unitService.GetUnitById(unitId);
            var lotResp = _unitService.GetUnitById(lotUnitId);
            if (!unitResp.Success || unitResp.Value is null || !lotResp.Success || lotResp.Value is null)
            {
                AddMessage("error", "De eenheid kon niet gevonden worden.", "Fout!");
                return Back();
            }

            var unit = unitResp.Value;
            var lot = lotResp.Value;
            if (unit.ProjectId != lot.ProjectId || lot.ProjectId != projectid)
            {
                AddMessage("error", "De eenheid hoort niet bij dit project.", "Fout!");
                return Back();
            }
            if (unit.IsLink || lot.IsLink)
            {
                AddMessage("error", "Een samengestelde koppeling kan hier niet gekoppeld worden.", "Fout!");
                return Back();
            }

            unit.AttachedUnitsId = lotUnitId;
            var parsedLandValue = ParseAmountV2(landValue);
            if (parsedLandValue.HasValue) unit.LandValue = parsedLandValue.Value;

            var response = _unitService.InsertUpdateUnit(unit);
            if (response.Success)
                AddMessage("success", $"{unit.Name} is gekoppeld aan {lot.Name}.", "Geslaagd!");
            else
                AddMessage("error", "De koppeling is niet gelukt, gelieve opnieuw te proberen of contact op te nemen met de administrator.", "Fout!");

            return Back();
        }

        /// <summary>De tegenhanger van AttachUnitV2 — zonder dit is koppelen een eenrichtingsdeur.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsUnits)]
        public IActionResult DetachUnitV2(int projectid, int unitId, string? returnUrl = null)
        {
            IActionResult Back() => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("DetailUnits", "Projecten", new { projectid });

            var unitResp = _unitService.GetUnitById(unitId);
            if (!unitResp.Success || unitResp.Value is null || unitResp.Value.ProjectId != projectid)
            {
                AddMessage("error", "De eenheid kon niet gevonden worden.", "Fout!");
                return Back();
            }

            var unit = unitResp.Value;
            unit.AttachedUnitsId = null;
            var response = _unitService.InsertUpdateUnit(unit);
            if (response.Success)
                AddMessage("success", $"{unit.Name} staat weer los.", "Geslaagd!");
            else
                AddMessage("error", "Het ontkoppelen is niet gelukt, gelieve opnieuw te proberen of contact op te nemen met de administrator.", "Fout!");

            return Back();
        }

        /// <summary>De Excel-helft van 16a's samengevoegde "Exporteren"-knop (§5). Vervangt de
        /// client-side DataTables-Buttons-export van de legacy pagina door een echte server-side .xlsx
        /// (ClosedXML, zelfde recept als ExportInvoicesExcel) — nodig omdat deze lijst geen DataTable
        /// meer is: de boom (gekoppelde eenheden onder hun hoofdeenheid) en de groepskoppen verdragen
        /// geen sorteer-/pagineer-plugin die rijen los van elkaar herschikt.</summary>
        [HttpGet]
        public IActionResult ExportUnitsExcel(int projectid)
        {
            var projectName = _projectService.GetProjectNameById(projectid) ?? "Project";
            var vm = BuildDetailUnitsV2Vm(projectid, projectName, _projectService.GetProjectLandshareById(projectid), canWrite: false);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Eenheden");

            var headers = new List<string>
            {
                "Groep", "Eenheid", "Adres / kadaster", "Type", "Subtype", "Verdieping",
                "Oppervlakte (m²)", "Grondoppervlakte (m²)", "Verkoopprijs",
                vm.HasBasisakte ? "Aandeel basisakte" : "Koper",
                "Status"
            };

            for (int c = 0; c < headers.Count; c++) ws.Cell(1, c + 1).Value = headers[c];
            var headerRange = ws.Range(1, 1, 1, headers.Count);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00532D");
            headerRange.Style.Font.FontColor = XLColor.White;

            int rij = 2;
            foreach (var group in vm.Groups)
            {
                foreach (var top in group.Rows)
                {
                    foreach (var r in new[] { top }.Concat(top.Children))
                    {
                        ws.Cell(rij, 1).Value = group.Label;
                        ws.Cell(rij, 2).Value = r.IsAttached ? $"    {r.Name}" : r.Name;
                        ws.Cell(rij, 3).Value = r.SubLine;
                        ws.Cell(rij, 4).Value = r.TypeGroupLabel;
                        ws.Cell(rij, 5).Value = r.TypeName;
                        ws.Cell(rij, 6).Value = r.LevelLabel;
                        if (r.Surface.HasValue) ws.Cell(rij, 7).Value = (double)r.Surface.Value;
                        if (r.GroundSurface.HasValue) ws.Cell(rij, 8).Value = (double)r.GroundSurface.Value;
                        ws.Cell(rij, 9).Value = (double)(r.PriceFinished ?? r.Price);
                        ws.Cell(rij, 9).Style.NumberFormat.Format = "€ #,##0.00";
                        if (vm.HasBasisakte)
                        {
                            if (r.Landshare.HasValue) ws.Cell(rij, 10).Value = (double)r.Landshare.Value;
                        }
                        else
                        {
                            ws.Cell(rij, 10).Value = r.IsAttached ? $"via {r.ParentName}" : (r.ClientName ?? "");
                        }
                        ws.Cell(rij, 11).Value = r.Status;
                        rij++;
                    }
                }
            }

            ws.Columns().AdjustToContents();
            if (rij > 2)
            {
                ws.Range(1, 1, rij - 1, headers.Count).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(1, 1, rij - 1, headers.Count).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var safeName = projectName.Replace(Path.GetInvalidFileNameChars(), '_');
            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Eenheden_{safeName}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>De PDF-helft van 16a's "Exporteren"-menu — een echt opgemaakt document in de
        /// huisstijl, zelfde recept als PrintClientList: UnitListDocument bovenop de gedeelde
        /// GroupLnPdfDocument-basis (A4 liggend, kop met logo, projectfiche, voet met paginering).
        /// Vervangt het printdialoog-behelp uit de eerste versie van deze pagina.</summary>
        [HttpGet]
        public IActionResult PrintUnitList(int projectid)
        {
            var projectName = _projectService.GetProjectNameById(projectid) ?? "Project";
            var vm = BuildDetailUnitsV2Vm(projectid, projectName, _projectService.GetProjectLandshareById(projectid), canWrite: false);

            // Adres + bouwheer voor de fiche: zelfde lezing als PrintClientList (rechtstreeks op de
            // entiteit, inclusief PostalCode/Builder) zodat beide documenten dezelfde kop tonen.
            var project = _db.Project
                .Include(p => p.PostalCode)
                .Include(p => p.Builder)
                .AsNoTracking()
                .FirstOrDefault(p => p.ProjectId == projectid);

            var builder = project?.Builder;
            var info = new CPMCore.Documents.UnitListProjectInfo
            {
                AddressLine = string.Join(" ", new[] { project?.Street, project?.Number }.Where(s => !string.IsNullOrWhiteSpace(s))),
                CityLine = project?.PostalCode == null
                    ? null
                    : string.Join(" ", new[] { project.PostalCode.Postcode, project.PostalCode.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s))),
                OpdrachtgeverName = builder?.BedrijfsNaam,
                OpdrachtgeverAddress = builder == null ? null : string.Join(", ", new[]
                {
                    string.Join(" ", new[] { builder.Straat, builder.Huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    string.Join(" ", new[] { builder.Postcode, builder.Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)))
                }.Where(s => !string.IsNullOrWhiteSpace(s)))
            };

            // Logo + Avenir-font, letterlijk zoals PrintClientList/PrintSupplierList.
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

            byte[] pdfBytes;
            try
            {
                pdfBytes = new CPMCore.Documents.UnitListDocument(vm, info, logoBytes, fontFamily).GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Eenhedenlijst-PDF genereren mislukt voor project {ProjectId}", projectid);
                return StatusCode(500, "De eenhedenlijst kon niet worden opgemaakt: " + ex.Message);
            }

            var safeName = (projectName ?? "Project").Replace(Path.GetInvalidFileNameChars(), '_');
            return File(pdfBytes, "application/pdf", $"Eenhedenlijst_{safeName}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ========== AANDELEN BASISAKTE VERDELEN ==========
        // Het scherm dat de waarschuwing op DetailUnitsV2 ("Basisakte niet verdeeld", design-handoff
        // 16a §4) een echte bestemming geeft. Stond niet in de handoff — 16a zet daar enkel een knop
        // "Aandelen verdelen" zonder te zeggen waar die heen gaat; de opbouw hieronder volgt de
        // gl-v2-formulierconventies (punt 8b: sectienavigatie niet nodig bij één tabel, maar wél de
        // vaste actiebalk onderaan) i.p.v. een nieuw patroon te verzinnen.

        [HttpGet]
        [Breadcrumb("Aandelen basisakte", FromAction = "DetailUnits")]
        public ActionResult Landshares(int projectid)
        {
            // Dit is een gl-v2-scherm zonder legacy tegenhanger (DESIGN.md: nieuwe schermen worden van
            // meet af aan in gl-v2 gebouwd). Zonder de preview-cookie zou _ViewStart _Layout kiezen en
            // de pagina volledig ongestyled renderen; dan liever terug naar de eenhedenlijst dan een
            // kapot scherm tonen.
            if (ViewData["UseGlV2Layout"] as bool? != true)
                return RedirectToAction("DetailUnits", "Projecten", new { projectid });

            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectUnits = _ps.HasWrite(PermissionCodes.ProjectsUnits);

            var projectName = _projectService.GetProjectNameById(projectid) ?? "";
            var vm = BuildDetailUnitsV2Vm(
                projectid,
                projectName,
                _projectService.GetProjectLandshareById(projectid),
                ViewBag.CanWriteProjectUnits as bool? ?? false);

            var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            // Hier is "Eenheden" WEL de juiste laatste kruimel: de titel van deze pagina is "Aandelen
            // basisakte", dus het pad herhaalt de titel niet (punt 13, regel 2).
            var projectUnits = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailUnits", "Projecten", "Eenheden")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectUnits;

            SetPageHeader("bx bx-building-house", $"{projectName} - Aandelen basisakte");
            return View("LandsharesV2", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsUnits)]
        public ActionResult SaveLandshares(int projectid, int[] unitIds, string[] landshares)
        {
            if (unitIds is null || landshares is null || unitIds.Length != landshares.Length)
            {
                AddMessage("error", "De aandelen konden niet gelezen worden, gelieve de pagina opnieuw te laden.", "Fout!");
                return RedirectToAction("Landshares", "Projecten", new { projectid });
            }

            // Een leeg veld betekent "geen aandeel" (null), niet "nul" — dat onderscheid blijft staan
            // zodat een eenheid die nog niet verdeeld is, verschilt van een eenheid die bewust 0 krijgt.
            var byUnitId = new Dictionary<int, decimal?>();
            for (int i = 0; i < unitIds.Length; i++)
                byUnitId[unitIds[i]] = ParseAmountV2(landshares[i]);

            var response = _unitService.UpdateUnitLandshares(projectid, byUnitId);
            if (response.Success)
                AddMessage("success", "De aandelen zijn opgeslagen.", "Geslaagd!");
            else
                AddMessage("error", "De aandelen zijn niet opgeslagen, gelieve opnieuw te proberen of contact op te nemen met de administrator.", "Fout!");

            return RedirectToAction("DetailUnits", "Projecten", new { projectid });
        }


    }
}
