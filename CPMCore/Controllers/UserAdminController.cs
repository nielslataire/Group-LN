using BOCore;
using CPMCore.Models;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Azure.Identity;
using CPMCore.Services.Security;

namespace CPMCore.Controllers;

[Authorize(Policy = "CpmAdmin")]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsUsers)]
public class UserAdminController : BaseController
{
    private readonly cpmRunningContext _db;
    private readonly ILogger<UserAdminController> _logger;
    private readonly GraphServiceClient? _graphClient;

    public UserAdminController(
           cpmRunningContext db,
           ILogger<UserAdminController> logger,
           IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _graphClient = CreateGraphClient(configuration, logger);
    }

    [AuthorizeForScopes(ScopeKeySection = "Graph:Scopes")]
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


        var roleNamesByUser = new Dictionary<int, List<string>>();
        var primaryRoleByUser = new Dictionary<int, (int RoleId, string RoleName)>();
        try
        {
            var roleRowsPerUser = await (from ur in _db.SecurityUserRole.AsNoTracking()
                                         join r in _db.SecurityRole.AsNoTracking() on ur.RoleId equals r.RoleId
                                         where r.IsActive
                                         select new { ur.UserId, ur.RoleId, r.Name })
                .ToListAsync();

            roleNamesByUser = roleRowsPerUser
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().OrderBy(x => x).ToList());

