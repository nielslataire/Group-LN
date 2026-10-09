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
using FacadeCore;
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
//using CPMCore.Attributes;
using ClosedXML.Excel;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
    [Authorize]
    [TypeFilter(typeof(CPMCore.Filters.BudgetVersieVergrendeldFilter))]
    public partial class ProjectenController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly cpmRunningContext _db; // TODO: vervangen door service methoden (UnitExecutionPlan, Users, CompanyContacts)
        private readonly IConfiguration Configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IProjectService _projectService;
        private readonly IUnitService _unitService;
        private readonly IClientService _clientService;
        private readonly ICompanyService _companyService;
        private readonly IActivityService _activityService;
        private readonly IInsuranceService _insuranceService;
        private readonly ICountryService _countryService;
        private readonly IPostalcodeService _postalcodeService;
        private readonly IProjectVoortgangService _voortgangService;
        private readonly IConstructionIssueService _issueService;
        private readonly IMijlpaalService _mijlpaalService;
        private readonly IInvoiceQueryService _invoiceQueryService;
        private readonly DALCore.UnitOfWorkCore _uow;
        private readonly IBudgetService _budgetService;
        private readonly BudgetActivityService    _budgetActivityService;
        private readonly BouwIndexService            _bouwIndex;
        private readonly BudgetBerekeningService     _berekeningService;
        private readonly IVerkoopVoorstelService     _verkoopVoorstelService;
        private readonly IBudgetPrijsReferentieService _prijsReferentieService;
        private readonly IBudgetReferentieProjectService _referentieProjectService;
        private readonly BudgetExcelService          _excelService;
        private readonly ServiceCore.Budget.BudgetFormulaService _formulaService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IEmailSendLogService _emailSendLogService;
        private readonly IUserSignatureService _userSignatureService;
        private readonly IEmailSender _emailSender;
        //private readonly IInvoicePdfService _pdf;         // QuestPDF
        //private readonly IUblService _ubl;
        private static readonly HashSet<string> _validImageTypes = new(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };

        private static readonly HashSet<string> _validVideoTypes = new(StringComparer.OrdinalIgnoreCase)
            { "video/mp4", "video/webm", "video/quicktime", "video/x-msvideo", "video/avi" };

        public ProjectenController(ILogger<HomeController> logger, IConfiguration configuration, IWebHostEnvironment env, cpmRunningContext db, IProjectService projectService, IUnitService unitService, IClientService clientService, ICompanyService companyService, IActivityService activityService, IInsuranceService insuranceService, ICountryService countryService, IPostalcodeService postalcodeService, IProjectVoortgangService voortgangService, IConstructionIssueService issueService, IMijlpaalService mijlpaalService, IInvoiceQueryService invoiceQueryService, DALCore.UnitOfWorkCore uow, IBudgetService budgetService, BudgetActivityService budgetActivityService, BouwIndexService bouwIndex, BudgetBerekeningService berekeningService, IVerkoopVoorstelService verkoopVoorstelService, IBudgetPrijsReferentieService prijsReferentieService, IBudgetReferentieProjectService referentieProjectService, BudgetExcelService excelService, ServiceCore.Budget.BudgetFormulaService formulaService, IEmailTemplateService emailTemplateService, IEmailSendLogService emailSendLogService, IUserSignatureService userSignatureService, IEmailSender emailSender)
        {
            _logger = logger;
            Configuration = configuration;
            _env = env;
            _db = db;
            _projectService = projectService;
            _unitService = unitService;
            _clientService = clientService;
            _companyService = companyService;
            _activityService = activityService;
            _insuranceService = insuranceService;
            _countryService = countryService;
            _postalcodeService = postalcodeService;
            _voortgangService = voortgangService;
            _issueService = issueService;
            _mijlpaalService = mijlpaalService;
            _invoiceQueryService = invoiceQueryService;
            _uow = uow;
            _budgetService          = budgetService;
            _budgetActivityService  = budgetActivityService;
            _bouwIndex              = bouwIndex;
            _berekeningService      = berekeningService;
            _verkoopVoorstelService = verkoopVoorstelService;
            _prijsReferentieService = prijsReferentieService;
            _referentieProjectService = referentieProjectService;
            _excelService           = excelService;
            _formulaService         = formulaService;
            _emailTemplateService   = emailTemplateService;
            _emailSendLogService    = emailSendLogService;
            _userSignatureService   = userSignatureService;
            _emailSender            = emailSender;
        }

        // De projecthub (Detail + alle onderliggende tabbladen met het linkermenu)
        // toont de linker-sidebar ingeklapt. Dat werd voorheen per actie met
        // "ViewBag.sidebarcollapsed" gezet en bij nieuwere pagina's (betalings-
        // schijven, budgetten, wijzigingsopdrachten, ...) telkens vergeten.
        // Hier centraal: elke View-actie van deze controller klapt de sidebar in,
        // behalve de overzichts-/losstaande pagina's hieronder. Een actie die de
        // waarde zelf al zet, blijft leidend.
        private static readonly HashSet<string> _sidebarExpandedActions = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(Index), nameof(Toevoegen), nameof(Edit), nameof(Weather)
        };

        public override void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.Result is ViewResult
                && context.RouteData.Values["action"] is string action
                && !_sidebarExpandedActions.Contains(action)
                && ViewBag.sidebarcollapsed is null)
            {
                ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            }
        }

        // Alle acties zijn opgesplitst in partials ProjectenController.<Groep>.cs met views in
        // Views/Projecten/<Groep>/ (okt. 2026, structureren; zie STRUCTUREREN_VOORTGANG.md).
        // Hier blijven enkel de velden, de constructor en de gedeelde filter over.
        // ===== Core ===== ProjectenController.Core.cs
        // ===== Clients ===== ProjectenController.Clients.cs
        // ===== Units ===== ProjectenController.Units.cs
        // ===== Contracts ===== ProjectenController.Contracts.cs
        // ===== IncomingInvoices ===== ProjectenController.IncomingInvoices.cs
        // ===== ChangeOrders ===== ProjectenController.ChangeOrders.cs
        // ===== Weather ===== ProjectenController.Weather.cs
        // ===== Media ===== ProjectenController.Media.cs
        // ===== Docs ===== ProjectenController.Docs.cs
        // ===== Insurances ===== ProjectenController.Insurances.cs
        // ===== Sales ===== ProjectenController.Sales.cs
        // ===== Coordinatie ===== ProjectenController.Coordinatie.cs
        // ===== Invoicing ===== ProjectenController.Invoicing.cs
        // ===== Lookups ===== ProjectenController.Lookups.cs
        // ===== Budget ===== ProjectenController.Budget.cs
    }
}

