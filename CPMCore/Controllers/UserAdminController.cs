using CPMCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CPMCore.Controllers;

[Authorize(Roles = "Admin,Administrator")]
public class UserAdminController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserAdminController> _logger;

    public UserAdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<UserAdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var viewModel = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModel.Add(new UserListItemViewModel
            {
                Id = user.Id,
                DisplayName = user.Displayname,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.OrderBy(r => r).ToList()
            });
        }

        return View(viewModel.OrderBy(u => u.DisplayName));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var vm = new EditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Name = user.Name,
            Forename = user.Forename,
            JobFunction = user.JobFunction,
            Cellphone = user.Cellphone,
            SelectedRoles = userRoles.ToList(),
            AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToListAsync();
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
            return NotFound();

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.Name = model.Name ?? string.Empty;
        user.Forename = model.Forename ?? string.Empty;
        user.JobFunction = model.JobFunction ?? string.Empty;
        user.Cellphone = model.Cellphone ?? string.Empty;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var selectedRoles = model.SelectedRoles ?? new List<string>();

        var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
        if (rolesToAdd.Any())
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
                foreach (var error in addResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
        }

        var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
        if (rolesToRemove.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
                foreach (var error in removeResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
        }

        if (!ModelState.IsValid)
            return View(model);

        TempData["Message"] = "Gebruiker bijgewerkt.";
        _logger.LogInformation("Gebruiker {User} bijgewerkt via UserAdmin.", user.UserName);

        return RedirectToAction(nameof(Index));
    }
}