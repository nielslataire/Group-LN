using BOCore;
using CPMCore.Models;
using CPMCore.Models.Klanten;
using CPMCore.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using CPMCore.Attributes;

namespace CPMCore.Controllers
{
    public class KlantenController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration Configuration;

        public KlantenController(UserManager<ApplicationUser> userManager, ILogger<HomeController> logger, IConfiguration configuration)
        {
            _userManager = userManager;
            _logger = logger;
            Configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }
        // KLANTEN
        public ActionResult Detail(int clientId, int projectId = 0)
        {
            var model = new ClientModel();
            var clientService = ServiceFactory.GetClientService();
            var unitService = ServiceFactory.GetUnitService();
            var projectService = ServiceFactory.GetProjectService();

            // 1. Get Client
            var clientResponse = clientService.GetClientAccountById(clientId);
            if (clientResponse.Success)
            {
                model.Client = clientResponse.Values.FirstOrDefault();
            }

            // 2. Get Units for Client
            model.UnitsGrouped = unitService.GetGroupedUnitsByAccountId(clientId)?.Values;

            // 3. Get Units with Payment Stages
            model.UnitsWithStages = unitService.GetClientUnitsWithStages(clientId)?.Values;

            // 4. Get Invoices for those Units
            var unitIds = model.UnitsWithStages?.Select(m => m.Unit.Id).ToList() ?? new List<int>();
            model.Invoices = projectService.GetInvoicesByUnitIds(unitIds)?.Values;

            // 5. Determine Project ID
            model.ProjectId = projectId != 0
                ? projectId
                : model.UnitsGrouped?.FirstOrDefault()?.Units?.FirstOrDefault()?.ProjectId ?? 0;

            // 6. Project Folder Path
            var deliveryDocPath = Configuration["URL:DeliveryDocLocalURL"];
            model.Folder = projectService.GetProjectFolderById(model.ProjectId) + deliveryDocPath;
            var imageUrl = Configuration["URL:ImageWebURL"];
            ViewBag.ImageWebURL = imageUrl;

            // 7. Get Gifts and PoAs
            model.Gifts = clientService.GetClientGiftByAccountId(clientId)?.Values;
            model.Poas = clientService.GetClientPoaByAccountId(clientId)?.Values;

            // 9. Execution Days
            model.ExecutionDays = (int)model.Client.ExecutionDays == 0
                ? projectService.GetProjectExecutionDays(model.ProjectId)
                : (int)model.Client.ExecutionDays;

            // 10. Start Date
            model.StartDate = model.Client.StartDateConstruction != null
                ? model.Client.StartDateConstruction.Value
                : projectService.GetProjectStartDateConstruction(model.ProjectId);

            // 11. Final Construction Date & Working Days Left
            model.WorkingDaysLeft = -9999;
            if (model.ExecutionDays > 0 && model.StartDate != DateOnly.FromDateTime(DateTime.MinValue))
            {
                model.FinalConstructionDate = projectService.GetFinalConstructionDay(model.ProjectId, model.StartDate, model.ExecutionDays);
                if (model.FinalConstructionDate != DateOnly.FromDateTime(DateTime.MinValue))
                {
                    model.WorkingDaysLeft = projectService.GetWorkingDaysLeft(model.FinalConstructionDate, model.ProjectId);
                }
            }

            // 12. Latest Documents
            var latestDocsResponse = projectService.GetLatestClientDocs(4, clientId);
            if (latestDocsResponse.Success)
            {
                model.LatestDocs = latestDocsResponse.Values;
            }

            // 13. Change Orders
            var changeOrderResponse = projectService.GetClientChangeOrders(4,clientId);
            if (changeOrderResponse.Success)
            {
                model.ChangeOrders = changeOrderResponse.Values;
            }

            return View(model);
        }

