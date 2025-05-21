using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Data;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("WebApplication4ContextConnection") ?? throw new InvalidOperationException("Connection string 'WebApplication4ContextConnection' not found.");

builder.Services.AddDbContext<WebApplication4Context>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<WebApplication4User>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<WebApplication4Context>();

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
