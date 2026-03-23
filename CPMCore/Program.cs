using CPMCore.Configuration;
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



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

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
builder.Services.AddScoped<FacadeCore.IProjectService, ServiceCore.ProjectService>();
builder.Services.AddScoped<FacadeCore.IUnitService, ServiceCore.UnitService>();
builder.Services.AddScoped<FacadeCore.IAuthenticationService, ServiceCore.AuthenticationService>();
builder.Services.AddScoped<FacadeCore.IActivityService, ServiceCore.ActivityService>();
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
builder.Services.AddHttpClient<IPeppolDirectoryClient, PeppolDirectoryClient>(client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<IPeppolSender, PeppolSender>();
builder.Services.Configure<OctopusOptions>(builder.Configuration.GetSection("Octopus"));
builder.Services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection("Features"));
builder.Services.AddHttpClient<IOctopusApiClient, OctopusApiClient>();
builder.Services.AddScoped<IOctopusTokenManager, OctopusTokenManager>();
builder.Services.AddScoped<IConstructionIssueService, ConstructionIssueService>();
builder.Services.AddScoped<IConstructionIssueReportService, ConstructionIssueReportService>();
builder.Services.AddScoped<IQRCodeService, QRCodeServiceStub>();
builder.Services.AddScoped<IContractorPortalService, ContractorPortalServiceStub>();

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
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPermissionResolver, PermissionResolver>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<PermissionConventionFilter>();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
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
    options.TokenValidationParameters.RoleClaimType = System.Security.Claims.ClaimTypes.Role;
    options.Events.OnTokenValidated = async context =>
    {
        var oid = context.Principal?.FindFirst("oid")?.Value
            ?? context.Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var candidateEmails = new[]
        {
            context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            context.Principal?.FindFirst("preferred_username")?.Value,
            context.Principal?.FindFirst("upn")?.Value,
            context.Principal?.FindFirst("email")?.Value,
            context.Principal?.FindFirst("mail")?.Value
        };

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Authentication");

        logger.LogInformation(
            "Entra login claims: oid={Oid}, emails={Emails}",
            oid ?? "<null>",
            string.Join(", ", candidateEmails.Where(value => !string.IsNullOrWhiteSpace(value))));

        var accessService = context.HttpContext.RequestServices.GetRequiredService<ICpmUserAccessService>();
        var accessResult = await accessService.ResolveAsync(oid, candidateEmails, context.HttpContext.RequestAborted);

        if (accessResult == null || context.Principal?.Identity is not System.Security.Claims.ClaimsIdentity identity)
        {
            context.Fail("Geen toegang tot CPMCore.");
            return;
        }

        await accessService.SyncUserPhotoAsync(accessResult, context.HttpContext.RequestAborted);
        accessService.ApplyClaims(identity, accessResult);
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

//TE VERWIJDEREN ALS DE BEVEILIGING MOET GETEST WORDEN
//if (app.Environment.IsDevelopment())
//    app.MapControllers().AllowAnonymous();
//else
//    app.MapControllers();

app.UseAuthentication();
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
