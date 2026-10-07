using CPMCore.Configuration;
using GroupLN.MarketData.Persistence.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Services;
using CPMCore.Services.Authorization;
using CPMCore.Services.Octopus;
using CPMCore.Services.Peppol;
using CPMCore.Services.Security;
using CPMCore.Filters;
using DALCore;
using DALCore.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using FacadeCore;
using ServiceCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using Rotativa.AspNetCore;
using ServiceCore.Invoicing;
using ServiceCore.Invoicing.Pdf;
using ServiceCore.Invoicing.Pdf.Sections;
using ServiceCore.Issues;
using ServiceCore.Stubs;
using SmartBreadcrumbs;
using SmartBreadcrumbs.Extensions;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var cultureInfo = new CultureInfo("nl-BE");

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// UserSecrets voor connectiestring
var configuration = builder.Configuration;

var connectionStringBase = configuration["CPMRUNNING:ConnectionString"]
    ?? throw new Exception("CPMRUNNING:ConnectionString ontbreekt");

var dbUser = configuration["CPMRUNNING:DbUser"]
    ?? throw new Exception("CPMRUNNING:DbUser ontbreekt");

var dbPassword = configuration["CPMRUNNING:DbPassword"]
    ?? throw new Exception("CPMRUNNING:DbPassword ontbreekt");

var conStrBuilder = new SqlConnectionStringBuilder(connectionStringBase)
{
    UserID = dbUser,
    Password = dbPassword,
    TrustServerCertificate = true
};

var connection = conStrBuilder.ConnectionString;
//string connectionString = configuration.GetSection("CPMRUNNING")["ConnectionString"].ToString();
//string DbPassword = configuration.GetSection("CPMRUNNING")["DbPassword"];
//string DbUser = configuration.GetSection("CPMRUNNING")["DbUser"];

//var conStrBuilder = new SqlConnectionStringBuilder(connectionString);
//conStrBuilder.Password = DbPassword;
//conStrBuilder.UserID = DbUser;
//conStrBuilder.TrustServerCertificate = true;
//var connection = conStrBuilder.ConnectionString;


// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
    options.Filters.Add<PermissionConventionFilter>();
    options.Filters.Add<CPMCore.Filters.DbUpdateExceptionFilter>();

    // Nederlandse standaardteksten voor model-binding / validatie (i.p.v. de
    // Engelse framework-defaults zoals "The value 'x' is not valid for Y").
    var p = options.ModelBindingMessageProvider;
    p.SetValueIsInvalidAccessor(v => $"De waarde '{v}' is ongeldig.");
    p.SetValueMustBeANumberAccessor(f => $"Het veld {f} moet een getal zijn.");
    p.SetNonPropertyValueMustBeANumberAccessor(() => "De waarde moet een getal zijn.");
    p.SetMissingBindRequiredValueAccessor(f => $"Een waarde voor '{f}' ontbreekt in de aanvraag.");
    p.SetMissingKeyOrValueAccessor(() => "Een waarde is verplicht.");
    p.SetMissingRequestBodyRequiredValueAccessor(() => "De aanvraag bevat geen gegevens.");
    p.SetValueMustNotBeNullAccessor(v => $"De waarde '{v}' is ongeldig.");
    p.SetAttemptedValueIsInvalidAccessor((v, f) => $"De waarde '{v}' is ongeldig voor {f}.");
    p.SetNonPropertyAttemptedValueIsInvalidAccessor(v => $"De waarde '{v}' is ongeldig.");
    p.SetUnknownValueIsInvalidAccessor(f => $"De opgegeven waarde is ongeldig voor {f}.");
    p.SetNonPropertyUnknownValueIsInvalidAccessor(() => "De opgegeven waarde is ongeldig.");

    // [Required] zonder eigen tekst (ook de impliciete op niet-nullable strings): "{veld} is verplicht."
    // i.p.v. "The X field is required." — het gedeelde Foutoverzicht (punt 24) toont ModelState letterlijk.
    options.ModelMetadataDetailsProviders.Add(new CPMCore.Helpers.DutchRequiredMessageProvider());
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    })
.AddSessionStateTempDataProvider();

// JS fetch()-aanroepen sturen het antiforgery-token via deze header (i.p.v. een form field,
// want de body is JSON). Zonder HeaderName kijkt [ValidateAntiForgeryToken] alleen naar form
// fields en falen alle AJAX POSTs met JSON body altijd, ongeacht rechten.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Persisteer Data Protection keys zodat ze app pool recycles overleven
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "dataprotection-keys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("CPMCore");

// Identity / UI context
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//    options.UseSqlServer(
//        connection,
//        sqlServerOptions => sqlServerOptions.CommandTimeout(5000));

//});

var domainDbOptions = new DbContextOptionsBuilder<cpmRunningContext>()
    .UseSqlServer(
        connection,
        sqlServerOptions => sqlServerOptions.CommandTimeout(5000))
    .Options;

// ⬇️ JOUW DOMEIN CONTEXT (DALCore) — gebruikt dezelfde connection
builder.Services.AddDbContext<cpmRunningContext>(options =>
    options.UseSqlServer(
        connection,
        sqlServerOptions => sqlServerOptions.CommandTimeout(5000))
);

// Maak de options ook beschikbaar voor ServiceFactory (gebruik static helpers)
ServiceFactory.Configure(domainDbOptions);

