using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Klanten;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using CPMCore.Services.Security;
using DALCore.Models;
using DinkToPdf;
using FacadeCore;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Configuration;
using ServiceCore;
using ServiceCore.Signing;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

namespace CPMCore.Controllers
{
    /// <summary>Klant op een project (ClientAccount): toevoegen, bewerken, mede-eigenaars/eenheden/schenkingen/volmachten, verwijderen. Opgesplitst uit KlantenController.cs (okt. 2026, structureren) - views in Views/Klanten/ClientAccount/. Zelfde partial class: alle private velden/services van KlantenController.cs blijven gewoon bruikbaar.</summary>
    public partial class KlantenController
    {
        // KLANT TOEVOEGEN
        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public ActionResult AddClientAccount(int id, int? unitId = null)
        {
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            TempData["Referrer"] = referrer;
            AddClientAccountModel model = new AddClientAccountModel();
            var service = _projectService;
            model.ProjectName = service.GetProjectNameById(id);
            model.ProjectId = id;
            model.ClientAccount.OwnerPercentage = 100;
            model.ClientAccount.OwnerType.Id = 1;
            FillInAddSelectLists(ref model);

            if (unitId.HasValue)
            {
                var unitResp = _unitService.GetUnitById(unitId.Value);
                if (unitResp.Success)
                {
                    var unit = unitResp.Value;
                    unit.LandValueSold = unit.LandValue;
                    foreach (var cv in unit.ConstructionValues)
                        cv.ValueSold = cv.Value;
                    model.AddedUnits.Add(unit);
                    // verwijder de eenheid uit de beschikbare lijst
                    model.AvailableUnits.RemoveAll(u => u.ID == unitId.Value);
                }
            }

            var bcIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var bcProjecten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = bcIndex };
            var bcProject = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName) { Parent = bcProjecten, RouteValues = new { projectid = id } };
            var bcKlanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten") { Parent = bcProject, RouteValues = new { projectid = id } };
            var bcAdd = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddClientAccount", "Klanten", "Klant toevoegen") { Parent = bcKlanten };
            ViewData["BreadcrumbNode"] = bcAdd;

