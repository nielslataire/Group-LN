using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CPMCore.Data;
using CPMCore.Models;
using System.Data.SqlClient;
using CPMCore.Helpers;
using Microsoft.AspNetCore.Mvc.Razor;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.Logging;



var builder = WebApplication.CreateBuilder(args);


// 🔧 Configuratie inladen
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

// 🔑 Connection details ophalen
string connectionString = configuration.GetSection("CPMRUNNING")["ConnectionString"];
string DbPassword = configuration.GetSection("CPMRUNNING")["DbPassword"];
string DbUser = configuration.GetSection("CPMRUNNING")["DbUser"];

// 🔨 Connection string bouwen
var conStrBuilder = new SqlConnectionStringBuilder(connectionString)
{
    Password = DbPassword,
    UserID = DbUser,
    TrustServerCertificate = true
};

var connection = conStrBuilder.ConnectionString;

// 📝 LOGGING – log naar console
//if (builder.Environment.IsDevelopment())
//{
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("ConnectionLogger");
    Console.WriteLine("=== DEBUG: Final connection string ===");
    Console.WriteLine(connection);
    Console.WriteLine("======================================");
//}


// ✅ Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connection,
        sqlServerOptions => sqlServerOptions.CommandTimeout(5000)
    )
);

builder.Services.AddSingleton<IConverter, SynchronizedConverter>(serviceProvider => new SynchronizedConverter(new PdfTools()));

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
builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();

//builder.Services.Configure<IdentityOptions>(options =>
//{
//    // Password settings.
//    options.Password.RequireDigit = true;
//    options.Password.RequireLowercase = true;
//    options.Password.RequireNonAlphanumeric = true;
//    options.Password.RequireUppercase = true;
//    options.Password.RequiredLength = 6;
//    options.Password.RequiredUniqueChars = 1;

//    // Lockout settings.
//    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
//    options.Lockout.MaxFailedAccessAttempts = 5;
//    options.Lockout.AllowedForNewUsers = true;

//    // User settings.
//    options.User.AllowedUserNameCharacters =
//    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
//    options.User.RequireUniqueEmail = false;
//});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

//Custom locations zoeker toevoegen
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new CustomViewLocationExpander());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

//TOT HIER

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapRazorPages();


app.Run();