// ⬇️ UnitOfWork + Services via DI (scoped per request)
builder.Services.AddScoped<DALCore.UnitOfWorkCore, DALCore.UnitOfWorkCore>();
builder.Services.AddSingleton<FacadeCore.ICoachmarkDefinitionProvider, CPMCore.Services.CoachmarkDefinitionProvider>();
builder.Services.AddScoped<FacadeCore.ICoachmarkService, ServiceCore.CoachmarkService>();
builder.Services.AddScoped<FacadeCore.IProjectService, ServiceCore.ProjectService>();
builder.Services.AddScoped<FacadeCore.IUnitService, ServiceCore.UnitService>();
builder.Services.AddScoped<FacadeCore.IAuthenticationService, ServiceCore.AuthenticationService>();
builder.Services.AddScoped<FacadeCore.IActivityService, ServiceCore.ActivityService>();
builder.Services.AddScoped<FacadeCore.IBlogArtikelService, ServiceCore.BlogArtikelService>();
builder.Services.AddScoped<FacadeCore.IVacatureService, ServiceCore.VacatureService>();
builder.Services.AddScoped<FacadeCore.IVacatureSollicitatieService, ServiceCore.VacatureSollicitatieService>();
builder.Services.AddScoped<FacadeCore.IEmailTemplateService, ServiceCore.EmailTemplateService>();
builder.Services.AddScoped<FacadeCore.IEmailSendLogService, ServiceCore.EmailSendLogService>();
builder.Services.AddScoped<FacadeCore.IUserSignatureService, ServiceCore.UserSignatureService>();
builder.Services.AddScoped<FacadeCore.IKostprijsService, ServiceCore.KostprijsService>();
builder.Services.AddScoped<FacadeCore.IBudgetPrijsReferentieService, ServiceCore.Budget.BudgetPrijsReferentieService>();
builder.Services.AddScoped<FacadeCore.IBudgetReferentieProjectService, ServiceCore.Budget.BudgetReferentieProjectService>();
builder.Services.AddScoped<FacadeCore.IProvinceService, ServiceCore.ProvinceService>();
builder.Services.AddScoped<FacadeCore.ICompanyService, ServiceCore.CompanyService>();
builder.Services.AddScoped<FacadeCore.ICountryService, ServiceCore.CountryService>();
builder.Services.AddScoped<FacadeCore.IPostalcodeService, ServiceCore.PostalcodeService>();
builder.Services.AddScoped<FacadeCore.IDepartmentService, ServiceCore.DepartmentService>();
builder.Services.AddScoped<FacadeCore.IContactService, ServiceCore.ContactService>();
builder.Services.AddScoped<FacadeCore.IClientService, ServiceCore.ClientService>();
builder.Services.AddScoped<FacadeCore.IInvoicingService, ServiceCore.InvoicingService>();
builder.Services.AddScoped<FacadeCore.IInsuranceService, ServiceCore.InsuranceService>();
builder.Services.AddScoped<IInvoiceQueryService, InvoiceQueryService>();
builder.Services.AddScoped<ICompanyQueryService, CompanyQueryService>();
builder.Services.AddScoped<IIssuerCompanyService, IssuerCompanyService>();
builder.Services.AddScoped<IIssuerBankAccountService, IssuerBankAccountService>();
builder.Services.AddScoped<IHomeHeroProjectService, HomeHeroProjectService>();
builder.Services.AddScoped<ICookieConsentStatsService, CookieConsentStatsService>();
builder.Services.AddScoped<IIssuerSeriesService, IssuerSeriesService>();
builder.Services.AddScoped<IPartyLookupService, PartyLookupService>();
builder.Services.AddScoped<IInvoiceCommandService, InvoiceCommandService>();
builder.Services.AddScoped<IInvoiceNumberingService, InvoiceNumberingService>();
builder.Services.AddScoped<IProjectSupplierLookupService, ProjectSupplierLookupService>();
builder.Services.AddScoped<IInvoiceCommunicationService, InvoiceCommunicationService>();
builder.Services.AddScoped<IInvoiceUblBuilder, InvoiceUblBuilder>();
builder.Services.AddScoped<IInvoiceLayoutTemplateService, InvoiceLayoutTemplateService>();
builder.Services.AddScoped<IOctopusBookyearService, OctopusBookyearService>();
builder.Services.AddScoped<IOctopusRelationSyncService, OctopusRelationSyncService>();
// Documentencentrum
builder.Services.Configure<CPMCore.Services.InvoiceExtraction.InvoiceExtractionOptions>(
    builder.Configuration.GetSection("InvoiceExtraction"));
builder.Services.AddScoped<CPMCore.Services.InvoiceExtraction.IAzureInvoiceAnalysisService,
                            CPMCore.Services.InvoiceExtraction.AzureInvoiceAnalysisService>();
// Offerte inlezen (20c). Twee implementaties achter één interface:
//  - AnthropicQuoteAnalysisService: vision (de uitsnede gaat als afbeelding mee), lay-out-onafhankelijk —
//    de voorkeur zodra QuoteExtraction:AnthropicApiKey gezet is (zelfde sleutel als de MarketData-crawler,
//    lokaal via user-secrets).
//  - AzureQuoteAnalysisService: Azure Document Intelligence "prebuilt-layout" + kolomheuristiek,
//    hergebruikt InvoiceExtractionOptions — terugval als er geen Anthropic-sleutel is.
builder.Services.Configure<CPMCore.Services.QuoteExtraction.QuoteExtractionOptions>(
    builder.Configuration.GetSection(CPMCore.Services.QuoteExtraction.QuoteExtractionOptions.Section));
