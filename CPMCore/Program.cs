using CPMCore.Data;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Service;
using CPMCore.Services.Octopus;
using CPMCore.Services.Peppol;
using DALCore;
using DALCore.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using FacadeCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using Rotativa.AspNetCore;
using ServiceCore;
using ServiceCore.Invoicing;
using ServiceCore.Invoicing.Pdf;
using ServiceCore.Invoicing.Pdf.Sections;
using SmartBreadcrumbs;
using SmartBreadcrumbs.Extensions;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

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
})
.AddSessionStateTempDataProvider();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Identity / UI context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        connection,
        sqlServerOptions => sqlServerOptions.CommandTimeout(5000));

});

// ⬇️ JOUW DOMEIN CONTEXT (DALCore) — gebruikt dezelfde connection
builder.Services.AddDbContext<cpmRunningContext>(options =>
    options.UseSqlServer(
        connection,
        sqlServerOptions => sqlServerOptions.CommandTimeout(5000))
);

// ⬇️ UnitOfWork + Services via DI (scoped per request)
builder.Services.AddScoped<DALCore.UnitOfWorkCore, DALCore.UnitOfWorkCore>();
builder.Services.AddScoped<FacadeCore.IProjectService, ServiceCore.ProjectService>();
builder.Services.AddScoped<FacadeCore.IUnitService, ServiceCore.UnitService>();
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
builder.Services.AddScoped<IOctopusBookyearService, OctopusBookyearService>();
builder.Services.AddScoped<IOctopusRelationSyncService, OctopusRelationSyncService>();
builder.Services.AddHttpClient<IPeppolDirectoryClient, PeppolDirectoryClient>(client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<IPeppolSender, PeppolSender>();
builder.Services.Configure<OctopusOptions>(builder.Configuration.GetSection("Octopus"));
builder.Services.AddHttpClient<IOctopusApiClient, OctopusApiClient>();
builder.Services.AddScoped<IOctopusTokenManager, OctopusTokenManager>();

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

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders()
.AddRoleManager<RoleManager<IdentityRole>>();

//builder.Services.AddDefaultIdentity<IdentityUser>(options =>
//    options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddDefaultUI()
//    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();
//builder.Services.AddRazorPages();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
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
