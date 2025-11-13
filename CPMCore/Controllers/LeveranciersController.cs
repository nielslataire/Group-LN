using CPMCore.Models.Leveranciers;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

[Authorize]
public class LeveranciersController : BaseController
{
    private readonly cpmRunningContext _db;
    public LeveranciersController(cpmRunningContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Breadcrumb("Leveranciers")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Leveranciers";

        var suppliers = await _db.CompanyInfo
            .AsNoTracking()
            .Select(c => new SupplierListItemViewModel
            {
                Id = c.CompanyId,
                Name = c.BedrijfsNaam,
                EnterpriseNumber = c.Ondernemingsnummer,
                Email = c.Email,
                Phone = c.Telefoon1,
                Mobile = c.Gsm,
                ContractCount = c.Contract.Count,
                TotalContractAmount = c.Contract
                    .SelectMany(contract => contract.ContractActivity)
                    .Sum(activity => (decimal?)activity.Price) ?? 0m,
                ActivityIds = c.Activity
                    .Select(a => a.ActivityId)
                    .ToList()
            })
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var activities = await _db.Activity
            .AsNoTracking()
            .Select(a => new ActivityFilterItemViewModel
            {
                Id = a.ActivityId,
                Name = a.Omschrijving,
                GroupName = a.Group != null ? a.Group.Name : null
            })
            .OrderBy(a => a.GroupName)
            .ThenBy(a => a.Name)
            .ToListAsync(ct);

        var vm = new SupplierIndexViewModel
        {
            Suppliers = suppliers,
            Activities = activities
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Lookup(string? term, int take = 20, CancellationToken ct = default)
    {
        var query = _db.CompanyInfo.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var like = $"%{term.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.BedrijfsNaam, like));
        }

        var results = await query
            .OrderBy(c => c.BedrijfsNaam)
            .Take(take)
            .Select(c => new
            {
                id = c.CompanyId,
                text = c.BedrijfsNaam
            })
            .ToListAsync(ct);

        return Json(new { results });
    }
}