builder.Services.AddHttpClient(CPMCore.Services.QuoteExtraction.AnthropicQuoteAnalysisService.HttpClientName, client =>
{
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
});
builder.Services.AddScoped<CPMCore.Services.QuoteExtraction.AzureQuoteAnalysisService>();
builder.Services.AddScoped<CPMCore.Services.QuoteExtraction.AnthropicQuoteAnalysisService>();
builder.Services.AddScoped<CPMCore.Services.QuoteExtraction.IQuoteRegionAnalysisService>(sp =>
{
    var anthropic = sp.GetRequiredService<CPMCore.Services.QuoteExtraction.AnthropicQuoteAnalysisService>();
    return anthropic.IsEnabled
        ? anthropic
        : sp.GetRequiredService<CPMCore.Services.QuoteExtraction.AzureQuoteAnalysisService>();
});
builder.Services.AddScoped<ServiceCore.IncomingInvoices.IOctopusIncomingInvoiceSyncService, CPMCore.Services.Octopus.OctopusIncomingInvoiceSyncService>();
builder.Services.AddScoped<FacadeCore.IIncomingInvoiceService, ServiceCore.IncomingInvoices.IncomingInvoiceService>();
// Verrijkingspipeline
builder.Services.AddScoped<FacadeCore.IProjectMatchingService, ServiceCore.IncomingInvoices.ProjectMatchingService>();
builder.Services.AddScoped<FacadeCore.IContractMatchingService, ServiceCore.IncomingInvoices.ContractMatchingService>();
builder.Services.AddScoped<FacadeCore.IAccountingSuggestionService, ServiceCore.IncomingInvoices.AccountingSuggestionService>();
builder.Services.AddScoped<FacadeCore.IInvoiceEnrichmentPipelineService, CPMCore.Services.InvoiceEnrichment.InvoiceEnrichmentPipelineService>();
builder.Services.AddHttpClient<IPeppolDirectoryClient, PeppolDirectoryClient>(client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<IPeppolSender, PeppolSender>();
builder.Services.Configure<OctopusOptions>(builder.Configuration.GetSection("Octopus"));
builder.Services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection("Features"));
builder.Services.Configure<CPMCore.Configuration.GlV2PdfCompanyOptions>(builder.Configuration.GetSection("GlV2PdfCompany"));
builder.Services.AddHttpClient<IOctopusApiClient, OctopusApiClient>();
builder.Services.AddHttpClient<FacadeCore.IRouteService, ServiceCore.RouteService>();
builder.Services.AddScoped<IOctopusTokenManager, OctopusTokenManager>();
builder.Services.AddScoped<FacadeCore.IProjectVoortgangService, ServiceCore.ProjectVoortgangService>();
builder.Services.AddScoped<FacadeCore.IBudgetService, ServiceCore.BudgetWizardService>();
builder.Services.AddScoped<ServiceCore.Budget.BouwIndexService>();
builder.Services.AddScoped<ServiceCore.Budget.SIndexScraperService>();
builder.Services.AddScoped<ServiceCore.Budget.I2021SyncService>();
builder.Services.AddSingleton<ServiceCore.Budget.BudgetFormulaRegistry>();
builder.Services.AddScoped<ServiceCore.Budget.BudgetFormulaService>();
builder.Services.AddHttpClient("SIndexScraper").ConfigurePrimaryHttpMessageHandler(() =>
    new System.Net.Http.HttpClientHandler { AllowAutoRedirect = true });
builder.Services.AddHttpClient("I2021Sync").ConfigurePrimaryHttpMessageHandler(() =>
    new System.Net.Http.HttpClientHandler { AllowAutoRedirect = true });
builder.Services.AddScoped<ServiceCore.Budget.BudgetActivityService>();
builder.Services.AddScoped<ServiceCore.Budget.BudgetActivityFormuleService>();
builder.Services.AddScoped<ServiceCore.Budget.BudgetBerekeningService>();
builder.Services.AddScoped<FacadeCore.IVerkoopVoorstelService, ServiceCore.Budget.VerkoopVoorstelService>();
builder.Services.AddScoped<FacadeCore.IMarktReferentieService, CPMCore.Services.MarktReferentieService>();
builder.Services.AddScoped<ServiceCore.Budget.BudgetExcelService>();
builder.Services.AddScoped<IConstructionIssueService, ConstructionIssueService>();
builder.Services.AddScoped<IConstructionIssueReportService, ConstructionIssueReportService>();
builder.Services.AddScoped<IQRCodeService, QRCodeServiceStub>();
builder.Services.AddScoped<IContractorPortalService, ContractorPortalServiceStub>();
builder.Services.AddScoped<IIssueNotificationSenderService, IssueNotificationSenderService>();
builder.Services.AddScoped<IIssueNotificationSchedulerService, IssueNotificationSchedulerService>();
builder.Services.AddScoped<IContractorPortalDigestService, ContractorPortalDigestService>();

