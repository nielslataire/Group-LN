using CPMCore.Data;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Service;
using DALCore;
using DALCore.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using FacadeCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rotativa.AspNetCore;
using ServiceCore;
using SmartBreadcrumbs;
using SmartBreadcrumbs.Extensions;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

var builder = WebApplication.CreateBuilder(args);

// UserSecrets voor connectiestring
ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
IConfiguration configuration = configurationBuilder.AddUserSecrets<Program>().Build();
string connectionString = configuration.GetSection("CPMRUNNING")["ConnectionString"].ToString();
string DbPassword = configuration.GetSection("CPMRUNNING")["DbPassword"];
string DbUser = configuration.GetSection("CPMRUNNING")["DbUser"];

var conStrBuilder = new SqlConnectionStringBuilder(connectionString);
conStrBuilder.Password = DbPassword;
conStrBuilder.UserID = DbUser;
conStrBuilder.TrustServerCertificate = true;
var connection = conStrBuilder.ConnectionString;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Identity / UI context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connection,
        sqlServerOptions => sqlServerOptions.CommandTimeout(5000))
);

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
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

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

//app.MapRazorPages();

app.Run();
