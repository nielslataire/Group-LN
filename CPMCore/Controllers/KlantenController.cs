using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Klanten;
using CPMCore.Models.Projecten;
using CPMCore.Service;
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
    [Authorize]
    [CPMCore.Filters.PermissionRead(PermissionCodes.Customers)]
    public class KlantenController : BaseController
    {
        private static readonly JsonSerializerOptions VatLookupSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration Configuration;
        private readonly cpmRunningContext _db;
        private readonly IOctopusApiClient _octopusClient;
        private readonly IOctopusTokenManager _octopusTokens;
        private const string CustomerCompanyPermissionPrefix = "Customers.Company.";

        public KlantenController(ILogger<HomeController> logger, IConfiguration configuration, cpmRunningContext db, IOctopusApiClient octopusClient, IOctopusTokenManager octopusTokens)
        {
            _logger = logger;
            Configuration = configuration;
            _db = db;
            _octopusClient = octopusClient;
            _octopusTokens = octopusTokens;
        }

        [HttpGet]
        [Breadcrumb("Klanten")]
        public async Task<IActionResult> Index(int? issuerCompanyId, CancellationToken ct)
        {
            var readScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bekijken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            var writeScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            var deleteScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Delete, ct);


            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var KlantenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Klanten", "Klanten")
            {
                Parent = Index,
            };

            ViewData["BreadcrumbNode"] = KlantenIndex;
            var issuerCompanies = await _db.IssuerCompany
                .AsNoTracking()
                   .Where(i => i.IsActive)
                  .Where(i => readScope.HasAllIssuers || readScope.AllowedIssuerIds.Contains(i.Id))
                .OrderBy(i => i.Name)
                .Select(i => new IssuerCompanyOptionViewModel
                {
                    Id = i.Id,
                    Name = i.Name
                })
                .ToListAsync(ct);

            var clientsQuery = _db.ClientAccount
                .Include(c => c.PostalCode)
                .Include(c => c.ClientContacts)
                 .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .AsNoTracking()
                .OrderBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? c.Name : c.CompanyName);

            if (issuerCompanyId.HasValue)
            {
                if (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(issuerCompanyId.Value))
                {
                    issuerCompanyId = null;
                }

                clientsQuery = clientsQuery
                    .Where(c => c.ClientAccountIssuerCompany.Any(i => i.IssuerCompanyId == issuerCompanyId))
                    .OrderBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? c.Name : c.CompanyName);
            }

            if (!readScope.HasAllIssuers)
            {
                clientsQuery = clientsQuery
                    .Where(c => c.ClientAccountIssuerCompany.Any(i => readScope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
                    .OrderBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? c.Name : c.CompanyName);
            }

            var clients = await clientsQuery
                .Select(c => new ClientListItemViewModel
                {
                    Id = c.Id,
                    DisplayName = string.IsNullOrWhiteSpace(c.CompanyName) ? c.Name : c.CompanyName,
                    EnterpriseNumber = c.Vatnumber,
                    City = c.PostalCode != null
                        ? c.PostalCode.Postcode + " " + c.PostalCode.Gemeente
                        : null,

                    Email = !string.IsNullOrWhiteSpace(c.Email)
                        ? c.Email
                        : c.ClientContacts
                            .OrderBy(cc => cc.Id)
                            .Select(cc => cc.Email)
                            .FirstOrDefault(),

                    Phone = c.ClientContacts
                        .OrderBy(cc => cc.Id)
                        .Select(cc => cc.Phone != null ? cc.Phone : cc.Cellphone)
                        .FirstOrDefault(),

                    IssuerCompanies = c.ClientAccountIssuerCompany
                        .Select(i => i.IssuerCompany.Name)
                        .ToList(),
                    IssuerCompanyIds = c.ClientAccountIssuerCompany
                        .Select(i => i.IssuerCompanyId)
                        .ToList(),

                    ContactCount = c.ClientContacts.Count,
                    CanEdit = false,
                    CanDelete = false
                })
                .ToListAsync(ct);


            bool HasAccessForClient(CustomerIssuerScope scope, IReadOnlyList<int> issuerIds)
            {
                if (!scope.HasAccess)
                {
                    return false;
                }

                if (scope.HasAllIssuers)
                {
                    return true;
                }

                return issuerIds.Any(scope.AllowedIssuerIds.Contains);
            }

            var clientsWithPermissions = clients
                .Select(client => new ClientListItemViewModel
                {
                    Id = client.Id,
                    DisplayName = client.DisplayName,
                    EnterpriseNumber = client.EnterpriseNumber,
                    City = client.City,
                    Email = client.Email,
                    Phone = client.Phone,
                    IssuerCompanies = client.IssuerCompanies,
                    IssuerCompanyIds = client.IssuerCompanyIds,
                    ContactCount = client.ContactCount,
                    CanEdit = HasAccessForClient(writeScope, client.IssuerCompanyIds),
                    CanDelete = HasAccessForClient(deleteScope, client.IssuerCompanyIds)
                })
                .ToList();

            var canCreateClient = writeScope.HasAccess
                && (!issuerCompanyId.HasValue
                    || writeScope.HasAllIssuers
                    || writeScope.AllowedIssuerIds.Contains(issuerCompanyId.Value));

            var model = new ClientIndexViewModel
            {
                Clients = clientsWithPermissions,
                IssuerCompanies = issuerCompanies,
                SelectedIssuerCompanyId = issuerCompanyId,
                CanCreateClient = canCreateClient
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {

            var readScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bekijken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            var client = await _db.ClientAccount
                .Include(c => c.PostalCode)
                .Include(c => c.InvoicePostalCode)
                .Include(c => c.ClientContacts)
               .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (client == null)
            {
                return NotFound();
            }

            if (!readScope.HasAllIssuers && !client.ClientAccountIssuerCompany.Any(i => readScope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om deze klant te bekijken.", "Geen toegang");
                return RedirectToAction("Index","Klanten");
            }

            var writeScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            var canEdit = writeScope.HasAccess
                && (writeScope.HasAllIssuers || client.ClientAccountIssuerCompany.Any(i => writeScope.AllowedIssuerIds.Contains(i.IssuerCompanyId)));

            var clientDisplayName = string.IsNullOrWhiteSpace(client.CompanyName) ? client.Name : client.CompanyName;
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var KlantenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Klanten", "Klanten")
            {
                Parent = Index,
            };

            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Details), "Klanten", clientDisplayName)
            {
                Parent = KlantenIndex,
                RouteValues = new { id }
            };



            var isCompany = !string.IsNullOrWhiteSpace(client.CompanyName) || !string.IsNullOrWhiteSpace(client.Vatnumber);
            Salutation? salutation = Enum.TryParse(client.Salutation, out Salutation parsed)
                    ? parsed
                    : null;
            var (vatCountryCode, vatNumber) = SplitEnterpriseNumber(client.Vatnumber);

            var model = new ClientFormViewModel
            {
                Id = client.Id,
                IsCompany = isCompany,
                CompanyName = isCompany ? client.CompanyName ?? client.Name : null,
                Name = client.Name,
                Salutation = isCompany ? null : salutation,
                EnterpriseNumber = isCompany ? vatNumber : null,
                EnterpriseNumberCountryCode = isCompany ? vatCountryCode ?? "BE" : "BE",
                Street = client.Street,
                HouseNumber = client.Housenumber,
                BusNumber = client.Busnumber,
                SelectedPostalCodeId = client.PostalCodeId,
                PostalCode = client.PostalCode?.Postcode,
                City = client.PostalCode?.Gemeente,
                SelectedCountryId = client.PostalCode?.CountryId,
                UseInvoiceAddress = client.InvoiceAddress ?? false,
                InvoiceStreet = client.InvoiceStreet,
                InvoiceHouseNumber = client.InvoiceHousenumber,
                InvoiceBusNumber = client.InvoiceBusnumber,
                SelectedInvoicePostalCodeId = client.InvoicePostalCodeId,
                InvoicePostalCode = client.InvoicePostalCode?.Postcode,
                InvoiceCity = client.InvoicePostalCode?.Gemeente,
                SelectedInvoiceCountryId = client.InvoicePostalCode?.CountryId,
                Email = client.Email,
                InvoiceEmail = client.InvoiceEmail,
                RequiresDigitalInvoice = client.RequiresDigitalInvoice,
                AttachUblByDefault = client.AttachUblByDefault,
                SelectedIssuerCompanyIds = client.ClientAccountIssuerCompany
                    .Select(i => i.IssuerCompanyId)
                    .ToList(),
                Contacts = client.ClientContacts
                    .OrderBy(c => c.Id)
                    .Select(c => new ContactInputViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Forename = c.Forename,
                        Email = c.Email,
                        Phone = c.Phone,
                        Mobile = c.Cellphone,
                        RequiresDigitalInvoice = c.RequiresDigitalInvoice,
                        AttachUblByDefault = c.AttachUblByDefault
                    }).ToList(),
                CanEdit = canEdit
            };

            await BuildFormAsync(model, ct);
            ViewBag.ReadOnly = true;
            return View("Form", model);
        }

        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Create(int? issuerCompanyId, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten toe te voegen.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            if (issuerCompanyId.HasValue && !scope.HasAllIssuers && !scope.AllowedIssuerIds.Contains(issuerCompanyId.Value))
            {
                issuerCompanyId = null;
            }

            var model = new ClientFormViewModel
            {
                SelectedIssuerCompanyIds = issuerCompanyId.HasValue
                    ? new List<int> { issuerCompanyId.Value }
                    : new List<int>()
            };
            await BuildFormAsync(model, ct, scope);
            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Create(ClientFormViewModel model, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten toe te voegen.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!scope.HasAllIssuers)
            {
                model.SelectedIssuerCompanyIds = (model.SelectedIssuerCompanyIds ?? new List<int>())
                    .Where(scope.AllowedIssuerIds.Contains)
                    .ToList();
            }

            RemoveEmptyContacts(model);
            await BuildFormAsync(model, ct, scope);
            ValidateClientType(model);
            ValidateIssuerCompanies(model);

            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var entity = new ClientAccount();
            MapToEntity(model, entity);
            AttachIssuerCompany(model, entity);
            AttachContacts(model, entity);

            _db.ClientAccount.Add(entity);
            await _db.SaveChangesAsync(ct);

            AddMessage("success", $"Klant {model.DisplayLabel} is toegevoegd", "Geslaagd!");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bewerken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            var client = await _db.ClientAccount
                .Include(c => c.ClientContacts)
                 .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .Include(c => c.PostalCode)
                .Include(c => c.InvoicePostalCode)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (client == null)
            {
                return NotFound();
            }

            if (!scope.HasAllIssuers && !client.ClientAccountIssuerCompany.Any(i => scope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om deze klant te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(Index));
            }


            var clientDisplayName = string.IsNullOrWhiteSpace(client.CompanyName) ? client.Name : client.CompanyName;
            var klantenNode = new MvcBreadcrumbNode(nameof(Index), "Klanten", "Klanten")
            {
                Parent = new MvcBreadcrumbNode("Index", "Home", "Home")
            };
            var detailsNode = new MvcBreadcrumbNode(nameof(Details), "Klanten", clientDisplayName)
            {
                Parent = klantenNode,
                RouteValues = new { id = client.Id }
            };
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Edit), "Klanten", "Bewerken")
            {
                Parent = detailsNode,
                RouteValues = new { id = client.Id }
            };

            var isCompany = !string.IsNullOrWhiteSpace(client.CompanyName) || !string.IsNullOrWhiteSpace(client.Vatnumber);
            Salutation? salutation = Enum.TryParse(client.Salutation, out Salutation parsed)
                ? parsed
                : null;
            var (vatCountryCode, vatNumber) = SplitEnterpriseNumber(client.Vatnumber);

            var model = new ClientFormViewModel
            {
                Id = client.Id,
                IsCompany = isCompany,
                CompanyName = isCompany ? client.CompanyName ?? client.Name : null,
                Name = client.Name,
                Salutation = isCompany ? null : salutation,
                EnterpriseNumber = isCompany ? vatNumber : null,
                EnterpriseNumberCountryCode = isCompany ? vatCountryCode ?? "BE" : "BE",
                Street = client.Street,
                HouseNumber = client.Housenumber,
                BusNumber = client.Busnumber,
                SelectedPostalCodeId = client.PostalCodeId,
                PostalCode = client.PostalCode?.Postcode,
                City = client.PostalCode?.Gemeente,
                SelectedCountryId = client.PostalCode?.CountryId,
                UseInvoiceAddress = client.InvoiceAddress ?? false,
                InvoiceStreet = client.InvoiceStreet,
                InvoiceHouseNumber = client.InvoiceHousenumber,
                InvoiceBusNumber = client.InvoiceBusnumber,
                SelectedInvoicePostalCodeId = client.InvoicePostalCodeId,
                InvoicePostalCode = client.InvoicePostalCode?.Postcode,
                InvoiceCity = client.InvoicePostalCode?.Gemeente,
                SelectedInvoiceCountryId = client.InvoicePostalCode?.CountryId,
                Email = client.Email,
                InvoiceEmail = client.InvoiceEmail,
                RequiresDigitalInvoice = client.RequiresDigitalInvoice,
                AttachUblByDefault = client.AttachUblByDefault,
                SelectedIssuerCompanyIds = client.ClientAccountIssuerCompany
                    .Select(i => i.IssuerCompanyId)
                    .ToList(),
                Contacts = client.ClientContacts
                    .OrderBy(c => c.Id)
                    .Select(c => new ContactInputViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Forename = c.Forename,
                        Email = c.Email,
                        Phone = c.Phone,
                        Mobile = c.Cellphone,
                        RequiresDigitalInvoice = c.RequiresDigitalInvoice,
                        AttachUblByDefault = c.AttachUblByDefault
                    }).ToList()
            };

            await BuildFormAsync(model, ct, scope);
            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Edit(int id, ClientFormViewModel model, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bewerken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            if (id != model.Id)
            {
                return BadRequest();
            }

            RemoveEmptyContacts(model);
            var client = await _db.ClientAccount
                .Include(c => c.ClientContacts)
                .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (client == null)
            {
                return NotFound();
            }
            if (!scope.HasAllIssuers && !client.ClientAccountIssuerCompany.Any(i => scope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om deze klant te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(Index));
            }

            if (!scope.HasAllIssuers)
            {
                model.SelectedIssuerCompanyIds = (model.SelectedIssuerCompanyIds ?? new List<int>())
                    .Where(scope.AllowedIssuerIds.Contains)
                    .ToList();
            }

            await BuildFormAsync(model, ct, scope);
            ValidateClientType(model);
            ValidateIssuerCompanies(model);

            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            var requiresOctopusSync = (client.OctopusRelationId ?? 0) > 0
               && HasClientDataChanged(client, model);

            MapToEntity(model, client);
            UpdateIssuerCompany(model, client);
            UpdateContacts(model, client);

            await _db.SaveChangesAsync(ct);

            if (requiresOctopusSync)
            {
                await TrySyncOctopusRelationAsync(client.Id, ct);
            }

            AddMessage("success", $"Klant {model.DisplayLabel} is bijgewerkt", "Geslaagd!");
            return RedirectToAction(nameof(Index));
        }



        // KLANTEN - PROJECT
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

            if (model.ProjectId > 0)
            {
                var projectName = projectService.GetProjectNameById(model.ProjectId);
                var clientName = model.Client?.DisplayName ?? clientService.GetClientAccountNameById(clientId);

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
                var klantDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Klanten", clientName)
                {
                    Parent = klanten,
                    RouteValues = new { clientid = clientId, projectid = model.ProjectId }
                };
                ViewData["BreadcrumbNode"] = klantDetail;
            }



            return View(model);
        }

        // KLANT TOEVOEGEN
        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
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
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public ActionResult AddClientAccount(AddClientAccountModel model, List<ClientContactBO> contacts, List<UnitBO> units)
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
            //model.ClientAccount.CoOwners = model.ClientAccount.CoOwners?.Any() == true ? model.ClientAccount.CoOwners : coowners;
            model.ClientAccount.Contacts = model.ClientAccount.Contacts?.Any() == true ? model.ClientAccount.Contacts : contacts;

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
            var sservice = ServiceFactory.GetClientService();
            var sresponse = sservice.GetClientOwnerTypeById(OwnerType);
            nCoOwner.CoOwnerType = sresponse.Value;
            var nlCulture = CultureInfo.GetCultureInfo("nl-BE");
            if (!decimal.TryParse(OwnerPercentage, NumberStyles.Number, nlCulture, out var parsedPercentage))
            {
                decimal.TryParse(OwnerPercentage, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedPercentage);
            }

            nCoOwner.CoOwnerPercentage = parsedPercentage;
            var countryService = ServiceFactory.GetCountryService();
            var countryResponse = countryService.GetVisibleCountriesForSelect();
            var ownerTypeService = ServiceFactory.GetClientService();
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
            var cservice = ServiceFactory.GetCountryService();
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
            var oservice = ServiceFactory.GetClientService();
            var oresponse = oservice.GetOwnerTypesForSelect();
            if ((oresponse.Success))
                model.OwnerTypes = oresponse.Values;
            var uservice = ServiceFactory.GetUnitService();
            var uresponse = uservice.GetAvailableUnitsByProjectId(model.ProjectId);
            if ((uresponse.Success))
                model.AvailableUnits = uresponse.Values;
        }
        public PartialViewResult BlankContactRow(string collectionName = "ClientAccount.Contacts")
        {
            var viewData = new ViewDataDictionary<ClientContactBO>(ViewData, new ClientContactBO())
            {
                { "ContactCollectionName", collectionName }
            };

            return new PartialViewResult
            {
                ViewName = "_ContactRow",
                ViewData = viewData
            };
        }
        public PartialViewResult BlankCoOwnerRow(string collectionName = "ClientAccount.CoOwners")
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
            var countries = countryResponse.Success && countryResponse.Values != null
                ? countryResponse.Values
                : Enumerable.Empty<IdNameBO>();
            var ownerTypeService = ServiceFactory.GetClientService();
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
                ViewName = "Partials/_CoOwnerRow",
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
        private async Task<ClientFormViewModel> BuildFormAsync(ClientFormViewModel model, CancellationToken ct, CustomerIssuerScope? scope = null)
        {
            var countries = await _db.Country
                .AsNoTracking()
                .Where(c => c.Selectable)
                .OrderBy(c => c.LandNaam)
                .Select(c => new CountryOptionViewModel
                {
                    Id = c.Id,
                    Name = c.LandNaam,
                    IsoCode = c.LandIsocode ?? string.Empty
                })
                .ToListAsync(ct);

            model.Countries = countries;

            if (string.IsNullOrWhiteSpace(model.EnterpriseNumberCountryCode))
            {
                model.EnterpriseNumberCountryCode = countries
                    .FirstOrDefault(c => string.Equals(c.IsoCode, "BE", StringComparison.OrdinalIgnoreCase))?.IsoCode
                    ?? countries.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.IsoCode))?.IsoCode
                    ?? "BE";
            }
            else
            {
                model.EnterpriseNumberCountryCode = SanitizeVatCountry(model.EnterpriseNumberCountryCode) ?? model.EnterpriseNumberCountryCode;
            }

            if (!model.SelectedCountryId.HasValue && countries.Any())
            {
                var defaultCountry = countries.FirstOrDefault(c => c.IsoCode.Equals("BE", StringComparison.OrdinalIgnoreCase)) ?? countries.First();
                model.SelectedCountryId = defaultCountry.Id;
                model.CountryIsoCode = defaultCountry.IsoCode;
            }

            if (!model.SelectedInvoiceCountryId.HasValue && countries.Any())
            {
                model.SelectedInvoiceCountryId = model.SelectedCountryId;
            }

            var issuers = await _db.IssuerCompany
                .AsNoTracking()
                .Where(i => i.IsActive)
                .Where(i => scope == null || scope.HasAllIssuers || scope.AllowedIssuerIds.Contains(i.Id))
                .OrderBy(i => i.Name)
                .Select(i => new IssuerCompanyOptionViewModel
                {
                    Id = i.Id,
                    Name = i.Name
                })
                .ToListAsync(ct);

            model.SelectedIssuerCompanyIds ??= new List<int>();
            if (scope != null && !scope.HasAllIssuers)
            {
                model.SelectedIssuerCompanyIds = model.SelectedIssuerCompanyIds
                    .Where(scope.AllowedIssuerIds.Contains)
                    .ToList();
            }
            model.IssuerCompanies = issuers;

            if (!model.SelectedIssuerCompanyIds.Any() && issuers.Any())
            {
                model.SelectedIssuerCompanyIds = issuers
                    .Take(1)
                    .Select(i => i.Id)
                    .ToList();
            }


            if (model.Contacts == null || model.Contacts.Count == 0)
            {
                model.Contacts = new List<ContactInputViewModel> { new ContactInputViewModel() };
            }

            return model;
        }
        private void ValidateClientType(ClientFormViewModel model)
        {
            if (model.IsCompany)
            {
                if (string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    ModelState.AddModelError(nameof(model.CompanyName), "Bedrijfsnaam is verplicht voor een onderneming.");
                }

                return;
            }

            if (!model.Salutation.HasValue)
            {
                ModelState.AddModelError(nameof(model.Salutation), "Kies een aanspreking.");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Naam is verplicht voor een particulier.");
            }
        }

        private void ValidateIssuerCompanies(ClientFormViewModel model)
        {
            if (model.SelectedIssuerCompanyIds == null || !model.SelectedIssuerCompanyIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedIssuerCompanyIds), "Kies minstens één facturatiebedrijf.");
            }
        }

        private async Task<CustomerIssuerScope> ResolveCustomerIssuerScopeAsync(PermissionAccessType accessType, CancellationToken ct)
        {
            var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            await permissionService.EnsureLoadedAsync(ct);

            var hasMainAccess = accessType switch
            {
                PermissionAccessType.Read => permissionService.HasRead(PermissionCodes.Customers),
                PermissionAccessType.Write => permissionService.HasWrite(PermissionCodes.Customers),
                PermissionAccessType.Delete => permissionService.HasDelete(PermissionCodes.Customers),
                _ => false
            };

            if (!hasMainAccess)
            {
                return new CustomerIssuerScope(false, false, new HashSet<int>());
            }

            var scopedIds = permissionService.EffectivePermissions
                .Where(x => x.Key.StartsWith(CustomerCompanyPermissionPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(x => accessType switch
                {
                    PermissionAccessType.Read => x.Value.Read,
                    PermissionAccessType.Write => x.Value.Write,
                    PermissionAccessType.Delete => x.Value.Delete,
                    _ => false
                })
                .Select(x =>
                {
                    var suffix = x.Key[CustomerCompanyPermissionPrefix.Length..];
                    return int.TryParse(suffix, out var companyId) ? (int?)companyId : null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
            if (scopedIds.Count == 0)
            {
                return new CustomerIssuerScope(true, true, new HashSet<int>());
            }

            return new CustomerIssuerScope(true, false, scopedIds.ToHashSet());
        }

        private sealed record CustomerIssuerScope(bool HasAccess, bool HasAllIssuers, HashSet<int> AllowedIssuerIds);


        private static void MapToEntity(ClientFormViewModel model, ClientAccount entity)
        {
            entity.Name = model.IsCompany ? model.CompanyName : model.Name;
            entity.CompanyName = model.IsCompany ? model.CompanyName : null;
            entity.Salutation = model.IsCompany ? null : model.Salutation?.ToString();
            entity.Vatnumber = model.IsCompany
                 ? NormalizeEnterpriseNumber(model.EnterpriseNumber)
               : null;
            entity.Street = model.Street;
            entity.Housenumber = model.HouseNumber;
            entity.Busnumber = model.BusNumber;
            entity.PostalCodeId = model.SelectedPostalCodeId;
            entity.InvoiceAddress = model.UseInvoiceAddress;
            entity.InvoiceStreet = model.UseInvoiceAddress ? model.InvoiceStreet : null;
            entity.InvoiceHousenumber = model.UseInvoiceAddress ? model.InvoiceHouseNumber : null;
            entity.InvoiceBusnumber = model.UseInvoiceAddress ? model.InvoiceBusNumber : null;
            entity.InvoicePostalCodeId = model.UseInvoiceAddress ? model.SelectedInvoicePostalCodeId : null;
            entity.Email = model.Email;
            entity.InvoiceEmail = model.InvoiceEmail;
            entity.RequiresDigitalInvoice = model.RequiresDigitalInvoice;
            entity.AttachUblByDefault = model.AttachUblByDefault;
            entity.OwnerPercentage ??= 100;
            entity.OwnerTypeId ??= 1;
        }

        private void AttachIssuerCompany(ClientFormViewModel model, ClientAccount entity)
        {
            if (model.SelectedIssuerCompanyIds == null || model.SelectedIssuerCompanyIds.Count == 0)
            {
                return;
            }

            var issuerIds = model.SelectedIssuerCompanyIds.Distinct().ToList();
            var issuers = _db.IssuerCompany
                .Where(i => issuerIds.Contains(i.Id))
                .ToList();

            foreach (var issuer in issuers)
            {
                if (entity.ClientAccountIssuerCompany.Any(link => link.IssuerCompanyId == issuer.Id))
                {
                    continue;
                }

                entity.ClientAccountIssuerCompany.Add(new ClientAccountIssuerCompany
                {
                    ClientAccount = entity,
                    ClientAccountId = entity.Id,
                    IssuerCompany = issuer,
                    IssuerCompanyId = issuer.Id
                });
            }
        }

        private void UpdateIssuerCompany(ClientFormViewModel model, ClientAccount entity)
        {
            entity.ClientAccountIssuerCompany.Clear();
            AttachIssuerCompany(model, entity);
        }

        private static void AttachContacts(ClientFormViewModel model, ClientAccount entity)
        {
            foreach (var contact in model.Contacts.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
            {
                entity.ClientContacts.Add(new ClientContacts
                {
                    Name = contact.Name,
                    Forename = contact.Forename,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Cellphone = contact.Mobile,
                    RequiresDigitalInvoice = contact.RequiresDigitalInvoice,
                    AttachUblByDefault = contact.AttachUblByDefault
                });
            }
        }

        private static void UpdateContacts(ClientFormViewModel model, ClientAccount entity)
        {
            var incomingIds = model.Contacts.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToList();
            var toRemove = entity.ClientContacts.Where(c => !incomingIds.Contains(c.Id)).ToList();

            foreach (var removal in toRemove)
            {
                entity.ClientContacts.Remove(removal);
            }

            foreach (var contactModel in model.Contacts)
            {
                if (contactModel.Id is int contactId)
                {
                    var existing = entity.ClientContacts.FirstOrDefault(c => c.Id == contactId);
                    if (existing != null)
                    {
                        existing.Name = contactModel.Name;
                        existing.Forename = contactModel.Forename;
                        existing.Email = contactModel.Email;
                        existing.Phone = contactModel.Phone;
                        existing.Cellphone = contactModel.Mobile;
                        existing.RequiresDigitalInvoice = contactModel.RequiresDigitalInvoice;
                        existing.AttachUblByDefault = contactModel.AttachUblByDefault;
                        continue;
                    }
                }

                entity.ClientContacts.Add(new ClientContacts
                {
                    Name = contactModel.Name,
                    Forename = contactModel.Forename,
                    Email = contactModel.Email,
                    Phone = contactModel.Phone,
                    Cellphone = contactModel.Mobile,
                    RequiresDigitalInvoice = contactModel.RequiresDigitalInvoice,
                    AttachUblByDefault = contactModel.AttachUblByDefault
                });
            }
        }

        private void RemoveEmptyContacts(ClientFormViewModel model)
        {
            if (model.Contacts == null)
            {
                model.Contacts = new List<ContactInputViewModel>();
                return;
            }

            var cleaned = new List<ContactInputViewModel>();
            for (var i = 0; i < model.Contacts.Count; i++)
            {
                var contact = model.Contacts[i];
                if (contact == null)
                {
                    continue;
                }

                var isEmpty = string.IsNullOrWhiteSpace(contact.Name)
                    && string.IsNullOrWhiteSpace(contact.Forename)
                    && string.IsNullOrWhiteSpace(contact.Email)
                    && string.IsNullOrWhiteSpace(contact.Phone)
                    && string.IsNullOrWhiteSpace(contact.Mobile);

                if (isEmpty)
                {
                    var prefix = $"Contacts[{i}].";
                    var keysToRemove = ModelState.Keys
                        .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var key in keysToRemove)
                    {
                        ModelState.Remove(key);
                    }
                    continue;
                }

                cleaned.Add(contact);
            }

            model.Contacts = cleaned;
        }
        private static bool HasClientDataChanged(ClientAccount entity, ClientFormViewModel model)
        {
            static bool Different(string? a, string? b)
                => !string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

            var newVat = model.IsCompany
                ? NormalizeEnterpriseNumber(model.EnterpriseNumber)
                : null;

            return Different(entity.Street, model.Street)
                || Different(entity.Housenumber, model.HouseNumber)
                || Different(entity.Busnumber, model.BusNumber)
                || entity.PostalCodeId != model.SelectedPostalCodeId
                || Different(entity.InvoiceStreet, model.UseInvoiceAddress ? model.InvoiceStreet : null)
                || Different(entity.InvoiceHousenumber, model.UseInvoiceAddress ? model.InvoiceHouseNumber : null)
                || Different(entity.InvoiceBusnumber, model.UseInvoiceAddress ? model.InvoiceBusNumber : null)
                || entity.InvoicePostalCodeId != (model.UseInvoiceAddress ? model.SelectedInvoicePostalCodeId : null)
                || Different(entity.Vatnumber, newVat)
                || Different(entity.Email, model.Email)
                || Different(entity.InvoiceEmail, model.InvoiceEmail);
        }

        private async Task TrySyncOctopusRelationAsync(int clientId, CancellationToken ct)
        {
            var client = await _db.ClientAccount
                .Include(c => c.PostalCode)!
                    .ThenInclude(pc => pc.Country)
                .Include(c => c.InvoicePostalCode)!
                    .ThenInclude(pc => pc.Country)
                 .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .FirstOrDefaultAsync(c => c.Id == clientId, ct);

            if (client?.OctopusRelationId is not int relationId || relationId <= 0)
            {
                return;
            }

            var request = BuildOctopusRelationRequest(client);

            foreach (var issuer in client.ClientAccountIssuerCompany
                           .Select(link => link.IssuerCompany)
                           .Where(i => i != null && !string.IsNullOrWhiteSpace(i.OctopusDossierNumber))!)
            {
                var dossierToken = await _octopusTokens.RefreshDossierTokenAsync(issuer.Id, issuer.OctopusDossierNumber, ct);
                await _octopusClient.UpsertRelationAsync(dossierToken.Token, issuer.OctopusDossierNumber, request, ct);
            }
        }

        private static OctopusRelationRequest BuildOctopusRelationRequest(ClientAccount client)
        {
            var countryCode = client.InvoicePostalCode?.Country?.LandIsocode
                ?? client.PostalCode?.Country?.LandIsocode;

            var isCompany = !string.IsNullOrWhiteSpace(client.CompanyName) || !string.IsNullOrWhiteSpace(client.Vatnumber);

            var name = string.IsNullOrWhiteSpace(client.CompanyName) ? client.Name : client.CompanyName;

            var request = new OctopusRelationRequest
            {
                RelationIdentificationServiceData = new OctopusRelationIdentificationData
                {
                    RelationKey = new OctopusRelationKey { Id = client.OctopusRelationId ?? 0 },
                    ExternalRelationId = client.Id
                },
                Name = name,
                Firstname = client.Name,
                Client = true,
                Supplier = false,
                Active = true,
                StreetAndNr = BuildStreetAndNumber(
                    client.InvoiceStreet ?? client.Street,
                    client.InvoiceHousenumber ?? client.Housenumber,
                    client.InvoiceBusnumber ?? client.Busnumber,
                    client.Street),
                PostalCode = client.InvoicePostalCode?.Postcode ?? client.PostalCode?.Postcode,
                City = client.InvoicePostalCode?.Gemeente ?? client.PostalCode?.Gemeente,
                Country = countryCode ?? "BE",
                VatNr = FormatVatNumberForOctopus(client.Vatnumber, countryCode),
                CurrencyCode = "EUR",
                Contactperson = name,
                DefaultBookingAccountClient = 0,
                DefaultBookingAccountSupplier = 0,
                SupplierPaymentMethod = 0,
                VatType = DetermineVatType(isCompany, countryCode)
            };

            return request;
        }

        private static string? BuildStreetAndNumber(string? street, string? houseNumber, string? busNumber, string? fallback)
        {
            var parts = new[] { street, houseNumber, string.IsNullOrWhiteSpace(busNumber) ? null : busNumber }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var result = string.Join(" ", parts);

            if (string.IsNullOrWhiteSpace(result))
                return fallback;

            return result;
        }

        private static string? FormatVatNumberForOctopus(string? vatNumber, string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                return null;

            var cleanedDigits = new string(vatNumber.Where(char.IsDigit).ToArray());
            if (cleanedDigits.Length >= 10)
            {
                var digits = cleanedDigits[^10..];
                var prefix = string.IsNullOrWhiteSpace(countryCode) ? "BE" : countryCode.Trim().ToUpperInvariant();
                return $"{prefix}{digits[..4]}.{digits.Substring(4, 3)}.{digits.Substring(7, 3)}";
            }

            return vatNumber;
        }

        private static int? DetermineVatType(bool isCompany, string? countryCode)
        {
            var isBelgian = string.Equals(countryCode, "BE", StringComparison.OrdinalIgnoreCase);

            if (isCompany)
                return isBelgian ? 1 : 4;

            return isBelgian ? 7 : 8;
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
            return View("EditProject", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProject(EditClientModel viewmodel)
        {
            var Referrer = TempData["Referrer"];
            if (!ModelState.IsValid || viewmodel.Client.Id == 0)
            {
                FillInAddSelectListsEdit(ref viewmodel);
                return View("EditProject", viewmodel);
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
                if (model.SelectedPostalcode.CountryId == 0)
                {
                    model.SelectedPostalcode.CountryId = defaultCountry.ID;
                }

                if (model.SelectedInvoicePostalcode.CountryId == 0)
                {
                    model.SelectedInvoicePostalcode.CountryId = defaultCountry.ID;
                }
            }

            var ownerTypeService = ServiceFactory.GetClientService();
            var ownerTypeResponse = ownerTypeService.GetOwnerTypesForSelect();
            if (ownerTypeResponse.Success)
            {
                model.OwnerTypes = ownerTypeResponse.Values;
            }
        }


        // KLANT VERWIJDEREN
        [CPMCore.Filters.PermissionDelete(PermissionCodes.Customers)]
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

        [HttpGet]
        public async Task<IActionResult> ValidateVat(string vatNumber, string vatCountryCode, CancellationToken ct)
        {
            var normalized = CombineEnterpriseNumber(vatCountryCode, vatNumber);

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return BadRequest(new { error = "Ongeldig ondernemingsnummer" });
            }

            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                using var response = await httpClient.GetAsync($"https://controleerbtwnummer.eu/api/validate/{Uri.EscapeDataString(normalized)}.json", ct);
                var rawContent = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = string.IsNullOrWhiteSpace(rawContent)
                        ? "De controle van het btw-nummer is mislukt."
                        : rawContent;

                    return StatusCode((int)response.StatusCode, new { error = errorMessage });
                }

                VatLookupResponse? payload;

                try
                {
                    payload = JsonSerializer.Deserialize<VatLookupResponse>(rawContent, VatLookupSerializerOptions);
                }
                catch (JsonException jsonEx)
                {
                    return BadRequest(new { error = $"Het antwoord van de btw-service kon niet worden gelezen: {jsonEx.Message}" });
                }

                if (payload == null)
                {
                    return BadRequest(new { error = "Ontving een leeg antwoord van de btw-service." });
                }

                payload.address ??= new VatLookupAddress();

                if (string.IsNullOrWhiteSpace(payload.countryCode))
                {
                    payload.countryCode = payload.address.countryCode;
                }

                return Json(payload);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                return StatusCode(504, new { error = "De btw-service heeft niet tijdig geantwoord. Probeer het later opnieuw." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FindPostalMatch(string postalCode, string city, string? countryIso, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
            {
                return BadRequest();
            }

            var trimmedPostal = postalCode.Trim();
            var trimmedCity = city?.Trim();
            var normalizedIso = countryIso?.Trim().ToUpperInvariant();

            var baseQuery = _db.PostalCode
                .Include(p => p.Country)
                .AsNoTracking()
                .Where(p => p.Postcode == trimmedPostal);

            if (!string.IsNullOrWhiteSpace(normalizedIso))
            {
                baseQuery = baseQuery.Where(p => p.Country != null && p.Country.LandIsocode != null && p.Country.LandIsocode.ToUpper() == normalizedIso);
            }

            var candidates = await baseQuery
                .OrderBy(p => p.Gemeente)
                .ToListAsync(ct);

            if (candidates.Count == 0)
            {
                return NotFound();
            }

            PostalCode? match = null;

            if (!string.IsNullOrWhiteSpace(trimmedCity))
            {
                match = candidates.FirstOrDefault(p => string.Equals(p.Gemeente?.Trim(), trimmedCity, StringComparison.OrdinalIgnoreCase));
            }

            match ??= candidates.First();

            return Json(new { id = match.PostcodeId, text = $"{match.Postcode} - {match.Gemeente}", countryId = match.CountryId });
        }

        private static string? CombineEnterpriseNumber(string? countryCode, string? number)
        {
            var sanitizedNumber = SanitizeVatPart(number);
            if (string.IsNullOrWhiteSpace(sanitizedNumber))
            {
                return null;
            }

            var sanitizedCountry = SanitizeVatCountry(countryCode);
            return string.IsNullOrEmpty(sanitizedCountry) ? sanitizedNumber : sanitizedCountry + sanitizedNumber;
        }

        private static (string? CountryCode, string? NumberPart) SplitEnterpriseNumber(string? value)
        {
            var sanitized = SanitizeVatPart(value);

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return (null, null);
            }

            if (sanitized.Length >= 2 && char.IsLetter(sanitized[0]) && char.IsLetter(sanitized[1]))
            {
                var prefix = sanitized.Substring(0, 2);
                var remainder = sanitized.Substring(2);
                return (prefix, string.IsNullOrWhiteSpace(remainder) ? null : remainder);
            }

            return (null, sanitized);
        }


        private static string? NormalizeEnterpriseNumber(string? value)
        {
            var (_, numberPart) = SplitEnterpriseNumber(value);
            return string.IsNullOrWhiteSpace(numberPart) ? null : numberPart;
        }

        private static string? SanitizeVatPart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                }
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private static string? SanitizeVatCountry(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return null;
            }

            var builder = new StringBuilder(2);
            foreach (var ch in countryCode.Trim())
            {
                if (char.IsLetter(ch))
                {
                    builder.Append(char.ToUpperInvariant(ch));
                }

                if (builder.Length == 2)
                {
                    break;
                }
            }

            return builder.Length == 0 ? null : builder.ToString();
        }

        private sealed class VatLookupResponse
        {
            public bool valid { get; set; }

            public string vatNumber { get; set; } = string.Empty;

            public string name { get; set; } = string.Empty;

            public string countryCode { get; set; } = string.Empty;

            public VatLookupAddress? address { get; set; }

            [JsonPropertyName("strAddress")]
            public string? strAddress { get; set; }
        }

        private sealed class VatLookupAddress
        {
            public string street { get; set; } = string.Empty;

            public string number { get; set; } = string.Empty;

            [JsonPropertyName("zip_code")]
            public string zip { get; set; } = string.Empty;

            public string city { get; set; } = string.Empty;

            public string country { get; set; } = string.Empty;

            public string countryCode { get; set; } = string.Empty;
        }


        //WIJZIGNGSOPDRACHTEN
        public ActionResult DetailCO(int projectid, int clientid)
        {
            return RedirectToAction("DetailsChangeOrder", "Projecten", new { projectid, clientid });
        }

    }
}
