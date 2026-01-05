using CPMCore.Models;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

namespace CPMCore.Controllers;

[Authorize(Policy = "CpmAdmin")]
public class UserAdminController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly ILogger<UserAdminController> _logger;
    private readonly GraphServiceClient _graphClient;

    public UserAdminController(cpmRunningContext db, ILogger<UserAdminController> logger, GraphServiceClient graphClient)
    {
        _db = db;
        _logger = logger;
        _graphClient = graphClient;
    }

    public async Task<IActionResult> Index()
    {
        var permissionsByUser = await _db.PermissionPerUser
            .AsNoTracking()
            .Include(p => p.PermissionNavigation)
            .GroupBy(p => p.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(p => p.PermissionNavigation.PermissionName)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList());

        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Familienaam)
            .ThenBy(u => u.Voornaam)
            .ToListAsync();

        var localUsers = users.Select(user =>
        {
            var permissions = permissionsByUser.TryGetValue(user.Id, out var list)
                ? list
                : new List<string>();

            return new UserListItemViewModel
            {
                Id = user.Id,
                DisplayName = string.Join(' ', new[] { user.Voornaam, user.Familienaam }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                UserName = user.UserId ?? string.Empty,
                Email = user.Email ?? string.Empty,
                EntraObjectId = user.EntraObjectId,
                IsActive = user.IsActive,
                Permissions = permissions
            };
        }).ToList();
        var entraUsers = new List<EntraUserListItemViewModel>();

        var page = await _graphClient.Users
            .Request()
            .Select("id,displayName,mail,userPrincipalName")
            .Top(999)
            .GetAsync();

        while (page != null)
        {
            if (page.CurrentPage != null)
            {
                entraUsers.AddRange(page.CurrentPage.Select(u => new EntraUserListItemViewModel
                {
                    Id = u.Id ?? "",
                    DisplayName = u.DisplayName ?? "",
                    Email = u.Mail ?? "",
                    UserPrincipalName = u.UserPrincipalName ?? ""
                }));
            }

            page = page.NextPageRequest != null
                ? await page.NextPageRequest.GetAsync()
                : null;
        }

        return View(new UserAdminIndexViewModel
        {
            LocalUsers = localUsers,
            EntraUsers = entraUsers
        });
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var permissions = await _db.Permission
            .AsNoTracking()
            .OrderBy(p => p.PermissionName)
            .Select(p => p.PermissionName)
            .ToListAsync();

        var selectedPermissions = await _db.PermissionPerUser
            .AsNoTracking()
            .Include(p => p.PermissionNavigation)
            .Where(p => p.UserId == user.Id)
            .Select(p => p.PermissionNavigation.PermissionName)
            .ToListAsync();

        var vm = new EditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserId ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Name = user.Familienaam,
            Forename = user.Voornaam,
            JobFunction = user.Functie,
            Cellphone = user.Gsm,
            EntraObjectId = user.EntraObjectId,
            IsActive = user.IsActive,
            AvailablePermissions = permissions,
            SelectedPermissions = selectedPermissions
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        model.AvailablePermissions = await _db.Permission
            .AsNoTracking()
            .OrderBy(p => p.PermissionName)
            .Select(p => p.PermissionName)
            .ToListAsync();

        if (!ModelState.IsValid)
            return View(model);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == model.Id);
        if (user == null)
            return NotFound();

        user.UserId = model.UserName;
        user.Email = model.Email;
        user.Familienaam = model.Name ?? string.Empty;
        user.Voornaam = model.Forename ?? string.Empty;
        user.Functie = model.JobFunction ?? string.Empty;
        user.Gsm = model.Cellphone ?? string.Empty;
        user.IsActive = model.IsActive;

        var currentPermissions = await _db.PermissionPerUser
            .Where(p => p.UserId == user.Id)
            .ToListAsync();

        _db.PermissionPerUser.RemoveRange(currentPermissions);

        var selected = model.SelectedPermissions ?? new List<string>();
        if (selected.Count > 0)
        {
            var permissionIds = await _db.Permission
                .Where(p => selected.Contains(p.PermissionName))
                .Select(p => p.PermissionId)
                .ToListAsync();

            foreach (var permissionId in permissionIds)
            {
                _db.PermissionPerUser.Add(new PermissionPerUser
                {
                    PermissionId = permissionId,
                    UserId = user.Id
                });
            }
        }

        await _db.SaveChangesAsync();

        TempData["Message"] = "Gebruiker bijgewerkt.";
        _logger.LogInformation("Gebruiker {User} bijgewerkt via UserAdmin.", user.UserId);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkEntra(int id, string? linkEntraObjectId)
    {
        if (string.IsNullOrWhiteSpace(linkEntraObjectId))
        {
            TempData["Error"] = "Geef een geldig Entra Object ID op.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var trimmed = linkEntraObjectId.Trim();
        var alreadyLinked = await _db.Users
            .AnyAsync(u => u.EntraObjectId == trimmed && u.Id != id);

        if (alreadyLinked)
        {
            TempData["Error"] = "Dit Entra Object ID is al gekoppeld aan een andere gebruiker.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        user.EntraObjectId = trimmed;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Entra account gekoppeld.";
        return RedirectToAction(nameof(Edit), new { id });
    }
}