        // KLANT TOEVOEGEN
        [HttpGet]
        public ActionResult AddClientAccount(int id)
        {
            AddClientAccountModel model = new AddClientAccountModel();
            var service = ServiceFactory.GetProjectService();
            model.ProjectName = service.GetProjectNameById(id);
            model.ProjectId = id;
            model.ClientAccount.OwnerPercentage = 100;
            model.ClientAccount.OwnerType.Id = 1;
            FillInAddSelectLists(ref model);
            return View(model);
        }
        [HttpPost]
        public PartialViewResult AddCoOwner(string Name, string Forename, string Salutation, string Street, string Housenumber, string Busnumber, int Zipcode, string Phone, string Cellphone, string Email, int OwnerType, string OwnerPercentage, string VatNumber, string CompanyName, string InvoiceAddress, string InvoiceStreet, string InvoiceHousenumber, string InvoiceBusnumber, string InvoiceZipcode)
        {
            ClientContactBO nCoOwner = new ClientContactBO();
            // ophalen postcode
            // Dim pservice = ServiceFactory.GetPostalcodeService()
            // Dim presponse = pservice.GetPostalcodeById(Zipcode)
            // If (presponse.Success) Then nCoOwner.Postalcode = presponse.Values.FirstOrDefault
            nCoOwner.Name = Name;
            nCoOwner.Firstname = Forename;
            nCoOwner.Salutation = Enum.Parse<Salutation>(Salutation);
            nCoOwner.Street = Street;
            nCoOwner.Housenumber = Housenumber;
            nCoOwner.Busnumber = Busnumber;
            nCoOwner.Postalcode.PostcodeId = Zipcode;
            nCoOwner.VATnumber = VatNumber;
            nCoOwner.CompanyName = CompanyName;
            if (InvoiceAddress == "True")
            {
                nCoOwner.InvoiceStreet = InvoiceStreet;
                nCoOwner.InvoiceHousenumber = InvoiceHousenumber;
                nCoOwner.InvoiceBusnumber = InvoiceBusnumber;
                if(InvoiceZipcode is not null)
                {
                    nCoOwner.InvoicePostalcode.PostcodeId = int.Parse(InvoiceZipcode);
                }
               
            }  
            if (Phone != null)
                nCoOwner.Phone = Regex.Replace(Phone, "[^0-9]", "");
            if (Cellphone != null)
                nCoOwner.Cellphone = Regex.Replace(Cellphone, "[^0-9]", "");
            nCoOwner.Email = Email;
            var sservice = ServiceFactory.GetClientService();
            var sresponse = sservice.GetClientOwnerTypeById(OwnerType);
            nCoOwner.CoOwnerType = sresponse.Value;
            try
            {
                nCoOwner.CoOwnerPercentage = decimal.Parse(OwnerPercentage);
            }
            catch (Exception ex)
            {
                try
                {
                    OwnerPercentage = OwnerPercentage.Replace(".", ",");
                    nCoOwner.CoOwnerPercentage = decimal.Parse(OwnerPercentage);
                }
                catch (Exception ex2)
                {
                }
            }
            nCoOwner.CoOwnerPercentage = decimal.Parse(OwnerPercentage);
            ViewData["mode"] = "add";
            return PartialView("_CoOwnerRow", nCoOwner);
        }

        private void FillInAddSelectLists(ref AddClientAccountModel model)
        {
            var cservice = ServiceFactory.GetCountryService();
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if ((cresponse.Success))
                model.Countries = cresponse.Values;
            var defCountry = model.Countries.Where(m => m.Group == "19").FirstOrDefault();
            if (model.SelectedCountry == 0)
            {
                if ((defCountry != null))
                    model.SelectedCountry = defCountry.ID;
            }
            var oservice = ServiceFactory.GetClientService();
            var oresponse = oservice.GetOwnerTypesForSelect();
            if ((oresponse.Success))
                model.OwnerTypes = oresponse.Values;
            var uservice = ServiceFactory.GetUnitService();
            var uresponse = uservice.GetAvailableUnitsByProjectId(model.ProjectId);
            if ((uresponse.Success))
                model.AvailableUnits = uresponse.Values;
        }
        public PartialViewResult BlankContactRow()
        {
            return PartialView("_ContactRow", new ClientContactBO());
        }



        public ActionResult PartialDeleteClientModal(int id)
        {
            var viewModel = new IdNameBO();
            if (id != 0)
            {
                var dservice = ServiceFactory.GetClientService();
                viewModel.Display = dservice.GetClientAccountNameById(id);
                viewModel.ID = id;
            }
            return PartialView("_DeleteClientModal", viewModel);
        }
        [HttpGet]
        public ActionResult DeleteClient(int id)
        {
            string stri = Request.Headers["Referer"].ToString();
            List<int> Idlist = new List<int>();
            Idlist.Add(id);
            if (id != 0)
            {
                var uservice = ServiceFactory.GetUnitService();
                var response = uservice.DeleteUnitFromClientAccountByAccountId(Idlist);
                var dservice = ServiceFactory.GetClientService();
                if (response.Success == true)
                {
                    response = dservice.Delete(Idlist);
                    if (response.Success == true)
                    {
                        AddMessage("", "De klant is verwijderd", "Geslaagd!");
                        return Redirect(stri);
                    }
                    else
                    {
                        AddMessage("error", "De klant niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                        return Redirect(stri);
                    }
                }
                else
                {
                    AddMessage("error", "De klant niet verwijderd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
                    return Redirect(stri);
                }
            }
            else
                return Redirect(stri);
        }
        public void AddMessage(string messagetype, string message, string messagetitle)
        {
            TempData["Message"] = message;
            TempData["MessageType"] = messagetype;
            TempData["MessageTitle"] = messagetitle;
        }

    }
}
