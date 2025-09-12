using BOCore;
using CPMCore.Models;
using CPMCore.Models.Klanten;
using CPMCore.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using static System.Net.Mime.MediaTypeNames;
using NuGet.Configuration;
using ServiceCore;
using DinkToPdf;
using Humanizer;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using CPMCore.Models.Projecten;
using SmartBreadcrumbs.Attributes;

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
        [Breadcrumb("Klanten", FromController = typeof(ProjectenController), FromAction = nameof(ProjectenController.Detail))]
        public ActionResult Detail(int clientId, int projectId = 0)
        {
            var referrer = Request.Headers["Referer"].ToString();
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
            model.ExecutionDays = model.Client.ExecutionDays.HasValue && model.Client.ExecutionDays.Value != 0
                ? model.Client.ExecutionDays.Value
                : projectService.GetProjectExecutionDays(model.ProjectId);

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
            var changeOrderResponse = projectService.GetClientChangeOrders(4, clientId);
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
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            TempData["Referrer"] = referrer;
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
        public ActionResult AddClientAccount(AddClientAccountModel model, List<ClientContactBO> contacts, List<ClientContactBO> coowners, List<UnitBO> units)
        {
            var Referrer = TempData["Referrer"];
            var errors = new Dictionary<string, ModelErrorCollection>();

            // Verzamel modelstate fouten
            foreach (var key in ModelState.Keys)
            {
                if (ModelState[key].Errors.Count > 0)
                {
                    errors[key] = ModelState[key].Errors;
                }
            }

            // Controleer of er minstens één eenheid gekozen werd
            if (units == null || !units.Any())
            {
                ModelState.AddModelError("CustomError", "U dient minstens één eenheid te kiezen voor deze klant");
            }

            if (!ModelState.IsValid)
            {
                FillInAddSelectLists(ref model);
                return View(model);
            }

            // Postcodes en contacten koppelen
            model.ClientAccount.Postalcode.PostcodeId = model.SelectedPostalcode;
            model.ClientAccount.InvoicePostalcode.PostcodeId = model.SelectedInvoicePostalcode;
            model.ClientAccount.CoOwners = coowners;
            model.ClientAccount.Contacts = contacts;

            var clientService = ServiceFactory.GetClientService();
            var unitService = ServiceFactory.GetUnitService();

            // 1. Voeg klantenaccount toe
            var response = clientService.InsertUpdate(model.ClientAccount);

            if (!response.Success || response.Messages == null || !response.Messages.Any())
            {
                AddMessage("error", $"De klantenaccount {model.ClientAccount.Name} is NIET toegevoegd", "Fout!");
                return View(model);
            }

            model.ClientAccount.Id = response.InsertedId;

            var failedUnits = new List<string>();
            var failedConstructionValues = new List<string>();
            bool everythingSucceeded = true;

            // 2. Voeg units toe
            foreach (var item in units)
            {
                var unitResponse = unitService.GetUnitById(item.Id);
                if (!unitResponse.Success)
                {
                    everythingSucceeded = false;
                    failedUnits.Add(item.Name);
                    continue;
                }

                var bo = unitResponse.Value;
                bo.ClientAccountId = model.ClientAccount.Id;
                bo.ConstructionValueSold = item.ConstructionValueSold;
                bo.LandValueSold = item.LandValueSold;

                var response2 = unitService.InsertUpdateUnit(bo);
                if (!response2.Success)
                {
                    everythingSucceeded = false;
                    failedUnits.Add(item.Name);
                    continue;
                }

                // 3. Update bouwwaardes
                foreach (var coitem in item.ConstructionValues)
                {
                    var coResponse = unitService.GetConstructionValue(coitem.Id);
                    if (!coResponse.Success)
                    {
                        everythingSucceeded = false;
                        failedConstructionValues.Add($"{item.Name} - {coitem.Id}");
                        continue;
                    }

                    var covalue = coResponse.Value;
                    covalue.ValueSold = coitem.ValueSold;

                    var response3 = unitService.InsertUpdateConstructionValue(covalue);
                    if (!response3.Success)
                    {
                        everythingSucceeded = false;
                        failedConstructionValues.Add($"{item.Name} - {coitem.Id}");
                    }
                }
            }

            // 4. Indien fouten → verwijder klant
            if (!everythingSucceeded)
            {
                var deleteResponse = clientService.Delete(new List<int> { model.ClientAccount.Id }); // hier in een lijst

                AddMessage("error", $"De klantenaccount {model.ClientAccount.Name} is NIET toegevoegd omwille van fouten", "Fout!");

                if (failedUnits.Any())
                {
                    AddMessage("error", $"Volgende eenheden konden niet toegevoegd worden: {string.Join(", ", failedUnits)}", "Fout!");
                }

                if (failedConstructionValues.Any())
                {
                    AddMessage("error", $"Volgende bouwwaardes konden niet geüpdatet worden: {string.Join(", ", failedConstructionValues)}", "Fout!");
                }

                FillInAddSelectLists(ref model);
                return View(model);
            }

            // Alles is gelukt
            AddMessage("success", $"De klantenaccount {model.ClientAccount.Name} en bijhorende eenheden zijn succesvol toegevoegd", "Geslaagd!");

            return Referrer != null
                ? Redirect(Referrer.ToString())
                : RedirectToAction("DetailClients", "Projecten", new { projectid = model.ProjectId });
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
                if (InvoiceZipcode is not null)
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
        public PartialViewResult BlankCoOwnerRow()
        {
            var countryService = ServiceFactory.GetCountryService();
            var countryResponse = countryService.GetVisibleCountriesForSelect();
            // Dummy country (bv. België)
            var country = new CountryBO { CountryId = 19 };

            var client = new ClientContactBO
            {
                IsCoOwner = true,
                Postalcode = new PostalCodeBO { Country = country },
                InvoicePostalcode = new PostalCodeBO { Country = country },
                CoOwnerType = new ClientOwnerTypeBO()
            };


            // ❗️ Haal de lijsten uit je service of statisch (pas dit aan naar je situatie)
            var countries = countryResponse.Values; ; // bijv. List<CountryBO>
            var ownerTypeService = ServiceFactory.GetClientService();
            var ownerTypeResponse = ownerTypeService.GetOwnerTypesForSelect();
            var ownerTypes = ownerTypeResponse.Values;


            var viewData = new ViewDataDictionary<ClientContactBO>(ViewData, client)
                {
                    { "Countries", countries.Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Display }).ToList() },
                    { "OwnerTypes", ownerTypes.Select(o => new SelectListItem { Value = o.ID.ToString(), Text = o.Display }).ToList() }
                };

            return new PartialViewResult
            {
                ViewName = "_CoOwnerRow",
                ViewData = viewData
            };
        }
        public PartialViewResult BlankGiftRow()
        {
            var Service = ServiceFactory.GetActivityService();
            var gift = new ClientGiftBO();
            var actResponse = Service.GetActivitiesForSelect();
            var activities = actResponse.Values;

            var viewData = new ViewDataDictionary<ClientGiftBO>(ViewData, gift)
                {
                    { "Listactivities", activities}
                };

            return new PartialViewResult
            {
                ViewName = "_GiftRow",
                ViewData = viewData
            };
        }
        public PartialViewResult BlankPoaRow()
        {
            var Service = ServiceFactory.GetActivityService();
            var poa = new ClientPoaBO();
            var actResponse = Service.GetActivitiesForSelect();
            var activities = actResponse.Values;

            var viewData = new ViewDataDictionary<ClientPoaBO>(ViewData, poa)
                {
                    { "Listactivities", activities}
                };

            return new PartialViewResult
            {
                ViewName = "_PoaRow",
                ViewData = viewData
            };
        }
        [HttpPost]
        public PartialViewResult AddSelectedUnits(int unitId, string unitName, string unitGroup)
        {
            var unitService = ServiceFactory.GetUnitService();
            var response = unitService.GetUnitById(unitId);

            if (!response.Success)
            {
                // Optioneel: behandel fout of geef lege partial terug
                return PartialView("_UnitRow", new UnitBO());
            }

            var unit = response.Value;

            unit.LandValueSold = unit.LandValue;

            foreach (var item in unit.ConstructionValues)
            {
                item.ValueSold = item.Value;
            }

            ViewData["mode"] = "add";
            return PartialView("_UnitRow", unit);
        }

        //KLANT BEWERKEN
        [HttpGet]
        public ActionResult Edit(int projectid, int clientid, int activetab)
        {
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            TempData["Referrer"] = referrer;
            var model = new EditClientModel();

            if (clientid != 0)
            {
                var clientService = ServiceFactory.GetClientService();
                var unitService = ServiceFactory.GetUnitService();
                var actService = ServiceFactory.GetActivityService();

                var clientResponse = clientService.GetClientAccountById(clientid);
                if (clientResponse.Success && clientResponse.Values.Any())
                {
                    var client = clientResponse.Values.First();
                    model.Client = client;

                    if (client.CompanyName is null || client.VATnumber is null)
                    {
                        model.IsCompany = false;
                    }
                    else
                    {
                        model.IsCompany = true;
                    }

                    model.SelectedPostalcode.CountryId = client.Postalcode.Country.CountryId;
                    model.SelectedPostalcodeId = client.Postalcode.PostcodeId ?? 0;

                    if (client.InvoicePostalcode.PostcodeId != 0)
                    {
                        model.SelectedInvoicePostalcode.CountryId = client.InvoicePostalcode.Country.CountryId;
                        model.SelectedInvoicePostalcode.PostalCodeId = client.InvoicePostalcode.PostcodeId ?? 0;
                    }

                    ViewData["PostcodeDisplayName"] = $"{client.Postalcode.Postcode} - {client.Postalcode.Gemeente}";
                    ViewData["activetab"] = activetab;

                    string title = "Klant bewerken";
                    title += client.CompanyName == null
                        ? $" - {client.Salutation.GetDisplayName()} {client.DisplayName}"
                        : $" - {client.DisplayName}";
                    ViewData["Title"] = title;

                    // Eenheden
                    var unitsResponse = unitService.GetUnitsByAccountId(clientid);
                    model.Units = unitsResponse.Values
                        .OrderBy(u => u.Type.GroupId)
                        .ThenBy(u => u.Type.Id)
                        .ToList();

                    // Geschenken
                    var giftsResponse = clientService.GetClientGiftByAccountId(clientid);
                    model.Gifts = giftsResponse.Values;
                    var actResponse = actService.GetActivitiesForSelect();
                    if (actResponse.Success)
                    {
                        model.ListActivities = actResponse.Values;
                    }
                    foreach (var gift in model.Gifts)
                    {
                        gift.SelectedActivityIds = gift.Activities?.Select(a => a.ID).ToList() ?? new List<int>();
                    }

                    // Aandachtspunten
                    var poasResponse = clientService.GetClientPoaByAccountId(clientid);
                    model.Poas = poasResponse.Values;
                    foreach (var poa in model.Poas)
                    {
                        poa.SelectedActivityIds = poa.Activities?.Select(a => a.ID).ToList() ?? new List<int>();
                    }
                }
            }

            model.ProjectId = projectid;
            FillInAddSelectListsEdit(ref model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditClientModel viewmodel)
        {
            var Referrer = TempData["Referrer"];
            if (!ModelState.IsValid || viewmodel.Client.Id == 0)
            {
                FillInAddSelectListsEdit(ref viewmodel);
                return View(viewmodel);
            }

            // 1) Één UoW voor alles in deze actie
            using var uow = ServiceFactory.CreateUoW();

            // 2) Services die DEZELFDE uow delen
            var clientService = ServiceFactory.CreateClientService(uow);
            var unitService = ServiceFactory.CreateUnitService(uow);
            var actService = ServiceFactory.CreateActivityService(uow);
            var contactService = ServiceFactory.CreateContactService(uow); // voorbeeld

            await using var tx = await uow.BeginTransactionAsync();

            try
            {
                // --- voorbereiden ---
                viewmodel.Client.Postalcode.PostcodeId = viewmodel.SelectedPostalcodeId;
                viewmodel.Client.InvoicePostalcode.PostcodeId = viewmodel.SelectedInvoicePostalcodeId;

                if (viewmodel.IsCompany)
                {
                    viewmodel.Client.Name = null;
                    viewmodel.Client.Salutation = 0;
                }
                else
                {
                    viewmodel.Client.CompanyName = null;
                    viewmodel.Client.VATnumber = null;
                }

                // 3) Eerste bewerking
                // Clientaccount updaten
                var r1 = clientService.InsertUpdate(viewmodel.Client);
                if (!r1.Success)
                {
                    await tx.RollbackAsync();
                    AddMessage("error", $"Klant {viewmodel.Client.DisplayName} is niet bijgewerkt", "Fout!");
                    FillInAddSelectListsEdit(ref viewmodel);
                    return View(viewmodel);
                }

                //Eenheden updaten
                foreach (var unit in viewmodel.Units)
                {
                    var r2 = unitService.UpdateLandValueSold(unit);
                    if (!r2.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Unit {unit.Name} is niet bijgewerkt", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewmodel);
                    }
                    foreach (var constructionvalue in unit.ConstructionValues)
                    {
                        var r3 = unitService.UpdateConstructionValueSold(constructionvalue);
                        if (!r3.Success)
                        {
                            await tx.RollbackAsync();
                            AddMessage("error", $"Unit {unit.Name} is niet bijgewerkt", "Fout!");
                            FillInAddSelectListsEdit(ref viewmodel);
                            return View(viewmodel);
                        }
                    }
                }
                //GIFTS
                var postedIds = (viewmodel.Gifts ?? Enumerable.Empty<ClientGiftBO>())
                    .Where(g => g.Id > 0)
                    .Select(g => g.Id)
                    .ToHashSet();

                // 2) Huidige gift-IDs in de database voor deze account
                var existingIds = uow.Context.Set<ClientGift>()
                    .Where(g => g.ClientAccountId == viewmodel.Client.Id)   // let op: klopt de FK? (soms AccountId)
                    .Select(g => g.Id)
                    .ToList();

                // 3) IDs die we moeten verwijderen
                var removeIds = existingIds.Where(id => !postedIds.Contains(id)).ToList();

                if (removeIds.Count > 0)
                {
                    var r6 = clientService.DeleteClientGift(removeIds);
                    if (!r6.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Gifts zijn niet verwijderd", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewmodel);
                    }
                }
                foreach (var gift in viewmodel.Gifts)
                {
                    gift.AccountId = viewmodel.Client.Id;
                    foreach (var i in gift.SelectedActivityIds)
                    {
                        gift.Activities.Add(actService.GetActivitybyId(i).Value);
                    }
                    var r4 = clientService.InsertUpdateClientGift(gift);
                    if (!r4.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Gift {gift.Description} is niet bijgewerkt", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewmodel);
                    }
                }

                //AANDACHTSPUNTEN

                var postedPoasIds = (viewmodel.Poas ?? Enumerable.Empty<ClientPoaBO>())
                    .Where(g => g.Id > 0)
                    .Select(g => g.Id)
                    .ToHashSet();

                // 2) Huidige poa-IDs in de database voor deze account
                var existingPoasIds = uow.Context.Set<ClientPoa>()
                    .Where(g => g.ClientAccountId == viewmodel.Client.Id)   // let op: klopt de FK? (soms AccountId)
                    .Select(g => g.Id)
                    .ToList();

                // 3) IDs die we moeten verwijderen
                var removePoasIds = existingPoasIds.Where(id => !postedPoasIds.Contains(id)).ToList();

                if (removePoasIds.Count > 0)
                {
                    var r6 = clientService.DeleteClientPoa(removeIds);
                    if (!r6.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Poa's zijn niet verwijderd", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewmodel);
                    }
                }
                foreach (var poa in viewmodel.Poas)
                {
                    poa.AccountId = viewmodel.Client.Id;
                    foreach (var i in poa.SelectedActivityIds)
                    {
                        poa.Activities.Add(actService.GetActivitybyId(i).Value);
                    }
                    var r5 = clientService.InsertUpdateClientPoa(poa);
                    if (!r5.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Poa {poa.Description} is niet bijgewerkt", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewmodel);
                    }
                }

                // 4) (Optioneel) andere bewerkingen met dezelfde UoW
                // var r2 = contactService.InsertUpdate(viewmodel.Contact);
                // if (!r2.Success) { await tx.RollbackAsync(); ... return View(viewmodel); }

                // 5) Alles OK? Opslaan + commit
                await uow.SaveChangesAsync();
                await tx.CommitAsync();

                AddMessage("success", $"Account {viewmodel.Client.DisplayName} is bijgewerkt", "Geslaagd!");
                return Referrer != null
                    ? Redirect(Referrer.ToString())
                    : RedirectToAction("Edit", new { projectid = viewmodel.ProjectId, clientid = viewmodel.Client.Id, activetab = 0 });
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                AddMessage("error", "Er is een onverwachte fout opgetreden.", "Fout!");
                FillInAddSelectListsEdit(ref viewmodel);
                return View(viewmodel);
            }

        }


        private void FillInAddSelectListsEdit(ref EditClientModel model)
        {
            var countryService = ServiceFactory.GetCountryService();
            var countryResponse = countryService.GetVisibleCountriesForSelect();
            if (countryResponse.Success)
            {
                model.SelectedPostalcode.Countries = countryResponse.Values;
                model.SelectedInvoicePostalcode.Countries = countryResponse.Values;
            }

            var defaultCountry = model.SelectedPostalcode.Countries
                .FirstOrDefault(c => c.Group == "19");
            if (defaultCountry != null)
            {
                model.SelectedPostalcode.CountryId = defaultCountry.ID;
            }

            var ownerTypeService = ServiceFactory.GetClientService();
            var ownerTypeResponse = ownerTypeService.GetOwnerTypesForSelect();
            if (ownerTypeResponse.Success)
            {
                model.OwnerTypes = ownerTypeResponse.Values;
            }
        }


        // KLANT VERWIJDEREN
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

        //WIJZIGNGSOPDRACHTEN
        public ActionResult DetailCO(int projectid, int clientid)
        {
            var referrer = Request.Headers["Referer"].ToString();
            TempData["Referrer"] = referrer;
            var model = new DetailChangeOrderModel();
            var service = ServiceFactory.GetProjectService();
            var cservice = ServiceFactory.GetClientService();
            var response = service.GetClientChangeOrders(0, clientid);

            if (response.Success)
                model.CO = response.Values;

            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);
            model.ClientName = cservice.GetClientAccountNameById(clientid);
            model.ClientAccountId = clientid;

            return View(model);

        }

    }
}