// ── Trajectopvolging ─────────────────────────────────────────────────────────
builder.Services.AddScoped<FacadeCore.IProjecttrajectService, ServiceCore.Traject.ProjecttrajectService>();
builder.Services.AddScoped<FacadeCore.IMijlpaalService, ServiceCore.Traject.MijlpaalService>();
builder.Services.AddScoped<FacadeCore.ITrajectSjabloonService, ServiceCore.Traject.TrajectSjabloonService>();
builder.Services.AddScoped<FacadeCore.ITrajectInstantiationService, ServiceCore.Traject.TrajectInstantiationService>();
builder.Services.AddScoped<FacadeCore.IMijlpaalBindingResolver, ServiceCore.Traject.MijlpaalBindingResolver>();
builder.Services.AddScoped<FacadeCore.ITrajectRecalculationService, ServiceCore.Traject.TrajectRecalculationService>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerDispatcher, ServiceCore.Traject.TrajectTriggerDispatcher>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.VerwittigRolAction>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.VerwittigGebruikerAction>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.PlanHerinneringAction>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.DeblokkeerVolgendeFaseAction>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.ZetProjectVlagAction>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.ZetProjectStatusAction>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.MaakDossierAction>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.StuurDossierAanvraagMailAction>();
builder.Services.AddScoped<FacadeCore.IProjectDossierService, ServiceCore.Traject.ProjectDossierService>();
builder.Services.AddScoped<FacadeCore.IDocumentService, ServiceCore.Documents.DocumentService>();
builder.Services.AddScoped<FacadeCore.ISigningService, ServiceCore.Documents.SigningService>();
builder.Services.AddScoped<CPMCore.Services.DocStorageService>();
builder.Services.AddScoped<CPMCore.Services.ChangeOrderPdfService>();
builder.Services.AddScoped<FacadeCore.INutsAansluitingService, ServiceCore.Traject.NutsAansluitingService>();
builder.Services.AddScoped<FacadeCore.IProjectTaakService, ServiceCore.Traject.ProjectTaakService>();
builder.Services.AddScoped<FacadeCore.ITrajectTriggerAction, ServiceCore.Traject.TriggerActions.MaakTaakAction>();
builder.Services.AddSingleton<CPMCore.Services.TrajectHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CPMCore.Services.TrajectHostedService>());

builder.Services.AddSingleton<IssueNotificationHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<IssueNotificationHostedService>());
builder.Services.AddSingleton<VoortgangHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<VoortgangHostedService>());

// ── Marktanalyse ─────────────────────────────────────────────────────────────
builder.Services.AddMarketDataPersistence(configuration);
builder.Services.AddScoped<IMarktanalyseService, MarktanalyseService>();
builder.Services.AddScoped<IMarketDataStatusService, MarketDataStatusService>();

builder.Services.AddSingleton<TemplateInterpolator>();
builder.Services.AddSingleton<BandsRenderer>();
builder.Services.AddSingleton<ISectionRenderer>(sp => sp.GetRequiredService<BandsRenderer>());
builder.Services.AddSingleton<ISectionRenderer, DefaultHeaderRenderer>();
builder.Services.AddSingleton<ISectionRenderer, HeaderRenderer>();
builder.Services.AddSingleton<ISectionRenderer, HeadlineRenderer>();
builder.Services.AddSingleton<ISectionRenderer, PartiesRenderer>();
builder.Services.AddSingleton<ISectionRenderer, LinesTableRenderer>();
builder.Services.AddSingleton<ISectionRenderer, TotalsRenderer>();
builder.Services.AddSingleton<ISectionRenderer, PaymentRenderer>();
builder.Services.AddSingleton<ISectionRenderer, LegalRenderer>();
builder.Services.AddSingleton<ISectionRenderer, FooterRenderer>();
builder.Services.AddSingleton<ISectionRenderer, DefaultFooterRenderer>();
builder.Services.AddSingleton<SectionRendererFactory>(sp => new SectionRendererFactory(sp.GetServices<ISectionRenderer>()));
// layoutA = de gl-v2-factuur (design-handoff 35b, okt. 2026); layoutB blijft de JSON-layout.
builder.Services.AddSingleton<IInvoiceTemplate, CPMCore.Services.Invoicing.GlV2InvoiceTemplate>();
builder.Services.AddSingleton<IInvoiceTemplate>(sp => new JsonInvoiceTemplate("layoutB", sp.GetRequiredService<SectionRendererFactory>(), sp.GetRequiredService<BandsRenderer>()));
builder.Services.AddSingleton<IInvoiceTemplateRegistry, InvoiceTemplateRegistry>();
builder.Services.AddSingleton<IEpcQrService, EpcQrService>();
builder.Services.AddSingleton<IStructuredReferenceService, StructuredReferenceService>();
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
builder.Services.AddSingleton<IConverter, SynchronizedConverter>(serviceProvider =>
    new SynchronizedConverter(new PdfTools())
);


builder.Services.AddScoped<ICpmUserAccessService, CpmUserAccessService>();
builder.Services.AddScoped<IEntraGuestInvitationService, EntraGuestInvitationService>();
builder.Services.AddScoped<IContractorInviteService, ContractorInviteService>();
builder.Services.AddScoped<IPortalInviteNotifier, PortalInviteNotifier>();
builder.Services.AddSingleton<IResendInviteUrlBuilder, ResendInviteUrlBuilder>();
builder.Services.AddScoped<ISecurityService, SecurityService>();

