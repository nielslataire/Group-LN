using CPMCore.Models;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

[Authorize(Policy = "CpmAdmin")]
public class AppRolesController : BaseController
{
    private readonly cpmRunningContext _db;

    public AppRolesController(cpmRunningContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var permissions = await _db.Permission
            .AsNoTracking()
            .OrderBy(p => p.PermissionName)
            .Select(p => new PermissionListItemViewModel
            {
                Id = p.PermissionId,
                Name = p.PermissionName
            })
            .ToListAsync();

        return View(permissions);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PermissionEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PermissionEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var exists = await _db.Permission.AnyAsync(p => p.PermissionName == model.Name);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Name), "Deze rol bestaat al.");
            return View(model);
        }

        var nextId = await _db.Permission.AnyAsync()
            ? await _db.Permission.MaxAsync(p => p.PermissionId) + 1
            : 1;

        _db.Permission.Add(new Permission
        {
            PermissionId = nextId,
            PermissionName = model.Name
        });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}