            SetPageHeader("bx bx-group", model.ProjectName);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "AddClientAccountV2" : "AddClientAccount", model);
        }
        [HttpPost]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public ActionResult AddClientAccount(AddClientAccountModel model, List<ClientContactBO> contacts, List<UnitBO> units)
        {
            SetPageHeader("bx bx-group", model.ProjectName);
            var viewName = ViewData["UseGlV2Layout"] as bool? == true ? "AddClientAccountV2" : "AddClientAccount";
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

            // ProjectName wordt niet mee gepost (enkel ProjectId) — zonder dit gaf de impliciete
            // required-validatie op die niet-nullable string een kale "The ProjectName field is
            // required." in de samenvatting, en bleef de projectnaam leeg in het inner menu bij redisplay.
            ModelState.Remove(nameof(model.ProjectName));
            if (string.IsNullOrWhiteSpace(model.ProjectName))
                model.ProjectName = _projectService.GetProjectNameById(model.ProjectId);

            // Minimum om een klant aan te maken (zelfde regels als gl-v2-klanten-addclient.js, hier als
            // vangnet): naam of bedrijfsnaam, verkoopdatum, minstens één eenheid mét prijzen. De sleutels
            // zijn veldnamen zodat de gl-v2-view het veld zelf ook rood kan zetten; de teksten staan in
            // de foutensamenvatting bovenaan de pagina.
            var hasNameError = ModelState.Values.SelectMany(v => v.Errors).Any(e => e.ErrorMessage.Contains("naam", StringComparison.OrdinalIgnoreCase));
            if (!hasNameError && string.IsNullOrWhiteSpace(model.ClientAccount.Name) && string.IsNullOrWhiteSpace(model.ClientAccount.CompanyName))
                ModelState.AddModelError("ClientAccount.Name", "Naam (of bedrijfsnaam) is verplicht.");
            if (model.ClientAccount.DateSalesAgreement is null)
                ModelState.AddModelError("ClientAccount.DateSalesAgreement", "Verkoopdatum is verplicht.");

            if (units == null || !units.Any())
            {
                ModelState.AddModelError("Units", "Voeg minstens één eenheid toe.");
            }
            else
            {
                foreach (var unit in units)
                {
                    var unitLabel = string.IsNullOrWhiteSpace(unit.Name) ? "Eenheid" : unit.Name;
                    if ((unit.LandValueSold ?? 0) <= 0)
                        ModelState.AddModelError("Units", $"{unitLabel}: grondwaarde ontbreekt.");
                    foreach (var cv in unit.ConstructionValues ?? new List<UnitConstructionValueBO>())
                    {
                        if ((cv.ValueSold ?? 0) <= 0)
                            ModelState.AddModelError("Units", $"{unitLabel}: {(string.IsNullOrWhiteSpace(cv.Description) ? "constructiewaarde" : cv.Description.ToLowerInvariant())} ontbreekt.");
                    }
                }
            }

            // Verdeelsleutel (migratie 057, design-handoff 23a "100 % KLOPT") — enkel relevant zodra er
            // mede-eigenaars zijn; een solo-eigenaar staat altijd op 100 (standaardwaarde bij Toevoegen).
            var coOwnersForShareCheck = model.ClientAccount.CoOwners?.Any() == true ? model.ClientAccount.CoOwners : contacts?.Where(c => c.IsCoOwner).ToList();
            if (coOwnersForShareCheck?.Any() == true)
            {
                var shareTotal = (model.ClientAccount.OwnerPercentage ?? 0) + coOwnersForShareCheck.Sum(c => c.CoOwnerPercentage ?? 0);
                if (Math.Abs(shareTotal - 100m) > 0.01m)
                    ModelState.AddModelError("CustomError", $"De verdeelsleutel moet 100% zijn (nu {shareTotal:0.##}%).");
            }

            if (!ModelState.IsValid)
            {
                // De gekozen eenheden komen binnen als "units" (BeginCollectionItem), niet als
                // model.AddedUnits — zonder dit waren álle eenheidskaarten weg na een serverfout.
                RestorePostedUnits(model, units);
                FillInAddSelectLists(ref model);
                ViewData["GlV2ErrorLocations"] = AddClientAccountErrorLocations();
                return View(viewName, model);
            }

            // Postcodes en contacten koppelen
            model.ClientAccount.Postalcode.PostcodeId = model.SelectedPostalcode;
            model.ClientAccount.InvoicePostalcode.PostcodeId = model.SelectedInvoicePostalcode;
            //model.ClientAccount.CoOwners = model.ClientAccount.CoOwners?.Any() == true ? model.ClientAccount.CoOwners : coowners;
            model.ClientAccount.Contacts = model.ClientAccount.Contacts?.Any() == true ? model.ClientAccount.Contacts : contacts;

            var clientService = _clientService;
            var unitService = _unitService;

            // 1. Voeg klantenaccount toe
            var response = clientService.InsertUpdate(model.ClientAccount);

            if (!response.Success || response.Messages == null || !response.Messages.Any())
            {
                AddMessage("error", $"De klantenaccount {model.ClientAccount.Name} is NIET toegevoegd", "Fout!");
                return View(viewName, model);
            }

            model.ClientAccount.Id = response.InsertedId;

            // Deze pagina (project-scoped AddClientAccount) heeft geen eigen "Facturatiebedrijven"-
            // keuzeveld zoals de globale Klanten/Create-Edit-flow (ClientFormViewModel.
            // SelectedIssuerCompanyIds, zie AttachIssuerCompany/ValidateIssuerCompanies verderop in
            // dit bestand) — zonder dit bleef ClientAccountIssuerCompany leeg voor elke klant die
            // hier werd toegevoegd, en verscheen die klant nergens bij "Facturatiebedrijven" op
            // Klanten/Detail. In plaats van een keuzeveld koppelt deze flow automatisch aan de twee
            // facturatiebedrijven die al op het PROJECT zelf staan ingesteld (ProjectBO.
            // IssuerCompanyIdLandOwner "Facturatiebedrijf grondeigenaar" / IssuerCompanyIdBuilder
            // "Facturatiebedrijf aannemer") — grond en constructies, als ze bestaan.
            LinkProjectIssuerCompanies(model.ClientAccount.Id, model.ProjectId);

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
                bo.IsOption = false;
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
                return View(viewName, model);
            }

            // Alles is gelukt
            AddMessage("success", $"De klantenaccount {model.ClientAccount.Name} en bijhorende eenheden zijn succesvol toegevoegd", "Geslaagd!");

            return Referrer != null
                ? Redirect(Referrer.ToString())
                : RedirectToAction("DetailClients", "Projecten", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public PartialViewResult AddCoOwner(string Name, string Forename, string Salutation, string Street, string Housenumber, string Busnumber, int Zipcode, string Phone, string Cellphone, string Email, int OwnerType, string OwnerPercentage, string VatNumber, string CompanyName, string InvoiceAddress, string InvoiceStreet, string InvoiceHousenumber, string InvoiceBusnumber, string InvoiceZipcode, bool RequiresDigitalInvoice, bool AttachUblByDefault)
        {
            ClientContactBO nCoOwner = new ClientContactBO
            {
                IsCoOwner = true,
                RequiresDigitalInvoice = RequiresDigitalInvoice,
                AttachUblByDefault = AttachUblByDefault
            };
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
            var hasInvoiceAddress = bool.TryParse(InvoiceAddress, out var invoiceAddressFlag) && invoiceAddressFlag;
            nCoOwner.InvoiceAddress = hasInvoiceAddress;
            if (hasInvoiceAddress)
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
            var sservice = _clientService;
            var sresponse = sservice.GetClientOwnerTypeById(OwnerType);
            nCoOwner.CoOwnerType = sresponse.Value;
            var nlCulture = CultureInfo.GetCultureInfo("nl-BE");
            if (!decimal.TryParse(OwnerPercentage, NumberStyles.Number, nlCulture, out var parsedPercentage))
            {
                decimal.TryParse(OwnerPercentage, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedPercentage);
            }

            nCoOwner.CoOwnerPercentage = parsedPercentage;
            var countryService = _countryService;
            var countryResponse = countryService.GetVisibleCountriesForSelect();
            var ownerTypeService = _clientService;
            var ownerTypeResponse = ownerTypeService.GetOwnerTypesForSelect();

            ViewData["Countries"] = countryResponse.Success
                ? countryResponse.Values.Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Display }).ToList()
                : new List<SelectListItem>();
            ViewData["OwnerTypes"] = ownerTypeResponse.Success
                ? ownerTypeResponse.Values.Where(o => o.ID != 1).Select(o => new SelectListItem { Value = o.ID.ToString(), Text = o.Display }).ToList()
                : new List<SelectListItem>();
            ViewData["CoOwnerCollectionName"] = "ClientAccount.CoOwners";
            ViewData["mode"] = "add";
            return PartialView("_CoOwnerRow", nCoOwner);
        }

        private void FillInAddSelectLists(ref AddClientAccountModel model)
        {
            var cservice = _countryService;
            var cresponse = cservice.GetVisibleCountriesForSelect();
            if ((cresponse.Success))
                model.Countries = cresponse.Values;
            var defCountry = model.Countries.Where(m => m.ID == 19).FirstOrDefault();
            if (defCountry != null)
            {
                if (model.SelectedCountry == 0)
                    model.SelectedCountry = defCountry.ID;
                if (model.SelectedInvoiceCountry == 0)
                    model.SelectedInvoiceCountry = defCountry.ID;
                if (model.SelectedCoOwnerCountry == 0)
                    model.SelectedCoOwnerCountry = defCountry.ID;
                if (model.SelectedCoOwnerInvoiceCountry == 0)
                    model.SelectedCoOwnerInvoiceCountry = defCountry.ID;
            }
            var oservice = _clientService;
            var oresponse = oservice.GetOwnerTypesForSelect();
            if ((oresponse.Success))
                model.OwnerTypes = oresponse.Values;
            var uservice = _unitService;
            var uresponse = uservice.GetAvailableUnitsByProjectId(model.ProjectId);
            if ((uresponse.Success))
                model.AvailableUnits = uresponse.Values;

            // gl-v2 (AddClientAccountV2, 23a/23b): volledige projectlijst voor de kiezer + de namen van de
            // betalingsgroepen voor de eenheidskaart (_UnitRowV2, "BETALINGSGROEP").
            model.UnitChoices = BuildUnitChoices(model.ProjectId);
            ViewData["PaymentGroupNames"] = PaymentGroupNamesFor(model.ProjectId);
        }

        /// <summary>Design-handoff 23b: álle (niet-gelinkte) eenheden van het project — beschikbare
        /// kiesbaar, verkochte/in optie zichtbaar maar niet kiesbaar mét de koper/optiehouder als reden.
        /// Zelfde beschikbaarheidsregel als IUnitService.GetAvailableUnitsByProjectId (geen klant, geen
        /// gelinkte eenheid); de naamopbouw volgt Units.GetIdName (type-naam vóór de naam voor type 11).</summary>
        private List<UnitChoiceVm> BuildUnitChoices(int projectId)
        {
            var paymentGroups = PaymentGroupNamesFor(projectId);
            var be = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");

            var units = _db.Units.AsNoTracking()
                // Exact dezelfde beschikbaarheidsregel als IUnitService.GetAvailableUnitsByProjectId
                // (project + geen gelinkte eenheid) — een extra !IsLink-filter liet hier álle eenheden
                // wegvallen.
                .Where(u => u.ProjectId == projectId && u.LinkedUnitId == null)
                .Include(u => u.Type).ThenInclude(t => t.Group)
                .Include(u => u.ClientAccount)
                .OrderBy(u => u.Type != null ? u.Type.GroupId : 0).ThenBy(u => u.Name)
                .ToList();

            return units.Select(u =>
            {
                var name = u.Type != null && u.Type.Id == 11 ? $"{u.Type.Name} {u.Name}" : u.Name;
                var subParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(u.Type?.Name)) subParts.Add(u.Type.Name.ToLowerInvariant());
                if (u.Surface is decimal surface && surface > 0) subParts.Add(surface.ToString("N0", be) + " m²");
                if (u.PaymentGroupId is int pgId && paymentGroups.TryGetValue(pgId, out var pgName)) subParts.Add(pgName);
                var sold = u.ClientAccountId != null;
                return new UnitChoiceVm
                {
                    Id = u.Id,
                    Name = name,
                    Sub = string.Join(" · ", subParts),
                    Price = (u.LandValue ?? 0) + (u.ConstructionValue ?? 0),
                    Available = !sold,
                    StatusLabel = !sold ? "BESCHIKBAAR" : (u.IsOption ? "IN OPTIE" : "VERKOCHT"),
                    Reason = sold && u.ClientAccount != null ? DisplayNameOf(u.ClientAccount) : null,
                    Group = u.Type?.Group?.Name
                };
            }).ToList();
        }

        /// <summary>Redisplay na een serverfout op AddClientAccount: de gekozen eenheden komen binnen als
        /// "units" (enkel Id + de ingevulde prijzen, via BeginCollectionItem), niet als model.AddedUnits.
        /// Zelfde opbouw als AddSelectedUnits (echte UnitBO ophalen, afwerkingsopties/constructiewaarden
        /// laden) en daar de gepóste prijzen overheen leggen, zodat _UnitRowV2 de kaarten opnieuw kan
        /// tonen mét wat de gebruiker al had ingevuld.</summary>
        private void RestorePostedUnits(AddClientAccountModel model, List<UnitBO>? postedUnits)
        {
            model.AddedUnits = new List<UnitBO>();
            if (postedUnits == null) return;

            foreach (var posted in postedUnits.Where(u => u.Id > 0))
            {
                var response = _unitService.GetUnitById(posted.Id);
                if (!response.Success) continue;

                var unit = response.Value;
                unit.LandValueSold = posted.LandValueSold ?? unit.LandValue;

                var postedCvIds = (posted.ConstructionValues ?? new List<UnitConstructionValueBO>()).Select(cv => cv.Id).ToHashSet();
                var optResp = _unitService.GetFinishingOptions(posted.Id);
                unit.FinishingOptions = optResp.Success ? optResp.Values : new List<UnitFinishingOptionBO>();
                if (unit.FinishingOptions.Any())
                {
                    var selectedOption = unit.FinishingOptions.FirstOrDefault(o => o.ConstructionValues.Any(cv => postedCvIds.Contains(cv.Id)))
                        ?? unit.FinishingOptions.FirstOrDefault(o => o.IsDefault)
                        ?? unit.FinishingOptions.First();
                    unit.ConstructionValues = selectedOption.ConstructionValues;
                }

                foreach (var cv in unit.ConstructionValues)
                {
                    var postedCv = posted.ConstructionValues?.FirstOrDefault(p => p.Id == cv.Id);
                    cv.ValueSold = postedCv?.ValueSold ?? cv.Value;
                }

                model.AddedUnits.Add(unit);
            }
        }

        /// <summary>Foutoverzicht (design-handoff punt 24) — ModelState-sleutel → locatietekst, voor
        /// Views/Shared/GlV2/_ErrorSummaryV2.cshtml se ViewData["GlV2ErrorLocations"]. Enkel de
        /// sleutels die hier ook echt met AddModelError gezet worden (zie de POST-actie hierboven);
        /// een sleutel zonder locatie toont gewoon geen badge, dat is geen fout.</summary>
        private static Dictionary<string, string> AddClientAccountErrorLocations() => new()
        {
            ["ClientAccount.Name"] = "Eigenaars",
            ["ClientAccount.DateSalesAgreement"] = "Klantenaccount",
            ["Units"] = "Eenheden",
            ["CustomError"] = "Eigenaars"
        };

        private Dictionary<int, string> PaymentGroupNamesFor(int projectId)
            => _db.InvoicingPaymentGroup.AsNoTracking()
                .Where(g => g.ProjectId == projectId)
                .ToDictionary(g => g.Id, g => g.Name ?? "");

        public PartialViewResult BlankContactRow(string collectionName = "ClientAccount.Contacts")
        {
            var viewData = new ViewDataDictionary<ClientContactBO>(ViewData, new ClientContactBO())
            {
                { "ContactCollectionName", collectionName }
            };

            return new PartialViewResult
            {
                ViewName = ViewData["UseGlV2Layout"] as bool? == true ? "Partials/_ProjectContactRowV2" : "_ContactRow",
                ViewData = viewData
            };
        }
        [HttpGet]
        public PartialViewResult BlankClientContactRow()
        {
            return PartialView("Partials/_ClientContactRowV2", new ContactInputViewModel());
        }

        public PartialViewResult BlankCoOwnerRow(string collectionName = "ClientAccount.CoOwners")
        {
            var countryService = _countryService;
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
            var countries = countryResponse.Success && countryResponse.Values != null
                ? countryResponse.Values
                : Enumerable.Empty<IdNameBO>();
            var ownerTypeService = _clientService;
            var ownerTypeResponse = ownerTypeService.GetOwnerTypesForSelect();
            var ownerTypes = ownerTypeResponse.Success && ownerTypeResponse.Values != null
                ? ownerTypeResponse.Values.Where(o => o.ID != 1)
                : Enumerable.Empty<IdNameBO>();


            var viewData = new ViewDataDictionary<ClientContactBO>(ViewData, client)
                {
                    { "Countries", countries.Select(c => new SelectListItem { Value = c.ID.ToString(), Text = c.Display }).ToList() },
                    { "OwnerTypes", ownerTypes.Select(o => new SelectListItem { Value = o.ID.ToString(), Text = o.Display }).ToList() },
                    { "CoOwnerCollectionName", collectionName }
                };

            return new PartialViewResult
            {
                ViewName = ViewData["UseGlV2Layout"] as bool? == true ? "Partials/_CoOwnerRowV2" : "Partials/_CoOwnerRow",
                ViewData = viewData
            };
        }
        public PartialViewResult BlankGiftRow()
        {
            var Service = _activityService;
            var gift = new ClientGiftBO();
            var actResponse = Service.GetActivitiesForSelect();
            var activities = actResponse.Values;

            var viewData = new ViewDataDictionary<ClientGiftBO>(ViewData, gift)
                {
                    { "Listactivities", activities}
                };

            return new PartialViewResult
            {
                ViewName = ViewData["UseGlV2Layout"] as bool? == true ? "Partials/_GiftRowV2" : "_GiftRow",
                ViewData = viewData
            };
        }

        public PartialViewResult BlankPoaRow()
        {
            var Service = _activityService;
            var poa = new ClientPoaBO();
            var actResponse = Service.GetActivitiesForSelect();
            var activities = actResponse.Values;

            var viewData = new ViewDataDictionary<ClientPoaBO>(ViewData, poa)
                {
                    { "Listactivities", activities}
                };

            return new PartialViewResult
            {
                ViewName = ViewData["UseGlV2Layout"] as bool? == true ? "Partials/_PoaRowV2" : "_PoaRow",
                ViewData = viewData
            };
        }
        [HttpPost]
        public PartialViewResult AddSelectedUnits(int unitId, string unitName, string unitGroup, int? finishingOptionId = null)
        {
            var unitService = _unitService;
            var response = unitService.GetUnitById(unitId);

            if (!response.Success)
                return PartialView("_UnitRow", new UnitBO());

            var unit = response.Value;
            unit.LandValueSold = unit.LandValue;

            // Laad finishing options
            var optResp = unitService.GetFinishingOptions(unitId);
            unit.FinishingOptions = optResp.Success ? optResp.Values : new List<UnitFinishingOptionBO>();

            if (unit.FinishingOptions.Any())
            {
                // Gebruik geselecteerde optie, of standaard/eerste
                var selectedOption = unit.FinishingOptions.FirstOrDefault(o => o.Id == finishingOptionId)
                    ?? unit.FinishingOptions.FirstOrDefault(o => o.IsDefault)
                    ?? unit.FinishingOptions.First();

                unit.ConstructionValues = selectedOption.ConstructionValues;
                foreach (var cv in unit.ConstructionValues)
                    cv.ValueSold = cv.Value;
            }
            else
            {
                foreach (var cv in unit.ConstructionValues)
                    cv.ValueSold = cv.Value;
            }

            ViewData["mode"] = "add";
            // gl-v2 (_UnitRowV2, 23a "BETALINGSGROEP"): naam van de betalingsgroep — UnitBO kent enkel het id.
            ViewData["PaymentGroupNames"] = PaymentGroupNamesFor(unit.ProjectId);
            return PartialView(ViewData["UseGlV2Layout"] as bool? == true ? "Partials/_UnitRowV2" : "_UnitRow", unit);
        }

        //KLANT BEWERKEN BIJ PROJECT
        [HttpGet]
        public ActionResult EditProject(int projectid, int clientid, int activetab)
        {
            var referrer = Request.Headers["Referer"].ToString();

            // Use the referrer URL as needed
            TempData["Referrer"] = referrer;
            var model = new EditClientModel();

            if (clientid != 0)
            {
                var clientService = _clientService;
                var unitService = _unitService;
                var actService = _activityService;

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
                    model.SelectedPostalcode.PostalCodeId = client.Postalcode.PostcodeId ?? 0;
                    model.SelectedPostalcodeId = client.Postalcode.PostcodeId ?? 0;

                    if (client.InvoicePostalcode.PostcodeId != 0)
                    {
                        model.SelectedInvoicePostalcode.CountryId = client.InvoicePostalcode.Country.CountryId;
                        model.SelectedInvoicePostalcode.PostalCodeId = client.InvoicePostalcode.PostcodeId ?? 0;
                        model.SelectedInvoicePostalcodeId = client.InvoicePostalcode.PostcodeId ?? 0;
                    }

                    ViewData["PostcodeDisplayName"] = $"{client.Postalcode.Postcode} - {client.Postalcode.Gemeente}";
                    ViewData["activetab"] = activetab;

                    // Titel = wát het is (DESIGN.md, "Topbar title & breadcrumb — long names") —
                    // hier is dat de klant zelf, niet de actie: zelfde "Klant - {naam}"-titel als
                    // Klanten/Detail (regel ~940 hierboven), dat blijft leesbaar tijdens een
                    // formulier waar de gebruiker een tijd op kan blijven, i.p.v. enkel het generieke
                    // "Klant bewerken". Het kruimelpad laat daarom, net als Klanten/Detail se eigen
                    // fix, de klantnaam-knoop vallen (zie hieronder) i.p.v. 'm daar een tweede keer
                    // te tonen — de "Klant bewerken"-leaf zelf blijft wél staan, dat is de actie/
                    // locatie, geen herhaling van de titel-tekst.
                    SetPageHeader("bx bx-group", "Klant - " + client.DisplayName);

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
            if (ViewData["PageIcon"] == null)
            {
                SetPageHeader("bx bx-group", model.Client?.DisplayName ?? "Klant bewerken");
            }

            if (model.ProjectId > 0)
            {
                var projectName = _projectService.GetProjectNameById(model.ProjectId);

                var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Home");
                var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
                {
                    Parent = dashboard
                };
                var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName)
                {
                    Parent = projectenIndex,
                    RouteValues = new { projectid = model.ProjectId }
                };
                var klanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten")
                {
                    Parent = projectDetail,
                    RouteValues = new { projectid = model.ProjectId }
                };
                // Kruimelpad stopt bij "Klanten" i.p.v. nog een knoop met de klantnaam toe te voegen —
                // de titel hierboven ("Klant - {naam}") zet die naam al neer, dus een middenitem met
                // (vrijwel) dezelfde naam zou 'm herhalen. Zelfde fix als Klanten/Detail (regel ~931
                // hierboven) en Projecten/Detail — design-handoff punt 13, regel 2. De "Klant
                // bewerken"-leaf zelf is geen herhaling (andere tekst dan de titel) en blijft staan.
                ViewData["BreadcrumbNode"] = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("EditProject", "Klanten", "Klant bewerken")
                {
                    Parent = klanten,
                    RouteValues = new { projectid = model.ProjectId, clientid = clientid, activetab = activetab }
                };
            }

            return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditProjectV2" : "EditProject", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProject(EditClientModel viewmodel)
        {
            SetPageHeader("bx bx-group", viewmodel.Client?.DisplayName is { } n ? "Klant - " + n : "Klant bewerken");
            var Referrer = TempData["Referrer"];
            // Elke redisplay in deze actie (validatie- of rollback-pad) moet hetzelfde view-pad
            // vertakken als de GET — anders zou een mislukte opslag onder gl-v2-preview alsnog stil
            // terugvallen op de legacy EditProject-view.
            var viewName = ViewData["UseGlV2Layout"] as bool? == true ? "EditProjectV2" : "EditProject";

            // Verdeelsleutel (migratie 057, design-handoff 23a) — enkel relevant zodra er mede-eigenaars zijn.
            if (viewmodel.Client?.CoOwners?.Any() == true)
            {
                var shareTotal = (viewmodel.Client.OwnerPercentage ?? 0) + viewmodel.Client.CoOwners.Sum(c => c.CoOwnerPercentage ?? 0);
                if (Math.Abs(shareTotal - 100m) > 0.01m)
                    ModelState.AddModelError("Client.CoOwners", $"De verdeelsleutel moet 100% zijn (nu {shareTotal:0.##}%).");
            }

            if (!ModelState.IsValid || viewmodel.Client.Id == 0)
            {
                FillInAddSelectListsEdit(ref viewmodel);
                return View(viewName, viewmodel);
            }

            // Alle geïnjecteerde services delen dezelfde scoped UoW — transactie werkt over alle services
            await using var tx = await _uow.BeginTransactionAsync();

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
                var r1 = _clientService.InsertUpdate(viewmodel.Client);
                if (!r1.Success)
                {
                    await tx.RollbackAsync();
                    AddMessage("error", $"Klant {viewmodel.Client.DisplayName} is niet bijgewerkt", "Fout!");
                    FillInAddSelectListsEdit(ref viewmodel);
                    return View(viewName, viewmodel);
                }

                //Eenheden updaten
                foreach (var unit in viewmodel.Units)
                {
                    var r2 = _unitService.UpdateLandValueSold(unit);
                    if (!r2.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Unit {unit.Name} is niet bijgewerkt", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewName, viewmodel);
                    }
                    foreach (var constructionvalue in unit.ConstructionValues)
                    {
                        var r3 = _unitService.UpdateConstructionValueSold(constructionvalue);
                        if (!r3.Success)
                        {
                            await tx.RollbackAsync();
                            AddMessage("error", $"Unit {unit.Name} is niet bijgewerkt", "Fout!");
                            FillInAddSelectListsEdit(ref viewmodel);
                            return View(viewName, viewmodel);
                        }
                    }
                }
                //GIFTS
                var postedIds = (viewmodel.Gifts ?? Enumerable.Empty<ClientGiftBO>())
                    .Where(g => g.Id > 0)
                    .Select(g => g.Id)
                    .ToHashSet();

                // 2) Huidige gift-IDs in de database voor deze account
                var existingIds = _uow.Context.Set<ClientGift>()
                    .Where(g => g.ClientAccountId == viewmodel.Client.Id)   // let op: klopt de FK? (soms AccountId)
                    .Select(g => g.Id)
                    .ToList();

                // 3) IDs die we moeten verwijderen
                var removeIds = existingIds.Where(id => !postedIds.Contains(id)).ToList();

                if (removeIds.Count > 0)
                {
                    var r6 = _clientService.DeleteClientGift(removeIds);
                    if (!r6.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Gifts zijn niet verwijderd", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewName, viewmodel);
                    }
                }
                foreach (var gift in viewmodel.Gifts)
                {
                    gift.AccountId = viewmodel.Client.Id;
                    foreach (var i in gift.SelectedActivityIds)
                    {
                        gift.Activities.Add(_activityService.GetActivitybyId(i).Value);
                    }
                    var r4 = _clientService.InsertUpdateClientGift(gift);
                    if (!r4.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Gift {gift.Description} is niet bijgewerkt", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewName, viewmodel);
                    }
                }

                //AANDACHTSPUNTEN

                var postedPoasIds = (viewmodel.Poas ?? Enumerable.Empty<ClientPoaBO>())
                    .Where(g => g.Id > 0)
                    .Select(g => g.Id)
                    .ToHashSet();

                // 2) Huidige poa-IDs in de database voor deze account
                var existingPoasIds = _uow.Context.Set<ClientPoa>()
                    .Where(g => g.ClientAccountId == viewmodel.Client.Id)   // let op: klopt de FK? (soms AccountId)
                    .Select(g => g.Id)
                    .ToList();

                // 3) IDs die we moeten verwijderen
                var removePoasIds = existingPoasIds.Where(id => !postedPoasIds.Contains(id)).ToList();

                if (removePoasIds.Count > 0)
                {
                    var r6 = _clientService.DeleteClientPoa(removeIds);
                    if (!r6.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Poa's zijn niet verwijderd", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewName, viewmodel);
                    }
                }
                foreach (var poa in viewmodel.Poas)
                {
                    poa.AccountId = viewmodel.Client.Id;
                    foreach (var i in poa.SelectedActivityIds)
                    {
                        poa.Activities.Add(_activityService.GetActivitybyId(i).Value);
                    }
                    var r5 = _clientService.InsertUpdateClientPoa(poa);
                    if (!r5.Success)
                    {
                        await tx.RollbackAsync();
                        AddMessage("error", $"Poa {poa.Description} is niet bijgewerkt", "Fout!");
                        FillInAddSelectListsEdit(ref viewmodel);
                        return View(viewName, viewmodel);
                    }
                }

                // 4) (Optioneel) andere bewerkingen met dezelfde UoW
                // var r2 = contactService.InsertUpdate(viewmodel.Contact);
                // if (!r2.Success) { await tx.RollbackAsync(); ... return View(viewmodel); }

                // 5) Alles OK? Opslaan + commit
                await _uow.SaveChangesAsync();
                await tx.CommitAsync();

                AddMessage("success", $"Account {viewmodel.Client.DisplayName} is bijgewerkt", "Geslaagd!");
                return Referrer != null
                    ? Redirect(Referrer.ToString())
                    : RedirectToAction("Edit", new { projectid = viewmodel.ProjectId, clientid = viewmodel.Client.Id, activetab = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij bewerken van klant {ClientId}", viewmodel.Client.Id);
                await tx.RollbackAsync();
                AddMessage("error", "Er is een onverwachte fout opgetreden.", "Fout!");
                FillInAddSelectListsEdit(ref viewmodel);
                return View(viewName, viewmodel);
            }

        }


        private void FillInAddSelectListsEdit(ref EditClientModel model)
        {
            var countryService = _countryService;
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
                if (model.SelectedPostalcode.CountryId == 0)
                {
                    model.SelectedPostalcode.CountryId = defaultCountry.ID;
                }

                if (model.SelectedInvoicePostalcode.CountryId == 0)
                {
                    model.SelectedInvoicePostalcode.CountryId = defaultCountry.ID;
                }
            }

            var ownerTypeService = _clientService;
            var ownerTypeResponse = ownerTypeService.GetOwnerTypesForSelect();
            if (ownerTypeResponse.Success)
            {
                model.OwnerTypes = ownerTypeResponse.Values;
            }

            // gl-v2 (Klanten/EditProjectV2): projectdossier-inner menu — zelfde velden/reden als
            // Klanten/DetailV2 (ClientModel.ProjectClientCount/IsCoordinationProject). Deze helper
            // wordt op ELK redisplay-pad aangeroepen (initiële GET én elke POST-validatiefout), dus
            // blijft het inner menu correct ook wanneer opslaan mislukt.
            if (model.ProjectId > 0)
            {
                model.ProjectName = _projectService.GetProjectNameById(model.ProjectId);
                var projectClientsResponse = _clientService.GetClientAccountsByProjectId(model.ProjectId);
                model.ProjectClientCount = projectClientsResponse.Success ? projectClientsResponse.Values.Count : 0;
                var projectResponse = _projectService.GetProjectByID(model.ProjectId);
                model.IsCoordinationProject = projectResponse.Success && projectResponse.Value?.IsOnlyCoordinationProject == true;
            }
        }


        // KLANT VERWIJDEREN
        [CPMCore.Filters.PermissionDelete(PermissionCodes.Customers)]
        public ActionResult PartialDeleteClientModal(int id)
        {
            var viewModel = new IdNameBO();
            if (id != 0)
            {
                var dservice = _clientService;
                viewModel.Display = dservice.GetClientAccountNameById(id);
                viewModel.ID = id;
            }
            return PartialView("_DeleteClientModal", viewModel);
        }

        /// <summary>gl-v2 layout-pilot — zelfde opzoeklogica als <see cref="PartialDeleteClientModal"/>
        /// hierboven, enkel een andere view: gl-v2's Type 1-bevestigingscomponent (Bootstrap-modal,
        /// Modals/_DeleteClientModalV2.cshtml) i.p.v. het magnific-popup-fragment. De knop erin blijft
        /// dezelfde `DeleteClient` GET-actie aanroepen (ongewijzigd, geen nieuwe POST/antiforgery-
        /// stap toegevoegd t.o.v. het bestaande gedrag).</summary>
        [CPMCore.Filters.PermissionDelete(PermissionCodes.Customers)]
        public ActionResult PartialDeleteClientModalV2(int id)
        {
            var viewModel = new IdNameBO();
            if (id != 0)
            {
                var dservice = _clientService;
                viewModel.Display = dservice.GetClientAccountNameById(id);
                viewModel.ID = id;
            }
            return PartialView("Modals/_DeleteClientModalV2", viewModel);
        }

        [HttpGet]
        [CPMCore.Filters.PermissionDelete(PermissionCodes.Customers)]
        public ActionResult DeleteClient(int id)
        {
            var scope = ResolveCustomerIssuerScopeAsync(PermissionAccessType.Delete, HttpContext.RequestAborted)
                .GetAwaiter()
                .GetResult();
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te verwijderen.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!scope.HasAllIssuers)
            {
                var canDeleteClient = _db.ClientAccountIssuerCompany
                    .AsNoTracking()
                    .Any(link => link.ClientAccountId == id && scope.AllowedIssuerIds.Contains(link.IssuerCompanyId));

                if (!canDeleteClient)
                {
                    AddMessage("error", "Je hebt geen rechten om deze klant te verwijderen.", "Geen toegang");
                    return RedirectToAction(nameof(Index));
                }
            }

            string stri = Request.Headers["Referer"].ToString();
            List<int> Idlist = new List<int>();
            Idlist.Add(id);
            if (id != 0)
            {
                var uservice = _unitService;
                var response = uservice.DeleteUnitFromClientAccountByAccountId(Idlist);
                var dservice = _clientService;
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

    }
}