// ── Elektronisch ondertekenen (ONDERTEKENEN_VOORSTEL.md, fase 0) ─────────────────────────────
// Storage-client: één HttpClient-gebaseerde client voor de externe Storage API, vervangt de privé
// helpers in ProjectenController/ProjectIssuesController (die delegeren er nu naartoe).
builder.Services.AddHttpClient<FacadeCore.Signing.IAssetStorageClient, ServiceCore.Signing.AssetStorageClient>();
builder.Services.Configure<ServiceCore.Signing.SigningOptions>(configuration.GetSection(ServiceCore.Signing.SigningOptions.SectionName));
// Fail closed: staat de module aan, dan moet de configuratie kloppen vóór er ook maar één request
// bediend wordt — een HMAC-sleutel of publieke URL die pas bij de eerste ondertekening blijkt te
// ontbreken, is precies wat we niet willen.
{
    var signingFeatures = configuration.GetSection("Features").Get<FeatureFlagsOptions>() ?? new FeatureFlagsOptions();
    if (signingFeatures.EnableSigning)
        (configuration.GetSection(ServiceCore.Signing.SigningOptions.SectionName).Get<ServiceCore.Signing.SigningOptions>() ?? new ServiceCore.Signing.SigningOptions()).Validate();
}
builder.Services.AddScoped<FacadeCore.Signing.ISigningEvidenceStore, ServiceCore.Signing.SigningEvidenceStore>();
builder.Services.AddScoped<FacadeCore.Signing.ISigningNotifier, ServiceCore.Signing.SigningNotifier>();
// Fase 2: ondertekend document + auditrapport (CPMCore/Documents, QuestPDF + PdfSharpCore).
builder.Services.AddScoped<FacadeCore.Signing.ISigningDocumentRenderer, CPMCore.Services.Signing.SignedDocumentComposer>();
// Strategy-registraties (zelfde recept als ITrajectTriggerAction): methodes/kanalen/bronnen op sleutel.
builder.Services.AddScoped<FacadeCore.Signing.ISignatureMethodProvider, ServiceCore.Signing.InternalSesProvider>();
builder.Services.AddScoped<FacadeCore.Signing.IVerificationMethod, ServiceCore.Signing.EmailOtpMethod>();
builder.Services.AddScoped<FacadeCore.Signing.IVerificationMethod, ServiceCore.Signing.SmsOtpMethod>();
builder.Services.AddScoped<FacadeCore.Signing.IMessageChannel, ServiceCore.Signing.EmailChannel>();
builder.Services.AddScoped<FacadeCore.Signing.IMessageChannel, ServiceCore.Signing.SmsChannel>();
// Documentbronnen (fase 1: wijzigingsopdrachten). ISmsProvider-adapters komen in fase 4.
builder.Services.AddScoped<CPMCore.Services.Signing.ChangeOrderPdfBuilder>();
builder.Services.AddScoped<FacadeCore.Signing.ISigningDocumentSource, CPMCore.Services.Signing.ChangeOrderSigningSource>();
builder.Services.AddScoped<ServiceCore.Signing.SigningRegistry>();
builder.Services.AddScoped<FacadeCore.Signing.ISigningService, ServiceCore.Signing.SigningService>();
// Fase 3: achtergrondjob (verlopen/herinneringen/retentie/hervat-finalisatie) + /api/trigger/signing hieronder.
builder.Services.AddSingleton<CPMCore.Services.SigningHostedService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CPMCore.Services.SigningHostedService>());

// Rate limiting (§6.4) — eerste gebruik in de app; enkel de signing-policies zijn benoemd, dus
// geen enkele bestaande route krijgt een limiet. Partities per client-IP; de limiet per
// ondertekenaar zit in SigningService zelf. Boven de limiet: 429 met een neutrale tekst.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Te veel aanvragen. Probeer het over enkele minuten opnieuw.", token);
    };
    static string ClientKey(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    static System.Threading.RateLimiting.FixedWindowRateLimiterOptions Window(int permits) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromMinutes(10),
        QueueLimit = 0,
        AutoReplenishment = true,
    };
    options.AddPolicy("signing-open", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(30)));
    options.AddPolicy("signing-otp-request", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(10)));
    options.AddPolicy("signing-otp-verify", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(20)));
    options.AddPolicy("signing-sign", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(10)));
    options.AddPolicy("signing-verify-page", ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(60)));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPermissionResolver, PermissionResolver>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<PermissionConventionFilter>();

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

authBuilder
.AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(configuration.GetSection("Graph"))
    .AddInMemoryTokenCaches();

// ── GOOGLE-LOGIN (tweede OIDC-schema, rechtstreeks naar Google, buiten Entra om) ─────────────────
// Voor portaalgasten (aannemers/klanten) met een Google-account, ook Google Workspace op een eigen
// domein: Entra B2B kan die niet federeren en dwong hen een Microsoft-account aan te maken. Enkel
// registreren als de client geconfigureerd is: een OIDC-handler zonder ClientId faalt bij opties-
// validatie en dat gebeurt in UseAuthentication voor élke request (alle request-handler-schema's
// worden daar geïnstantieerd), dus dan zou heel de app plat liggen.
var googleClientId     = configuration["Google:ClientId"];
var googleClientSecret = configuration["Google:ClientSecret"];
var googleLoginEnabled = !string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret);
builder.Services.AddSingleton(new GoogleLoginSettings { Enabled = googleLoginEnabled });

