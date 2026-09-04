using BOCore;
using CPMCore.Attributes;
using CPMCore.Models;
using CPMCore.Models.Home;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Definition;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using ServiceCore.Invoicing.Pdf.Templates;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using SystemTextJsonSerializer = System.Text.Json.JsonSerializer;

namespace CPMCore.Controllers;


[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Settings)]
public class InstellingenController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IIssuerCompanyService _issuers;
    private readonly IIssuerBankAccountService _bank;
    private readonly IIssuerSeriesService _series;
    private readonly IInvoiceLayoutTemplateService _invoiceTemplates;
    private readonly IOctopusApiClient _octopusClient;
    private readonly IOctopusTokenManager _octopusTokens;
    private readonly IOctopusBookyearService _octopusBookyears;
    private readonly IOctopusRelationSyncService _octopusRelations;
    private readonly IActivityService _activityService;
    private readonly IProjectService _projectService;
    private readonly IKostprijsService _kostprijsService;
    private readonly ServiceCore.Budget.BouwIndexService _bouwIndex;
    private readonly ServiceCore.Budget.SIndexScraperService _sIndexScraper;
    private readonly ServiceCore.Budget.I2021SyncService _i2021Sync;
    private readonly CPMCore.Services.IMarketDataStatusService _marketDataStatus;
    private readonly ServiceCore.Budget.BudgetActivityFormuleService _budgetFormules;

    private static readonly JsonSerializerOptions LayoutSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly string LayoutDefaultsJson =
        System.Text.Json.JsonSerializer.Serialize(new
        {
            layoutA = DefaultLayouts.LayoutA,
        layoutB = DefaultLayouts.LayoutB
    }, LayoutSerializerOptions);

    private static readonly string LayoutSchemaJson = LayoutSchemaProvider.GetSchemaJson();

    public InstellingenController(ILogger<HomeController> logger, IIssuerCompanyService issuers, IIssuerBankAccountService bank, IIssuerSeriesService series, IInvoiceLayoutTemplateService invoiceTemplates, IOctopusApiClient octopusClient, IOctopusTokenManager octopusTokens, IOctopusBookyearService octopusBookyears, IOctopusRelationSyncService octopusRelations, IActivityService activityService, IProjectService projectService, IKostprijsService kostprijsService, ServiceCore.Budget.BouwIndexService bouwIndex, ServiceCore.Budget.SIndexScraperService sIndexScraper, ServiceCore.Budget.I2021SyncService i2021Sync, CPMCore.Services.IMarketDataStatusService marketDataStatus, ServiceCore.Budget.BudgetActivityFormuleService budgetFormules)
    {
        _logger = logger;
        _issuers = issuers;
        _bank = bank;
        _series = series;
        _invoiceTemplates = invoiceTemplates;
        _octopusClient = octopusClient;
        _octopusTokens = octopusTokens;
        _octopusBookyears = octopusBookyears;
        _octopusRelations = octopusRelations;
        _activityService = activityService;
        _projectService = projectService;
        _kostprijsService = kostprijsService;
        _bouwIndex = bouwIndex;
        _sIndexScraper = sIndexScraper;
        _i2021Sync = i2021Sync;
        _marketDataStatus = marketDataStatus;
        _budgetFormules = budgetFormules;
    }

    [HttpGet]
    [Breadcrumb("Instellingen")]
    public IActionResult Index()
    {
        SetPageHeader("bx bx-cog", "Instellingen");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };

        ViewData["BreadcrumbNode"] = instellingenIndex;
        return View();
    }


   



    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsActivities)]
    [Breadcrumb("Activiteiten")]
    public IActionResult Activities()
    {
        SetPageHeader("bx bx-cog", "Activiteiten");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };
        var instellingenActivities = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Activities", "Instellingen", "Activiteiten")
        {
            Parent = instellingenIndex
        };

        ViewData["BreadcrumbNode"] = instellingenActivities;

        var activitiesResponse = _activityService.GetActivities();
        var groupsResponse = _activityService.GetActivityGroups();

        var model = new ActivitySettingsViewModel
        {
            Activities = activitiesResponse.Values?.OrderBy(a => a.Name).ToList() ?? new List<ActivityBO>(),
            ActivityGroups = groupsResponse.Values?.OrderBy(g => g.Lot).ThenBy(g => g.Name).ToList() ?? new List<ActivityGroupBO>()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityCreate(string name, int groupId)
    {
        if (groupId <= 0)
        {
            TempData["Error"] = "Selecteer een activiteitgroep.";
            return RedirectToAction(nameof(Activities));
        }

        var activity = new ActivityBO
        {
            Name = name?.Trim() ?? string.Empty,
            Group = new ActivityGroupBO { ID = groupId }
        };

        var response = _activityService.InsertUpdate(activity);
        SetActivityResponseMessage(response, "Activiteit toegevoegd.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityUpdate(int id, string name, int groupId)
    {
        if (groupId <= 0)
        {
            TempData["Error"] = "Selecteer een activiteitgroep.";
            return RedirectToAction(nameof(Activities));
        }

        var activity = new ActivityBO
        {
            ID = id,
            Name = name?.Trim() ?? string.Empty,
            Group = new ActivityGroupBO { ID = groupId }
        };

        var response = _activityService.InsertUpdate(activity);
        SetActivityResponseMessage(response, "Activiteit bijgewerkt.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityDelete(int id)
    {
        var response = _activityService.Delete(new List<int> { id });
        SetActivityResponseMessage(response, "Activiteit verwijderd.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityGroupCreate(string name, int lot)
    {
        var group = new ActivityGroupBO
        {
            Name = name?.Trim() ?? string.Empty,
            Lot = lot
        };

        var response = _activityService.InsertUpdateGroup(group);
        SetActivityResponseMessage(response, "Activiteitgroep toegevoegd.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityGroupUpdate(int id, string name, int lot)
    {
        var group = new ActivityGroupBO
        {
            ID = id,
            Name = name?.Trim() ?? string.Empty,
            Lot = lot
        };

        var response = _activityService.InsertUpdateGroup(group);
        SetActivityResponseMessage(response, "Activiteitgroep bijgewerkt.");

        return RedirectToAction(nameof(Activities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ActivityGroupDelete(int id)
    {
        var response = _activityService.DeleteGroup(new List<int> { id });
        SetActivityResponseMessage(response, "Activiteitgroep verwijderd.");

        return RedirectToAction(nameof(Activities));
    }

    //VAKANTIEDAGEN
    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsHolidays)]
    [Breadcrumb("Vakantiedagen")]
    public ActionResult Vacationdays()
    {
        SetPageHeader("bx bx-cog", "Vakantiedagen");

        HomeModel model = new HomeModel();

        return View(model);
    }
    [HttpGet]
    public IActionResult GetVacationDays()
    {
        var response = _projectService.GetVacationDaysGeneral();

        var rows = response.Success
            ? response.Values.Select(b => new
            {
                id = b.Id,
                title = "verlofdag",
                year = b.VacationDay.Year,
                month = b.VacationDay.Month,
                day = b.VacationDay.Day
            }).ToArray()
            : Array.Empty<object>();

        return Json(rows); // of: return Ok(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public int AddVacationDay(DateOnly dag)
    {
        var day = new VacationDayBO { VacationDay = dag };
        var response = _projectService.InsertUpdateVacationDay(day);

        if (!response.Success) return 0;

        var idMsg = response.Messages.FirstOrDefault(m => m.Type == MessageType.Value)?.Message
                    ?? response.Messages.FirstOrDefault()?.Message;

        return int.TryParse(idMsg, out var id) ? id : 0;
    }

    [HttpPost]
    public bool DeleteVacationDay(int id)
    {
        var ids = new List<int> { id };
        var response = _projectService.DeleteVacationDays(ids);
        return response.Success;
    }

    //MIJN BEDRIJVEN
    // GET /Admin/IssuerCompanies
    [HttpGet("IssuerCompanies")]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsBillingCompanies)]
    public async Task<IActionResult> IssuerCompanies()
    {
        SetPageHeader("bx bx-cog", "Mijn bedrijven");

        var list = await _issuers.GetAllAsync();
        var vms = list.Select(x => new IssuerCompanyVM
        {
            Id = x.Id,
            Name = x.Name,
            LegalName = x.LegalName,
            VatNumber = x.VatNumber,
            EnterpriseNumber = x.EnterpriseNumber,
            AddressLine1 = x.AddressLine1,
            AddressLine2 = x.AddressLine2,
            PostalCode = x.PostalCode,
            City = x.City,
            CountryCode = x.CountryCode,
            Email = x.Email,
            Phone = x.Phone,
            Phone2 = x.Phone2,
            Website = x.Website,
            LogoPath = x.LogoPath,
            TemplateKey = x.TemplateKey,
            TemplateJson = x.TemplateJson,
            BrandPrimaryColor = x.BrandPrimaryColor,
            BrandSecondaryColor = x.BrandSecondaryColor,
            FontFamily = x.FontFamily,
            LogoBytes = x.LogoBytes,
            DefaultPaymentTermId = x.DefaultPaymentTermId,
            DefaultVatTypeId = x.DefaultVatTypeId,
            IsActive = x.IsActive,
            EInvoiceEnabled = x.EInvoiceEnabled,
            PeppolParticipantId = x.PeppolParticipantId,
            UblAttachPdf = x.UblAttachPdf,
            EmailSubjectTemplate = x.EmailSubjectTemplate,
            EmailBodyTemplate = x.EmailBodyTemplate,
            EmailPaidBodyTemplate = x.EmailPaidBodyTemplate,
            InvoiceFooterHtml = x.InvoiceFooterHtml,
            DefaultLanguage = x.DefaultLanguage,
            DefaultCurrency = x.DefaultCurrency,
            InvoiceNumberPattern = x.InvoiceNumberPattern,
            EpcQrEnabled = x.EpcQrEnabled,
            EpcBeneficiaryName = x.EpcBeneficiaryName,
            EpcIban = x.EpcIban,
            EpcBic = x.EpcBic,
            EpcRemittanceType = x.EpcRemittanceType,
            EpcRemittanceTemplate = x.EpcRemittanceTemplate,
            FooterLegalText = x.FooterLegalText,
            PeppolEnabled = x.PeppolEnabled,

        }).ToList();

        //BREADCRUMBS
        var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = Index,
        };
        var InstellingenIssuer = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("IssuerCompanies", "Instellingen", "Mijn Bedrijven")
        {
            Parent = instellingenIndex,
        };
      
        ViewData["BreadcrumbNode"] = InstellingenIssuer;

        return View(vms);
    }

    // GET /Admin/IssuerCompanies/Create
    [HttpGet("IssuerCompanies/Create")]
    public async Task<IActionResult> IssuerCompaniesCreate()
    {
        var referrer = Request.Headers["Referer"].ToString();
        // Use the referrer URL as needed
        TempData["Referrer"] = referrer;
        ViewBag.PaymentTerms = await _issuers.GetPaymentTermOptionsAsync();
        ViewBag.CompanyLegalForms = await _issuers.ListLegalFormsAsync();
        var templates = await GetInvoiceTemplateOptionsAsync();


        //BREADCRUMBS
        var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = Index,
        };
        var InstellingenIssuer = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("IssuerCompanies", "Instellingen", "Mijn Bedrijven")
        {
            Parent = instellingenIndex,
        };
        var InstellingenNewIssuer = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("IssuerCompaniesCreate", "Instellingen", "Nieuw")
        {
            Parent = InstellingenIssuer,
        };

        ViewData["BreadcrumbNode"] = InstellingenNewIssuer;

        var vm = new IssuerCompanyVM
        {
            InvoiceTemplates = templates
        };
        ApplyInvoiceTemplateDefaults(vm, templates);

        await PopulateInvoiceLayoutViewDataAsync();
        SetPageHeader("bx bx-cog", "Nieuw bedrijf");
        return View(vm);
    }

    // POST /Admin/IssuerCompanies/Create
    [HttpPost("IssuerCompanies/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompaniesCreate(IssuerCompanyVM vm)
    {
        ValidateTemplateJson(vm.TemplateJson);
        if (!ModelState.IsValid)
        {
            ViewBag.PaymentTerms = await _issuers.GetPaymentTermOptionsAsync();
            ViewBag.CompanyLegalForms = await _issuers.ListLegalFormsAsync();
            var templates = await GetInvoiceTemplateOptionsAsync();
            vm.InvoiceTemplates = templates;
            ApplyInvoiceTemplateDefaults(vm, templates);
            await PopulateInvoiceLayoutViewDataAsync();
            SetPageHeader("bx bx-cog", "Nieuw bedrijf");
            return View(vm);
        }
        if (vm.LogoUpload != null && vm.LogoUpload.Length > 0)
        {
            using var ms = new MemoryStream();
            await vm.LogoUpload.CopyToAsync(ms);
            vm.LogoBytes = ms.ToArray();
            vm.LogoPath = vm.LogoUpload.FileName;
        }

        var bo = new IssuerCompanyBO
        {
            Name = vm.Name,
            LegalName = vm.LegalName,
            VatNumber = vm.VatNumber,
            EnterpriseNumber = vm.EnterpriseNumber,
            AddressLine1 = vm.AddressLine1,
            AddressLine2 = vm.AddressLine2,
            PostalCode = vm.PostalCode,
            City = vm.City,
            CountryCode = vm.CountryCode,
            Email = vm.Email,
            Phone = vm.Phone,
            Phone2 = vm.Phone2,
            LogoPath = vm.LogoPath,
            Website = vm.Website,
            TemplateKey = vm.TemplateKey,
            TemplateJson = vm.TemplateJson,
            BrandPrimaryColor = vm.BrandPrimaryColor,
            BrandSecondaryColor = vm.BrandSecondaryColor,
            FontFamily = vm.FontFamily,
            LogoBytes = vm.LogoBytes,
            DefaultPaymentTermId = vm.DefaultPaymentTermId,
            DefaultVatTypeId = vm.DefaultVatTypeId,
            IsActive = vm.IsActive,
            IsExternalCoordinationDefault = vm.IsExternalCoordinationDefault,
            EInvoiceEnabled = vm.EInvoiceEnabled,
            PeppolParticipantId = vm.PeppolParticipantId,
            UblAttachPdf = vm.UblAttachPdf,
            EmailSubjectTemplate = vm.EmailSubjectTemplate,
            EmailBodyTemplate = vm.EmailBodyTemplate,
            EmailPaidBodyTemplate = vm.EmailPaidBodyTemplate,
            InvoiceSendEmail = vm.InvoiceSendEmail,
            InvoiceFooterHtml = vm.InvoiceFooterHtml,
            DefaultLanguage = vm.DefaultLanguage,
            DefaultCurrency = vm.DefaultCurrency,
            InvoiceNumberPattern = vm.InvoiceNumberPattern,
            FooterLegalText = vm.FooterLegalText,
            PeppolEnabled = vm.PeppolEnabled,
            EpcQrEnabled = vm.EpcQrEnabled,
            EpcBeneficiaryName = vm.EpcBeneficiaryName,
            EpcIban = vm.EpcIban,
            EpcBic = vm.EpcBic,
            EpcRemittanceType = vm.EpcRemittanceType,
            EpcRemittanceTemplate = vm.EpcRemittanceTemplate,
            CompanyLegalFormId = vm.CompanyLegalFormId,
            OctopusUsername = vm.OctopusUsername,
            OctopusPassword = vm.OctopusPassword,
            OctopusAuthenticateToken = vm.OctopusAuthenticateToken,
            OctopusAuthenticateTokenValidUntil = vm.OctopusAuthenticateTokenValidUntil,
            OctopusDossierToken = vm.OctopusDossierToken,
            OctopusDossierTokenValidUntil = vm.OctopusDossierTokenValidUntil,
            OctopusDossierNumber = vm.OctopusDossierNumber,
            OctopusCustomFieldsJson = vm.OctopusCustomFieldsJson,
            OctopusCustomFieldMappingsJson = BuildCustomFieldMappingJson(vm.CustomFieldMappings),
            OctopusDownloadLinkCustomFieldKeyId = vm.OctopusDownloadLinkCustomFieldKeyId,
            OctopusPurchaseJournalKeyPrefix = vm.OctopusPurchaseJournalKeyPrefix,
        };
        await _issuers.CreateAsync(bo);
        AddMessage("success", "Mijn bedrijf " + vm.Name + " toegevoegd.", "Geslaagd!");
        return RedirectToAction(nameof(IssuerCompanies));
    }

    // GET /Admin/IssuerCompanies/Edit/5
    [HttpGet("IssuerCompanies/Edit/{id:int}")]
    public async Task<IActionResult> IssuerCompaniesEdit(int id, CancellationToken ct)
    {
        var referrer = Request.Headers["Referer"].ToString();
        // Use the referrer URL as needed
        TempData["Referrer"] = referrer;
        var bo = await _issuers.GetAsync(id, ct);
        if (bo == null) return NotFound();

        ViewBag.PaymentTerms = await _issuers.GetPaymentTermOptionsAsync(id, ct);
        ViewBag.BankAccounts = await _bank.ListByIssuerAsync(id, ct);
        ViewBag.InvoiceSeries = await _series.ListByIssuerAsync(id, ct);
        ViewBag.CompanyLegalForms = await _issuers.ListLegalFormsAsync(ct);
        ViewBag.OctopusBookyears = await _octopusBookyears.ListByIssuerAsync(id, ct);
        ViewBag.OctopusVatCodes = await _issuers.ListVatTypeAsync(id, ct);
        ViewBag.InvoiceFieldOptions = GetInvoiceFieldOptions();
        ViewBag.OctopusRelationSuggestions = ReadOctopusSuggestions();

        var customFields = await _issuers.ListOctopusCustomFieldsAsync(id, ct);
        var storedMappings = DeserializeCustomFieldMappings(bo?.OctopusCustomFieldMappingsJson);
        var vatTypes = (ViewBag.OctopusVatCodes as IReadOnlyList<VatTypeBO> ?? Array.Empty<VatTypeBO>()).ToList();
        var paymentTerms = await _issuers.ListPaymentTermsAsync(id, ct);
        var templates = await GetInvoiceTemplateOptionsAsync(ct);

        var vm = new IssuerCompanyVM
        {
            Id = bo.Id,
            Name = bo.Name,
            LegalName = bo.LegalName,
            VatNumber = bo.VatNumber,
            EnterpriseNumber = bo.EnterpriseNumber,
            AddressLine1 = bo.AddressLine1,
            AddressLine2 = bo.AddressLine2,
            PostalCode = bo.PostalCode,
            City = bo.City,
            CountryCode = bo.CountryCode,
            Email = bo.Email,
            Phone = bo.Phone,
            Phone2 = bo.Phone2,
            Website = bo.Website,
            LogoPath = bo.LogoPath,
            TemplateKey = bo.TemplateKey,
            TemplateJson = bo.TemplateJson,
            BrandPrimaryColor = bo.BrandPrimaryColor,
            BrandSecondaryColor = bo.BrandSecondaryColor,
            FontFamily = bo.FontFamily,
            LogoBytes = bo.LogoBytes,
            DefaultPaymentTermId = bo.DefaultPaymentTermId,
            DefaultVatTypeId = bo.DefaultVatTypeId,
            IsActive = bo.IsActive,
            IsExternalCoordinationDefault = bo.IsExternalCoordinationDefault,
            EInvoiceEnabled = bo.EInvoiceEnabled,
            PeppolParticipantId = bo.PeppolParticipantId,
            UblAttachPdf = bo.UblAttachPdf,
            EmailSubjectTemplate = bo.EmailSubjectTemplate,
            EmailBodyTemplate = bo.EmailBodyTemplate,
            EmailPaidBodyTemplate = bo.EmailPaidBodyTemplate,
            InvoiceSendEmail = bo.InvoiceSendEmail,
            InvoiceFooterHtml = bo.InvoiceFooterHtml,
            DefaultLanguage = bo.DefaultLanguage,
            DefaultCurrency = bo.DefaultCurrency,
            InvoiceNumberPattern = bo.InvoiceNumberPattern,
            FooterLegalText = bo.FooterLegalText,
            PeppolEnabled = bo.PeppolEnabled,
            EpcQrEnabled = bo.EpcQrEnabled,
            EpcBeneficiaryName = bo.EpcBeneficiaryName,
            EpcIban = bo.EpcIban,
            EpcBic = bo.EpcBic,
            EpcRemittanceType = bo.EpcRemittanceType,
            EpcRemittanceTemplate = bo.EpcRemittanceTemplate,
            CompanyLegalFormId = bo.CompanyLegalFormId,
            CompanyLegalFormAbbreviation = bo.CompanyLegalFormAbbreviation,
            OctopusUsername = bo.OctopusUsername,
            OctopusPassword = bo.OctopusPassword,
            OctopusAuthenticateToken = bo.OctopusAuthenticateToken,
            OctopusAuthenticateTokenValidUntil = bo.OctopusAuthenticateTokenValidUntil,
            OctopusDossierToken = bo.OctopusDossierToken,
            OctopusDossierTokenValidUntil = bo.OctopusDossierTokenValidUntil,
            OctopusDossierNumber = bo.OctopusDossierNumber,
            OctopusCustomFieldsJson = bo.OctopusCustomFieldsJson,
            OctopusCustomFieldMappingsJson = bo.OctopusCustomFieldMappingsJson,
            OctopusDownloadLinkCustomFieldKeyId = bo.OctopusDownloadLinkCustomFieldKeyId,
            CustomFieldMappings = BuildCustomFieldMappings(customFields, storedMappings),
            VatTypes = vatTypes.Select(v => new VatTypeVM
            {
                Id = v.Id,
                BasePercentage = v.BasePercentage,
                Code = v.Code,
                Description = v.Description,
                Type = v.Type,
                DefaultSellBookingAccountNr = v.DefaultSellBookingAccountNr,
                InvoiceMention = v.InvoiceMention
            }).ToList(),
            PaymentTerms = paymentTerms
                .Where(t => t.IssuerCompanyId == id)
                .Select(t => new PaymentTermVM
                {
                    Id = t.Id,
                    Name = t.Name,
                    Days = t.Days,
                    Description = t.Description,
                    TermType = t.TermType,
                    DisplayMode = t.DisplayMode,
                    DisplayText = t.DisplayText
                })
                .ToList()
        };
        vm.InvoiceTemplates = templates;
        ApplyInvoiceTemplateDefaults(vm, templates);

        // Standaard uurtarieven laden
        var userRates = await _issuers.GetUserRatesAsync(id, ct);
        vm.UserRates = userRates.Select(r => new IssuerUserRateVM
        {
            UserId = r.UserId,
            UserFullName = r.UserFullName,
            HourlyRate = r.HourlyRate
        }).ToList();

        // RatePerKm uit de BO (is al in GetAsync gemapt)
        vm.RatePerKm = bo.RatePerKm;

        // Beschikbare gebruikers voor de dropdown (via _projectService context)
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DALCore.Models.cpmRunningContext>();
        vm.AvailableUsers = db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && !u.UserCompanyAccess.Any())
            .OrderBy(u => u.Familienaam).ThenBy(u => u.Voornaam)
            .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = u.UserId,
                Text = (u.Voornaam + " " + u.Familienaam).Trim()
            })
            .ToList();

        if (TempData["OctopusDossiers"] is string dossiersJson && !string.IsNullOrWhiteSpace(dossiersJson))
        {
            ViewBag.OctopusDossiers = JsonConvert.DeserializeObject<List<OctopusDossierItem>>(dossiersJson) ?? new List<OctopusDossierItem>();
        }
        ViewBag.OctopusMessage = TempData["OctopusMessage"] as string;
        ViewBag.OctopusMessageType = TempData["OctopusMessageType"] as string;

        //BREADCRUMBS
        var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = Index,
        };
        var InstellingenIssuer = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("IssuerCompanies", "Instellingen", "Mijn Bedrijven")
        {
            Parent = instellingenIndex,
        };
        var InstellingenEditIssuer = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("IssuerCompaniesEdit", "Instellingen", "Bewerken " + vm.Name)
        {
            Parent = InstellingenIssuer,
        };

        ViewData["BreadcrumbNode"] = InstellingenEditIssuer;

        await PopulateInvoiceLayoutViewDataAsync(ct);
        SetPageHeader("bx bx-cog", $"Bedrijf bewerken — {vm.Name}");
        return View(vm);
    }

    // POST /Instellingen/IssuerCompanies/Edit/5
    [HttpPost("IssuerCompanies/Edit/{id:int}", Name = "Instellingen_IssuerCompanies_Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompaniesEdit(int id, IssuerCompanyVM vm, CancellationToken ct)
    {
        if (id != vm.Id) return BadRequest();
        ValidateTemplateJson(vm.TemplateJson);
        if (!ModelState.IsValid)
        {
            ViewBag.PaymentTerms = await _issuers.GetPaymentTermOptionsAsync(id, ct);
            ViewBag.BankAccounts = await _bank.ListByIssuerAsync(id);
            ViewBag.InvoiceSeries = await _series.ListByIssuerAsync(id);
            ViewBag.CompanyLegalForms = await _issuers.ListLegalFormsAsync();
            ViewBag.OctopusVatCodes = await _issuers.ListVatTypeAsync(id);
            ViewBag.InvoiceFieldOptions = GetInvoiceFieldOptions();
            vm.CustomFieldMappings ??= new List<OctopusCustomFieldMappingVM>();
            vm.VatTypes = vm.VatTypes?.Any() == true
                ? vm.VatTypes
                : (ViewBag.OctopusVatCodes as IReadOnlyList<VatTypeBO> ?? Array.Empty<VatTypeBO>())
                    .Select(v => new VatTypeVM
                    {
                        Id = v.Id,
                        BasePercentage = v.BasePercentage,
                        Code = v.Code,
                        Description = v.Description,
                        Type = v.Type,
                        DefaultSellBookingAccountNr = v.DefaultSellBookingAccountNr,
                        InvoiceMention = v.InvoiceMention
                    })
                    .ToList();
            vm.PaymentTerms ??= new List<PaymentTermVM>();
            var templates = await GetInvoiceTemplateOptionsAsync(ct);
            vm.InvoiceTemplates = templates;
            ApplyInvoiceTemplateDefaults(vm, templates);
            await PopulateInvoiceLayoutViewDataAsync(ct);
            SetPageHeader("bx bx-cog", $"Bedrijf bewerken — {vm.Name}");
            return View(vm);
        }
        if (vm.LogoUpload != null && vm.LogoUpload.Length > 0)
        {
            using var ms = new MemoryStream();
            await vm.LogoUpload.CopyToAsync(ms);
            vm.LogoBytes = ms.ToArray();
            if (string.IsNullOrWhiteSpace(vm.LogoPath))
            {
                vm.LogoPath = vm.LogoUpload.FileName;
            }
        }
        else
        {
            var existing = await _issuers.GetAsync(vm.Id);
            if (existing != null)
            {
                vm.LogoBytes ??= existing.LogoBytes;
                if (string.IsNullOrWhiteSpace(vm.LogoPath))
                {
                    vm.LogoPath = existing.LogoPath;
                }
            }
        }

        var bo = new IssuerCompanyBO
        {
            Id = vm.Id,
            Name = vm.Name,
            LegalName = vm.LegalName,
            VatNumber = vm.VatNumber,
            EnterpriseNumber = vm.EnterpriseNumber,
            AddressLine1 = vm.AddressLine1,
            AddressLine2 = vm.AddressLine2,
            PostalCode = vm.PostalCode,
            City = vm.City,
            CountryCode = vm.CountryCode,
            Email = vm.Email,
            Phone = vm.Phone,
            Phone2 = vm.Phone2,
            LogoPath = vm.LogoPath,
            Website = vm.Website,
            TemplateKey = vm.TemplateKey,
            TemplateJson = vm.TemplateJson,
            BrandPrimaryColor = vm.BrandPrimaryColor,
            BrandSecondaryColor = vm.BrandSecondaryColor,
            FontFamily = vm.FontFamily,
            LogoBytes = vm.LogoBytes,
            DefaultPaymentTermId = vm.DefaultPaymentTermId,
            DefaultVatTypeId = vm.DefaultVatTypeId,
            IsActive = vm.IsActive,
            IsExternalCoordinationDefault = vm.IsExternalCoordinationDefault,
            EInvoiceEnabled = vm.EInvoiceEnabled,
            PeppolParticipantId = vm.PeppolParticipantId,
            UblAttachPdf = vm.UblAttachPdf,
            EmailSubjectTemplate = vm.EmailSubjectTemplate,
            EmailBodyTemplate = vm.EmailBodyTemplate,
            EmailPaidBodyTemplate = vm.EmailPaidBodyTemplate,
            InvoiceSendEmail = vm.InvoiceSendEmail,
            InvoiceFooterHtml = vm.InvoiceFooterHtml,
            DefaultLanguage = vm.DefaultLanguage,
            DefaultCurrency = vm.DefaultCurrency,
            InvoiceNumberPattern = vm.InvoiceNumberPattern,
            FooterLegalText = vm.FooterLegalText,
            PeppolEnabled = vm.PeppolEnabled,
            EpcQrEnabled = vm.EpcQrEnabled,
            EpcBeneficiaryName = vm.EpcBeneficiaryName,
            EpcIban = vm.EpcIban,
            EpcBic = vm.EpcBic,
            EpcRemittanceType = vm.EpcRemittanceType,
            EpcRemittanceTemplate = vm.EpcRemittanceTemplate,
            CompanyLegalFormId = vm.CompanyLegalFormId,
            OctopusUsername = vm.OctopusUsername,
            OctopusPassword = vm.OctopusPassword,
            OctopusAuthenticateToken = vm.OctopusAuthenticateToken,
            OctopusAuthenticateTokenValidUntil = vm.OctopusAuthenticateTokenValidUntil,
            OctopusDossierToken = vm.OctopusDossierToken,
            OctopusDossierTokenValidUntil = vm.OctopusDossierTokenValidUntil,
            OctopusDossierNumber = vm.OctopusDossierNumber,
            OctopusCustomFieldsJson = vm.OctopusCustomFieldsJson,
            OctopusCustomFieldMappingsJson = BuildCustomFieldMappingJson(vm.CustomFieldMappings),
            OctopusDownloadLinkCustomFieldKeyId = vm.OctopusDownloadLinkCustomFieldKeyId,
            OctopusPurchaseJournalKeyPrefix = vm.OctopusPurchaseJournalKeyPrefix,
        };
        await _issuers.UpdateAsync(bo, ct);

        var vatTypes = vm.VatTypes ?? new List<VatTypeVM>();
        if (vatTypes.Count > 0)
        {
            await _issuers.SyncVatTypesAsync(
                vm.Id,
                vatTypes.Select(v => new VatTypeBO
                {
                    Id = v.Id,
                    IssuerCompanyId = vm.Id,
                    Code = v.Code,
                    Description = v.Description,
                    Type = v.Type,
                    BasePercentage = v.BasePercentage,
                    DefaultSellBookingAccountNr = v.DefaultSellBookingAccountNr,
                    InvoiceMention = v.InvoiceMention
                }),
                ct);
        }
        var paymentTerms = vm.PaymentTerms ?? new List<PaymentTermVM>();
        await _issuers.SyncPaymentTermsAsync(
            vm.Id,
            paymentTerms.Select(t => new PaymentTermBO
            {
                Id = t.Id,
                Name = t.Name ?? string.Empty,
                Days = t.Days,
                Description = t.Description,
                TermType = t.TermType,
                DisplayMode = t.DisplayMode,
                DisplayText = t.DisplayText,
                IsDeleted = t.IsDeleted,
                IssuerCompanyId = vm.Id
            }),
            ct);

        // Standaard uurtarieven per gebruiker opslaan
        await _issuers.SyncUserRatesAsync(
            vm.Id,
            (vm.UserRates ?? new List<IssuerUserRateVM>()).Select(r => new IssuerCompanyUserRateBO
            {
                IssuerCompanyId = vm.Id,
                UserId = r.UserId,
                HourlyRate = r.HourlyRate
            }),
            ct);

        // Km-tarief opslaan
        await _issuers.UpdateRatePerKmAsync(vm.Id, vm.RatePerKm, ct);

        AddMessage("success", "Mijn bedrijf " +vm.Name + " opgeslagen.", "Geslaagd!");
        return RedirectToAction(nameof(IssuerCompanies));
    }

    // GET /Admin/InvoiceTemplates
    [HttpGet("InvoiceTemplates")]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsInvoiceTemplates)]
    public async Task<IActionResult> InvoiceTemplates(CancellationToken ct)
    {
        await _invoiceTemplates.EnsureDefaultsAsync(ct);
        var templates = await _invoiceTemplates.ListAsync(ct);
        var vms = templates.Select(t => new InvoiceLayoutTemplateVM
        {
            Id = t.Id,
            Key = t.Key,
            Name = t.Name,
            Description = t.Description,
            TemplateJson = t.TemplateJson,
            IsSystem = t.IsSystem
        }).ToList();

        var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = index
        };
        var templatesNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("InvoiceTemplates", "Instellingen", "Factuurtemplates")
        {
            Parent = instellingenIndex
        };
        ViewData["BreadcrumbNode"] = templatesNode;

        SetPageHeader("bx bx-cog", "Factuurtemplates");
        return View(vms);
    }

    // GET /Admin/InvoiceTemplates/Create
    [HttpGet("InvoiceTemplates/Create")]
    public IActionResult InvoiceTemplatesCreate()
    {
        var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = index
        };
        var templatesNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("InvoiceTemplates", "Instellingen", "Factuurtemplates")
        {
            Parent = instellingenIndex
        };
        var createNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("InvoiceTemplatesCreate", "Instellingen", "Nieuw template")
        {
            Parent = templatesNode
        };
        ViewData["BreadcrumbNode"] = createNode;

        SetPageHeader("bx bx-cog", "Nieuw factuurtemplate");
        return View(new InvoiceLayoutTemplateVM
        {
            TemplateJson = DefaultLayouts.LayoutA
        });
    }

    // POST /Admin/InvoiceTemplates/Create
    [HttpPost("InvoiceTemplates/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InvoiceTemplatesCreate(InvoiceLayoutTemplateVM vm, CancellationToken ct)
    {
        ValidateTemplateJson(vm.TemplateJson, nameof(InvoiceLayoutTemplateVM.TemplateJson));
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-cog", "Nieuw factuurtemplate");
            return View(vm);
        }

        try
        {
            await _invoiceTemplates.CreateAsync(new InvoiceLayoutTemplateBO
            {
                Key = vm.Key,
                Name = vm.Name,
                Description = vm.Description,
                TemplateJson = vm.TemplateJson,
                IsSystem = false
            }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            ModelState.AddModelError(nameof(InvoiceLayoutTemplateVM.Key), "Deze sleutel bestaat al.");
            return View(vm);
        }

        AddMessage("success", $"Template {vm.Name} toegevoegd.", "Geslaagd!");
        return RedirectToAction(nameof(InvoiceTemplates));
    }

    // GET /Admin/InvoiceTemplates/Edit/5
    [HttpGet("InvoiceTemplates/Edit/{id:int}")]
    public async Task<IActionResult> InvoiceTemplatesEdit(int id, CancellationToken ct)
    {
        var template = await _invoiceTemplates.GetAsync(id, ct);
        if (template == null)
            return NotFound();

        var index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = index
        };
        var templatesNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("InvoiceTemplates", "Instellingen", "Factuurtemplates")
        {
            Parent = instellingenIndex
        };
        var editNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("InvoiceTemplatesEdit", "Instellingen", "Bewerk template")
        {
            Parent = templatesNode
        };
        ViewData["BreadcrumbNode"] = editNode;

        SetPageHeader("bx bx-cog", $"Factuurtemplate bewerken — {template.Name}");
        return View(new InvoiceLayoutTemplateVM
        {
            Id = template.Id,
            Key = template.Key,
            Name = template.Name,
            Description = template.Description,
            TemplateJson = template.TemplateJson,
            IsSystem = template.IsSystem
        });
    }

    // POST /Admin/InvoiceTemplates/Edit/5
    [HttpPost("InvoiceTemplates/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InvoiceTemplatesEdit(int id, InvoiceLayoutTemplateVM vm, CancellationToken ct)
    {
        if (id != vm.Id)
            return BadRequest();

        ValidateTemplateJson(vm.TemplateJson, nameof(InvoiceLayoutTemplateVM.TemplateJson));
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-cog", $"Factuurtemplate bewerken — {vm.Name}");
            return View(vm);
        }

        try
        {
            await _invoiceTemplates.UpdateAsync(new InvoiceLayoutTemplateBO
            {
                Id = vm.Id,
                Key = vm.Key,
                Name = vm.Name,
                Description = vm.Description,
                TemplateJson = vm.TemplateJson,
                IsSystem = vm.IsSystem
            }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            ModelState.AddModelError(nameof(InvoiceLayoutTemplateVM.Key), "Deze sleutel bestaat al.");
            return View(vm);
        }

        AddMessage("success", $"Template {vm.Name} opgeslagen.", "Geslaagd!");
        return RedirectToAction(nameof(InvoiceTemplates));
    }

    // POST /Admin/InvoiceTemplates/Delete/5
    [HttpPost("InvoiceTemplates/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InvoiceTemplatesDelete(int id, CancellationToken ct)
    {
        var template = await _invoiceTemplates.GetAsync(id, ct);
        if (template == null)
            return NotFound();

        if (template.IsSystem)
        {
            AddMessage("warning", "Standaard templates kunnen niet verwijderd worden.", "Niet toegelaten");
            return RedirectToAction(nameof(InvoiceTemplates));
        }

        await _invoiceTemplates.DeleteAsync(id, ct);
        AddMessage("success", $"Template {template.Name} verwijderd.", "Geslaagd!");
        return RedirectToAction(nameof(InvoiceTemplates));
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Octopus/authenticate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompanyOctopusAuthenticate(int issuerId, OctopusAuthRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            TempData["OctopusMessage"] = "Gebruikersnaam en wachtwoord zijn verplicht.";
            TempData["OctopusMessageType"] = "danger";
            return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
        }

        var bo = await _issuers.GetAsync(issuerId, ct);
        if (bo == null)
            return NotFound();

        try
        {
            var authResult = await _octopusClient.AuthenticateAsync(request.Username!, request.Password!, ct);

            bo.OctopusUsername = request.Username;
            bo.OctopusPassword = request.Password;
            bo.OctopusAuthenticateToken = authResult;
            bo.OctopusAuthenticateTokenValidUntil = DateTime.UtcNow.AddMinutes(9);
            await _issuers.UpdateAsync(bo, ct);

            TempData["OctopusMessage"] = "Authenticatietoken opgehaald en opgeslagen.";
            TempData["OctopusMessageType"] = "success";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Octopus authenticatie mislukt voor issuer {IssuerId}", issuerId);
            TempData["OctopusMessage"] = $"Token aanvragen mislukt: {ex.Message}";
            TempData["OctopusMessageType"] = "danger";
        }

        return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Octopus/dossiers")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompanyOctopusDossiers(int issuerId, CancellationToken ct)
    {
        var bo = await _issuers.GetAsync(issuerId, ct);
        if (bo == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(bo.OctopusUsername) || string.IsNullOrWhiteSpace(bo.OctopusPassword))
        {
            TempData["OctopusMessage"] = "Octopus login ontbreekt. Vul gebruikersnaam en wachtwoord in en authenticatie opnieuw.";
            TempData["OctopusMessageType"] = "warning";
            return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
        }

        try
        {
            var dossiers = await _octopusTokens.GetDossiersWithRetryAsync(issuerId, ct);
            TempData["OctopusDossiers"] = JsonConvert.SerializeObject(dossiers);
            TempData["OctopusMessage"] = $"Dossierlijst geladen ({dossiers.Count}).";
            TempData["OctopusMessageType"] = "success";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Octopus dossiers laden mislukt voor issuer {IssuerId}", issuerId);
            TempData["OctopusMessage"] = $"Dossiers laden mislukt: {ex.Message}";
            TempData["OctopusMessageType"] = "danger";
        }

        return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Octopus/dossier")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompanyOctopusConnect(int issuerId, OctopusDossierRequest request, CancellationToken ct)
    {
        var bo = await _issuers.GetAsync(issuerId, ct);
        if (bo == null)
            return NotFound();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DossierNumber))
            {
                TempData["OctopusMessage"] = "Kies een dossier.";
                TempData["OctopusMessageType"] = "warning";
            }
            else
            {
                bo.OctopusDossierNumber = request.DossierNumber;
                await _issuers.UpdateAsync(bo, ct);

                TempData["OctopusMessage"] = "Dossiernummer opgeslagen.";
                TempData["OctopusMessageType"] = "success";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Octopus dossier koppelen mislukt voor issuer {IssuerId}", issuerId);
            TempData["OctopusMessage"] = $"Dossier koppelen mislukt: {ex.Message}";
            TempData["OctopusMessageType"] = "danger";
        }

        return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Octopus/bookyears")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompanyOctopusBookyears(int issuerId, CancellationToken ct)
        => await SyncOctopusDossierAsync(issuerId, ct);

    [HttpPost("IssuerCompanies/{issuerId:int}/Octopus/sync")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompanyOctopusSync(int issuerId, CancellationToken ct)
        => await SyncOctopusDossierAsync(issuerId, ct);

    [HttpPost("IssuerCompanies/{issuerId:int}/Octopus/relations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompanyOctopusRelations(int issuerId, CancellationToken ct)
    {
        try
        {
            var result = await _octopusRelations.SyncRelationsAsync(issuerId, ct);
            if (result.Suggestions.Any())
            {
                TempData["OctopusRelationSuggestions"] = SystemTextJsonSerializer.Serialize(result.Suggestions);
                TempData["OctopusMessage"] = $"Relaties gesynchroniseerd. {result.Suggestions.Count} mogelijke matches vragen bevestiging.";
                TempData["OctopusMessageType"] = "warning";
                return RedirectToAction(nameof(IssuerCompanyRelationSuggestions), new { issuerId });
            }
            else
            {
                TempData["OctopusMessage"] = $"Relaties gesynchroniseerd. Nieuw: {result.SuppliersCreated} leveranciers, {result.ClientsCreated} klanten. Gekoppeld: {result.SuppliersLinked} leveranciers, {result.ClientsLinked} klanten. Klantenflag geüpdatet: {result.CustomerFlagsUpdated}.";
                TempData["OctopusMessageType"] = "success";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Octopus relaties synchroniseren mislukt voor issuer {IssuerId}", issuerId);
            TempData["OctopusMessage"] = $"Relaties ophalen mislukt: {ex.Message}";
            TempData["OctopusMessageType"] = "danger";
        }

        return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
    }

    [HttpGet("IssuerCompanies/{issuerId:int}/Octopus/relations/suggestions")]
    public async Task<IActionResult> IssuerCompanyRelationSuggestions(int issuerId, CancellationToken ct)
    {
        var issuer = await _issuers.GetAsync(issuerId, ct);
        if (issuer == null)
        {
            return NotFound();
        }

        var suggestions = ReadOctopusSuggestions();
        if (suggestions == null || suggestions.Count == 0)
        {
            TempData["OctopusMessage"] = "Geen koppelsuggesties gevonden.";
            TempData["OctopusMessageType"] = "info";
            return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
        }

        TempData.Keep("OctopusRelationSuggestions");

        var vm = new OctopusRelationSuggestionsVM
        {
            IssuerId = issuerId,
            IssuerName = issuer.Name,
            Suggestions = suggestions
        };

        ViewBag.OctopusMessage = TempData["OctopusMessage"];
        ViewBag.OctopusMessageType = TempData["OctopusMessageType"];

        SetPageHeader("bx bx-cog", "Octopus-relaties koppelen");
        return View("OctopusRelationSuggestions", vm);
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Octopus/relations/link")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompanyConfirmRelationLink(int issuerId, OctopusRelationLinkRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
        }

        request.IssuerId = request.IssuerId == 0 ? issuerId : request.IssuerId;

        try
        {
            await _octopusRelations.LinkExistingAsync(request.IssuerId, request.IsSupplier, request.CandidateId, request.OctopusRelationId, ct);
            var successMessage = "Relatie gekoppeld.";

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = successMessage });
            }

            TempData["OctopusMessage"] = successMessage;
            TempData["OctopusMessageType"] = "success";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Octopus relatie handmatig koppelen mislukt voor issuer {IssuerId}", request.IssuerId);
            var errorMessage = $"Relatie koppelen mislukt: {ex.Message}";

            if (IsAjaxRequest())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = errorMessage });
            }

            TempData["OctopusMessage"] = errorMessage;
            TempData["OctopusMessageType"] = "danger";
        }

        return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = request.IssuerId });
    }

    private async Task<IActionResult> SyncOctopusDossierAsync(int issuerId, CancellationToken ct)
    {
        try
        {
            var issuer = await _issuers.GetAsync(issuerId, ct);
            if (issuer == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
            {
                TempData["OctopusMessage"] = "Koppel eerst een dossier.";
                TempData["OctopusMessageType"] = "warning";
                return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
            }

            var syncResult = await _octopusTokens.SyncDossierAsync(issuerId, issuer.OctopusDossierNumber, ct);

            // Na de sync: voor elk niet-afgesloten boekjaar zonder sequentie automatisch een sequentie aanmaken.
            var bookyears = await _octopusBookyears.ListByIssuerAsync(issuerId, ct);
            var openBookyears = bookyears.Where(b => !b.Closed).ToList();

            var activeSeries = await _series.ListByIssuerAsync(issuerId, ct);
            var activeSeriesList = activeSeries.Where(s => s.IsActive).ToList();

            // Bouw een map journalId → journalKey over alle bekende boekjaren voor dagboek-continuïteit.
            var journalKeyById = bookyears
                .SelectMany(b => b.Journals)
                .Where(j => j.Id > 0 && !string.IsNullOrWhiteSpace(j.JournalKey))
                .GroupBy(j => j.Id)
                .ToDictionary(g => g.Key, g => g.First().JournalKey);

            // Bepaal per serie welke boekjaren al gedekt zijn en wat het laatste gebruikte dagboek was.
            var autoCreated = new List<string>();
            var needsManual = new List<string>();

            foreach (var series in activeSeriesList)
            {
                var sequences = await _series.ListSequencesAsync(series.Id, ct);

                var coveredBookyearIds = sequences
                    .Where(s => s.BookyearId.HasValue)
                    .Select(s => s.BookyearId!.Value)
                    .ToHashSet();

                // Zoek het JournalKey van de meest recente sequentie voor dagboek-continuïteit.
                string? preferredJournalKey = sequences
                    .Where(s => s.JournalId.HasValue && journalKeyById.ContainsKey(s.JournalId.Value))
                    .OrderByDescending(s => s.Id)
                    .Select(s => journalKeyById[s.JournalId!.Value])
                    .FirstOrDefault();

                foreach (var bookyear in openBookyears.Where(b => !coveredBookyearIds.Contains(b.Id)))
                {
                    // Kies het dagboek: voorkeur voor hetzelfde JournalKey als vorige sequentie.
                    var journal = (!string.IsNullOrWhiteSpace(preferredJournalKey)
                        ? bookyear.Journals.FirstOrDefault(j => j.JournalKey == preferredJournalKey && !j.Closed)
                        : null)
                        ?? bookyear.Journals.FirstOrDefault(j => !j.Closed);

                    if (journal == null)
                    {
                        needsManual.Add(bookyear.BookyearDescription);
                        continue;
                    }

                    // startAt = 0: CurrentNumber = 0, zodat nextNumber = max(0, highestSentNr, lastBookedNr) + 1 = 1 voor een nieuw boekjaar.
                    // fiscalYear = EndDate.Year: meerjarig boekjaar (bv. okt 2024 – sep 2025) → eindejaar is bepalend.
                    var fiscalYearForSeq = bookyear.EndDate.Year;
                    await _series.CreateSequenceAsync(
                        series.Id,
                        fiscalYearForSeq,
                        startAt: 0,
                        bookyearId: bookyear.Id,
                        journalId: journal.Id,
                        ct: ct);

                    autoCreated.Add($"{bookyear.BookyearDescription} – {series.Code} (dagboek: {journal.Name ?? journal.JournalKey})");
                }
            }

            var baseMessage = $"Dossier gesynchroniseerd: {syncResult.BookyearCount} boekjaren, {syncResult.VatCodeCount} verkoop-btw-codes en {syncResult.CustomFieldCount} customvelden.";

            if (autoCreated.Count > 0)
            {
                var createdList = string.Join(", ", autoCreated);
                baseMessage += $" Nummerreeks automatisch aangemaakt voor: {createdList}. Controleer de instellingen en pas het startnummer aan indien nodig.";
            }

            if (needsManual.Count > 0)
            {
                var manualList = string.Join(", ", needsManual.Distinct());
                TempData["OctopusMessage"] = $"{baseMessage} Let op: voor de volgende boekjaren kon geen dagboek gevonden worden, maak de sequentie handmatig aan: {manualList}.";
                TempData["OctopusMessageType"] = "warning";
            }
            else
            {
                TempData["OctopusMessage"] = baseMessage;
                TempData["OctopusMessageType"] = autoCreated.Count > 0 ? "info" : "success";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Octopus dossier synchronisatie mislukt voor issuer {IssuerId}", issuerId);
            TempData["OctopusMessage"] = $"Dossier synchroniseren mislukt: {ex.Message}";
            TempData["OctopusMessageType"] = "danger";
        }

        return RedirectToAction(nameof(IssuerCompaniesEdit), new { id = issuerId });
    }

    private IReadOnlyList<OctopusRelationSuggestion> ReadOctopusSuggestions()
    {
        if (TempData.TryGetValue("OctopusRelationSuggestions", out var raw) && raw is string json && !string.IsNullOrWhiteSpace(json))
        {
            return SystemTextJsonSerializer.Deserialize<List<OctopusRelationSuggestion>>(json) ?? new List<OctopusRelationSuggestion>();
        }

        return new List<OctopusRelationSuggestion>();
    }

    private bool IsAjaxRequest()
    {
        if (Request?.Headers == null)
        {
            return false;
        }

        if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
            requestedWith.Any(v => string.Equals(v, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (Request.Headers.TryGetValue("Accept", out var accepts) &&
            accepts.Any(v => v != null && v.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    // POST /Admin/IssuerCompanies/Disable/5
    [HttpPost("IssuerCompanies/Disable/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompaniesDisable(int id)
    {
        await _issuers.DisableAsync(id);
        return RedirectToAction(nameof(IssuerCompanies));
    }

    // BANKREKENINGEN BEHEREN
    [HttpGet("IssuerCompanies/{issuerId:int}/BankAccounts/Create")]
    public IActionResult BankAccountCreate(int issuerId)
    {
        SetPageHeader("bx bx-cog", "Nieuwe bankrekening");
        return View(new IssuerBankAccountVM { IssuerCompanyId = issuerId, IsDefault = false });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/BankAccounts/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankAccountCreate(int issuerId, IssuerBankAccountVM vm)
    {
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-cog", "Nieuwe bankrekening");
            return View(vm);
        }
        var bo = new IssuerBankAccountBO
        {
            IssuerCompanyId = issuerId,
            Iban = vm.Iban,
            Bic = vm.Bic,
            DisplayName = vm.DisplayName,
            IsDefault = vm.IsDefault,
            ValidFrom = vm.ValidFrom,
            ValidTo = vm.ValidTo
        };
        await _bank.CreateAsync(bo);
        AddMessage("success", "Bankrekening toegevoegd.", "Geslaagd!");
        return RedirectToAction("IssuerCompaniesEdit", new { id = issuerId });
    }

    [HttpGet("IssuerCompanies/{issuerId:int}/BankAccounts/Edit/{id:int}")]
    public async Task<IActionResult> BankAccountEdit(int issuerId, int id)
    {
        var bo = await _bank.GetAsync(id);
        if (bo == null || bo.IssuerCompanyId != issuerId) return NotFound();
        var vm = new IssuerBankAccountVM
        {
            Id = bo.Id,
            IssuerCompanyId = bo.IssuerCompanyId,
            Iban = bo.Iban,
            Bic = bo.Bic,
            DisplayName = bo.DisplayName,
            IsDefault = bo.IsDefault,
            ValidFrom = bo.ValidFrom,
            ValidTo = bo.ValidTo
        };
        SetPageHeader("bx bx-cog", "Bankrekening bewerken");
        return View(vm);
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/BankAccounts/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankAccountEdit(int issuerId, int id, IssuerBankAccountVM vm)
    {
        if (id != vm.Id || issuerId != vm.IssuerCompanyId) return BadRequest();
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-cog", "Bankrekening bewerken");
            return View(vm);
        }

        var bo = new IssuerBankAccountBO
        {
            Id = vm.Id,
            IssuerCompanyId = vm.IssuerCompanyId,
            Iban = vm.Iban,
            Bic = vm.Bic,
            DisplayName = vm.DisplayName,
            IsDefault = vm.IsDefault,
            ValidFrom = vm.ValidFrom,
            ValidTo = vm.ValidTo
        };
        await _bank.UpdateAsync(bo);
        AddMessage("success", "Bankrekening opgeslagen.", "Geslaagd!");
        return RedirectToAction("IssuerCompaniesEdit", new { id = issuerId });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/BankAccounts/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankAccountDelete(int issuerId, int id)
    {
        await _bank.DeleteAsync(id);
        AddMessage("success", "Bankrekening verwijderd.", "Geslaagd!");
        return RedirectToAction("IssuerCompaniesEdit", new { id = issuerId });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/BankAccounts/SetDefault/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankAccountSetDefault(int issuerId, int id)
    {
        await _bank.SetDefaultAsync(id);
        return RedirectToAction("IssuerCompaniesEdit", new { id = issuerId });
    }

    // FACTUURREEKSEN BEHEREN

    [HttpGet("IssuerCompanies/{issuerId:int}/Series/Create")]
    public IActionResult SeriesCreate(int issuerId)
    {
        SetPageHeader("bx bx-cog", "Nieuwe nummerreeks");
        return View(new InvoiceSeriesVM { IssuerCompanyId = issuerId });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Series/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeriesCreate(int issuerId, InvoiceSeriesVM vm)
    {
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-cog", "Nieuwe nummerreeks");
            return View(vm);
        }
        await _series.CreateAsync(new InvoiceSeriesBO { IssuerCompanyId = issuerId, Code = vm.Code, Description = vm.Description, IsCreditNote = vm.IsCreditNote, IsActive = true });
        AddMessage("success", "Nummerreeks toegevoegd.", "Geslaagd!");
        return RedirectToAction("IssuerCompaniesEdit", new { id = issuerId });
    }

    // GET: /Instellingen/IssuerCompanies/{issuerId}/Series/Edit/{id}
    [HttpGet("IssuerCompanies/{issuerId:int}/Series/Edit/{id:int}")]
    public async Task<IActionResult> SeriesEdit(int issuerId, int id)
    {
        var bo = await _series.GetAsync(id);
        if (bo == null || bo.IssuerCompanyId != issuerId) return NotFound();

        var vm = new InvoiceSeriesVM
        {
            Id = bo.Id,
            IssuerCompanyId = bo.IssuerCompanyId,
            Code = bo.Code,
            Description = bo.Description,
            IsCreditNote = bo.IsCreditNote,
            IsActive = bo.IsActive
        };
        SetPageHeader("bx bx-cog", "Nummerreeks bewerken");
        return View(vm); // Views/Instellingen/SeriesEdit.cshtml
    }

    // POST: /Instellingen/IssuerCompanies/{issuerId}/Series/Edit/{id}
    [HttpPost("IssuerCompanies/{issuerId:int}/Series/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeriesEdit(int issuerId, int id, InvoiceSeriesVM vm)
    {
        if (id != vm.Id || issuerId != vm.IssuerCompanyId) return BadRequest();

        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-cog", "Nummerreeks bewerken");
            return View(vm);
        }

        try
        {
            var bo = new InvoiceSeriesBO
            {
                Id = vm.Id,
                IssuerCompanyId = vm.IssuerCompanyId,
                Code = vm.Code?.Trim(),
                Description = vm.Description,
                IsCreditNote = vm.IsCreditNote,
                IsActive = vm.IsActive
            };

            await _series.UpdateAsync(bo);
            AddMessage("success", "Nummerreeks opgeslagen.", "Geslaagd!");
            return RedirectToAction("IssuerCompaniesEdit", new { id = issuerId });
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Unieke code per issuer geschonden
            ModelState.AddModelError(nameof(vm.Code), "Deze code bestaat al voor dit bedrijf.");
            SetPageHeader("bx bx-cog", "Nummerreeks bewerken");
            return View(vm);
        }
    }


    [HttpPost("IssuerCompanies/{issuerId:int}/Series/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeriesDelete(int issuerId, int id)
    {
        try
        {
            await _series.DeleteAsync(id);
            TempData["Flash"] = "Nummerreeks is verwijderd.";
        }
        catch (DbUpdateException)
        {
            // FK-conflict → zet inactief i.p.v. crash
            await _series.DisableAsync(id);
            TempData["Flash"] = "Nummerreeks kon niet verwijderd worden (in gebruik). Ze is gedeactiveerd.";
        }

        return RedirectToAction("IssuerCompaniesEdit", new { id = issuerId });
    }


    [HttpGet("IssuerCompanies/{issuerId:int}/Series/{seriesId:int}/Sequences")]
    //[HttpGet("Series/{seriesId:int}/Sequences")]
    public async Task<IActionResult> Sequences(int seriesId, int issuerId)
    {
        var items = await _series.ListSequencesAsync(seriesId);
        var bookyears = await _octopusBookyears.ListByIssuerAsync(issuerId);
        var vm = new InvoiceSequencesPageVM
        {
            IssuerId = issuerId,
            SeriesId = seriesId,
            Items = items.Select(x => new InvoiceSequenceVM
            {
                Id = x.Id,
                SeriesId = x.SeriesId,
                BookyearId = x.BookyearId,
                JournalId = x.JournalId,
                FiscalYear = x.FiscalYear,
                CurrentNumber = x.CurrentNumber,
                BookyearDescription = x.BookyearDescription,
                JournalName = x.JournalName
            }).ToList()
        };
        ViewBag.OctopusBookyears = bookyears;
        SetPageHeader("bx bx-cog", "Sequenties");
        return View(vm);
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Series/{seriesId:int}/Sequences/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SequenceCreate(int seriesId, int issuerId, int fiscalYear, int startAt = 0, int? bookyearId = null, int? journalId = null)
    {
        if (bookyearId.HasValue)
        {
            var bookyear = await _octopusBookyears.GetAsync(bookyearId.Value, CancellationToken.None);
            if (bookyear != null)
            {
                // Meerjarig boekjaar (bv. okt 2024 – sep 2025): gebruik eindejaar als fiscaal jaar,
                // conform de Belgische boekhoudconventie waarbij het afsluitjaar bepalend is.
                fiscalYear = bookyear.EndDate.Year;
            }
        }

        await _series.CreateSequenceAsync(seriesId, fiscalYear, startAt, bookyearId, journalId);
        return RedirectToAction("Sequences", new { seriesId, issuerId });
    }
    [HttpPost("IssuerCompanies/{issuerId:int}/Series/{seriesId:int}/Sequences/Update/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SequenceUpdate(int id, int currentNumber, int seriesId, int issuerId, int? bookyearId = null, int? journalId = null)
    {
        await _series.UpdateSequenceAsync(id, currentNumber, bookyearId, journalId);
        return RedirectToAction("Sequences", new { seriesId, issuerId });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Series/{seriesId:int}/Sequences/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SequenceDelete(int seriesId, int issuerId, int id)
    {
        try
        {
            await _series.DeleteSequenceAsync(id);
            TempData["Flash"] = "Sequentie verwijderd.";
        }
        catch (InvalidOperationException)
        {
            TempData["Flash"] = "Sequentie kan niet verwijderd worden: er bestaan facturen die deze sequentie gebruiken.";
        }
        catch (DbUpdateException)
        {
            TempData["Flash"] = "Sequentie kon niet verwijderd worden door database-relaties.";
        }

        return RedirectToAction("Sequences", new { seriesId, issuerId });
    }

    private async Task PopulateInvoiceLayoutViewDataAsync(CancellationToken ct = default)
    {
        await _invoiceTemplates.EnsureDefaultsAsync(ct);
        var templates = await _invoiceTemplates.ListAsync(ct);
        var defaults = templates.ToDictionary(t => t.Key, t => t.TemplateJson);

        ViewBag.InvoiceLayoutDefaultsJson = SystemTextJsonSerializer.Serialize(defaults, LayoutSerializerOptions);
        ViewBag.InvoiceLayoutSchemaJson = LayoutSchemaJson;
    }

    private async Task<IReadOnlyList<InvoiceLayoutTemplateOptionVM>> GetInvoiceTemplateOptionsAsync(CancellationToken ct = default)
    {
        await _invoiceTemplates.EnsureDefaultsAsync(ct);
        var templates = await _invoiceTemplates.ListAsync(ct);
        return templates
            .Select(t => new InvoiceLayoutTemplateOptionVM
            {
                Key = t.Key,
                Name = t.Name,
                TemplateJson = t.TemplateJson,
                IsSystem = t.IsSystem
            })
            .ToList();
    }

    private static void ApplyInvoiceTemplateDefaults(IssuerCompanyVM vm, IReadOnlyList<InvoiceLayoutTemplateOptionVM> templates)
    {
        if (vm == null || templates == null || templates.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(vm.TemplateKey))
            vm.TemplateKey = templates[0].Key;

        var selected = templates.FirstOrDefault(t => t.Key == vm.TemplateKey) ?? templates[0];
        if (string.IsNullOrWhiteSpace(vm.TemplateJson))
            vm.TemplateJson = selected.TemplateJson;
    }

    private void ValidateTemplateJson(string? templateJson)
    {
        ValidateTemplateJson(templateJson, nameof(IssuerCompanyVM.TemplateJson));
    }

    private void ValidateTemplateJson(string? templateJson, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
            return;

        try
        {
            var token = JToken.Parse(templateJson);
            var schema = LayoutSchemaProvider.GetSchema();

            IList<ValidationError> errors;
            if (!token.IsValid(schema, out errors))
            {
                var message = string.Join("; ", errors.Select(e => e.Message));
                ModelState.AddModelError(fieldName, $"Layout JSON ongeldig: {message}");
            }
        }
        catch (JsonReaderException ex)
        {
            ModelState.AddModelError(fieldName, $"Layout JSON is niet geldig: {ex.Message}");
        }
    }

    //HELPERS
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // SQL Server: 2601 (duplicate key), 2627 (unique index/constraint)
        if (ex.InnerException is SqlException sqlEx)
            return sqlEx.Number == 2601 || sqlEx.Number == 2627;

        // Fallback: string check (laat staan voor zekerheid)
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", System.StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate", System.StringComparison.OrdinalIgnoreCase);
    }

    private void SetActivityResponseMessage(Response response, string fallbackSuccessMessage)
    {
        if (response.Success)
        {
            TempData["Message"] = response.Messages?.FirstOrDefault()?.Message ?? fallbackSuccessMessage;
            return;
        }

        var errorMessage = response.Messages?.FirstOrDefault(m => m.Type == MessageType.Error)?.Message;
        TempData["Error"] = string.IsNullOrWhiteSpace(errorMessage) ? "Er ging iets mis bij het bewaren." : errorMessage;
    }

    private static List<OctopusCustomFieldMappingBO> DeserializeCustomFieldMappings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<OctopusCustomFieldMappingBO>();
        }

        return System.Text.Json.JsonSerializer.Deserialize<List<OctopusCustomFieldMappingBO>>(json)
               ?? new List<OctopusCustomFieldMappingBO>();
    }

    private static List<OctopusCustomFieldMappingVM> BuildCustomFieldMappings(IEnumerable<OctopusCustomFieldBO> fields, IEnumerable<OctopusCustomFieldMappingBO> mappings)
    {
        var mappingByKey = mappings?
            .GroupBy(m => m.CustomFieldKeyId)
            .ToDictionary(g => g.Key, g => g.First().InvoiceField) ?? new Dictionary<int, string?>();

        var list = new List<OctopusCustomFieldMappingVM>();

        foreach (var field in fields ?? Array.Empty<OctopusCustomFieldBO>())
        {
            list.Add(new OctopusCustomFieldMappingVM
            {
                CustomFieldKeyId = field.CustomFieldKeyId,
                TemplateCode = field.TemplateCode,
                DescriptionNl = field.DescriptionNl,
                DescriptionFr = field.DescriptionFr,
                DescriptionEn = field.DescriptionEn,
                DescriptionDe = field.DescriptionDe,
                InvoiceField = mappingByKey.TryGetValue(field.CustomFieldKeyId, out var invoiceField)
                    ? invoiceField
                    : null
            });
        }

        return list;
    }

    private static string BuildCustomFieldMappingJson(IEnumerable<OctopusCustomFieldMappingVM>? mappings)
    {
        var data = (mappings ?? Array.Empty<OctopusCustomFieldMappingVM>())
            .Where(m => m != null && m.CustomFieldKeyId > 0 && !string.IsNullOrWhiteSpace(m.InvoiceField))
            .Select(m => new OctopusCustomFieldMappingBO
            {
                CustomFieldKeyId = m.CustomFieldKeyId,
                InvoiceField = m.InvoiceField!
            })
            .ToList();

        return System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static IReadOnlyList<KeyValuePair<string, string>> GetInvoiceFieldOptions()
    {
        return new List<KeyValuePair<string, string>>
        {
            new("PublicId", "Factuurnummer"),
            new("StructuredCommOgm", "Gestructureerde mededeling"),
            new("ClientName", "Klantnaam"),
            new("VatNumber", "BTW-nummer"),
            new("VatRegime", "BTW regime"),
            new("CurrencyCode", "Valuta"),
            new("BankAccount", "Bankrekening"),
            new("HeaderDescription", "Koptekst"),
            new("Text", "Omschrijving"),
            new("ExtraInfo", "Extra info"),
            new("QrEpcPayload", "EPC QR payload"),
            new("InvoiceMode", "Factuurmodus"),
            new("Date", "Factuurdatum"),
            new("ExpirationDate", "Vervaldatum")
        };
    }

    // ─── Kostprijzen materialen ─────────────────────────────────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsKostprijsMaterialen)]
    [Breadcrumb("Kostprijzen materialen")]
    public IActionResult KostprijsMaterialen()
    {
        SetPageHeader("bx bx-cog", "Kostprijzen materialen");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };
        var instellingenKM = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("KostprijsMaterialen", "Instellingen", "Kostprijs materialen")
        {
            Parent = instellingenIndex
        };

        ViewData["BreadcrumbNode"] = instellingenKM;

        var vm = new KostprijsMaterialenViewModel
        {
            IndexTypes         = _kostprijsService.GetIndexTypes().Values         ?? new(),
            ActivityGroepen    = _kostprijsService.GetActivityGroepen().Values    ?? new(),
            Materialen         = _kostprijsService.GetMaterialen().Values         ?? new(),
            PercentageGroepen  = _kostprijsService.GetPercentageGroepen().Values  ?? new(),
            Percentages        = _kostprijsService.GetPercentages().Values        ?? new(),
            FormulaKoppelingen = _kostprijsService.GetFormulaKoppelingen().Values ?? new()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsMateriaalCreate(int categorieId, string naam, string eenheid, string toepassingsgebied, int kmIndexTypeId, decimal referentiePrijs, DateTime referentieDatum, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdateMateriaal(new KostprijsMateriaalBO { CategorieId = categorieId, Naam = naam, Eenheid = eenheid, Toepassingsgebied = toepassingsgebied, KmIndexTypeId = kmIndexTypeId, ReferentiePrijs = referentiePrijs, ReferentieDatum = referentieDatum, Volgorde = volgorde }), "Materiaal aangemaakt.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsMateriaalUpdate(int id, int categorieId, string naam, string eenheid, string toepassingsgebied, int kmIndexTypeId, decimal referentiePrijs, DateTime referentieDatum, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdateMateriaal(new KostprijsMateriaalBO { Id = id, CategorieId = categorieId, Naam = naam, Eenheid = eenheid, Toepassingsgebied = toepassingsgebied, KmIndexTypeId = kmIndexTypeId, ReferentiePrijs = referentiePrijs, ReferentieDatum = referentieDatum, Volgorde = volgorde }), "Materiaal opgeslagen.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsMateriaalDelete(int id)
    {
        var r = _kostprijsService.DeleteMateriaal(id);
        SetActivityResponseMessage(r, "Materiaal verwijderd.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost]
    public IActionResult FormulaKoppelingOpslaanAjax([FromBody] FormulaKoppelingAjaxRequest req)
    {
        if (req == null) return BadRequest();
        var r = _kostprijsService.SaveFormulaKoppeling(req.Sleutel, req.MateriaalId);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, message = r.Success ? "Koppeling opgeslagen." : fout });
    }

    [HttpGet]
    public IActionResult GetFormulaKoppelingen()
    {
        var lijst = _kostprijsService.GetFormulaKoppelingen().Values ?? new();
        return Json(lijst.Select(k => new {
            k.Sleutel, k.Omschrijving, k.MateriaalId, k.MateriaalNaam,
            k.MateriaalReferentiePrijs, k.MateriaalEenheid
        }));
    }

    [HttpPost]
    public IActionResult FormulaKoppelingAanmaken([FromBody] FormulaKoppelingAanmakenRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Naam)) return BadRequest();
        var r = _kostprijsService.CreateFormulaKoppeling(req.Naam, req.MateriaalId);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        var k = r.Values?.FirstOrDefault();
        return Json(new
        {
            ok = r.Success,
            message = r.Success ? "Formule-koppeling aangemaakt." : fout,
            koppeling = k == null ? null : new
            {
                k.Id, k.Sleutel, k.Omschrijving, k.MateriaalId, k.MateriaalNaam,
                k.MateriaalReferentiePrijs, k.MateriaalEenheid
            }
        });
    }

    [HttpPost]
    public IActionResult FormulaKoppelingVerwijderen([FromBody] int id)
    {
        var r = _kostprijsService.DeleteFormulaKoppeling(id);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, message = r.Success ? "Koppeling verwijderd." : fout });
    }

    // ─── Budgetformules (voorstellen BudgetActivityLijnen) ─────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsCalculatie)]
    [Breadcrumb("Budgetformules")]
    public async Task<IActionResult> BudgetFormules()
    {
        SetPageHeader("bx bx-math", "Budgetformules");

        var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen")
        {
            Parent = dashboard
        };
        var formulesNode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("BudgetFormules", "Instellingen", "Budgetformules")
        {
            Parent = instellingenIndex
        };
        ViewData["BreadcrumbNode"] = formulesNode;

        var vm = new BudgetFormulesViewModel
        {
            Formules     = await _budgetFormules.GetFormulesAsync(),
            Activiteiten = await _budgetFormules.GetActiviteitenMetLotAsync(),
            TestVersies  = await _budgetFormules.GetTestVersiesAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgetFormuleParameters(int? versieId)
    {
        var parameters = await _budgetFormules.GetParametersAsync(versieId);
        return Json(parameters.Select(p => new { p.Naam, p.Omschrijving, p.Eenheid, p.Categorie, p.Waarde }));
    }

    [HttpPost]
    public async Task<IActionResult> BudgetFormuleOpslaan([FromBody] BudgetFormuleOpslaanRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Formule)) return BadRequest();
        var r = await _budgetFormules.SaveAsync(req.ActivityId, req.Formule, req.Omschrijving, req.Actief);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        var f = r.Value;
        return Json(new
        {
            ok = r.Success,
            message = r.Success ? "Formule opgeslagen." : fout,
            formule = f == null ? null : new
            {
                f.Id, f.ActivityId, f.ActivityOmschrijving, f.LotNaam, f.LotNummer,
                f.Formule, f.Omschrijving, f.Actief, laatstGewijzigd = f.LaatstGewijzigd.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> BudgetFormuleDelete([FromBody] int id)
    {
        var r = await _budgetFormules.DeleteAsync(id);
        var fout = r.Messages.FirstOrDefault(m => m.Type == BOCore.MessageType.Error)?.Message;
        return Json(new { ok = r.Success, message = r.Success ? "Formule verwijderd." : fout });
    }

    [HttpPost]
    public async Task<IActionResult> BudgetFormuleTest([FromBody] BudgetFormuleTestRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Formule) || req.VersieId <= 0) return BadRequest();
        var r = await _budgetFormules.TestAsync(req.VersieId, req.Formule);
        return Json(new
        {
            ok              = r.Ok,
            fout            = r.Fout,
            totaal          = r.Totaal,
            perEenheid      = r.PerEenheid,
            aantalEenheden  = r.AantalEenheden,
            onbekendeParams = r.OnbekendeParams,
            termen          = r.Termen.Select(t => new { t.ExprNamen, t.ExprWaarden, t.Waarde })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageGroepCreate(string naam, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentageGroep(new BouwkostPercentageGroepBO { Naam = naam, Volgorde = volgorde }), "Groep aangemaakt.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageGroepUpdate(int id, string naam, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentageGroep(new BouwkostPercentageGroepBO { Id = id, Naam = naam, Volgorde = volgorde }), "Groep opgeslagen.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageGroepDelete(int id)
    {
        SetActivityResponseMessage(_kostprijsService.DeletePercentageGroep(id), "Groep verwijderd.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageCreate(int groepId, string naam, decimal percentage, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentage(new BouwkostPercentageBO { GroepId = groepId, Naam = naam, Percentage = percentage, Volgorde = volgorde }), "Percentage aangemaakt.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageUpdate(int id, int groepId, string naam, decimal percentage, int volgorde)
    {
        SetActivityResponseMessage(_kostprijsService.InsertUpdatePercentage(new BouwkostPercentageBO { Id = id, GroepId = groepId, Naam = naam, Percentage = percentage, Volgorde = volgorde }), "Percentage opgeslagen.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult BouwkostPercentageDelete(int id)
    {
        SetActivityResponseMessage(_kostprijsService.DeletePercentage(id), "Percentage verwijderd.");
        return RedirectToAction(nameof(KostprijsMaterialen));
    }

    [HttpGet]
    public IActionResult KostprijsUpdatePreview(int projectId)
    {
        ViewBag.ProjectId = projectId;
        SetPageHeader("bx bx-cog", "Kostprijzen bijwerken — preview");
        return View(_kostprijsService.GetUpdatePreview(projectId).Values ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult KostprijsUpdateBevestigen(int projectId)
    {
        SetActivityResponseMessage(_kostprijsService.BevestigUpdate(projectId), "Kostprijzen bijgewerkt.");
        return RedirectToAction("Details", "Project", new { id = projectId });
    }

    // ─── Bouwindexen ────────────────────────────────────────────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsBouwIndexen)]
    [Breadcrumb("Bouwindexen")]
    public async Task<IActionResult> Bouwindexen()
    {
        SetPageHeader("bx bx-cog", "Bouwindexen");

        var dashboard       = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen    = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var bouwindexen     = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Bouwindexen", "Instellingen", "Bouwindexen") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = bouwindexen;

        var model = new CPMCore.Models.Instellingen.BouwindexenModel
        {
            SIndexen     = await _bouwIndex.GetGefilterdAsync("S"),
            I2021Indexen = await _bouwIndex.GetGefilterdAsync("I2021"),
            IPlusIndexen = await _bouwIndex.GetGefilterdAsync("I+"),
            AbexIndexen  = await _bouwIndex.GetGefilterdAsync("ABEX")
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SyncSIndex()
    {
        var result = await _sIndexScraper.ScrapeAsync();
        var herlaad = result.Geslaagd && (result.AantalNieuw > 0 || result.AantalBijgewerkt > 0);
        return Json(new { ok = herlaad, waarschuwing = !herlaad && result.Geslaagd, bericht = result.Samenvatting });
    }

    [HttpPost]
    public async Task<IActionResult> SyncI2021()
    {
        var result = await _i2021Sync.SyncAsync();
        var herlaad = result.Geslaagd && (result.AantalNieuw > 0 || result.AantalBijgewerkt > 0);
        return Json(new { ok = herlaad, waarschuwing = !herlaad && result.Geslaagd, bericht = result.Samenvatting });
    }

    [HttpGet]
    public async Task<IActionResult> GetBouwIndexen(string indexType)
    {
        var lijst = await _bouwIndex.GetGefilterdAsync(indexType);
        return Json(lijst);
    }

    [HttpPost]
    public async Task<IActionResult> OpslaanBouwIndex([FromBody] DALCore.Models.BouwIndex index)
    {
        try
        {
            await _bouwIndex.OpslaanAsync(index);
            return Json(new { ok = true });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, bericht = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> VerwijderBouwIndex(int id)
    {
        var ok = await _bouwIndex.VerwijderenAsync(id);
        return Json(new { ok });
    }

    [HttpPost]
    public async Task<IActionResult> SetActiefBouwIndex(string indexType, int id)
    {
        await _bouwIndex.SetActiefAsync(indexType, id);
        return Json(new { ok = true });
    }

    // ─── Marktdata-status ───────────────────────────────────────────────────────

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsMarketDataStatus)]
    [Breadcrumb("Marktdata-status")]
    public async Task<IActionResult> MarketDataStatus(CancellationToken ct)
    {
        SetPageHeader("bx bx-radar", "Marktdata-status");

        var dashboard    = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var instellingen = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Instellingen", "Instellingen") { Parent = dashboard };
        var status       = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("MarketDataStatus", "Instellingen", "Marktdata-status") { Parent = instellingen };
        ViewData["BreadcrumbNode"] = status;

        var model = await _marketDataStatus.GetStatusAsync(ct: ct);
        return View(model);
    }

}
public class OctopusRelationLinkRequest
{
    public int IssuerId { get; set; }
    public bool IsSupplier { get; set; }
    public int CandidateId { get; set; }
    public int? OctopusRelationId { get; set; }
}