            primaryRoleByUser = roleRowsPerUser
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.OrderBy(x => x.Name).First();
                        return (first.RoleId, first.Name);
                    });
        }
        catch
        {
            // tables may not exist yet
        }


        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Familienaam)
            .ThenBy(u => u.Voornaam)
            .ToListAsync();

        var localUsers = users.Select(user =>
        {
            var permissions = roleNamesByUser.TryGetValue(user.Id, out var roles)
                ? roles
                : (permissionsByUser.TryGetValue(user.Id, out var list) ? list : new List<string>());
            var hasPrimaryRole = primaryRoleByUser.TryGetValue(user.Id, out var primaryRole);

            return new UserListItemViewModel
            {
                Id = user.Id,
                DisplayName = string.Join(' ', new[] { user.Voornaam, user.Familienaam }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                UserName = user.UserId ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Cellphone = user.Gsm,
                EntraObjectId = user.EntraObjectId,
                IsActive = user.IsActive,
                CurrentRoleId = hasPrimaryRole ? primaryRole.RoleId : (int?)null,
                CurrentRoleName = hasPrimaryRole ? primaryRole.RoleName : null,
                Permissions = permissions
            };
        }).ToList();
        var entraUsers = await LoadEntraUsersAsync();
        var roleRows = new List<RolePermissionAssignmentViewModel>();
        try
        {
            roleRows = await _db.SecurityRole.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new RolePermissionAssignmentViewModel { RoleId = x.RoleId, RoleName = x.Name })
                .ToListAsync();
        }
        catch { }


        return View(new UserAdminIndexViewModel
        {
            LocalUsers = localUsers,
            EntraUsers = entraUsers,
            Roles = roleRows,
            PermissionDefinitions = PermissionCatalog.All
                .Select(x => new PermissionMatrixItemViewModel { Code = x.Code, Name = x.Name, ParentCode = x.ParentCode, SortOrder = x.SortOrder })
                .ToList()
        });
    }

    private static GraphServiceClient? CreateGraphClient(IConfiguration configuration, ILogger logger)
    {
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            logger.LogWarning(
                "Graph client not configured because AzureAd settings are missing. TenantId={TenantId}, ClientId={ClientId}.",
                tenantId ?? "<null>",
                clientId ?? "<null>");
            return null;
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        return new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
    }

    private async Task<List<EntraUserListItemViewModel>> LoadEntraUsersAsync()
    {
        var entraUsers = new List<EntraUserListItemViewModel>();
        if (_graphClient == null)
            return entraUsers;
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
    

            return entraUsers
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.UserPrincipalName)
            .ToList();
    }


    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var permissions = await _db.Permission
            .AsNoTracking()
            .OrderBy(p => p.PermissionName)
            .Select(p => p.PermissionName)
            .ToListAsync();

        var vm = new CreateUserViewModel
        {
            AvailablePermissions = permissions,
            EntraUsers = await LoadEntraUsersAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        model.AvailablePermissions = await _db.Permission
            .AsNoTracking()
            .OrderBy(p => p.PermissionName)
            .Select(p => p.PermissionName)
            .ToListAsync();
        model.EntraUsers = await LoadEntraUsersAsync();

        if (await _db.Users.AnyAsync(u => u.UserId == model.UserName))
        {
            ModelState.AddModelError(nameof(model.UserName), "Deze gebruikersnaam bestaat al.");
        }

        if (await _db.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Dit e-mailadres bestaat al.");
        }

        if (!string.IsNullOrWhiteSpace(model.SelectedEntraObjectId))
        {
            var entraId = model.SelectedEntraObjectId.Trim();
            if (await _db.Users.AnyAsync(u => u.EntraObjectId == entraId))
            {
                ModelState.AddModelError(nameof(model.SelectedEntraObjectId), "Dit Entra Object ID is al gekoppeld aan een andere gebruiker.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        var nextId = await _db.Users.AnyAsync()
            ? await _db.Users.MaxAsync(u => u.Id) + 1
            : 1;

        var user = new Users
        {
            Id = nextId,
            UserId = model.UserName,
            Email = model.Email,
            Familienaam = model.Name ?? string.Empty,
            Voornaam = model.Forename ?? string.Empty,
            Functie = model.JobFunction ?? string.Empty,
            Gsm = model.Cellphone ?? string.Empty,
            IsActive = model.IsActive,
            EntraObjectId = string.IsNullOrWhiteSpace(model.SelectedEntraObjectId)
                ? null
                : model.SelectedEntraObjectId.Trim(),
            Password = string.Empty
        };

        _db.Users.Add(user);

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

        TempData["Message"] = "Gebruiker toegevoegd.";
        _logger.LogInformation("Gebruiker {User} toegevoegd via UserAdmin.", user.UserId);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> CreateModal(string userName, string email, string? forename, string? name, string? jobFunction, string? cellphone, int? selectedRoleId, string? selectedEntraObjectId, bool isActive)
    {
        var normalizedUserName = userName?.Trim() ?? string.Empty;
        var normalizedEmail = email?.Trim() ?? string.Empty;
        var normalizedEntraObjectId = string.IsNullOrWhiteSpace(selectedEntraObjectId) ? null : selectedEntraObjectId.Trim();

        if (string.IsNullOrWhiteSpace(normalizedUserName))
        {
            TempData["Error"] = "Gebruikersnaam is verplicht.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            TempData["Error"] = "E-mailadres is verplicht.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Users.AnyAsync(u => u.UserId == normalizedUserName))
        {
            TempData["Error"] = "Deze gebruikersnaam bestaat al.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            TempData["Error"] = "Dit e-mailadres bestaat al.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(normalizedEntraObjectId) && await _db.Users.AnyAsync(u => u.EntraObjectId == normalizedEntraObjectId))
        {
            TempData["Error"] = "Dit Entra Object ID is al gekoppeld aan een andere gebruiker.";
            return RedirectToAction(nameof(Index));
        }

        var nextId = await _db.Users.AnyAsync()
            ? await _db.Users.MaxAsync(u => u.Id) + 1
            : 1;

        var user = new Users
        {
            Id = nextId,
            UserId = normalizedUserName,
            Email = normalizedEmail,
            Familienaam = name?.Trim() ?? string.Empty,
            Voornaam = forename?.Trim() ?? string.Empty,
            Functie = jobFunction?.Trim() ?? string.Empty,
            Gsm = cellphone?.Trim() ?? string.Empty,
            IsActive = isActive,
            EntraObjectId = normalizedEntraObjectId,
            Password = string.Empty
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (selectedRoleId.HasValue)
        {
            var roleExists = await _db.SecurityRole.AnyAsync(x => x.RoleId == selectedRoleId.Value && x.IsActive);
            if (roleExists)
            {
                _db.SecurityUserRole.Add(new SecurityUserRole { UserId = user.Id, RoleId = selectedRoleId.Value });
                await _db.SaveChangesAsync();
            }
        }

        TempData["Message"] = "Gebruiker toegevoegd.";
        return RedirectToAction(nameof(Index));
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
            SelectedPermissions = selectedPermissions,
            EntraUsers = await LoadEntraUsersAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> QuickUpdate(int id, string? forename, string? name, string? email, string? jobFunction, string? cellphone, int? selectedRoleId, bool isActive)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "E-mailadres is verplicht.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedEmail = email.Trim();
        var emailExists = await _db.Users.AnyAsync(u => u.Id != id && u.Email == normalizedEmail);
        if (emailExists)
        {
            TempData["Error"] = "Dit e-mailadres is al in gebruik.";
            return RedirectToAction(nameof(Index));
        }

        user.Voornaam = forename?.Trim() ?? string.Empty;
        user.Familienaam = name?.Trim() ?? string.Empty;
        user.Email = normalizedEmail;
        user.Functie = jobFunction?.Trim() ?? string.Empty;
        user.Gsm = cellphone?.Trim() ?? string.Empty;
        user.IsActive = isActive;

        try
        {
            var existingRoles = await _db.SecurityUserRole.Where(x => x.UserId == id).ToListAsync();
            _db.SecurityUserRole.RemoveRange(existingRoles);

            if (selectedRoleId.HasValue)
            {
                var roleExists = await _db.SecurityRole.AnyAsync(x => x.RoleId == selectedRoleId.Value && x.IsActive);
                if (roleExists)
                {
                    _db.SecurityUserRole.Add(new SecurityUserRole { UserId = id, RoleId = selectedRoleId.Value });
                }
            }
        }
        catch
        {
            // security tables may not exist yet
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = "Gebruiker bijgewerkt.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
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

    [HttpGet]
    public async Task<IActionResult> ModalDelete(int id)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var displayName = string.Join(' ', new[] { user.Voornaam, user.Familienaam }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

        var vm = new UserDeleteViewModel
        {
            Id = user.Id,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? user.UserId ?? string.Empty : displayName,
            UserName = user.UserId ?? string.Empty
        };

        return PartialView("Modals/_ModalDeleteUser", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var projects = await _db.Project
            .Where(p => p.AspNetUserId == user.UserId)
            .ToListAsync();

        foreach (var project in projects)
        {
            project.AspNetUserId = null;
        }

        var permissions = await _db.PermissionPerUser
            .Where(p => p.UserId == user.Id)
            .ToListAsync();

        _db.PermissionPerUser.RemoveRange(permissions);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Gebruiker verwijderd.";
        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> PermissionMatrix(int userId)
    {
        var roleIds = await _db.SecurityUserRole.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.RoleId).ToListAsync();
        var roleRights = await _db.SecurityRolePermission.AsNoTracking().Where(x => roleIds.Contains(x.RoleId)).ToListAsync();
        var userOverrides = await _db.SecurityUserPermissionOverride.AsNoTracking().Where(x => x.UserId == userId).ToListAsync();

        var effective = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in PermissionCatalog.All.Select(x => x.Code))
        {
            var rr = roleRights.Where(x => string.Equals(x.PermissionCode, code, StringComparison.OrdinalIgnoreCase));
            var baseRead = rr.Any(x => x.CanRead);
            var baseWrite = rr.Any(x => x.CanWrite);
            var baseDelete = rr.Any(x => x.CanDelete);
            var ov = userOverrides.FirstOrDefault(x => string.Equals(x.PermissionCode, code, StringComparison.OrdinalIgnoreCase));
            var read = ov?.CanRead ?? baseRead;
            var write = ov?.CanWrite ?? baseWrite;
            var del = ov?.CanDelete ?? baseDelete;
            effective[code] = new
            {
                read,
                write,
                delete = del,
                baseRead,
                baseWrite,
                baseDelete,
                overridden = ov != null
            };
        }

        return Json(effective);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> SaveRolePermission(int roleId, string permissionCode, bool canRead, bool canWrite, bool canDelete)
    {
        var row = await _db.SecurityRolePermission.FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionCode == permissionCode);
        if (row == null)
        {
            row = new SecurityRolePermission { RoleId = roleId, PermissionCode = permissionCode };
            _db.SecurityRolePermission.Add(row);
        }

        row.CanRead = canRead;
        row.CanWrite = canWrite;
        row.CanDelete = canDelete;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    [CPMCore.Filters.PermissionRead(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> RolePermissionMatrix(int roleId)
    {
        var roleRights = await _db.SecurityRolePermission.AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .ToListAsync();

        var matrix = PermissionCatalog.All.ToDictionary(
            x => x.Code,
            x =>
            {
                var row = roleRights.FirstOrDefault(r => string.Equals(r.PermissionCode, x.Code, StringComparison.OrdinalIgnoreCase));
                return new
                {
                    read = row?.CanRead ?? false,
                    write = row?.CanWrite ?? false,
                    delete = row?.CanDelete ?? false
                };
            },
            StringComparer.OrdinalIgnoreCase);

        return Json(matrix);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> DeleteRole(int roleId)
    {
        var role = await _db.SecurityRole.FirstOrDefaultAsync(x => x.RoleId == roleId);
        if (role == null) return NotFound();

        var rolePermissions = await _db.SecurityRolePermission.Where(x => x.RoleId == roleId).ToListAsync();
        var roleUsers = await _db.SecurityUserRole.Where(x => x.RoleId == roleId).ToListAsync();

        _db.SecurityRolePermission.RemoveRange(rolePermissions);
        _db.SecurityUserRole.RemoveRange(roleUsers);
        _db.SecurityRole.Remove(role);

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> CreateRole(string roleName, int? copyFromRoleId)
    {
        var name = roleName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Rolnaam is verplicht.");

        var exists = await _db.SecurityRole.AnyAsync(x => x.Name == name);
        if (exists)
            return BadRequest("Rolnaam bestaat al.");

        var role = new SecurityRole
        {
            Name = name,
            IsActive = true
        };
        _db.SecurityRole.Add(role);
        await _db.SaveChangesAsync();

        if (copyFromRoleId.HasValue)
        {
            var sourcePermissions = await _db.SecurityRolePermission.AsNoTracking()
                .Where(x => x.RoleId == copyFromRoleId.Value)
                .ToListAsync();

            foreach (var permission in sourcePermissions)
            {
                _db.SecurityRolePermission.Add(new SecurityRolePermission
                {
                    RoleId = role.RoleId,
                    PermissionCode = permission.PermissionCode,
                    CanRead = permission.CanRead,
                    CanWrite = permission.CanWrite,
                    CanDelete = permission.CanDelete
                });
            }

            await _db.SaveChangesAsync();
        }

        return Json(new { roleId = role.RoleId, roleName = role.Name });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> SetUserActive(int userId, bool isActive)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null) return NotFound();

        user.IsActive = isActive;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.SettingsUsers)]
    public async Task<IActionResult> SaveUserPermissionOverride(int userId, string permissionCode, bool? canRead, bool? canWrite, bool? canDelete)
    {
        var row = await _db.SecurityUserPermissionOverride.FirstOrDefaultAsync(x => x.UserId == userId && x.PermissionCode == permissionCode);
        if (row == null)
        {
            row = new SecurityUserPermissionOverride { UserId = userId, PermissionCode = permissionCode };
            _db.SecurityUserPermissionOverride.Add(row);
        }

        row.CanRead = canRead;
        row.CanWrite = canWrite;
        row.CanDelete = canDelete;

        await _db.SaveChangesAsync();
        return Ok();
    }

}