if (googleLoginEnabled)
{
    authBuilder.AddOpenIdConnect(GoogleAuthDefaults.Scheme, "Google", options =>
    {
        options.SignInScheme  = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Authority     = "https://accounts.google.com";
        options.ClientId      = googleClientId;
        options.ClientSecret  = googleClientSecret;
        options.ResponseType  = Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectResponseType.Code;
        options.ResponseMode  = Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectResponseMode.Query;
        options.UsePkce       = true;
        options.SaveTokens    = false;
        // Eigen paden: de Entra-handler bezit al /signin-oidc, /signout-callback-oidc en /signout-oidc.
        options.CallbackPath          = GoogleAuthDefaults.CallbackPath;
        options.SignedOutCallbackPath = "/signout-callback-google";
        options.RemoteSignOutPath     = "/signout-google";
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        options.Scope.Add("profile");
        // Alles wat we nodig hebben (sub, email, email_verified, name, picture) zit in het id_token.
        options.GetClaimsFromUserInfoEndpoint = false;
        // Ruwe claimnamen behouden (sub/email/...) i.p.v. de lange schemas.xmlsoap-typen.
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = System.Security.Claims.ClaimTypes.Role;
        options.Prompt = "select_account"; // Zelfde gedrag als Entra: geen stille SSO-herauthenticatie

        options.Events.OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            var subject = principal?.FindFirst("sub")?.Value
                ?? principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = principal?.FindFirst("email")?.Value
                ?? principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var emailVerified = string.Equals(principal?.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase);
            var picture = principal?.FindFirst("picture")?.Value;
            var hostedDomain = principal?.FindFirst("hd")?.Value; // enkel bij Google Workspace

            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Authentication");
            logger.LogInformation(
                "Google login claims: sub={Sub}, email={Email}, verified={Verified}, hd={Hd}",
                subject ?? "<null>", email ?? "<null>", emailVerified, hostedDomain ?? "<geen>");

            var accessService = context.HttpContext.RequestServices.GetRequiredService<ICpmUserAccessService>();
            var accessResult = await accessService.ResolveGoogleAsync(subject, email, emailVerified, context.HttpContext.RequestAborted);

            if (accessResult == null || principal?.Identity is not System.Security.Claims.ClaimsIdentity identity)
            {
                context.Fail(GoogleLoginErrors.NotLinked);
                return;
            }

            await accessService.SyncGooglePhotoAsync(accessResult, picture, context.HttpContext.RequestAborted);
            accessService.ApplyClaims(identity, accessResult);
        };

        options.Events.OnRemoteFailure = context =>
        {
            var error = context.Failure?.Message ?? "";
            var friendlyMessage =
                error.Contains(GoogleLoginErrors.NotLinked)
                    ? "Dit Google-account is niet gekend als uitgenodigde gebruiker. Meld u aan met het e-mailadres waarop u de uitnodiging ontving, of neem contact op met de beheerder."
                : error.Contains("access_denied", StringComparison.OrdinalIgnoreCase)
                    ? "Aanmelden met Google werd geannuleerd."
                : "Inloggen met Google mislukt. Probeer opnieuw of neem contact op met de beheerder.";

            context.Response.Redirect(LoginRedirect.WithError(context.Properties, friendlyMessage));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization(options =>
{
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .RequireClaim(CPMCore.Helpers.CpmClaims.UserId)
    .Build();
options.AddPolicy("CpmAdmin", policy =>
    policy.RequireAssertion(context =>
        context.User.Claims
            .Where(claim => claim.Type == System.Security.Claims.ClaimTypes.Role)
            .Any(claim =>
                string.Equals(claim.Value, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(claim.Value, "Administrator", StringComparison.OrdinalIgnoreCase))));
});

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    // Secure altijd (productie is https); in Development laat het http://localhost-profiel nog toe.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    // Lax, niet Strict: Strict zou de terugkeer van Microsoft/Google na het inloggen (cross-site redirect) breken.
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;

    // Sessie blijft bestaan na het sluiten van de app/browser (PWA!) voor medewerkers; portaalgasten
    // (aannemers/klanten, vaker op een gedeeld of vreemd toestel) behouden de sessiecookie zoals voorheen.
    options.Events.OnSigningIn = context =>
    {
        var userType = context.Principal?.FindFirst(CPMCore.Helpers.CpmClaims.UserType)?.Value ?? "internal";
        if (userType == "internal")
        {
            context.Properties.IsPersistent = true;
        }
        return Task.CompletedTask;
    };

    // Bij elke aanvraag: (1) absolute bovengrens van 30 dagen sinds het inloggen, ook al verlengt de
    // glijdende vervaldatum de cookie; (2) de gebruiker moet nog actief zijn — een gedeactiveerde
    // gebruiker of verloren telefoon verliest zo binnen enkele minuten toegang i.p.v. na 14 dagen.
    // De actief-controle wordt 2 minuten onthouden per gebruiker (geen databasequery per aanvraag).
    options.Events.OnValidatePrincipal = async context =>
    {
        var issued = context.Properties.IssuedUtc;
        if (issued.HasValue && DateTimeOffset.UtcNow - issued.Value > TimeSpan.FromDays(30))
        {
            context.RejectPrincipal();
            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        if (!int.TryParse(context.Principal?.FindFirst(CPMCore.Helpers.CpmClaims.UserId)?.Value, out var userId)) return;

        var cache = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        var cacheKey = "cpm:user-active:" + userId;
        bool isActive = false;
        if (cache == null || !Microsoft.Extensions.Caching.Memory.CacheExtensions.TryGetValue<bool>(cache, cacheKey, out isActive))
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<DALCore.Models.cpmRunningContext>();
            isActive = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.IsActive).FirstOrDefaultAsync(context.HttpContext.RequestAborted);
            if (cache != null) Microsoft.Extensions.Caching.Memory.CacheExtensions.Set(cache, cacheKey, isActive, TimeSpan.FromMinutes(2));
        }

        if (!isActive)
        {
            context.RejectPrincipal();
            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };
});
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Prompt = "select_account"; // Voorkom stille SSO-herauthenticatie
    options.TokenValidationParameters.RoleClaimType = System.Security.Claims.ClaimTypes.Role;
    options.Events.OnTokenValidated = async context =>
    {
        var oid = context.Principal?.FindFirst("oid")?.Value
            ?? context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var tid = context.Principal?.FindFirst("tid")?.Value
            ?? context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        var candidateEmails = new[]
        {
            context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            context.Principal?.FindFirst("preferred_username")?.Value,
            context.Principal?.FindFirst("upn")?.Value,
            context.Principal?.FindFirst("email")?.Value,
            context.Principal?.FindFirst("mail")?.Value,
            // Entra B2B-specifieke claims
            context.Principal?.FindFirst("signInNames.emailAddress")?.Value,
            context.Principal?.FindFirst("otherMails")?.Value,
        };

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Authentication");

        logger.LogInformation(
            "Entra login claims: oid={Oid}, emails={Emails}",
            oid ?? "<null>",
            string.Join(", ", candidateEmails.Where(value => !string.IsNullOrWhiteSpace(value))));

        var accessService = context.HttpContext.RequestServices.GetRequiredService<ICpmUserAccessService>();
        var accessResult = await accessService.ResolveAsync(oid, candidateEmails, context.HttpContext.RequestAborted, tid);

        if (accessResult == null || context.Principal?.Identity is not System.Security.Claims.ClaimsIdentity identity)
        {
            context.Fail("Geen toegang tot CPMCore.");
            return;
        }

        await accessService.SyncUserPhotoAsync(accessResult, context.HttpContext.RequestAborted);
        accessService.ApplyClaims(identity, accessResult);
    };

    options.Events.OnRemoteFailure = context =>
    {
        var error = context.Failure?.Message ?? "";

        // AADSTS90123: Email OTP niet ingeschakeld / claim issuance policy blocked
        // AADSTS50020 / access_denied: gebruiker geweigerd door tenant policy
        var friendlyMessage =
            error.Contains("AADSTS50020")
                ? "Uw account is nog niet geactiveerd als gast in ons systeem. Controleer uw e-mail voor een uitnodiging of neem contact op met de beheerder."
            : error.Contains("AADSTS90123") || error.Contains("access_denied")
                ? "Toegang geweigerd door Microsoft. Neem contact op met de beheerder."
            : "Inloggen mislukt. Probeer opnieuw of neem contact op met de beheerder.";

        context.Response.Redirect(LoginRedirect.WithError(context.Properties, friendlyMessage));
        context.HandleResponse();
        return Task.CompletedTask;
    };
});


