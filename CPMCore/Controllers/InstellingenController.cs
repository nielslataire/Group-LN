using BOCore;
using CPMCore.Attributes;
using CPMCore.Models;
using CPMCore.Models.Home;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Projecten;
using CPMCore.Service;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using ServiceCore.Invoicing.Pdf.Templates;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Collections.Generic;

namespace CPMCore.Controllers;


[Authorize]
public class InstellingenController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private UserManager<ApplicationUser> _userManager;
    private readonly IIssuerCompanyService _issuers;
    private readonly IIssuerBankAccountService _bank;
    private readonly IIssuerSeriesService _series;

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

    public InstellingenController(UserManager<ApplicationUser> userManager, ILogger<HomeController> logger, IIssuerCompanyService issuers, IIssuerBankAccountService bank, IIssuerSeriesService series)
    {
        _userManager = userManager;
        _logger = logger;
        _issuers = issuers;
        _bank = bank;
        _series = series;
    }


    //VAKANTIEDAGEN
    [HttpGet]
    [Breadcrumb("Vakantiedagen")]
    public ActionResult Vacationdays()
    {
        HomeModel model = new HomeModel();
      
        return View(model);
    }
    [HttpGet]
    public IActionResult GetVacationDays()
    {
        var service = ServiceFactory.GetProjectService();
        var response = service.GetVacationDaysGeneral();

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
        var service = ServiceFactory.GetProjectService();
        var response = service.InsertUpdateVacationDay(day);

        if (!response.Success) return 0;

        var idMsg = response.Messages.FirstOrDefault(m => m.Type == MessageType.Value)?.Message
                    ?? response.Messages.FirstOrDefault()?.Message;

        return int.TryParse(idMsg, out var id) ? id : 0;
    }

    [HttpPost]
    public bool DeleteVacationDay(int id)
    {
        var ids = new List<int> { id };
        var service = ServiceFactory.GetProjectService();
        var response = service.DeleteVacationDays(ids);
        return response.Success;
    }

    //MIJN BEDRIJVEN
    // GET /Admin/IssuerCompanies
    [HttpGet("IssuerCompanies")]
    public async Task<IActionResult> IssuerCompanies()
    {
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
            IsActive = x.IsActive,
            EInvoiceEnabled = x.EInvoiceEnabled,
            PeppolParticipantId = x.PeppolParticipantId,
            UblAttachPdf = x.UblAttachPdf,
            EmailSubjectTemplate = x.EmailSubjectTemplate,
            EmailBodyTemplate = x.EmailBodyTemplate,
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

        PopulateInvoiceLayoutViewData();
        return View(new IssuerCompanyVM());
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
            PopulateInvoiceLayoutViewData();
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
            IsActive = vm.IsActive,
            EInvoiceEnabled = vm.EInvoiceEnabled,
            PeppolParticipantId = vm.PeppolParticipantId,
            UblAttachPdf = vm.UblAttachPdf,
            EmailSubjectTemplate = vm.EmailSubjectTemplate,
            EmailBodyTemplate = vm.EmailBodyTemplate,
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
        };
        await _issuers.CreateAsync(bo);
        AddMessage("success", "Mijn bedrijf " + vm.Name + " toegevoegd.", "Geslaagd!");
        return RedirectToAction(nameof(IssuerCompanies));
    }

    // GET /Admin/IssuerCompanies/Edit/5
    [HttpGet("IssuerCompanies/Edit/{id:int}")]
    public async Task<IActionResult> IssuerCompaniesEdit(int id)
    {
        var referrer = Request.Headers["Referer"].ToString();
        // Use the referrer URL as needed
        TempData["Referrer"] = referrer;
        var bo = await _issuers.GetAsync(id);
        if (bo == null) return NotFound();

        ViewBag.PaymentTerms = await _issuers.GetPaymentTermOptionsAsync();
        ViewBag.BankAccounts = await _bank.ListByIssuerAsync(id);
        ViewBag.InvoiceSeries = await _series.ListByIssuerAsync(id);
        ViewBag.CompanyLegalForms = await _issuers.ListLegalFormsAsync();

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
            IsActive = bo.IsActive,
            EInvoiceEnabled = bo.EInvoiceEnabled,
            PeppolParticipantId = bo.PeppolParticipantId,
            UblAttachPdf = bo.UblAttachPdf,
            EmailSubjectTemplate = bo.EmailSubjectTemplate,
            EmailBodyTemplate = bo.EmailBodyTemplate,
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
        };

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

        PopulateInvoiceLayoutViewData();
        return View(vm);
    }

    // POST /Instellingen/IssuerCompanies/Edit/5
    [HttpPost("IssuerCompanies/Edit/{id:int}", Name = "Instellingen_IssuerCompanies_Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IssuerCompaniesEdit(int id, IssuerCompanyVM vm)
    {
        if (id != vm.Id) return BadRequest();
        ValidateTemplateJson(vm.TemplateJson);
        if (!ModelState.IsValid)
        {
            ViewBag.PaymentTerms = await _issuers.GetPaymentTermOptionsAsync();
            ViewBag.BankAccounts = await _bank.ListByIssuerAsync(id);
            ViewBag.InvoiceSeries = await _series.ListByIssuerAsync(id);
            ViewBag.CompanyLegalForms = await _issuers.ListLegalFormsAsync();
            PopulateInvoiceLayoutViewData();
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
            IsActive = vm.IsActive,
            EInvoiceEnabled = vm.EInvoiceEnabled,
            PeppolParticipantId = vm.PeppolParticipantId,
            UblAttachPdf = vm.UblAttachPdf,
            EmailSubjectTemplate = vm.EmailSubjectTemplate,
            EmailBodyTemplate = vm.EmailBodyTemplate,
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
        };
        await _issuers.UpdateAsync(bo);

        AddMessage("success", "Mijn bedrijf " +vm.Name + " opgeslagen.", "Geslaagd!");
        return RedirectToAction(nameof(IssuerCompanies));
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
        return View(new IssuerBankAccountVM { IssuerCompanyId = issuerId, IsDefault = false });
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/BankAccounts/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankAccountCreate(int issuerId, IssuerBankAccountVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
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
        return View(vm);
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/BankAccounts/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankAccountEdit(int issuerId, int id, IssuerBankAccountVM vm)
    {
        if (id != vm.Id || issuerId != vm.IssuerCompanyId) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

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
    public IActionResult SeriesCreate(int issuerId) => View(new InvoiceSeriesVM { IssuerCompanyId = issuerId });

    [HttpPost("IssuerCompanies/{issuerId:int}/Series/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeriesCreate(int issuerId, InvoiceSeriesVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
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
        return View(vm); // Views/Instellingen/SeriesEdit.cshtml
    }

    // POST: /Instellingen/IssuerCompanies/{issuerId}/Series/Edit/{id}
    [HttpPost("IssuerCompanies/{issuerId:int}/Series/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeriesEdit(int issuerId, int id, InvoiceSeriesVM vm)
    {
        if (id != vm.Id || issuerId != vm.IssuerCompanyId) return BadRequest();

        if (!ModelState.IsValid)
            return View(vm);

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
        var vm = new InvoiceSequencesPageVM
        {
            IssuerId = issuerId,
            SeriesId = seriesId,
            Items = items.Select(x => new InvoiceSequenceVM
            {
                Id = x.Id,
                SeriesId = x.SeriesId,
                FiscalYear = x.FiscalYear,
                CurrentNumber = x.CurrentNumber
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost("IssuerCompanies/{issuerId:int}/Series/{seriesId:int}/Sequences/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SequenceCreate(int seriesId,int issuerId, int fiscalYear, int startAt = 0)
    {
        await _series.CreateSequenceAsync(seriesId, fiscalYear, startAt);
        return RedirectToAction("Sequences", new { seriesId, issuerId });
    }
    [HttpPost("IssuerCompanies/{issuerId:int}/Series/{seriesId:int}/Sequences/Update/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SequenceUpdate(int id, int currentNumber, int seriesId, int issuerId)
    {
        await _series.UpdateSequenceAsync(id, currentNumber);
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

    private void PopulateInvoiceLayoutViewData()
    {
        ViewBag.InvoiceLayoutDefaultsJson = LayoutDefaultsJson;
        ViewBag.InvoiceLayoutSchemaJson = LayoutSchemaJson;
    }

    private void ValidateTemplateJson(string? templateJson)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
            return;

        try
        {
            var token = JToken.Parse(templateJson);
            var schema = LayoutSchemaProvider.GetSchema();

            IList<ValidationError> errors;   // <-- expliciet type kiezen
            if (!token.IsValid(schema, out errors))
            {
                var message = string.Join("; ", errors.Select(e => e.Message));
                ModelState.AddModelError(nameof(IssuerCompanyVM.TemplateJson),
                    $"Layout JSON ongeldig: {message}");
            }

        }
        catch (JsonReaderException ex)
        {
            ModelState.AddModelError(nameof(IssuerCompanyVM.TemplateJson), $"Layout JSON is niet geldig: {ex.Message}");
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

    public void AddMessage(string messagetype, string message, string messagetitle)
    {
        TempData["Message"] = message;
        TempData["MessageType"] = messagetype;
        TempData["MessageTitle"] = messagetitle;
    }


}
