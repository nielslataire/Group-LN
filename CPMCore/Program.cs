using CPMCore.Configuration;
using GroupLN.MarketData.Persistence.Extensions;
using Microsoft.AspNetCore.DataProtection;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Service;
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
using ServiceCore;
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
builder.Services.AddScoped<ServiceCore.Budget.BudgetExcelService>();
builder.Services.AddScoped<IConstructionIssueService, ConstructionIssueService>();
builder.Services.AddScoped<IConstructionIssueReportService, ConstructionIssueReportService>();
builder.Services.AddScoped<IQRCodeService, QRCodeServiceStub>();
builder.Services.AddScoped<IContractorPortalService, ContractorPortalServiceStub>();
builder.Services.AddScoped<IIssueNotificationSenderService, IssueNotificationSenderService>();
builder.Services.AddScoped<IIssueNotificationSchedulerService, IssueNotificationSchedulerService>();
builder.Services.AddScoped<IContractorPortalDigestService, ContractorPortalDigestService>();
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
builder.Services.AddSingleton<IInvoiceTemplate>(sp => new JsonInvoiceTemplate("layoutA", sp.GetRequiredService<SectionRendererFactory>(), sp.GetRequiredService<BandsRenderer>()));
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPermissionResolver, PermissionResolver>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<PermissionConventionFilter>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(configuration.GetSection("Graph"))
    .AddInMemoryTokenCaches();

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
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
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

        context.Response.Redirect($"/Account/Login?error={Uri.EscapeDataString(friendlyMessage)}");
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

app.UseRouting();

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
    if (!(ctx.User.Identity?.IsAuthenticated == true))
    {
        var path = ctx.Request.Path.Value ?? "";
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