// Custom locations zoeker toevoegen
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
});

// SMTP server
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<CPMCore.Services.GraphMailSender>();

// BREADCRUMBS
builder.Services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options =>
{
    // Optioneel: laat opties leeg, je gebruikt toch je eigen view
});



//builder.Services.AddBreadcrumbs(typeof(CPMCore.Controllers.HomeController).Assembly, options =>
//{
//    // Voorbeeld extra opties...
//});


//QUESTPDF

QuestPDF.Settings.License = LicenseType.Community;
RegisterAvenirFonts(builder.Environment);

var app = builder.Build();

// ROTATIVA INSTELLEN VOOR PDFS
RotativaConfiguration.Setup(app.Environment.WebRootPath, "lib/rotativa");

// Configure the HTTP request pipeline.

// Vooraan, vóór alles wat RemoteIpAddress/schema leest (rate limiter, signing-audit-IP's, HTTPS-
// redirect): SmarterASP.NET is gedeelde IIS-hosting, dus de app zit altijd achter IIS op dezelfde
// host. ForwardLimit=1 + lege KnownNetworks/KnownProxies = vertrouw exact die ene, onmiddellijke hop
// (Microsoft's eigen aanbeveling voor "IIS vóór Kestrel/ASP.NET Core op dezelfde machine"). Zonder
// dit zag de signingmodule's rate limiter en IP-logging altijd het IIS-adres i.p.v. de echte
// ondertekenaar (DEPLOY.md, "Operationele randvoorwaarden" — fase 3 lost dit op). Onbevestigde
// aanname: als SmarterASP zelf nog een laag vóór IIS heeft (CDN/eigen load balancer), volstaat
// ForwardLimit=1 niet — dat is van hieruit niet te verifiëren.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1,
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();


