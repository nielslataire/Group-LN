using Microsoft.AspNetCore.Mvc;
using BOCore;
using DALCore;
using FacadeCore;
using ServiceCore;
using CPMCore.Models;
using CPMCore.Models.Projecten;
using CPMCore.Models.Klanten;
using CPMCore.Service;
using Microsoft.AspNetCore.Identity;
using System.Web;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Collections.Specialized;
using Microsoft.Extensions.Options;
using Rotativa.AspNetCore;
using System.Collections;
using Microsoft.CodeAnalysis;
using CPMCore.Attributes;

namespace CPMCore.Controllers
{
    //tes
    public class ProjectenController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration Configuration;

        public ProjectenController(UserManager<ApplicationUser> userManager, ILogger<HomeController> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _logger = logger;
            Configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [Breadcrumb("Info")]
        public ActionResult Detail(int projectid, bool EditGeneralData = false)
        {
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
            return View(model);
        }
        //KLANTEN
        [HttpGet]
        [Breadcrumb("Klanten")]
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
            return View(model);
        }

        //UNITS
        [HttpGet]
        [Breadcrumb("Eenheden")]
        public ActionResult DetailUnits(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            return View(FillDetailUnitModel(projectid));
        }
        [HttpGet]
        [Breadcrumb("Eenheid toevoegen")]
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
        [Breadcrumb("Eenheid bewerken")]
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
            ViewBag.paymentgroups = model.PaymentGroups;
            if (model.Unit.PaymentGroupId is not null)
                model.SelectedPaymentGroup = model.Unit.PaymentGroupId;
            else
                model.SelectedPaymentGroup = 0;

            model.ProjectId = model.Unit.ProjectId;
            model.ProjectName = service2.GetProjectNameById(model.Unit.ProjectId);

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

        //CONTRACTEN
        [HttpGet]
        [Breadcrumb("Contracten")]
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

            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Nacalculatie")]
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
            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Nacalculatie detail")]
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

            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Contract toevoegen")]
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
            //// Set sitemap names
            //var node = SiteMaps.Current.CurrentNode;
            //if ((node != null & node.ParentNode != null))
            //{
            //    if ((node.ParentNode.ParentNode != null && node.ParentNode.ParentNode.ParentNode != null))
            //        node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService().GetProjectNameById(model.ProjectId);
            //}
            model.Insurance.Startdate = DateOnly.FromDateTime(DateTime.Now);
            var iservice = ServiceFactory.GetInsuranceService();
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;
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
        [Breadcrumb("Contract bewerken")]
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
            //// Set sitemap names
            //var node = SiteMaps.Current.CurrentNode;
            //if ((node != null & node.ParentNode != null))
            //{
            //    if ((node.ParentNode.ParentNode != null && node.ParentNode.ParentNode.ParentNode != null))
            //        node.ParentNode.ParentNode.Title = ServiceFactory.GetProjectService().GetProjectNameById(model.ProjectId);
            //}
            model.Insurance.Startdate = DateOnly.FromDateTime(DateTime.Now);
            var iservice = ServiceFactory.GetInsuranceService();
            var response = iservice.GetInsuranceCompaniesForSelect();
            if (response.Success)
                model.InsuranceCompanies = response.Values;
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
                foreach (var type in responselevels.Values) {
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

        //INCOMMING INVOICES
        [HttpGet]
        [Breadcrumb("Inkomende factuur toevoegen")]
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

            //// Set sitemap names
            //var node = SiteMaps.Current.CurrentNode;
            //if (node != null && node.ParentNode != null)
            //{
            //    if (node.ParentNode.ParentNode != null &&
            //        node.ParentNode.ParentNode.ParentNode != null &&
            //        node.ParentNode.ParentNode.ParentNode.ParentNode != null)
            //    {
            //        node.ParentNode.ParentNode.ParentNode.Title =
            //            ServiceFactory.GetProjectService().GetProjectNameById(model.ProjectId);
            //    }
            //}

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

            if(model.IncommingInvoice.ContractID is null && model.IncommingInvoice.CompanyId is null)
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
        [Breadcrumb("Inkomende factuur bewerken")]
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
        [Breadcrumb("Inkomende factuur")]
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
            if( model.IncommingInvoice.ContractID is not null)
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


            return View(model);
        }

        //SHARED
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
        public void CheckDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }


    }
}