// ── Request DTO voor BudgetVergelijken ────────────────────────────────────────
public class VergelijkRequest
{
    public List<int> VersieIds { get; set; } = new();
}

// ── Request DTOs voor BudgetVerkoop ───────────────────────────────────────────
public class SaveVerkoopRequest
{
    public int BudgetVersieId { get; set; }
    public List<VerkoopLijnDto> Lijnen { get; set; } = new();
}

/// <summary>Eén verkooplijn uit het JS van stap 8. Bewust geen EF-entiteit: System.Text.Json bouwt dan de hele navigatiegraaf
/// (Unit → … → CompanyInfo) op en struikelt daar over botsende propertynamen (postCode/PostCode) → 500 vóór de actie start.</summary>
public class VerkoopLijnDto
{
    public string   EenheidNaam     { get; set; }
    public int?     UnitId          { get; set; }
    public int?     CodeBouw        { get; set; }
    public int?     CodeGrond       { get; set; }
    public bool     IsRuil          { get; set; }
    public decimal? ExtraForfait    { get; set; }
    public decimal? Grondwaarde     { get; set; }
    public decimal? Bouwwaarde      { get; set; }
    public decimal? Vraagprijs      { get; set; }
    public decimal? BouwPrijsPerM2  { get; set; }
    public decimal? GrondPrijsPerM2 { get; set; }
    public byte?    PrijsBron       { get; set; }

    public BudgetVerkoopLijn NaarEntiteit(int versieId, int sortOrder) => new()
    {
        BudgetVersieId  = versieId,
        SortOrder       = sortOrder,
        EenheidNaam     = EenheidNaam,
        UnitId          = UnitId > 0 ? UnitId : null,
        CodeBouw        = CodeBouw,
        CodeGrond       = CodeGrond,
        IsRuil          = IsRuil,
        ExtraForfait    = ExtraForfait,
        Grondwaarde     = Grondwaarde,
        Bouwwaarde      = Bouwwaarde,
        Vraagprijs      = Vraagprijs,
        BouwPrijsPerM2  = BouwPrijsPerM2,
        GrondPrijsPerM2 = GrondPrijsPerM2,
        PrijsBron       = PrijsBron
    };
}

// ── Request DTOs voor BudgetActivityLijnen ────────────────────────────────────
public class SaveActivityLijnenRequest
{
    public int                         BudgetVersieId { get; set; }
    public List<ActivityLijnUpdateDto> Lijnen         { get; set; } = new();
}

public class ActivityLijnUpdateDto
{
    public int      ActivityId                  { get; set; }
    public decimal? AlternatievePrijsPerEenheid { get; set; }
    public decimal? NacalcPrijsPerEenheid       { get; set; }
    public decimal  Correctiefactor             { get; set; } = 1m;
    public bool     IsManueel                   { get; set; }
    /// <summary>Opmerking bij de correctie (39k); null = niet meegestuurd.</summary>
    public string   Opmerking                   { get; set; }
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