var supportedCultures = new[] { new CultureInfo("nl-BE") };

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("nl-BE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

app.UseRequestLocalization(localizationOptions);

// Security headers voor de publieke signing-pagina's (/ondertekenen, /verifieer) — vóór routing,
// raakt geen enkele andere route (ONDERTEKENEN_VOORSTEL.md §6.5).
app.UseMiddleware<CPMCore.Middleware.SigningSecurityHeadersMiddleware>();
app.UseRouting();
// Na UseRouting (de limiter leest de endpoint-metadata [EnableRateLimiting]); enkel benoemde
// policies, dus zonder attribuut geen limiet.
app.UseRateLimiter();

// ── EXTERNE TRIGGER ENDPOINTS (vóór auth – geen login vereist) ────────────────
app.Map("/api/trigger", triggerApp =>
{
    triggerApp.Run(async ctx =>
    {
        var cfg = ctx.RequestServices.GetRequiredService<IConfiguration>();
        var path = ctx.Request.Path.Value ?? "";

        if (path.Equals("/issue-notifications", StringComparison.OrdinalIgnoreCase)
            && ctx.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var expectedKey = cfg["TriggerKeys:IssueNotifications"];
            var key = ctx.Request.Query["key"].FirstOrDefault();
            if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new { error = "Ongeldige sleutel." });
                return;
            }
            var hosted = ctx.RequestServices.GetRequiredService<IssueNotificationHostedService>();
            _ = Task.Run(() => hosted.RunJobsAsync("http-trigger"));
            ctx.Response.StatusCode = 202;
            await ctx.Response.WriteAsJsonAsync(new { status = "Accepted", timestamp = DateTime.UtcNow });
            return;
        }

        if (path.Equals("/traject-recalc", StringComparison.OrdinalIgnoreCase)
            && ctx.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var expectedKey = cfg["TriggerKeys:TrajectRecalc"] ?? cfg["TriggerKeys:IssueNotifications"];
            var key = ctx.Request.Query["key"].FirstOrDefault();
            if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new { error = "Ongeldige sleutel." });
                return;
            }
            var trajectHosted = ctx.RequestServices.GetRequiredService<TrajectHostedService>();
            _ = Task.Run(() => trajectHosted.RunAsync("http-trigger"));
            ctx.Response.StatusCode = 202;
            await ctx.Response.WriteAsJsonAsync(new { status = "Accepted", timestamp = DateTime.UtcNow });
            return;
        }

        if (path.Equals("/signing", StringComparison.OrdinalIgnoreCase)
            && ctx.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var expectedKey = cfg["TriggerKeys:Signing"];
            var key = ctx.Request.Query["key"].FirstOrDefault();
            if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
            {
                ctx.Response.StatusCode = 401;
                await ctx.Response.WriteAsJsonAsync(new { error = "Ongeldige sleutel." });
                return;
            }
            var signingHosted = ctx.RequestServices.GetRequiredService<CPMCore.Services.SigningHostedService>();
            _ = Task.Run(() => signingHosted.RunJobsAsync("http-trigger"));
            ctx.Response.StatusCode = 202;
            await ctx.Response.WriteAsJsonAsync(new { status = "Accepted", timestamp = DateTime.UtcNow });
            return;
        }

        if (path.Equals("/ping", StringComparison.OrdinalIgnoreCase))
        {
            await ctx.Response.WriteAsJsonAsync(new { status = "alive", timestamp = DateTime.UtcNow });
            return;
        }

        ctx.Response.StatusCode = 404;
    });
});

//TE VERWIJDEREN ALS DE BEVEILIGING MOET GETEST WORDEN
//if (app.Environment.IsDevelopment())
//    app.MapControllers().AllowAnonymous();
//else
//    app.MapControllers();

app.UseAuthentication();

// ── PORTAAL-SHORTCUTS: redirect naar login met juiste type zodat de loginpagina de juiste layout toont ──
// Redirect (302) i.p.v. path rewriting: UseRouting is al gelopen en rewriting had geen effect.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    var isAuthenticated = ctx.User.Identity?.IsAuthenticated == true;

    if (!isAuthenticated)
    {
        if (path is "/aannemer" or "/Aannemer" or "/portaal" or "/Portaal" or "/werfportaal" or "/Werfportaal")
        {
            ctx.Response.Redirect("/Account/Login?type=contractor&returnUrl=/Werfportaal");
            return;
        }
        if (path is "/klantenportaal" or "/Klantenportaal")
        {
            ctx.Response.Redirect("/Account/Login?type=customer&returnUrl=/Klantenportaal");
            return;
        }
    }
    else
    {
        // Al aangemeld (bv. de link uit de uitnodigingsmail geopend in een browser waar nog een
        // sessie loopt): de snelkoppelingen bestaan niet als route, dus zonder dit gaf dat een 404.
        if (path is "/aannemer" or "/Aannemer" or "/portaal" or "/Portaal" or "/werfportaal")
        {
            ctx.Response.Redirect("/Werfportaal");
            return;
        }
        if (path is "/klantenportaal")
        {
            ctx.Response.Redirect("/Klantenportaal");
            return;
        }
    }
    await next();
});

app.UseMiddleware<PermissionContextMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

void RegisterAvenirFonts(IWebHostEnvironment environment)
{
    try
    {
        var fontsDirectory = Path.Combine(environment.ContentRootPath, "wwwroot", "fonts");
        if (!Directory.Exists(fontsDirectory))
            return;

        var fontFiles = new[]
        {
            "Avenir-Book.ttf",
            "Avenir-BookOblique.ttf",
            "Avenir-Black.ttf",
            "Avenir-BlackOblique.ttf",
            "Avenir-Heavy.ttf",
            "Avenir-HeavyOblique.ttf",
            "Avenir-Medium.ttf",
            "Avenir-MediumOblique.ttf",
            "Avenir-Light.ttf",
            "Avenir-LightOblique.ttf",
            "Avenir-Oblique.ttf",
            "Avenir-Roman.ttf"
        };

        foreach (var fontFile in fontFiles)
        {
            var fontPath = Path.Combine(fontsDirectory, fontFile);
            if (!File.Exists(fontPath))
                continue;

            using var stream = File.OpenRead(fontPath);
            FontManager.RegisterFont(stream);
        }
    }
    catch
    {
        // ignored: fall back to default QuestPDF font configuration
    }
}

//app.MapRazorPages();

app.Run();
