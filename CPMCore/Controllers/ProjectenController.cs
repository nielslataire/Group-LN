using BOCore;
using CPMCore.Documents;
using CPMCore.Models;
using CPMCore.Models.Klanten;
using CPMCore.Models.Projecten;
using CPMCore.Service;
using DALCore;
using FacadeCore;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using ServiceCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
//using CPMCore.Attributes;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Web;


namespace CPMCore.Controllers
{

    public class ProjectenController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration Configuration;
        private readonly IWebHostEnvironment _env;
        //private readonly IInvoicePdfService _pdf;         // QuestPDF
        //private readonly IUblService _ubl;
        // Content-types die we toelaten
        private static readonly HashSet<string> _validImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif"
    };

        public ProjectenController(UserManager<ApplicationUser> userManager, ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _logger = logger;
            Configuration = configuration;
            _env = env;
        }

        // ========== PROJECT DETAIL ==========
        [Breadcrumb("Projecten", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
        public IActionResult Index()
        {
            var model = new ShowProjectsModel();
            var service = ServiceFactory.GetProjectService();

            var response = service.GetProjectsForList();
            if (response.Success && response.Values is not null)
            {
                model.Projects = response.Values
                    .OrderByDescending(m => m.DeliveryDate == null)
                    .ThenByDescending(m => m.DeliveryDate)
                    .ToList();

                var ids = model.Projects.Select(p => p.Id).ToList();
                if (ids.Count > 0)
                {
                    var salesResponse = service.GetProjectSalesData(ids);
                    if (salesResponse.Success && salesResponse.Values is not null)
                    {
                        model.SalesData = salesResponse.Values
                            .GroupBy(v => v.ProjectId)
                            .ToDictionary(g => g.Key, g => g.First());
                    }
                }
            }

            var statusResponse = service.GetStatuses();
            if (statusResponse.Success && statusResponse.Values is not null)
            {
                model.Statuses = statusResponse.Values;
            }

            ViewData["Title"] = "Projecten - Alle";
            ViewData["SubTitle"] = "Alle projecten";
            ViewData["SubTitleText"] = "Overzicht van alle projecten binnen CPM.";

            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Eigen projecten", FromAction = nameof(Index))]
        public IActionResult ProjectsByUserId(string userId)
        {
            var model = new ShowProjectsModel();
            var service = ServiceFactory.GetProjectService();

            var response = service.GetProjectsForList(0, 0, userId);
            if (response.Success && response.Values is not null)
            {
                model.Projects = response.Values
                    .OrderByDescending(m => m.DeliveryDate == null)
                    .ThenByDescending(m => m.DeliveryDate)
                    .ToList();

                var ids = model.Projects.Select(p => p.Id).ToList();
                if (ids.Count > 0)
                {
                    var salesResponse = service.GetProjectSalesData(ids);
                    if (salesResponse.Success && salesResponse.Values is not null)
                    {
                        model.SalesData = salesResponse.Values
                            .GroupBy(v => v.ProjectId)
                            .ToDictionary(g => g.Key, g => g.First());
                    }
                }
            }

            var statusResponse = service.GetStatuses();
            if (statusResponse.Success && statusResponse.Values is not null)
            {
                model.Statuses = statusResponse.Values;
            }

            ViewData["Title"] = "Projecten - Alle";
            ViewData["SubTitle"] = "Alle projecten";
            ViewData["SubTitleText"] = "Overzicht van alle projecten binnen CPM.";

            return View("Index", model);
        }


        [HttpGet]
        //[Breadcrumb("Info")]
        [Breadcrumb("Info", FromAction = "Index")]
        public ActionResult Detail(int projectid, bool EditGeneralData = false)
        {


            //NEXT

            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ShowProjectDetail model = new ShowProjectDetail();
            var Service = ServiceFactory.GetProjectService();
            var cservice = ServiceFactory.GetClientService();
            var response = Service.GetProjectByID(projectid);
            if ((response.Success))
                model.Project = response.Value;
            model.Project.Postalcode.Country.CountryId = 19;
            model.Project.Postalcode.Country.ISOCode = "BE";
            model.ProjectName = model.Project.Name;
            FillInAddSelectListsDetail(ref model);
            model.GeneralDataEditMode = EditGeneralData;
            model.SelectedPostalcode = (int)model.Project.Postalcode.PostcodeId;
            model.Docs = Service.GetProjectDocs(projectid).Values;
            model.Users = _userManager.Users.ToList();
            model.Users = model.Users.OrderBy(m => m.Displayname).ToList();
            if (model.Project.ExecutionDays == 0)
                model.ExecutionDays = Service.GetProjectExecutionDays(model.Project.Id);
            else
                model.ExecutionDays = (int)model.Project.ExecutionDays;
            if (model.Project.StartDateConstruction is not null)
                model.StartDate = (DateOnly)model.Project.StartDateConstruction;
            else
                model.StartDate = Service.GetProjectStartDateConstruction(model.Project.Id);
            model.WorkingDaysLeft = -9999;
            if (model.ExecutionDays != 0 && model.StartDate != DateOnly.MinValue)
            {
                model.FinalConstructionDate = Service.GetFinalConstructionDay(model.Project.Id, model.StartDate, model.ExecutionDays);
                if (model.FinalConstructionDate != DateOnly.MinValue)
                    model.WorkingDaysLeft = Service.GetWorkingDaysLeft(model.FinalConstructionDate, model.Project.Id);
            }
            var response2 = cservice.GetClientAccountsByProjectIdLast5(projectid);
            if ((response2.Success))
                model.RecentClients = response2.Values;
            var response3 = Service.GetLatestProjectNews(1, projectid);
            if ((response3.Success))
                model.LatestNews = response3.Values.FirstOrDefault();
            if (model.LatestNews is not null)
            {
                if (model.LatestNews.TextNL is not null & model.LatestNews.TextNL.Length > 250)
                    model.LatestNews.TextNL = model.LatestNews.TextNL.Substring(0, 250).ToString() + " ...";
            }
            var response4 = Service.GetLatestProjectPictures(1, projectid);
            if ((response4.Success))
                model.LatestPicture = response4.Values.FirstOrDefault();
            var response5 = Service.GetLatestProjectDocs(5, projectid);
            if ((response5.Success))
                model.LatestDocs = response5.Values;
            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };

            // leaf: Detail met dynamische titel + id in de link
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.Project.Name)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }               // ← zorgt voor /Projecten/Detail/{id}
            };

            ViewData["BreadcrumbNode"] = projectDetail;

            return View(model);
        }

        // ========== PROJECT DETAIL KLANTEN ==========

        [HttpGet]
        //[Breadcrumb("Klanten")]
        [Breadcrumb("Klanten", FromAction = "Detail")]
        public ActionResult DetailClients(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            DetailClientsModel model = new DetailClientsModel();
            var service = ServiceFactory.GetClientService();
            var service2 = ServiceFactory.GetProjectService();
            var response = service.GetClientAccountsByProjectIdWithUnits(projectid);
            if ((response.Success))
                model.ClientAccounts = response.Values;

            if (model.ClientAccounts.SelectMany(m => m.Units.Where(i => i.Type.GroupId == 1)).Count() > 0)
                model.ClientAccounts = model.ClientAccounts.OrderBy(m => m.Units.Where(a => a.Type.GroupId == 1).Count() > 0 ? m.Units.Where(a => a.Type.GroupId == 1).FirstOrDefault().Name : "", new ServiceCore.Helpers.AlphanumComparator()).ToList();
            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);
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
            var projectKlanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }            
            };
            ViewData["BreadcrumbNode"] = projectKlanten;
            return View(model);
        }

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
            ViewData["BreadcrumbNode"] = projectUnits;
            return View(model);
        }
        [HttpGet]
        //[Breadcrumb("Eenheid toevoegen")]
        [Breadcrumb("Eenheid toevoegen", FromAction = "DetailUnits")]
        public ActionResult AddUnit(int projectid)
        {
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            ViewData["Referrer"] = referrer;
            var model = new AddUnitModel();
            var service = ServiceFactory.GetUnitService();
            var service2 = ServiceFactory.GetProjectService();

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
            var service = ServiceFactory.GetUnitService();
            var service2 = ServiceFactory.GetProjectService();
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
        public ActionResult EditUnit(int projectid, int unitid)
        {
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            TempData["Referrer"] = referrer;
            EditUnitModel model = new EditUnitModel();
            var service = ServiceFactory.GetUnitService();
            var service2 = ServiceFactory.GetProjectService();

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

            return View(model);
        }
        [HttpPost]
        public async Task<ActionResult> EditUnit(EditUnitModel Model, IFormFile? file)
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
            if (ModelState.IsValid)
            {
                if ((file != null && file.Length > 0))
                {
                    // Local Upload directory
                    var Uploaddir = Configuration["URL:PlanLocalURL"];
                    // Uploadname per directory
                    var imagepath = Path.Combine(Uploaddir, filename);
                    // Check if directories exists
                    CheckDir(Uploaddir);
                    // save image to directories
                    using (Stream fileStream = new FileStream(imagepath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    Model.Unit.Plan = filename;
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
                var service = ServiceFactory.GetUnitService();
                var service2 = ServiceFactory.GetProjectService();
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
            return View(Model);
        }

        public DetailUnitsModel FillDetailUnitModel(int id)
        {
            var model = new DetailUnitsModel();
            var service = ServiceFactory.GetUnitService();
            var service2 = ServiceFactory.GetProjectService();



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
        public PartialViewResult BlankConstructionValueRow(int unitid, int projectid)
        {
            UnitConstructionValueBO bo = new UnitConstructionValueBO();
            bo.UnitId = unitid;
            bo.PaymentGroupId = 0;
            var service2 = ServiceFactory.GetProjectService();
            var responsepaymentgroups = service2.GetProjectPaymentGroupsForSelect(projectid);
            ViewBag.paymentgroups = responsepaymentgroups.Values;
            return PartialView("_ConstructionValueRow", bo);
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
                var dservice = ServiceFactory.GetUnitService();
                viewModel = dservice.GetUnitById(id).Value;
            }
            return PartialView("_ModalDeleteUnit", viewModel);
        }
        public ActionResult DeleteUnit(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = ServiceFactory.GetUnitService();
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
        [HttpGet]
        public ActionResult ModalAddUnitLink(int id)
        {
            var viewModel = new AddUnitLinkModel();
            var service = ServiceFactory.GetUnitService();
            // Get Unit
            var response = service.GetUnitById(id);
            if ((response.Success))
                viewModel.SelectedUnit = response.Value;
            List<int> ids = new List<int>();
            ids.Add(id);
            var response2 = service.GetUnitsByProjectIdForSelect(viewModel.SelectedUnit.ProjectId, viewModel.SelectedUnit.Type.Id);
            if ((response2.Success))
                viewModel.Units = response2.Values;
            viewModel.Units.Remove(viewModel.Units.Find(m => m.ID == id));

            return PartialView("_ModalAddLink", viewModel);
        }
        [HttpPost]
        public ActionResult AddUnitLink(AddUnitLinkModel model)
        {
            Response response = new Response();
            if (ModelState.IsValid)
            {
                var service = ServiceFactory.GetUnitService();
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

        // ========== PROJECT DETAIL CONTRACTEN ==========
        [HttpGet]
        //[Breadcrumb("Leveranciers")]
        [Breadcrumb("Leveranciers", FromAction = "Detail")]
        public ActionResult DetailContracts(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new DetailContractsModel();
            var service = ServiceFactory.GetProjectService();
            var response = service.GetProjectContracts(projectid);

            if (response.Success)
            {
                model.Contracts = response.Values;
            }

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
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContracts", "Projecten", "Leveranciers")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectContracts;
            return View(model);
        }
        [HttpGet]

        //[Breadcrumb("Nacalculatie")]
        [Breadcrumb("Nacalculatie", FromAction = "Detail")]
        public ActionResult Recalculation(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ProjectContractsModel model = new ProjectContractsModel();
            var service = ServiceFactory.GetProjectService();
            var aservice = ServiceFactory.GetActivityService();
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

            return View(model);
        }
        [HttpGet]
        public IActionResult PrintRecalculation(int projectid)
        {
            // 1) Haal het model op zoals je Recalculation-view dat ook doet
            ProjectContractsModel model = new ProjectContractsModel();
            var service = ServiceFactory.GetProjectService();
            var aservice = ServiceFactory.GetActivityService();
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

        [HttpGet]
        [Breadcrumb("Nacalculatie Detail", FromAction = "Recalculation")]
        //[Breadcrumb("Nacalculatie detail")]
        public ActionResult RecalculationDetail(int projectId, int activityId, int groupid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectRecalculationDetailModel();
            var projectService = ServiceFactory.GetProjectService();
            var activityService = ServiceFactory.GetActivityService();

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
            var projectRecalcAct = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("RecalculationDetail", "Projecten", model.Activity.Name)
            {
                Parent = projectRecalc,
                RouteValues = new {
                    projectId = projectId,
                    activityId = activityId,
                    groupid = groupid
                }
            };
            ViewData["BreadcrumbNode"] = projectRecalcAct;

            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Contract toevoegen", FromAction = "DetailContracts")]
        //[Breadcrumb("Contract toevoegen")]
        public ActionResult AddContract(int projectid, int contractid = 0)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            //Sidebar collapse
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            //Fill model
            ProjectAddContractModel model = new ProjectAddContractModel();
            var service = ServiceFactory.GetProjectService();
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

            model.Insurance.Startdate = DateOnly.FromDateTime(DateTime.Now);
            var iservice = ServiceFactory.GetInsuranceService();
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;

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
            ViewData["BreadcrumbNode"] = projectContractsAdd;

            return View(model);
        }
        [HttpPost]
        public ActionResult AddContract(ProjectAddContractModel model, List<ContractActivityBO> activities, List<ContractAdditionalOrderBO> additionalorders)
        {

            var errors = new Dictionary<string, List<string>>();

            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state != null && state.Errors.Count > 0)
                {
                    errors[key] = state.Errors.Select(e => e.ErrorMessage).ToList();
                }
            }

            if ((!ModelState.IsValid))
                return View(model);
            if ((ModelState.IsValid))
            {
                //Referrer
                var Referrer = TempData["Referrer"];
                foreach (var contractactivity in activities)
                {
                    if (contractactivity.Activity.ID == 142 && contractactivity.ContractId == 0)
                    {
                        InsuranceBO i = new InsuranceBO();
                        i.Startdate = DateOnly.FromDateTime(DateTime.Now);
                        contractactivity.InsuranceData = i;
                    }
                    model.Contract.Activities.Add(contractactivity);
                }

                var service = ServiceFactory.GetProjectService();
                var response = service.InsertUpdateProjectContract(model.Contract);
                if (response.Success)
                {
                    AddMessage("success", "Het contract is toegevoegd aan het project " + model.ProjectName, "Geslaagd!");
                    return Redirect(Referrer.ToString());
                }
                else
                {
                    AddMessage("error", "Het contract is NIET toegevoegd aan het project " + model.ProjectName, "Fout!");
                    return View(model);
                }
            }
            else
                return View(model);
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
            var service = ServiceFactory.GetProjectService();
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
            var pservice = ServiceFactory.GetCompanyService();
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
            var iservice = ServiceFactory.GetInsuranceService();
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;

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
            var projectContractsEdit = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditContract", "Projecten", "Toevoegen")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = projectContractsEdit;

            return View(model);
        }
        [HttpPost]
        public ActionResult EditContract(ProjectAddContractModel model, List<ContractActivityBO> activities, List<ContractAdditionalOrderBO> additionalorders)
        {

            var errors = new Dictionary<string, List<string>>();

            foreach (var key in ModelState.Keys)
            {
                var state = ModelState[key];
                if (state != null && state.Errors.Count > 0)
                {
                    errors[key] = state.Errors.Select(e => e.ErrorMessage).ToList();
                }
            }

            if ((!ModelState.IsValid))
                return View(model);
            if ((ModelState.IsValid))
            {
                //Referrer
                var Referrer = TempData["Referrer"];
                foreach (var contractactivity in activities)
                {
                    if (contractactivity.Activity.ID == 142 && contractactivity.ContractId == 0)
                    {
                        InsuranceBO i = new InsuranceBO();
                        i.Startdate = DateOnly.FromDateTime(DateTime.Now);
                        contractactivity.InsuranceData = i;
                    }
                    model.Contract.Activities.Add(contractactivity);
                }

                var service = ServiceFactory.GetProjectService();
                var response = service.InsertUpdateProjectContract(model.Contract);
                if (response.Success)
                {
                    AddMessage("success", "Het contract is toegevoegd aan het project " + model.ProjectName, "Geslaagd!");
                    return Redirect(Referrer.ToString());
                }
                else
                {
                    AddMessage("error", "Het contract is NIET toegevoegd aan het project " + model.ProjectName, "Fout!");
                    return View(model);
                }
            }
            else
                return View(model);
        }
        [HttpGet]
        public ActionResult ModalDeleteContract(int id)
        {
            var viewModel = new ContractBO();

            if (id != 0)
            {
                var dservice = ServiceFactory.GetProjectService();
                var response = dservice.GetContract(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                    ViewBag.CompanyName = GetCompanyName(viewModel.Company.ID);
                }
            }

            return PartialView("_ModalDeleteContract", viewModel);
        }
        public ActionResult DeleteContract(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = ServiceFactory.GetProjectService();
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

            var service2 = ServiceFactory.GetUnitService();

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
            var pservice = ServiceFactory.GetCompanyService();
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
            var pservice = ServiceFactory.GetProjectService();
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
            return PartialView("_ActivityRow", nContractActivity);
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
                ProjectName = ServiceFactory.GetProjectService().GetProjectNameById(projectid)
            };

            // Groups
            var aservice = ServiceFactory.GetActivityService();
            var groupsResp = aservice.GetActivityGroups();
            model.ActivityGroups = groupsResp.Values?.ToList() ?? new();

            // Reeds ingestelde budgetregels
            var pservice = ServiceFactory.GetProjectService();
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

            ViewData["Title"] = $"Project - {model.ProjectName}";

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


            return View(model);
        }

        [HttpPost]
        public IActionResult CalculationSettings(ProjectCalculationSettings model, List<BudgetActivityBO> budgetactivities)
        {
            if (!ModelState.IsValid)
                return View(model);

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

            var service = ServiceFactory.GetProjectService();
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
            return View(model);
        }

        [HttpPost]
        public IActionResult AddBudgetActivity(int actId)
        {
            var aservice = ServiceFactory.GetActivityService();
            var response = aservice.GetActivitybyId(actId);
            var act = response.Value;

            var budget = new BudgetActivityBO
            {
                Activity = act
            };

            return PartialView("_BudgetActivityRow", budget);
        }

        // ========== PROJECT - INKOMENDE FACTUREN ==========

        [HttpGet]
        //[Breadcrumb("Inkomende factuur toevoegen")]
        [Breadcrumb("Inkomende factuur toevoegen", FromAction = "DetailContracts")]
        public ActionResult AddIncommingInvoice(int projectid, int type, int invoiceid = 0)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var model = new ProjectIncommingInvoiceAddUpdateModel();

            var service2 = ServiceFactory.GetProjectService();
            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);

            // Get the activities
            var service = ServiceFactory.GetActivityService();
            var response = service.GetActivitiesForSelect();

            if (response.Success)
            {
                model.ListActivities = response.Values;
            }

            model.ListActivities = model.ListActivities.OrderBy(m => m.Group).ToList();

            if (type == 0)
            {
                var i = new IdNameBO { Group = "Contractactiviteiten" };
                model.ListActivities.Insert(0, i);
            }
            else if (type == 1)
            {
                var i = new IdNameBO { Group = "Bedrijfsactiviteiten" };
                model.ListActivities.Insert(0, i);
            }

            //if (invoiceid != 0)
            //{
            //    var chresponse = service2.GetIncommingInvoice(invoiceid);
            //    if (chresponse.Success)
            //    {
            //        model.IncommingInvoice = chresponse.Values.FirstOrDefault();
            //    }
            //    model.Type = type;
            //}
            //else
            //{
            model.Type = type;
            model.IncommingInvoice = new IncommingInvoiceBO();
            model.IncommingInvoice.IncommingInvoiceDate = DateOnly.FromDateTime(DateTime.Now);
            model.IncommingInvoice.ContractID = 0;
            //}

            IncommingInvoiceFillInSelectList(model);


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
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddIncommingInvoice", "Projecten", "Factuur toevoegen")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }
        [HttpPost]
        public ActionResult AddIncommingInvoice(ProjectIncommingInvoiceAddUpdateModel model, List<IncommingInvoiceDetailBO> details)
        {
            // Koppel elk detail aan de factuur en voeg toe aan het model
            foreach (var invoiceRow in details)
            {
                invoiceRow.IncommingInvoiceID = model.IncommingInvoice.Id;
                model.IncommingInvoice.Details.Add(invoiceRow);
            }

            if (model.IncommingInvoice.ContractID is null && model.IncommingInvoice.CompanyId is null)
            {
                ModelState.AddModelError("CustomError", "Er is geen bedrijf geselecteerd");
            }

            // Controleer of de totalen overeenkomen
            if (model.IncommingInvoice.InvoicePrice != details.Sum(m => m.Price))
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuuronderdelen komt niet overeen met de totale factuurprijs");
            }
            if (model.IncommingInvoice.InvoicePrice == 0)
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuur is niet ingegeven");
            }

            // Als het model ongeldig is, toon de view opnieuw met foutmeldingen
            if (!ModelState.IsValid)
            {

                ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
                IncommingInvoiceFillInSelectList(model);

                if (model.IncommingInvoice.ContractID != 0)
                {
                    // Indien er ooit logica nodig is om contractactiviteiten op te halen, hier plaatsen
                }

                var activityService = ServiceFactory.GetActivityService();
                var response = activityService.GetActivitiesForSelect();
                if (response.Success)
                {
                    model.ListActivities = response.Values.OrderBy(m => m.Group).ToList();
                }

                var label = model.Type == 0 ? "Contractactiviteiten" : "Bedrijfsactiviteiten";
                model.ListActivities.Insert(0, new IdNameBO { Group = label });

                return View(model);
            }
            var Referrer = TempData["Referrer"];
            // Als het model wél geldig is, bewaar de factuur
            var projectService = ServiceFactory.GetProjectService();
            model.IncommingInvoice.ProjectId = model.ProjectId;

            var saveResponse = projectService.InsertUpdateProjectIncommingInvoice(model.IncommingInvoice);

            if (saveResponse.Success)
            {
                AddMessage("success", $"De factuur is toegevoegd aan het project {model.ProjectName}", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }
            else
            {
                AddMessage("error", $"De factuur is NIET toegevoegd aan het project {model.ProjectName}", "Fout!");
                return View(model);
            }
        }
        [HttpGet]
        //[Breadcrumb("Inkomende factuur bewerken")]
        [Breadcrumb("Inkomende factuur bewerken", FromAction = "DetailContracts")]
        public ActionResult EditIncommingInvoice(int projectid, int invoiceid)
        {
            // Referrer bijhouden

            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectIncommingInvoiceAddUpdateModel
            {
                ProjectId = projectid
            };

            // Projectgegevens ophalen
            var projectService = ServiceFactory.GetProjectService();
            model.ProjectName = projectService.GetProjectNameById(projectid);

            // Activiteiten ophalen
            var activityService = ServiceFactory.GetActivityService();
            var activityResponse = activityService.GetActivitiesForSelect();
            if (activityResponse.Success)
            {
                model.ListActivities = activityResponse.Values
                    .OrderBy(a => a.Group)
                    .ToList();
            }

            // Factuur ophalen
            var invoiceResponse = projectService.GetIncommingInvoice(invoiceid);
            if (invoiceResponse.Success)
            {
                model.IncommingInvoice = invoiceResponse.Values.FirstOrDefault();
            }

            // Type bepalen en optionele group header toevoegen
            if (model.IncommingInvoice?.ContractID is not null)
            {
                model.Type = 0;
                model.ListActivities.Insert(0, new IdNameBO { Group = "Contractactiviteiten" });
            }
            else
            {
                model.Type = 1;
                model.ListActivities.Insert(0, new IdNameBO { Group = "Bedrijfsactiviteiten" });
            }

            // Bedrijfsnaam bepalen op basis van contract of companyId
            var companyService = ServiceFactory.GetCompanyService();
            if (model.IncommingInvoice?.ContractID is null || model.IncommingInvoice.ContractID == 0)
            {
                model.CompanyName = companyService.GetCompanyNameById((int)model.IncommingInvoice.CompanyId);
            }
            else
            {
                model.CompanyName = companyService.GetCompanyNameByContractId((int)model.IncommingInvoice.ContractID);
            }

            // Selectielijsten vullen
            IncommingInvoiceFillInSelectList(model);


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
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditIncommingInvoice", "Projecten", "Factuur bewerken")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = lastnode;


            return View(model);
        }
        [HttpPost]
        public ActionResult EditIncommingInvoice(ProjectIncommingInvoiceAddUpdateModel model, List<IncommingInvoiceDetailBO> details)
        {
            // Koppel elk detail aan de factuur en voeg toe aan het model
            foreach (var invoiceRow in details)
            {
                invoiceRow.IncommingInvoiceID = model.IncommingInvoice.Id;
                model.IncommingInvoice.Details.Add(invoiceRow);
            }

            // Controleer of de totalen overeenkomen
            if (model.IncommingInvoice.ContractID is null && model.IncommingInvoice.CompanyId is null)
            {
                ModelState.AddModelError("CustomError", "Er is geen bedrijf geselecteerd");
            }

            // Controleer of de totalen overeenkomen
            if (model.IncommingInvoice.InvoicePrice != details.Sum(m => m.Price))
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuuronderdelen komt niet overeen met de totale factuurprijs");
            }
            if (model.IncommingInvoice.InvoicePrice == 0)
            {
                ModelState.AddModelError("CustomError", "De prijs van de factuur is niet ingegeven");
            }

            // Als het model ongeldig is, toon de view opnieuw met foutmeldingen
            if (!ModelState.IsValid)
            {


                IncommingInvoiceFillInSelectList(model);

                if (model.IncommingInvoice.ContractID != 0)
                {
                    // Indien er ooit logica nodig is om contractactiviteiten op te halen, hier plaatsen
                }

                var activityService = ServiceFactory.GetActivityService();
                var response = activityService.GetActivitiesForSelect();
                if (response.Success)
                {
                    model.ListActivities = response.Values.OrderBy(m => m.Group).ToList();
                }

                var label = model.Type == 0 ? "Contractactiviteiten" : "Bedrijfsactiviteiten";
                model.ListActivities.Insert(0, new IdNameBO { Group = label });

                return View(model);
            }
            var Referrer = TempData["Referrer"];
            // Als het model wél geldig is, bewaar de factuur
            var projectService = ServiceFactory.GetProjectService();
            model.IncommingInvoice.ProjectId = model.ProjectId;

            var saveResponse = projectService.InsertUpdateProjectIncommingInvoice(model.IncommingInvoice);

            if (saveResponse.Success)
            {
                AddMessage("success", $"De factuur is aangepast aan het project {model.ProjectName}", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }
            else
            {
                AddMessage("error", $"De factuur is NIET aangepast aan het project {model.ProjectName}", "Fout!");
                return View(model);
            }
        }
        [HttpPost]
        public PartialViewResult AddIncommingInvoiceDetailRow(int ActivityId, string ActivityName, int ContractId, int CompanyId)
        {
            var nIncommingInvoiceDetail = new IncommingInvoiceDetailBO();
            var service = ServiceFactory.GetProjectService();
            var response = service.GetContractChangeOrdersForSelect(ContractId);

            if (response.Success)
            {
                nIncommingInvoiceDetail.ChangeOrders = response.Values;
            }

            if (ContractId == 0)
            {
                nIncommingInvoiceDetail.ActivityID = ActivityId;
                nIncommingInvoiceDetail.ContractActivityText = ActivityName;
                nIncommingInvoiceDetail.IncommingInvoiceType = IncommingInvoiceType.Geen_Contract;
            }
            else
            {
                nIncommingInvoiceDetail.ContractActivityID = ActivityId;
                nIncommingInvoiceDetail.ContractActivityText = ActivityName;
                nIncommingInvoiceDetail.IncommingInvoiceType = IncommingInvoiceType.Contract;
            }

            ViewData["mode"] = "add";

            return PartialView("_IncommingInvoiceDetailRow", nIncommingInvoiceDetail);
        }
        [HttpGet]
        public ActionResult ModalDeleteIncommingInvoice(int id, string companyname)
        {
            var viewModel = new IncommingInvoiceBO();

            if (id != 0)
            {
                var dservice = ServiceFactory.GetProjectService();
                var response = dservice.GetIncommingInvoice(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                    ViewBag.CompanyName = companyname;
                }
            }

            return PartialView("_ModalDeleteIncommingInvoice", viewModel);
        }
        public ActionResult DeleteIncommingInvoice(int id, int projectid)
        {
            if (id != 0 && projectid != 0)
            {
                var service = ServiceFactory.GetProjectService();
                var ids = new List<int> { id };
                var response = service.DeleteIncommingInvoices(ids);

                if (response.Success)
                {
                    AddMessage("success", "De factuur is verwijderd", "Geslaagd!");
                    return RedirectToAction("Recalculation", "Projecten", new { projectid = projectid });
                }
                else
                {
                    AddMessage("error", "De factuur is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                    return RedirectToAction("Recalculation", "Projecten", new { projectid = projectid });
                }
            }
            var Referrer = TempData["Referrer"];
            return Redirect(Referrer.ToString());
        }
        [HttpGet]
        //[Breadcrumb("Inkomende factuur")]
        [Breadcrumb("Inkomende factuur", FromAction = "DetailContracts")]
        public ActionResult IncommingInvoiceDetail(int projectid, int invoiceid)
        {
            //Referrer
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var model = new ProjectIncommingInvoiceModel();

            var service = ServiceFactory.GetCompanyService();
            var service2 = ServiceFactory.GetProjectService();
            int companyid = 0;
            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);

            var response = service2.GetIncommingInvoice(invoiceid);
            if (response.Success)
            {
                model.IncommingInvoice = response.Value;
            }
            companyid = model.IncommingInvoice.CompanyId ?? companyid;
            if (model.IncommingInvoice.ContractID is not null)
            {
                var response3 = service2.GetContract((int)model.IncommingInvoice.ContractID);
                if (response3.Success)
                {
                    model.Contract = response3.Value;
                }
            }
            if (companyid == 0)
            {
                companyid = model.Contract?.Company?.ID ?? companyid;
            }

            var response2 = service.GetCompanyByID(companyid);
            if (response2.Success)
            {
                model.Company = response2.Value;
            }

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
            var projectContracts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Recalculation", "Projecten", "Nacalculatie")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddIncommingInvoice", "Projecten", "Inkomende factuur")
            {
                Parent = projectContracts
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }

        // ========== WIJZIGINGSOPDRACHTEN KLANTEN/PROJECTEN ==========

        [HttpGet]
        [Breadcrumb("Wijzigingsopdracht toevoegen", FromController = typeof(KlantenController), FromAction = nameof(KlantenController.Detail))]
        //[Breadcrumb("Wijzigingsopdracht toevoegen")]
        public ActionResult AddChangeOrder(int projectid, int type, int clientaccountid = 0)
        {
            // 1) Veilige referrer voor je "terug"-link (enkel van dezelfde host)
            var refHeader = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(refHeader, UriKind.Absolute, out var refUri) &&
                string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Referrer"] = refHeader;
            }

            // 2) Services ophalen
            var projectService = ServiceFactory.GetProjectService();
            var clientService = ServiceFactory.GetClientService();

            // 3) Model opbouwen (zorgt ervoor dat ChangeOrder nooit null is)
            var model = new ProjectChangeOrderAddUpdateModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid),
                ChangeOrder = new ChangeOrderBO
                {
                    // Als je 'type' wil mappen naar een enum, zie comment onderaan.
                    ChangeOrderConditions = DefaultChangeOrderConditions,
                    // DateOnly: gebruik Today voor datum‑zonder‑tijd
                    ChangeOrderDate = DateOnly.FromDateTime(DateTime.Today),
                    ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
                }
            };

            if (clientaccountid > 0)
            {
                model.ChangeOrder.ClientAccountID = clientaccountid;
                model.ClientName = clientService.GetClientAccountNameById(clientaccountid);

            }



            // 4) Minstens één detailrij
            if (model.ChangeOrder.Details == null || model.ChangeOrder.Details.Count == 0)
            {
                model.ChangeOrder.Details = new List<ChangeOrderDetailBO>
        {
            new ChangeOrderDetailBO
            {
                MeasurementType = MeasurementType.Vermoedelijk,
                MeasurementUnit = MeasurementUnit.stuk,
                Number = 1,
                Commision = 20,
            }
        };
            }

            // 5) Dropdowns / selects vullen
            ChangeOrderFillInSelectList(model);

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
            var projectKlanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddChangeOrder", "Projecten", "Wijzingsopdracht toevoegen")
            {
                Parent = projectKlanten,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }
        private const string DefaultChangeOrderConditions =
            "Het bedrag zal verrekend worden bij de laatste facturatieschijf 'voorlopige oplevering'";

        [HttpPost]
        public ActionResult AddChangeOrder(ProjectChangeOrderAddUpdateModel model, List<ChangeOrderDetailBO> Details)
        {

            // Merge posted details
            if (Details != null)
            {
                foreach (var d in Details)
                    model.ChangeOrder.Details.Add(d);
            }

            // Early return on invalid model
            if (!ModelState.IsValid)
            {
                ChangeOrderFillInSelectList(model);
                return View(model);
            }

            var service = ServiceFactory.GetProjectService();
            var response = service.InsertUpdateProjectChangeOrder(model.ChangeOrder);

            if (response.Success)
            {
                var Referrer = TempData["Referrer"];
                AddMessage("success", "De wijzigingsopdracht is toegevoegd", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }

            AddMessage("error", "De wijzigingsopdracht is NIET toegevoegd", "Fout!");
            ChangeOrderFillInSelectList(model);
            return View(model);
        }

        [HttpGet]
        public PartialViewResult AddChangeOrderDetailRow()
        {
            var model = new ChangeOrderDetailBO
            {
                MeasurementType = MeasurementType.Vermoedelijk,
                MeasurementUnit = MeasurementUnit.stuk,
                Number = 1,
                Price = 0m,
                Commision = 20,
            };
            return PartialView("_ChangeOrderDetailRow", model);
        }
        [HttpGet]
        //[Breadcrumb("Wijzigingsopdracht bewerken")]
        [Breadcrumb("Wijzigingsopdracht bewerken", FromController = typeof(KlantenController), FromAction = nameof(KlantenController.Detail))]
        public ActionResult EditChangeOrder(int projectid, int clientid, int coid)
        {
            // 0) Basisvalidatie
            if (projectid <= 0)
            {
                AddMessage("error", "Ongeldig project.", "Fout!");
                return RedirectToAction("Index", "Projecten");
            }

            // 1) Veilige referrer (relative URL bewaren) + fallback
            var refHeader = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(refHeader, UriKind.Absolute, out var refUri) &&
                string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase) &&
                Url.IsLocalUrl(refUri.PathAndQuery))
            {
                TempData["Referrer"] = refUri.PathAndQuery; // relative is veiliger
            }
            else
            {
                // Fallback als er geen geldige referrer is
                TempData["Referrer"] = Url.Action("Detail", "Projecten", new { projectid, clientid });
            }

            // 2) Services
            var projectService = ServiceFactory.GetProjectService();

            // 3) Model opbouwen met veilige defaults
            var model = new ProjectChangeOrderAddUpdateModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid) ?? string.Empty,
                ChangeOrder = new ChangeOrderBO
                {
                    ProjectId = projectid,
                    ClientAccountID = clientid
                }
            };

            // 4) Bestaande CO ophalen (indien coid > 0)
            if (coid > 0)
            {
                var resp = projectService.GetChangeOrder(coid);
                if (resp?.Success == true)
                {
                    var co = resp.Values?.FirstOrDefault();
                    if (co != null)
                    {
                        model.ChangeOrder = co;
                    }
                    else
                    {
                        AddMessage("warning", "De gevraagde wijzigingsopdracht werd niet gevonden.", "Opgelet");
                    }
                }
                else
                {
                    AddMessage("error", "Kon de wijzigingsopdracht niet ophalen.", "Fout!");
                }
            }

            // 5) Dropdowns / Selects vullen
            ChangeOrderFillInSelectList(model);

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
            var projectKlanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var projectKlantenDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Klanten", model.ClientName)
            {
                Parent = projectKlanten,
                RouteValues = new { projectId = projectid,
                    clientId = clientid
                }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddChangeOrder", "Projecten", "Wijzingsopdracht bewerken")
            {
                Parent = projectKlantenDetail,
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }

        [HttpPost]
        public ActionResult EditChangeOrder(ProjectChangeOrderAddUpdateModel model, List<ChangeOrderDetailBO> Details)
        {

            // Merge posted details
            if (Details != null)
            {
                foreach (var d in Details)
                    model.ChangeOrder.Details.Add(d);
            }

            // Early return on invalid model
            if (!ModelState.IsValid)
            {
                ChangeOrderFillInSelectList(model);
                return View(model);
            }

            var service = ServiceFactory.GetProjectService();
            var response = service.InsertUpdateProjectChangeOrder(model.ChangeOrder);

            if (response.Success)
            {
                var Referrer = TempData["Referrer"];
                AddMessage("success", "De wijzigingsopdracht is bewerkt", "Geslaagd!");
                return Redirect(Referrer.ToString());
            }

            AddMessage("error", "De wijzigingsopdracht is NIET bewerkt", "Fout!");
            ChangeOrderFillInSelectList(model);
            return View(model);
        }
        [HttpGet]
        public ActionResult ModalDeleteChangeOrder(int id)
        {

            var viewModel = new ChangeOrderBO();

            if (id != 0)
            {
                var dservice = ServiceFactory.GetProjectService();
                var response = dservice.GetChangeOrder(id);

                if (response.Success && response.Values.Any())
                {
                    viewModel = response.Values.First();
                }
            }

            return PartialView("_ModalDeleteChangeOrder", viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteChangeOrder(int id)
        {
            if (id == 0)
                return Json(new { success = false, message = "Ongeldig ID." });

            var service = ServiceFactory.GetProjectService();
            var response = service.DeleteChangeOrders(new List<int> { id });

            if (response.Success)
            {
                // Optioneel: server-side toast registreren
                AddMessage("success", "De wijzigingsopdracht is verwijderd", "Geslaagd!");

                // Eenvoudigste aanpak: laat de client de pagina (of enkel de lijst) herladen
                return Json(new { success = true, message = "Verwijderd." });
            }

            AddMessage("error", "De wijzigingsopdracht is niet verwijderd. Probeer opnieuw.", "Fout!");
            return Json(new { success = false, message = "Verwijderen mislukt." });
        }
        [HttpGet]
        public IActionResult ChangeOrderPDF(int changeorderid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectChangeOrderExportModel();
            var clientService = ServiceFactory.GetClientService();
            var projectService = ServiceFactory.GetProjectService();

            var changeOrderResponse = projectService.GetChangeOrder(changeorderid);
            if (changeOrderResponse.Success)
                model.ChangeOrder = changeOrderResponse.Values.FirstOrDefault();

            var projectResponse = projectService.GetProjectByID(model.ChangeOrder.ProjectId);
            if (projectResponse.Success)
                model.Project = projectResponse.Values.FirstOrDefault();

            var salesSettingsResponse = projectService.GetSalesSettings(model.Project.Id);
            if (salesSettingsResponse.Success)
                model.ProjectSalesSettings = salesSettingsResponse.Values.FirstOrDefault();

            var clientResponse = clientService.GetClientAccountById(model.ChangeOrder.ClientAccountID);
            if (clientResponse.Success)
                model.ClientAccount = clientResponse.Values.FirstOrDefault();

            model.Units = clientService.GetClientAccountUnitsNameById(model.ChangeOrder.ClientAccountID);


            // Gebruik juiste constructor om model + view te combineren
            return new ViewAsPdf("ChangeOrderPDF", model)
            {
                PageOrientation = Orientation.Portrait,
                PageMargins = new Margins(10, 5, 0, 5),
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                FileName = $"Wijzigingsopdracht - {model.Project.Name} {DateTime.Now:yyyyMMdd}_{model.ChangeOrder.Id}.pdf",
                //CustomSwitches = $"--footer-html {Url.Action("ChangeOrderFooter", "Projecten", new { text = model.ChangeOrder.ChangeOrderConditions }, "http")} --footer-spacing 0"
            };
        }
        [AllowAnonymous]
        [HttpGet]
        public PartialViewResult ChangeOrderFooter(string text)
        {
            return PartialView("ChangeOrderFooter", text);
        }
        public IActionResult MinimalTestPDF()
        {
            var pdf = new ViewAsPdf("MinimalTestPDF", "Dit is een test.")
            {
                PageOrientation = Orientation.Portrait,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Margins(10, 5, 40, 5),
                FileName = "MinimalTest.pdf"
            };
            return pdf;
        }

        // ========== WEERVERLET ==========

        [HttpGet]
        //[Breadcrumb("Weerverlet")]
        [Breadcrumb("Weerverlet", FromAction = "Index")]
        public ActionResult Weather()
        {
            var model = new BWDModel();
            var service = ServiceFactory.GetProjectService();
            var response = service.GetWheaterstationsSelect();
            if (response.Success)
            {
                model.WeatherStations = response.Values;
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult GetCalendarBundle(int weatherstationid, int year)
        {
            var service = ServiceFactory.GetProjectService();


            var rain = new List<object>();
            var wind = new List<object>();
            var vacation = new List<object>();

            var rainResponse = service.GetBadWeatherDays(weatherstationid, 0);
            if (rainResponse.Success)
            {
                rain = rainResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "Regen/Vorst",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();
            }
            var windResponse = service.GetBadWeatherDays(weatherstationid, 1);
            if (windResponse.Success)
            {
                wind = windResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "Wind",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();
            }
            var vacationResponse = service.GetVacationDays();
            if (vacationResponse.Success)
            {
                vacation = vacationResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "verlofdag",
                    year = b.VacationDay.Year,
                    month = b.VacationDay.Month,
                    day = b.VacationDay.Day,
                    color = "#777"
                }).Cast<object>().ToList();
            }
            return Ok(new { rain = rain, wind = wind, vacation = vacation });
        }

        [HttpGet]
        public JsonResult GetBadWeatherDays(int type, int weatherstationid, int year)
        {
            var service = ServiceFactory.GetProjectService();
            var response = service.GetBadWeatherDays(weatherstationid, type);

            var rows = new List<object>();
            var vacationRows = new List<object>();

            if (response.Success)
            {
                rows = response.Values.Select(b => new
                {
                    id = b.Id,
                    title = "vorst",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();

                var response2 = service.GetVacationDays();
                if (response2.Success)
                {
                    vacationRows = response2.Values.Select(b => new
                    {
                        id = b.Id,
                        title = "verlofdag",
                        year = b.VacationDay.Year,
                        month = b.VacationDay.Month,
                        day = b.VacationDay.Day,
                        color = "#777"
                    }).Cast<object>().ToList();
                }
            }

            var allResults = new List<object>(rows.Count + vacationRows.Count);
            allResults.AddRange(rows);
            allResults.AddRange(vacationRows);


            return new JsonResult(allResults);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // in Core ook geldig
        public int AddBadWeatherDay(DateOnly dag, int weatherstationid, int type)
        {
            var bwd = new BadWeatherDayBO
            {
                BWDate = dag,
                WeatherStationId = weatherstationid,
                Type = type
            };

            var service = ServiceFactory.GetProjectService();
            var response = service.InsertUpdateBadWeatherDay(bwd);

            if (!response.Success) return 0;

            var msg = response.Messages.FirstOrDefault()?.Message;
            return int.TryParse(msg, out var id) ? id : 0;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public bool DeleteBadWeatherDay(int id)
        {
            var list = new List<int> { id };
            var service = ServiceFactory.GetProjectService();
            var response = service.DeleteBadWeatherDays(list);
            return response.Success;
        }

        [HttpGet]
        public JsonResult GetVacationDays()
        {
            var service = ServiceFactory.GetProjectService();
            var response = service.GetVacationDays();

            var rows = new List<object>();
            if (response.Success)
            {
                rows = response.Values.Select(b => new
                {
                    year = b.VacationDay.Year,
                    month = b.VacationDay.Month,
                    day = b.VacationDay.Day
                }).Cast<object>().ToList();
            }

            // Classic MVC:
            return Json(rows);

            // ASP.NET Core alternatief:
            // return new JsonResult(rows);
            // of: return Ok(rows);
        }

        // ========== PROJECT DETAIL FOTO'S ==========

        [HttpGet]
        //[Breadcrumb("Foto's")]
        [Breadcrumb("Foto's", FromAction = "Detail")]
        public ActionResult DetailPhotos(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ViewBag.ImageWebURL = Configuration["URL:ImageWebURL"];
            var model = new DetailPhotosModel();
            var service = ServiceFactory.GetProjectService();
            var response = service.GetPicturesByProjectId(projectid);

            if (response.Success)
            {
                model.Photos = response.Values
                                       .OrderByDescending(m => m.DateTimeUploaded)
                                       .ToList();
            }

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
            var projectRecalc = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailPhotos", "Projecten", "Foto's")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectRecalc;


            return View(model);
        }
        [HttpGet]
        public ActionResult ModalAddPhoto(int id)
        {
            var viewModel = new ProjectPictureBO();
            viewModel.ProjectId = id;
            viewModel.Type = PictureType.Werffoto;
            return PartialView("_ModalAddPhoto", viewModel);
        }

        [HttpGet]
        public ActionResult ModalDeletePhoto(int id)
        {
            var viewModel = new ProjectPictureBO();

            if (id != 0)
            {
                var dservice = ServiceFactory.GetProjectService();
                viewModel = dservice.GetPictureById(id).Value;
            }

            return PartialView("_ModalDeletePhoto", viewModel);
        }

        public ActionResult DeletePhoto(int id, int projectid, PictureType type)
        {
            if (id != 0 && projectid != 0)
            {
                if (type == PictureType.Hoofdfoto)
                {
                    var service = ServiceFactory.GetProjectService();
                    var ids = new List<int> { id };

                    var response1 = service.SetDefaultProjectPicture(projectid, 0);
                    if (response1.Success)
                    {
                        DeletePictureFile(id);
                        var response2 = service.DeletePicture(ids);

                        if (response2.Success)
                        {
                            AddMessage("success", "De foto is verwijderd", "Geslaagd!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                        }
                        else
                        {
                            AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                        }
                    }
                    else
                    {
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                }
                else
                {
                    DeletePictureFile(id);
                    var service = ServiceFactory.GetProjectService();
                    var ids = new List<int> { id };
                    var response = service.DeletePicture(ids);

                    if (response.Success)
                    {
                        AddMessage("success", "De foto is verwijderd", "Geslaagd!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                    else
                    {
                        AddMessage("error", "De foto is niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
                    }
                }
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid });
        }

        public ActionResult UpdatePhotoType(int id, PictureType type)
        {
            var service = ServiceFactory.GetProjectService();
            var picture = service.GetPictureById(id).Value;

            if (picture != null)
            {
                if (type != PictureType.Hoofdfoto)
                {
                    picture.Type = type;
                    var response = service.InsertUpdatePicture(picture);

                    if (response.Success)
                    {
                        AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                    }
                    else
                    {
                        AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!");
                        return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                    }
                }
                else
                {
                    picture.Type = type;

                    var response1 = service.SetDefaultProjectPicture(picture.ProjectId, picture.Id);
                    if (response1.Success)
                    {
                        var response = service.InsertUpdatePicture(picture);
                        if (response.Success)
                        {
                            AddMessage("success", "Het type van de foto is gewijzigd", "Geslaagd!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                        }
                        else
                        {
                            AddMessage("error", "Het type van de foto is NIET gewijzigd", "Fout!");
                            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = picture.ProjectId });
                        }
                    }
                }
            }

            // fallback: als picture null is, vermijden we NullReference
            var fallbackProjectId = picture != null ? picture.ProjectId : 0;
            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = fallbackProjectId });
        }


        // ========== PROJECT DETAIL NIEUWS ==========

        [HttpGet]
        [Breadcrumb("Nieuws", FromAction = "Detail")]
        //[Breadcrumb("Nieuws")]
        public IActionResult DetailNews(int projectId)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var _projectService = ServiceFactory.GetProjectService();

            var model = new DetailNewsModel
            {
                ProjectId = projectId,

                ProjectName = _projectService.GetProjectNameById(projectId),
                News = _projectService.GetNewsByProjectId(projectId).Success
                              ? _projectService.GetNewsByProjectId(projectId).Values
                              : new List<ProjectNewsBO>()
            };
            ViewData["NewsBaseUrl"] = $"{Configuration["URL:ImageWebUrl"]?.TrimEnd('/')}/pictures/News/";


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
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailNews", "Projecten", "Nieuws")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectId }
            };
            ViewData["BreadcrumbNode"] = lastnode;



            return View(model);
        }

        [HttpGet]
        public IActionResult ModalAddNews(int id)
        {
            var vm = new ProjectNewsBO
            {
                NewsDate = DateOnly.FromDateTime(DateTime.Now),
                ProjectId = id 
            };

            return PartialView("_ModalAddNews", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddNews(ProjectNewsBO newsItem, IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                AddMessage("error", "Formulier ongeldig.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }
            var _projectService = ServiceFactory.GetProjectService();
            if (file is not null && file.Length > 0 && IsValidImage(file))
            {

                // bestandsnaam
                var filename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";

                // basis directory uit config (bv. "C:\\images\\")
                var baseDir = (Configuration["URL:ImageLocalURL"] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(baseDir))
                {
                    AddMessage("error", "Upload pad ontbreekt (ImageLocalURL).", "Fout!");
                    return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
                }

                // submappen
                var newsDir = Path.Combine(baseDir, "News");
                var newsOrigDir = Path.Combine(newsDir, "Original");
                var news800Dir = Path.Combine(newsDir, "800");

                EnsureDir(newsDir);
                EnsureDir(newsOrigDir);
                EnsureDir(news800Dir);

                var pMain = Path.Combine(newsDir, filename);
                var pOrig = Path.Combine(newsOrigDir, filename);
                var p800 = Path.Combine(news800Dir, filename);

                using (var stream = System.IO.File.Create(pMain))
                    file.CopyTo(stream);
                System.IO.File.Copy(pMain, pOrig, overwrite: true);
                System.IO.File.Copy(pMain, p800, overwrite: true);

                // afbeeldingsbewerkingen (gebruik jouw bestaande helpers)
                ScaleAndCropImage(pMain, 1280, 500);
                ScaleImage(p800, 800, 800);

                // koppel picture aan nieuwsitem
                var picture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = newsItem.TitleNL,
                    ProjectId = newsItem.ProjectId,
                    Type = PictureType.Nieuws,
                    DateTimeUploaded = DateTime.Now
                };
                newsItem.Picture = picture;
            }

            // auteur (gebruik wat voorhanden is)
            newsItem.Author = (string?)ViewData["fullname"] ?? User?.Identity?.Name ?? "onbekend";

            var response = _projectService.InsertUpdateNews(newsItem);
            if (response.Success)
            {
                AddMessage("success", "Het nieuwsbericht is toegevoegd.", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "Het nieuwsbericht is NIET toegevoegd. Probeer opnieuw of contacteer de administrator.", "Fout!");
            }

            return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
        }

        [HttpGet]
        public IActionResult ModalDeleteNews(int id)
        {
            var vm = new ProjectNewsBO();
            if (id != 0)
            {
                var _projectService = ServiceFactory.GetProjectService();
                var resp = _projectService.GetNewsById(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalDeleteNews", vm);
        }

        public ActionResult DeleteNews(int id, int projectId, int pictureId)
        {
            if (id == 0 || projectId == 0)
                return RedirectToAction("DetailNews", new { projectId });
            var _projectService = ServiceFactory.GetProjectService();
            var resp = _projectService.DeleteNews(new List<int> { id });
            if (!resp.Success)
            {
                AddMessage("error", "Het nieuwsitem is niet verwijderd.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId });
            }

            // eventueel gekoppelde foto verwijderen
            if (pictureId > 0)
            {
                try { DeletePictureFile(pictureId); } catch { /* log indien gewenst */ }

                var respPic = _projectService.DeletePicture(new List<int> { pictureId });
                if (!respPic.Success)
                {
                    AddMessage("error", "Nieuws verwijderd maar foto kon niet verwijderd worden.", "Opgelet");
                    return RedirectToAction("DetailNews", new { projectId });
                }
            }

            AddMessage("success", "Het nieuwsitem is verwijderd.", "Geslaagd!");
            return RedirectToAction("DetailNews", new { projectId });
        }

        [HttpGet]
        public IActionResult ModalEditNews(int id)
        {
            var vm = new ProjectNewsBO();
            if (id != 0)
            {
                var _projectService = ServiceFactory.GetProjectService();
                var resp = _projectService.GetNewsById(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalEditNews", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditNews(ProjectNewsBO newsItem, IFormFile? file, bool RemovePicture = false)
        {
            if (!ModelState.IsValid)
            {
                AddMessage("error", "Formulier ongeldig.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }

            var _projectService = ServiceFactory.GetProjectService();

            // Bewaar referentie naar de (mogelijke) bestaande foto om na succes op te ruimen
            var oldPicId = newsItem.Picture?.Id ?? 0;
            var oldPicName = newsItem.Picture?.Name;

            // Validatie op bestandstype indien er een upload is
            if (file is not null && file.Length > 0 && !IsValidImage(file))
            {
                AddMessage("error", "Verkeerd bestandstype. Kies een JPG/PNG/GIF.", "Fout!");
                return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
            }

            // Nieuwe upload?
            ProjectPictureBO? newPicture = null;
            if (file is not null && file.Length > 0)
            {
                var baseDir = (Configuration["URL:ImageLocalURL"] ?? Configuration["ImageLocalURL"] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(baseDir))
                {
                    AddMessage("error", "Upload pad ontbreekt (URL:ImageLocalURL / ImageLocalURL).", "Fout!");
                    return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
                }

                var newsDir = Path.Combine(baseDir, "News");
                var newsOrigDir = Path.Combine(newsDir, "Original");
                EnsureDir(newsDir);
                EnsureDir(newsOrigDir);

                var filename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.jpg";
                var pMain = Path.Combine(newsDir, filename);
                var pOrig = Path.Combine(newsOrigDir, filename);

                using (var stream = System.IO.File.Create(pMain))
                    file.CopyTo(stream);
                System.IO.File.Copy(pMain, pOrig, overwrite: true);

                // Crop naar headerformaat (pas je helper aan indien nodig)
                ScaleAndCropImage(pMain, 1280, 500);

                newPicture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = newsItem.TitleNL,
                    ProjectId = newsItem.ProjectId,
                    Type = PictureType.Nieuws,
                    DateTimeUploaded = DateTime.Now
                };

                // Koppel nieuwe foto aan het nieuwsitem
                newsItem.Picture = newPicture;
            }
            else if (RemovePicture)
            {
                // Alleen verwijderen (geen nieuwe upload)
                newsItem.Picture = null;               // verwijder koppeling
                                                       // de eigenlijke oude foto (record + bestand) ruimen we op ná een geslaagde update
            }
            // Else: niets doen → bestaande foto blijft gekoppeld

            var response = _projectService.InsertUpdateNews(newsItem);

            if (response.Success)
            {
                // Als we een nieuwe foto hebben gezet of expliciet verwijderen, oude foto opruimen
                if ((newPicture is not null || RemovePicture) && oldPicId > 0)
                {
                    try { DeletePictureFile(oldPicId); } catch { /* loggen indien gewenst */ }
                    _projectService.DeletePicture(new List<int> { oldPicId });
                }

                AddMessage("success", "Het nieuwsbericht is bijgewerkt.", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "Het nieuwsbericht is NIET bijgewerkt.", "Fout!");
            }

            return RedirectToAction("DetailNews", new { projectId = newsItem.ProjectId });
        }

        // ========== PROJECT DETAIL DOCS ==========

        [HttpGet]
        [Breadcrumb("Documenten", FromAction = "Detail")]
        //[Breadcrumb("Documenten")]
        public IActionResult DetailDocs(int projectid, int? clientaccountid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ViewBag.DocWebUrl = Configuration["URL:DocWebUrl"];

            var service = ServiceFactory.GetProjectService();
            var cservice = ServiceFactory.GetClientService();
            var model = new DetailDocsModel
            {
                ProjectId = projectid,
                ProjectName = service.GetProjectNameById(projectid),

            };

            // Als er een client is: clientdocs, anders projectdocs
            if (clientaccountid.HasValue && clientaccountid.Value > 0)
            {

                var respClient = service.GetClientDocs(clientaccountid.Value);
                model.ClientAccountId = (int)clientaccountid;
                model.ClientName = cservice.GetClientAccountNameById(model.ClientAccountId);


                if (respClient.Success)
                    model.Docs = respClient.Values;
                else
                    model.Docs = new List<ProjectDocBO>();
            }
            else
            {
                var respProj = service.GetProjectDocs(projectid);
                if (respProj.Success)
                    model.Docs = respProj.Values;
                else
                    model.Docs = new List<ProjectDocBO>();
            }

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
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailDocs", "Projecten", "Documenten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;


            return View(model);
        }

        [HttpGet]
        public IActionResult ModalAddDoc(int id, int? clientaccountid)
        {
            var vm = new ProjectDocBO
            {
                ProjectId = id,
                ClientAccountId = clientaccountid ?? 0
            };
            return PartialView("_ModalAddDoc", vm);
        }
        [HttpPost]
        public async Task<IActionResult> AddDocument(ProjectDocBO model, IFormFile file)
        {

            if (file == null || file.Length <= 0)
            {
                ModelState.AddModelError("Upload", "U moet een bestand kiezen");
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }
            if (model.ClientAccountId is int i && i <= 0)
                model.ClientAccountId = null;
            var ext = Path.GetExtension(file.FileName) ?? string.Empty;
            var filename = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;

            var uploadDir = Configuration["URL:DocLocalURL"];
            if (string.IsNullOrWhiteSpace(uploadDir))
                uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Docs");

            Directory.CreateDirectory(uploadDir);
            var fullPath = Path.Combine(uploadDir, filename);

            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(fs);
            }

            // Zet filename in het BO vóór de service call
            model.Filename = filename;

            var service = ServiceFactory.GetProjectService();
            var response = service.InsertUpdateProjectDoc(model);

            var clientIdVal = (model.ClientAccountId is int v && v > 0) ? v : 0;

            if (response.Success)
            {
                AddMessage("success", "Het document is toegevoegd / bijgewerkt", "Gelukt!");
                return clientIdVal > 0
                    ? RedirectToAction("Detail", "Klanten", new { clientid = clientIdVal, projectid = model.ProjectId })
                    : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }

            AddMessage("error", "Het document is NIET toegevoegd / bijgewerkt, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            return clientIdVal > 0
                ? RedirectToAction("DetailDocs", new { clientaccountid = clientIdVal, projectid = model.ProjectId })
                : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
        }
        [HttpGet]
        public IActionResult DownloadDoc(int id, string? asName = null)
        {
            var svc = ServiceFactory.GetProjectService();
            var resp = svc.GetProjectDoc(id);
            if (!resp.Success || resp.Value == null) return NotFound();

            var doc = resp.Value;

            // Pad naar opslag
            var uploadDir = Configuration["URL:DocLocalURL"];
            if (string.IsNullOrWhiteSpace(uploadDir))
                uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Docs");

            var path = Path.Combine(uploadDir, doc.Filename ?? "");
            if (!System.IO.File.Exists(path)) return NotFound();

            // ContentType
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            // Gewenste downloadnaam (zorg dat extensie klopt)
            var desired = string.IsNullOrWhiteSpace(asName) ? doc.Filename : asName;
            if (!desired.EndsWith(ext ?? "", StringComparison.OrdinalIgnoreCase))
                desired = Path.GetFileNameWithoutExtension(desired) + ext;  // ext van het echte bestand

            var stream = System.IO.File.OpenRead(path);
            return File(stream, contentType, fileDownloadName: desired);
        }

        [HttpGet]
        public IActionResult ModalDeleteDoc(int id)
        {
            var vm = new ProjectDocBO();
            if (id != 0)
            {
                var _projectService = ServiceFactory.GetProjectService();
                var resp = _projectService.GetProjectDoc(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalDeleteDoc", vm);
        }

        public ActionResult DeleteDoc(int id, int projectId)
        {
            if (id == 0 || projectId == 0)
                return RedirectToAction("DetailDocs", new { projectId });
            var _projectService = ServiceFactory.GetProjectService();
            var resp = _projectService.DeleteProjectDoc(new List<int> { id });
            if (!resp.Success)
            {
                AddMessage("error", "Het document is niet verwijderd.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectId });
            }

            AddMessage("success", "Het document is verwijderd.", "Geslaagd!");
            return RedirectToAction("DetailDocs", new { projectId });
        }

        // ========== PROJECT DETAIL INSURANCES ==========

        [HttpGet]
        //[Breadcrumb("Verzekeringen")]
        [Breadcrumb("Verzekeringen", FromAction = "Detail")]
        public IActionResult DetailInsurances(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new DetailInsurancesModel();
            var service = ServiceFactory.GetProjectService();
            var response = service.GetProjectInsurances(projectid);

            if (response.Success)
                model.Insurances = response.Values;

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
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailInsurances", "Projecten", "Verzekeringen")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditInsurance(ProjectAddInsurancesModel viewmodel)
        {
            var response = new Response();

            if (ModelState.IsValid)
            {
                if (viewmodel?.Insurance == null)
                {
                    response.AddError("Insurance model is leeg.");
                }
                else
                {
                    viewmodel.Insurance.ProjectID = viewmodel.ProjectId;
                    var service = ServiceFactory.GetInsuranceService();
                    response = service.InsertUpdate(viewmodel.Insurance);
                }
            }

            if (response.Success)
            {
                AddMessage("success", "De verzekering is toegevoegd", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De verzekering is NIET toegevoegd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            }

            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = viewmodel?.Insurance?.ProjectID ?? viewmodel?.ProjectId });
        }


        [HttpGet]
        public IActionResult ModalDeleteInsurance(int id)
        {
            var viewModel = new InsuranceBO();

            if (id != 0)
            {
                var dservice = ServiceFactory.GetInsuranceService();
                viewModel = dservice.GetInsuranceById(id).Value;
            }

            return PartialView("_ModalDeleteInsurance", viewModel);
        }

        [HttpGet]
        public IActionResult ModalEndInsurance(int id)
        {
            var viewModel = new InsuranceBO();

            if (id != 0)
            {
                var dservice = ServiceFactory.GetInsuranceService();
                viewModel = dservice.GetInsuranceById(id).Value;

                if (viewModel != null)
                {
                    if (viewModel.Type == InsuranceType.ABR && viewModel.Startdate.HasValue)
                    {
                        var start = viewModel.Startdate.Value;
                        viewModel.Enddate = start.AddMonths(
                            (viewModel.Period ?? 0) + (viewModel.ExtensionPeriod ?? 0) + (viewModel.GuaranteePeriod ?? 0)
                        );
                    }
                    else
                    {
                        viewModel.Enddate = DateOnly.FromDateTime(DateTime.Now);
                    }
                }
            }

            return PartialView("_ModalStopInsurance", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EndInsurance(InsuranceBO viewmodel)
        {
            var response = new Response();

            if (ModelState.IsValid)
            {
                var service = ServiceFactory.GetInsuranceService();
                response = service.InsertUpdate(viewmodel);
            }

            if (response.Success)
            {
                AddMessage("success", "De verzekering is beëindigd", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De verzekering is NIET beëindigd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            }

            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = viewmodel.ProjectID });
        }

        [HttpGet]
        public IActionResult ModalEditInsurance(int id)
        {
            var viewModel = new ProjectAddInsurancesModel();

            if (id != 0)
            {
                var dservice = ServiceFactory.GetInsuranceService();
                viewModel.Insurance = dservice.GetInsuranceById(id).Value;
            }

            // zeker dat ProjectId gezet is
            viewModel.ProjectId = viewModel.Insurance?.ProjectID ?? viewModel.ProjectId;

            var service = ServiceFactory.GetInsuranceService();
            var cservice = ServiceFactory.GetCompanyService();

            var cresponse = cservice.GetCompanyForSelectByActivity(142);
            if (cresponse.Success) viewModel.Brokers = cresponse.Values;

            var response = service.GetInsuranceCompaniesForSelect();
            if (response.Success) viewModel.Companies = response.Values;

            // Zelfde partial als Add
            return PartialView("_ModalEditInsurance", viewModel);
        }

        // ========== PROJECT DETAIL SALES ==========

        [HttpGet]
        //[Breadcrumb("Verkoop")]
        [Breadcrumb("Verkoop", FromAction = "Detail")]
        public IActionResult DetailSales(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectSalesModel();
            var service = ServiceFactory.GetProjectService();
            var uservice = ServiceFactory.GetUnitService();

            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);

            var response = uservice.GetUnitsWithAttachedByProjectId(projectid);
            model.ProjectUnits = response.Values;


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
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailSales", "Projecten", "Verkoop")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(model);
        }

        [HttpGet]
        public IActionResult SalesListPdf(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new ProjectSalesExportModel();
            var service2 = ServiceFactory.GetProjectService();
            var service3 = ServiceFactory.GetUnitService();

            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);

            // Get Units
            var response = service3.GetGroupedUnitsForSaleWithDetailsByProjectId(projectid);
            model.UnitsGrouped = response.Values;

            // Get SurfaceTypes
            model.SurfaceTypes = service3.GetUniqueRoomTypesInProjectByProjectId(projectid).Values;

            // PDF
            // Belangrijk: model via constructor doorgeven
            var pdf = new ViewAsPdf("SalesListPDF", model)
            {
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                PageSize = Rotativa.AspNetCore.Options.Size.A3,
                FileName = $"Verkooplijst - {model.ProjectName} {DateTime.Now:yyyyMMdd}.pdf"
            };

            // var pdfBytes = a.BuildPdf(ControllerContext); // kan, maar niet nodig om te returnen
            return pdf;
            // return File(pdfBytes, "application/pdf"); // alternatief
        }

        [HttpGet]
        [Breadcrumb("Verkoopsinstellingen", FromAction = "DetailSales")]
        //[Breadcrumb("Verkoopsinstellingen")]
        public IActionResult SalesSettings(int projectid)
        {
            // 1) Veilige referrer voor je "terug"-link (enkel van dezelfde host)
            var refHeader = Request.Headers["Referer"].ToString();
            if (Uri.TryCreate(refHeader, UriKind.Absolute, out var refUri) &&
                string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Referrer"] = refHeader;
            }
            var viewModel = new ProjectSalesSettingsModel();
            if (projectid != 0)
            {
                viewModel.ProjectId = projectid;
            }

            var service = ServiceFactory.GetProjectService();
            var response = service.GetSalesSettings(projectid);
            if (response.Success) viewModel.Settings = response.Value;

            var presponse = service.GetProjectByID(projectid);
            if (presponse.Success) viewModel.Project = presponse.Value;


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", viewModel.Project.Name)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectSales = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailSales", "Projecten", "Verkoop")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("SalesSettings", "Projecten", "Instellingen")
            {
                Parent = projectSales,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult SalesSettings(ProjectSalesSettingsModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var service = ServiceFactory.GetProjectService();

            // Eerste bewerking
            var response1 = service.InsertUpdateSalesSettings(model.Settings);

            // Tweede bewerking
            var response2 = service.InsertUpdateSalesText(model.Project);

            // Beide resultaten samen beoordelen
            if (response1.Success && response2.Success)
            {
                AddMessage("success", "De instellingen voor de verkoop zijn aangepast", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De instellingen voor de verkoop zijn NIET aangepast", "Fout!");
            }

            return RedirectToAction("DetailSales", "Projecten", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalculateCosts(int projectId, [FromBody] int[] unitIds)
        {
            if (unitIds == null || unitIds.Length == 0)
                return BadRequest("Geen eenheden geselecteerd.");

            var ids = unitIds.Distinct().ToList();

            // Settings ophalen
            var ps = ServiceFactory.GetProjectService();
            var settingsResp = ps.GetSalesSettings(projectId); // Response<ProjectSalesSettingsBO>
            if (!settingsResp.Success || settingsResp.Value == null)
                return BadRequest("Geen verkoopinstellingen gevonden.");
            var settings = settingsResp.Value;

            // Units ophalen
            var uservice = ServiceFactory.GetUnitService();
            var unitsResp = uservice.GetUnitsById(ids);
            var units = unitsResp.Success && unitsResp.Values != null
                ? unitsResp.Values.ToList()
                : new List<UnitBO>();
            if (units.Count == 0) return BadRequest("Geen eenheden gevonden.");

            // Eerste render: kortingen 0, geen overrides (null)
            var vm = BuildCalculationVM(projectId, settings, units, discounts: null, overrides: null);

            return PartialView("_CostCalculationCard", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecalculateCosts(int projectId, [FromBody] RecalculateRequest payload)
        {
            if (payload == null || payload.Units == null || payload.Units.Count == 0)
                return BadRequest("Ongeldige herberekening.");

            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var projectService = scope.ServiceProvider.GetRequiredService<FacadeCore.IProjectService>();
                var unitService = scope.ServiceProvider.GetRequiredService<FacadeCore.IUnitService>();

                var ids = payload.Units.Select(x => x.UnitId).Distinct().ToList();

                var settingsResp = projectService.GetSalesSettings(projectId);
                if (!settingsResp.Success || settingsResp.Value == null)
                    return BadRequest("Geen verkoopinstellingen gevonden.");

                var unitsResp = unitService.GetUnitsById(ids);
                var units = unitsResp.Success && unitsResp.Values != null
                    ? unitsResp.Values.ToList()
                    : new List<UnitBO>();
                if (units.Count == 0) return BadRequest("Geen eenheden gevonden.");

                var discounts = payload.Units.ToDictionary(k => k.UnitId, v => v);
                var vm = BuildCalculationVM(projectId, settingsResp.Value, units, discounts, payload);

                return PartialView("_CostCalculationCard", vm);
            }


           


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PrintCalculation(int projectId, [FromBody] RecalculateRequest payload)
        {
            if (payload == null || payload.Units == null || payload.Units.Count == 0)
                return BadRequest("Geen eenheden opgegeven.");

            // Zorg voor verse scoped services (concurrency-safe)
            using var scope = HttpContext.RequestServices.CreateScope();
            var projectService = scope.ServiceProvider.GetRequiredService<FacadeCore.IProjectService>();
            var unitService = scope.ServiceProvider.GetRequiredService<FacadeCore.IUnitService>();

            var unitIds = payload.Units.Select(x => x.UnitId).Distinct().ToList();

            var settingsResp = projectService.GetSalesSettings(projectId);
            if (!settingsResp.Success || settingsResp.Value == null)
                return BadRequest("Geen verkoopinstellingen gevonden.");

            var unitsResp = unitService.GetUnitsById(unitIds);
            var units = unitsResp.Success ? unitsResp.Values.ToList() : new List<UnitBO>();
            if (units.Count == 0) return BadRequest("Geen eenheden gevonden.");

            var discounts = payload.Units.ToDictionary(k => k.UnitId, v => v);
            var vm = BuildCalculationVM(projectId, settingsResp.Value, units, discounts, payload);

            var doc = new CostCalculationDocument(vm, $"Kostencalculatie – {vm.UnitCount} eenheid(en)");
            var pdf = doc.GeneratePdf();

            var fileName = $"Kostencalculatie_{projectId}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        private ProjectCostCalculationVM BuildCalculationVM(
            int projectId,
            ProjectSalesSettingsBO settings,
            List<UnitBO> units,
            Dictionary<int, UnitDiscountInput> discounts,
            RecalculateRequest overrides)
        {
            var vatPctBase = overrides?.VatPercent ?? settings.VatPercentage ?? 0m;
            var regPctBase = overrides?.RegistrationPercent ?? settings.RegistrationPercentage ?? 0m;

            // per-unit kosten
            var surveyorEx = overrides?.SurveyorCost ?? settings.SurveyorCost ?? 0m;
            var connectionEx = overrides?.ConnectionFees ?? settings.ConnectionFees ?? 0m;
            var baseDeedEx = overrides?.BaseCertificateCost ?? settings.BaseCertificateCost ?? 0m;
            var parcelEx = overrides?.ParcelCost ?? settings.ParcelCost ?? 0m;

            // globale kosten
            var fixedActeEx = overrides?.FixedCertificateCost ?? settings.FixedCertificateCost ?? 0m;
            var mortgageEx = overrides?.MortageRegistrationCost ?? settings.MortageRegistrationCost ?? 0m;

            var vatPctCostsOther = 21m;
            var vatPctCostsConnection = vatPctBase;

            var vm = new ProjectCostCalculationVM
            {
                ProjectId = projectId,
                VatPercent = vatPctBase,
                RegistrationPercent = regPctBase,
                RegistrationType = (Models.Projecten.RegistrationType)settings.RegistrationType,
                MixedVatRegistration = settings.MixedVatRegistration ?? false,

                FixedCertificateCost = fixedActeEx,
                SurveyorFee = surveyorEx,
                ConnectionFee = connectionEx,
                BaseDeedShare = baseDeedEx,
                ParcelCost = parcelEx,
                MortgageRegistrationCost = mortgageEx,

                VatPctCostsOther = vatPctCostsOther,
                VatPctCostsConnection = vatPctCostsConnection
            };

            var mode = vm.RegistrationType;
            if (vm.MixedVatRegistration && mode != Models.Projecten.RegistrationType.Mixed) mode = Models.Projecten.RegistrationType.Mixed;

            decimal totalNetLand = 0m, totalNetBuild = 0m;
            int countIncluded = 0;

            foreach (var u in units)
            {
                var land = u.LandValue ?? 0m;
                var build = (u.ConstructionValues?.Sum(x => x.Value ?? 0m)) ?? 0m;

                decimal landDisc = 0m, buildDisc = 0m;
                bool include = ShouldDefaultInclude(u);

                if (discounts != null && discounts.TryGetValue(u.Id, out var d))
                {
                    if (d.LandDiscount > 0) landDisc = Math.Min(d.LandDiscount.Value, land);
                    if (d.BuildDiscount > 0) buildDisc = Math.Min(d.BuildDiscount.Value, build);
                    if (d.IncludePerUnitCosts.HasValue) include = d.IncludePerUnitCosts.Value;
                    if (d.IncludePerUnitCosts.HasValue)
                        include = d.IncludePerUnitCosts.Value;
                }

                var netLand = Math.Max(0m, land - landDisc);
                var netBuild = Math.Max(0m, build - buildDisc);
                var baseSum = netLand + netBuild;

                totalNetLand += netLand;
                totalNetBuild += netBuild;

                decimal vatOnBase = 0m, regOnBase = 0m;
                switch (mode)
                {
                    case Models.Projecten.RegistrationType.Vat:
                        vatOnBase = Percent(baseSum, vatPctBase); break;
                    case Models.Projecten.RegistrationType.Registration:
                        regOnBase = Percent(baseSum, regPctBase); break;
                    case Models.Projecten.RegistrationType.Mixed:
                        vatOnBase = Percent(netBuild, vatPctBase);
                        regOnBase = Percent(netLand, regPctBase);
                        break;
                }

                decimal costsExcl = 0m, costsVat = 0m;
                if (include)
                {
                    countIncluded++;
                    costsExcl = surveyorEx + connectionEx + baseDeedEx + parcelEx;
                    costsVat = Percent(surveyorEx + baseDeedEx + parcelEx, vatPctCostsOther)
                              + Percent(connectionEx, vatPctCostsConnection);
                }

                vm.Lines.Add(new UnitCostLineVM
                {
                    UnitId = u.Id,
                    Code = string.IsNullOrWhiteSpace(u.Name) ? $"U{u.Id}" : u.Name,

                    LandBase = land,
                    BuildBase = build,
                    LandDiscount = landDisc,
                    BuildDiscount = buildDisc,

                    VatAmount = vatOnBase,
                    RegistrationAmount = regOnBase,

                    IncludePerUnitCosts = include,
                    CostsExcl = costsExcl,
                    CostsVat = costsVat
                });
            }

            // Globaal: notaris (schijven) + 21% btw
            var notaryEx = CalculateNotaryFeesFromTotals(totalNetLand, totalNetBuild, mode == Models.Projecten.RegistrationType.Mixed);
            var notaryVat = Percent(notaryEx, 21m);

            // Globaal: vaste akte + hypo (21% btw)
            var fixedActeVat = Percent(fixedActeEx, 21m);
            var mortgageVat = Percent(mortgageEx, 21m);

            // Per-unit totalen (alleen de inbegrepen rijen)
            vm.CostTotals = new CostTotalsVM
            {
                NotaryExcl = notaryEx,
                NotaryVat = notaryVat,
                FixedActeExcl = fixedActeEx,
                FixedActeVat = fixedActeVat,
                MortgageExcl = mortgageEx,
                MortgageVat = mortgageVat,

                SurveyorExcl = surveyorEx * countIncluded,
                SurveyorVat = Percent(surveyorEx, vatPctCostsOther) * countIncluded,

                ConnectionExcl = connectionEx * countIncluded,
                ConnectionVat = Percent(connectionEx, vatPctCostsConnection) * countIncluded,

                BaseDeedExcl = baseDeedEx * countIncluded,
                BaseDeedVat = Percent(baseDeedEx, vatPctCostsOther) * countIncluded,

                ParcelExcl = parcelEx * countIncluded,
                ParcelVat = Percent(parcelEx, vatPctCostsOther) * countIncluded
            };

            return vm;
        }



        // ========== PROJECT DETAIL INVOICING ==========

        // GET: /Projecten/Invoicing?projectid=123
        [HttpGet]
        public IActionResult Invoicing(int projectid)
        {
            // In je bestaande code gaat dit via ServiceFactory.
            var projectService = ServiceFactory.GetProjectService();
            var clientService = ServiceFactory.GetClientService();
            var unitService = ServiceFactory.GetUnitService();

            var model = new ProjectInvoicingModel
            {
                ProjectId = projectid,
                ProjectName = projectService.GetProjectNameById(projectid)
            };

            // ===== Schijven (vordering der werken) =====
            var respUnits = projectService.GetProjectInvoicableUnits(projectid);
            if (respUnits.Success)
            {
                var accountIds = respUnits.Values.Select(v => v.Unit.ClientAccountId)
                                                 .Where(id => id.HasValue).Select(id => id!.Value)
                                                 .Distinct().ToList();

                var respClients = clientService.GetClientAccountByIds(accountIds);
                if (respClients.Success)
                {
                    foreach (var client in respClients.Values)
                    {
                        var group = new ClientAccountWithInvoicableBO { Client = client };
                        foreach (var u in respUnits.Values.Where(v => v.Unit.ClientAccountId == client.Id))
                            group.Units.Add(u);
                        model.ClientAccounts.Add(group);
                    }
                }
            }

            // ===== Meer-/minwerken =====
            var respCo = projectService.GetProjectInvoicableChangeOrders(projectid);
            if (respCo.Success)
            {
                var accountIds = respCo.Values.Select(v => v.ClientAccountID).Distinct().ToList();
                var respClients = clientService.GetClientAccountByIds(accountIds);
                if (respClients.Success)
                {
                    foreach (var client in respClients.Values)
                    {
                        var group = new ClientAccountWithInvoicableChangeOrderBO { Client = client };
                        foreach (var co in respCo.Values.Where(v => v.ClientAccountID == client.Id))
                            group.ChangeOrders.Add(co);
                        model.ClientChangeOrders.Add(group);
                    }
                }
            }

            // Sortering zoals je VB: op eerste woon-unitnaam (GroupId 1) anders eerste unitnaam
            model.ClientAccounts = model.ClientAccounts
                .OrderBy(m =>
                {
                    var woon = m.Units.FirstOrDefault(a => a.Unit.Type.GroupId == 1)?.Unit.Name
                               ?? m.Units.FirstOrDefault()?.Unit.Name
                               ?? "";
                    return woon;
                }, new ServiceCore.Helpers.AlphanumComparator()) // jouw bestaande comparer
                .ToList();

            //// ===== Nuts (optioneel) =====
            //var respClientUnits = clientService.GetClientAccountsByProjectIdWithUnits(projectid);
            //if (respClientUnits.Success)
            //{
            //    foreach (var client in respClientUnits.Values)
            //    {
            //        if (client.Units.Any(u => u.Type.GroupId == 1 || u.Type.GroupId == 4))
            //        {
            //            // Deze methode heb je al in jouw code
            //            var cuc = projectService.GetClientUtilityCost(client.Client.Id, projectid); // moet ClientUtilityCostVM opleveren
            //            if (cuc != null) model.ClientUtilityCosts.Add(cuc);
            //        }
            //    }
            //}

            return View(model);
        }
        // POST: schijven factureren
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MakeInvoices([FromBody] MakeInvoicesRequest request)
        {
            if (request?.Invoices == null || request.Invoices.Count == 0)
                return Json(new { projectid = 0 });

            var clientService = ServiceFactory.GetClientService();
            var unitService = ServiceFactory.GetUnitService();
            var projectService = ServiceFactory.GetProjectService();

            var response = new Response();
            int projectId = 0;

            // Alle betrokken klanten ophalen
            var clientIds = request.Invoices.Select(x => x.ClientAccountId).Distinct().ToList();
            var clientResp = clientService.GetClientAccountByIds(clientIds);
            var clientAccounts = clientResp.Success ? clientResp.Values : new List<ClientAccountBO>();

            foreach (var client in clientAccounts)
            {
                var iu = new List<UnitWithStagesBO>();

                // Units + stages
                foreach (var unitId in request.Invoices.Where(i => i.ClientAccountId == client.Id).Select(i => i.Unitid).Distinct())
                {
                    var unitbo = new UnitWithStagesBO();
                    var ru = unitService.GetUnitById(unitId);
                    if (ru.Success)
                    {
                        unitbo.Unit = ru.Value;
                        foreach (var item in request.Invoices.Where(i => i.Unitid == unitbo.Unit.Id))
                        {
                            var rStage = projectService.GetProjectPaymentStage(item.StageId);
                            if (rStage.Success) unitbo.PaymentStages.Add(rStage.Value);
                        }
                        iu.Add(unitbo);
                    }
                }

                // Project + salessettings
                var project = new ProjectBO();
                var settings = new ProjectSalesSettingsBO();
                if (iu.Count > 0)
                {
                    var pr = projectService.GetProjectByID(iu.First().Unit.ProjectId);
                    if (pr.Success)
                    {
                        project = pr.Value;
                        var sr = projectService.GetSalesSettings(project.Id);
                        if (sr.Success) settings = sr.Value;
                    }
                }

                //// === JOUW bestaande factuur-constructie ===
                //var result = MakeNewWordInvoiceFTP(client, iu, project, settings);
                //// Bovenstaande functie genereerde vroeger een Word; vervang intern door
                //// jouw nieuwe implementatie die InvoiceBO + Rows maakt en:
                ////  - PDF = _pdf.Render(...)
                ////  - UBL = _ubl.BuildUblXml(...)
                ////  - (optioneel) _ubl.SendToPeppolAsync(...)

                //if (!result.Success) response.AddError(result.Messages);

                projectId = project.Id;
            }

            if (response.Success)
            {
                AddMessage("success", "De facturen zijn aangemaakt", "Gelukt!");
                return Json(new { projectid = projectId });
            }
            else
            {
                AddMessage("Error", "Niet alle facturen zijn aangemaakt. Probeer opnieuw of contacteer de administrator.", "Fout!");
                return Json(new { projectid = projectId });
            }
        }

        // POST: meerwerken factureren
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MakeInvoicesCO([FromBody] MakeInvoicesCoRequest request)
        {
            if (request?.Invoices == null || request.Invoices.Count == 0)
                return Json(new { projectid = 0 });

            var clientService = ServiceFactory.GetClientService();
            var unitService = ServiceFactory.GetUnitService();
            var projectService = ServiceFactory.GetProjectService();

            var response = new Response();
            int projectId = 0;

            var clientIds = request.Invoices.Select(x => x.ClientAccountId).Distinct().ToList();
            var clientResp = clientService.GetClientAccountByIds(clientIds);
            var clientAccounts = clientResp.Success ? clientResp.Values : new List<ClientAccountBO>();

            foreach (var client in clientAccounts)
            {
                var respUnits = unitService.GetUnitsByAccountId(client.Id);
                var units = respUnits.Success ? respUnits.Values : new List<UnitBO>();

                var changeOrders = new List<ChangeOrderBO>();
                foreach (var coId in request.Invoices.Where(i => i.ClientAccountId == client.Id).Select(i => i.ChangeOrderId).Distinct())
                {
                    var rco = projectService.GetChangeOrder(coId);
                    if (rco.Success)
                    {
                        var co = rco.Value;
                        // Filter alleen de aangevinkte details
                        for (int i = co.Details.Count - 1; i >= 0; i--)
                        {
                            var keep = request.Invoices.Any(l => l.ChangeOrderDetailId == co.Details[i].Id);
                            if (!keep) co.Details.RemoveAt(i);
                        }
                        changeOrders.Add(co);
                    }
                }

                var project = new ProjectBO();
                var settings = new ProjectSalesSettingsBO();
                if (changeOrders.Count > 0)
                {
                    var pr = projectService.GetProjectByID(changeOrders.First().ProjectId);
                    if (pr.Success)
                    {
                        project = pr.Value;
                        var sr = projectService.GetSalesSettings(project.Id);
                        if (sr.Success) settings = sr.Value;
                    }
                }

                //var result = MakeNewWordInvoiceCOFTP(client, changeOrders, units, project, settings);
                //if (!result.Success) response.AddError(result.Messages);

                projectId = project.Id;
            }

            if (response.Success)
            {
                AddMessage("success", "De facturen zijn aangemaakt", "Gelukt!");
                return Json(new { projectid = projectId });
            }
            else
            {
                AddMessage("Error", "Niet alle facturen zijn aangemaakt. Probeer opnieuw of contacteer de administrator.", "Fout!");
                return Json(new { projectid = projectId });
            }
        }

        // ====== DTO’s voor de JSON-body ======
        public class MakeInvoicesRequest
        {
            public List<ClientAccountUnitInvoiceBO> Invoices { get; set; } = new();
        }

        public class MakeInvoicesCoRequest
        {
            public List<ClientAccountChangeOrderInvoiceBO> Invoices { get; set; } = new();
        }


        //SHARED
        public void ChangeOrderFillInSelectList(ProjectChangeOrderAddUpdateModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var projectService = ServiceFactory.GetProjectService();
            var clientService = ServiceFactory.GetClientService();

            var cresponse = clientService.GetClientAccountsByProjectIdForSelect(model.ProjectId);
            model.ClientAccounts = cresponse.Success ? cresponse.Values : model.ClientAccounts;

            var aresponse = projectService.GetProjectContractActivitiesForSelect(model.ProjectId);
            model.ProjectContractActivities = aresponse.Success ? aresponse.Values : model.ProjectContractActivities;
        }

        private void FillInAddSelectListsDetail(ref ShowProjectDetail model)
        {
            // get the activities
            var cservice = ServiceFactory.GetCountryService();
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if ((cresponse.Success))
                model.Countries = cresponse.Values;
            var defCountry = model.Countries.Where(m => m.Group == "19").FirstOrDefault();
            if ((defCountry != null))
                model.SelectedCountry = defCountry.ID;
            // Get statuses
            var service = ServiceFactory.GetProjectService();
            var response = service.GetStatusesForSelect();
            if ((response.Success))
                model.Statuses = response.Values;
            model.SelectedStatus = model.Project.Status.Id;
        }
        public void IncommingInvoiceFillInSelectList(ProjectIncommingInvoiceAddUpdateModel model)
        {
            var service2 = ServiceFactory.GetProjectService();
            var aresponse = service2.GetProjectContractsForSelect(model.ProjectId);
            if (aresponse.Success)
            {
                model.ProjectContracts = aresponse.Values;
            }
        }
        public void AddMessage(string messagetype, string message, string messagetitle)
        {
            TempData["Message"] = message;
            TempData["MessageType"] = messagetype;
            TempData["MessageTitle"] = messagetitle;
        }
        public string GetCompanyName(int companyid)
        {
            var pservice = ServiceFactory.GetCompanyService();
            var presponse = pservice.GetCompanyNameById(companyid);
            return presponse;
        }
        [HttpPost]
        public JsonResult GetCompanys(string term)
        {
            var pservice = ServiceFactory.GetCompanyService();
            var presponse = pservice.GetCompanyForSearchList(term);
            var iList = new List<SelectBO>();

            if (presponse.Success)
            {
                iList = presponse.Values;
            }

            return Json(iList);
        }
        public class Select2DTO
        {
            // Select2 expects objects with 'id' and 'text' fields
            public int id { get; set; }
            public string text { get; set; }
            public string group { get; set; }
        }

        //IMAGE HANDLERS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(ProjectPictureBO UploadedBO, IFormFile file)
        {
            // 0) Validatie
            if (file == null || file.Length == 0)
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif"
    };
            if (!validTypes.Contains(file.ContentType))
            {
                ModelState.AddModelError("ImageUpload", "Verkeerd type gekozen, kies een gif, jpeg of png");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            // 1) Upload-map (lokaal) – zelfde stijl als je snippet
            var uploadDir = (Configuration["URL:ImageLocalURL"] ?? Path.Combine(Path.GetTempPath(), "project-pictures")).Trim();
            CheckDir(uploadDir);                           // jouw bestaande helper die Directory.CreateDirectory doet

            // submappen voor varianten
            var dir447 = Path.Combine(uploadDir, "447");
            var dir800 = Path.Combine(uploadDir, "800");
            CheckDir(dir447);
            CheckDir(dir800);

            // 2) Bestandsnaam (zoals VB): altijd .jpg
            var filename = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".jpg";

            // 3) Volledige paden
            var imagePath = Path.Combine(uploadDir, filename);
            var imagePath2 = Path.Combine(dir447, filename);
            var imagePath3 = Path.Combine(dir800, filename);

            // 4) Opslaan zoals in je snippet (één bestand, FileStream)
            using (var fs = new FileStream(imagePath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            // Kopieën maken voor bewerking (zelfde patroon als je VB)
            System.IO.File.Copy(imagePath, imagePath2, true);
            System.IO.File.Copy(imagePath, imagePath3, true);

            // 5) Bewerken (je bestaande ImageSharp-helpers die we eerder maakten)
            // - 447x447 gecropt
            ScaleAndCropImage(imagePath2, 447, 447);
            // - 800x800 gecropt
            ScaleAndCropImage(imagePath3, 800, 800);
            // - origineel schalen naar max 1280x960
            ScaleImage(imagePath, 1280, 960);

            // 6) DB opslaan (zelfde logica als VB)
            var picture = new ProjectPictureBO
            {
                Name = filename,
                Caption = UploadedBO.Caption,
                ProjectId = UploadedBO.ProjectId,
                Type = UploadedBO.Type,
                DateTimeUploaded = DateTime.Now
            };

            var service = ServiceFactory.GetProjectService();
            var response = service.InsertUpdatePicture(picture);

            if (picture.Type == PictureType.Hoofdfoto && response?.Messages != null)
            {
                foreach (var msg in response.Messages)
                {
                    if (msg.Type == MessageType.Value && int.TryParse(msg.Message, out var pictureId))
                    {
                        _ = service.SetDefaultProjectPicture(UploadedBO.ProjectId, pictureId);
                    }
                }
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImageFtp(ProjectPictureBO UploadedBO, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif"
    };

            if (!validTypes.Contains(photo.ContentType))
            {
                ModelState.AddModelError("ImageUpload", "Verkeerd type gekozen, kies een gif, jpeg of png");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            // 1) Lokaal temp-pad
            var tempRoot = (Configuration["URL:LocalTempPath"] ?? Path.Combine(Path.GetTempPath(), "project-pictures")).Trim();
            Directory.CreateDirectory(tempRoot);

            // 2) FTP root-pad
            var baseFtp = (Configuration["URL:PicturesFtpPath"] ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseFtp))
            {
                ModelState.AddModelError("", "FTP-pad ontbreekt (URL:PicturesFtpPath).");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            await using var ftp = await ConnectAsync();
            if (ftp == null)
            {
                ModelState.AddModelError("", "FTP-verbinding mislukt.");
                return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
            }

            var filename = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".jpg";

            // 3) Lokaal opslaan
            var localOriginal = Path.Combine(tempRoot, filename);
            var local447 = Path.Combine(tempRoot, "447_" + filename);
            var local800 = Path.Combine(tempRoot, "800_" + filename);

            try
            {
                using (var fs = new FileStream(localOriginal, FileMode.Create))
                {
                    await photo.CopyToAsync(fs);
                }

                System.IO.File.Copy(localOriginal, local447, true);
                System.IO.File.Copy(localOriginal, local800, true);

                // 4) Bewerken (met ImageSharp helpers)
                ScaleAndCropImage(local447, 447, 447);
                ScaleAndCropImage(local800, 800, 800);
                ScaleImage(localOriginal, 1280, 960);

                // 5) Zorg dat remote directories bestaan
                var remoteRoot = $"{baseFtp}";
                var remote447 = $"{baseFtp}/447";
                var remote800 = $"{baseFtp}/800";

                await EnsureDirectoryAsync(ftp, remoteRoot);
                await EnsureDirectoryAsync(ftp, remote447);
                await EnsureDirectoryAsync(ftp, remote800);

                // 6) Upload bestanden
                var ok1 = await UploadAsync(ftp, localOriginal, $"{remoteRoot}/{filename}");
                var ok2 = await UploadAsync(ftp, local447, $"{remote447}/{filename}");
                var ok3 = await UploadAsync(ftp, local800, $"{remote800}/{filename}");

                if (!(ok1 && ok2 && ok3))
                {
                    await DeleteFileAsync(ftp, $"{remoteRoot}/{filename}");
                    await DeleteFileAsync(ftp, $"{remote447}/{filename}");
                    await DeleteFileAsync(ftp, $"{remote800}/{filename}");

                    ModelState.AddModelError("ImageUpload", "Upload naar server is mislukt.");
                    return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });
                }

                // 7) DB opslaan
                var picture = new ProjectPictureBO
                {
                    Name = filename,
                    Caption = UploadedBO.Caption,
                    ProjectId = UploadedBO.ProjectId,
                    Type = UploadedBO.Type,
                    DateTimeUploaded = DateTime.Now
                };

                var service = ServiceFactory.GetProjectService();
                var response = service.InsertUpdatePicture(picture);

                if (picture.Type == PictureType.Hoofdfoto && response?.Messages != null)
                {
                    foreach (var msg in response.Messages)
                    {
                        if (msg.Type == MessageType.Value && int.TryParse(msg.Message, out var pictureId))
                        {
                            _ = service.SetDefaultProjectPicture(UploadedBO.ProjectId, pictureId);
                        }
                    }
                }
            }
            finally
            {
                TryDelete(localOriginal);
                TryDelete(local447);
                TryDelete(local800);
            }

            return RedirectToAction("DetailPhotos", "Projecten", new { projectid = UploadedBO.ProjectId });

            // --- helpers ---
            void TryDelete(string p)
            {
                try { if (System.IO.File.Exists(p)) System.IO.File.Delete(p); } catch { /* ignore/log */ }
            }
        }

        public void ScaleAndCropImage(string imagePath, int maxWidth, int maxHeight, int quality = 65)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(imagePath))
            {
                double ratioX = (double)maxWidth / image.Width;
                double ratioY = (double)maxHeight / image.Height;
                double ratio = Math.Max(ratioX, ratioY);

                int newWidth = (int)(image.Width * ratio);
                int newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));

                var cropX = (newWidth - maxWidth) / 2;
                var cropY = (newHeight - maxHeight) / 2;

                image.Mutate(x => x.Crop(new Rectangle(cropX, cropY, maxWidth, maxHeight)));

                var encoder = new JpegEncoder { Quality = quality };
                image.Save(imagePath, encoder); // <-- schrijf terug naar hetzelfde pad
            }
        }
        public void ScaleImage(string imagePath, int maxWidth, int maxHeight, int quality = 65)
        {
            using (var image = SixLabors.ImageSharp.Image.Load(imagePath))
            {
                double ratioX = (double)maxWidth / image.Width;
                double ratioY = (double)maxHeight / image.Height;
                double ratio = Math.Max(ratioX, ratioY);

                int newWidth = (int)(image.Width * ratio);
                int newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));

                var encoder = new JpegEncoder { Quality = quality };
                image.Save(imagePath, encoder); // <-- in-place
            }
        }
        private static bool IsValidImage(IFormFile file) => _validImageTypes.Contains(file.ContentType);


        private static void EnsureDir(string dir)
        {
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        // ==== FTP CONNECT ====
        public async Task<AsyncFtpClient?> ConnectToFtpAsync()
        {
            var host = Configuration["FTP:Host"];
            var user = Configuration["FTP:User"];
            var pass = Configuration["FTP:Password"];
            var port = int.TryParse(Configuration["FTP:Port"], out var p) ? p : 21;
            var useFtps = bool.TryParse(Configuration["FTP:UseFtps"], out var ftps) && ftps;
            var validateCert = bool.TryParse(Configuration["FTP:ValidateCert"], out var vc) && vc;

            var client = new AsyncFtpClient(host, new NetworkCredential(user, pass), port);

            // Config
            client.Config.DataConnectionType = FtpDataConnectionType.PASV; // passive
            if (useFtps)
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;  // AUTH TLS
                client.Config.DataConnectionEncryption = true;

                client.ValidateCertificate += (control, e) =>
                {
                    e.Accept = !validateCert || e.PolicyErrors == SslPolicyErrors.None;
                };
            }
            else
            {
                client.Config.EncryptionMode = FtpEncryptionMode.None;
                client.Config.DataConnectionEncryption = false;
            }

            try
            {
                await client.Connect();  // AsyncFtpClient: async connect heet 'Connect()'
                return client;
            }
            catch (Exception ex)
            {
                // jouw logging hier
                Console.WriteLine("FTP connect failed: " + ex.Message);
                await client.DisposeAsync();
                return null;
            }
        }

        // ==== LOCAL DIR CHECK ====
        public void CheckDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        // ==== REMOTE DIR CHECK/CREATE ====
        public async Task CheckDirFtpAsync(string remoteDir, AsyncFtpClient ftp)
        {
            // Maakt recursief aan; geen chdir nodig bij FluentFTP
            await ftp.CreateDirectory(remoteDir, true);
        }

        // ==== LOKALE FOTO'S VERWIJDEREN (ongewijzigd, puur filesystem) ====
        public void DeletePictureFile(int id)
        {
            var service = ServiceFactory.GetProjectService();
            var resp = service.GetPictureById(id);
            var pic = resp.Success ? resp.Value : null;
            if (pic == null) return;

            var baseDir = Configuration["URL:ImageLocalURL"];

            string[] paths = pic.Type switch
            {
                PictureType.Hoofdfoto or PictureType.Nevenfoto or PictureType.Werffoto => new[]
                {
            Path.Combine(baseDir, pic.Name),
            Path.Combine(baseDir, "447", pic.Name),
            Path.Combine(baseDir, "800", pic.Name)
        },
                PictureType.Nieuws => new[]
                {
            Path.Combine(baseDir, "news", pic.Name),
            Path.Combine(baseDir, "news", "original", pic.Name)
        },
                _ => Array.Empty<string>()
            };

            foreach (var p in paths)
            {
                if (System.IO.File.Exists(p))
                {
                    try { System.IO.File.Delete(p); }
                    catch (IOException ex) { Console.WriteLine($"Bestand kon niet verwijderd worden: {ex.Message}"); }
                }
            }
        }

        // ==== REMOTE FILE DELETE ====
        public async Task<bool> DeleteFtpFileAsync(string remoteDir, string filename, AsyncFtpClient ftp)
        {
            try
            {
                var remotePath = $"{remoteDir.TrimEnd('/')}/{filename}";
                if (await ftp.FileExists(remotePath))
                {
                    await ftp.DeleteFile(remotePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FTP delete failed: {ex.Message}");
                return false;
            }
        }


        //FTP HELPERS
        private AsyncFtpClient CreateClient()
        {
            var host = Configuration["FTP:Host"];
            var user = Configuration["FTP:User"];
            var pass = Configuration["FTP:Password"];
            var port = int.TryParse(Configuration["FTP:Port"], out var p) ? p : 21;
            var useFtps = bool.TryParse(Configuration["FTP:UseFtps"], out var ftps) && ftps;
            var validateCert = bool.TryParse(Configuration["FTP:ValidateCert"], out var vc) && vc;

            var client = new AsyncFtpClient(host, new NetworkCredential(user, pass), port);

            if (useFtps)
            {
                client.Config.EncryptionMode = FtpEncryptionMode.Explicit;   // FTPS (AUTH TLS)
                client.Config.DataConnectionEncryption = true;
                client.ValidateCertificate += (control, e) =>
                {
                    e.Accept = !validateCert || e.PolicyErrors == SslPolicyErrors.None;
                };
            }
            else
            {
                client.Config.EncryptionMode = FtpEncryptionMode.None;
                client.Config.DataConnectionEncryption = false;
            }

            client.Config.DataConnectionType = FtpDataConnectionType.PASV;  // passive

            return client;
        }

        public async Task<AsyncFtpClient> ConnectAsync()
        {
            var client = CreateClient();
            await client.Connect();   // AsyncFtpClient: async method heet Connect()
            return client;
        }

        public async Task EnsureDirectoryAsync(AsyncFtpClient client, string remoteDir)
            => await client.CreateDirectory(remoteDir, true);   // async

        public async Task<bool> UploadAsync(AsyncFtpClient client, string localPath, string remoteFullPath)
        {
            var status = await client.UploadFile(localPath, remoteFullPath,
                FtpRemoteExists.Overwrite, true);
            return status == FtpStatus.Success;
        }

        public async Task<bool> DeleteFileAsync(AsyncFtpClient client, string remoteFullPath)
        {
            try
            {
                if (await client.FileExists(remoteFullPath))
                    await client.DeleteFile(remoteFullPath);
                return true;
            }
            catch { return false; }
        }

        //SALES HELPERS
        private static decimal Percent(decimal amount, decimal pct)
=> Math.Round(amount * (pct / 100m), 2, MidpointRounding.AwayFromZero);

        private static decimal SafePos(decimal? v) => v.HasValue && v.Value > 0 ? v.Value : 0m;

        private static decimal SafeLand(UnitBO u) => u.LandValue ?? 0m;

        private static decimal SafeConstruction(UnitBO u) =>
            (u.ConstructionValues?.Sum(x => x.Value ?? 0m)) ?? 0m;

        private static decimal CalculateNotaryFeesFromTotals(decimal totalNetLand, decimal totalNetBuild, bool mixedVatRegistration)
        {
            decimal baseValue = mixedVatRegistration
                ? (totalNetLand + totalNetBuild)
                : (totalNetLand + (totalNetBuild / 2m));

            if (baseValue <= 0m) return 0m;

            var parts = new (decimal amount, decimal pct)[]
            {
        (  7500m, 4.56m),
        ( 10000m, 2.85m),
        ( 12500m, 2.28m),
        ( 15495m, 1.71m),
        ( 18600m, 1.14m),
        (186000m, 0.57m),
            };
            const decimal pctRest = 0.057m;

            decimal remaining = baseValue;
            decimal fee = 0m;

            foreach (var (amount, pct) in parts)
            {
                if (remaining <= 0m) break;
                var take = Math.Min(remaining, amount);
                fee += take * (pct / 100m);
                remaining -= take;
            }

            if (remaining > 0m) fee += remaining * (pctRest / 100m);

            return Math.Round(fee, 2, MidpointRounding.AwayFromZero);
        }
        private static bool ShouldDefaultInclude(UnitBO u)
        {
            // Zorg dat UnitBO.Type.GroupId gevuld is in je translator
            var gid = u?.Type?.GroupId ?? 0;
            return gid == 1 || gid == 4; // woning/appartement of commerciële ruimte
        }

    }
}
// Payloads voor herberekenen
public class RecalculateRequest
{
    // overrides voor instellingen
    public decimal? VatPercent { get; set; }
    public decimal? RegistrationPercent { get; set; }

    public decimal? FixedCertificateCost { get; set; }
    public decimal? SurveyorCost { get; set; }
    public decimal? ConnectionFees { get; set; }
    public decimal? BaseCertificateCost { get; set; }
    public decimal? ParcelCost { get; set; }
    public decimal? MortageRegistrationCost { get; set; }

    public List<UnitDiscountInput> Units { get; set; }
}

public class UnitDiscountInput
{
    public int UnitId { get; set; }
    public decimal? LandDiscount { get; set; }
    public decimal? BuildDiscount { get; set; }
    public bool? IncludePerUnitCosts { get; set; }
}
internal static class PathExtensions
{
    public static string Replace(this string text, char[] invalidChars, char replacement)
    {
        if (string.IsNullOrEmpty(text)) return text;
        foreach (var ch in invalidChars)
            text = text.Replace(ch, replacement);
        return text;
    }
}