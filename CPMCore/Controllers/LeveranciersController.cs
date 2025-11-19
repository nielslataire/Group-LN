using CPMCore.Models.Leveranciers;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System;

namespace CPMCore.Controllers;

[Authorize]
public class LeveranciersController : BaseController
{
    private readonly cpmRunningContext _db;
    public LeveranciersController(cpmRunningContext db)
    {
        _db = db;
    }

    private async Task<SupplierFormViewModel> BuildFormAsync(SupplierFormViewModel model, CancellationToken ct)
    {
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

        model.Activities = activities;

        var countries = await _db.Country
            .AsNoTracking()
            .Where(c => c.Selectable)
            .OrderBy(c => c.LandNaam)
            .Select(c => new CountryOptionViewModel
            {
                Id = c.Id,
                Name = c.LandNaam,
                IsoCode = c.LandIsocode ?? string.Empty
            })
            .ToListAsync(ct);
        model.Countries = countries;

        var legalForms = await _db.CompanyLegalForm
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LegalFormOptionViewModel
            {
                Id = l.Id,
                Name = l.Name,
                Abbreviation = l.Abbreviation
            })
            .ToListAsync(ct);
        model.LegalForms = legalForms;

        if (!model.SelectedCountryId.HasValue && countries.Any())
        {
            var defaultCountry = countries.First();
            model.SelectedCountryId = defaultCountry.Id;
            model.CountryCode ??= defaultCountry.IsoCode;
        }

        if (model.SelectedPostalCodeId.HasValue && (string.IsNullOrWhiteSpace(model.City) || string.IsNullOrWhiteSpace(model.PostalCode)))
        {
            var postal = await _db.PostalCode
                .Include(p => p.Country)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PostcodeId == model.SelectedPostalCodeId.Value, ct);

            if (postal != null)
            {
                model.PostalCode ??= postal.Postcode;
                model.City ??= postal.Gemeente;
                model.SelectedCountryId ??= postal.CountryId;
                model.CountryCode ??= postal.Country?.LandIsocode;
            }
        }

        if (model.Departments == null || model.Departments.Count == 0)
        {
            model.Departments = new List<DepartmentInputViewModel> { new() };
        }

        if (model.Contacts == null || model.Contacts.Count == 0)
        {
            model.Contacts = new List<ContactInputViewModel> { new() };
        }

        return model;
    }

    private static void MapToEntity(SupplierFormViewModel model, CompanyInfo entity)
    {
        entity.BedrijfsNaam = model.Name;
        entity.Ondernemingsnummer = model.EnterpriseNumber;
        entity.Straat = model.Street;
        entity.Huisnummer = model.HouseNumber;
        entity.Busnummer = model.BusNumber;
        entity.Postcode = model.PostalCode;
        entity.Gemeente = model.City;
        entity.LandCode = model.CountryCode;
        entity.Telefoon1 = model.Phone;
        entity.Gsm = model.Mobile;
        entity.Email = model.Email;
        entity.InvoiceEmail = model.InvoiceEmail;
        entity.RequiresDigitalInvoice = model.RequiresDigitalInvoice;
        entity.AttachUblByDefault = model.AttachUblByDefault;
        entity.PostCodeId = model.SelectedPostalCodeId;
        entity.Weburl = model.WebUrl;
    }

    private async Task PopulateSelectionsAsync(SupplierFormViewModel model, CancellationToken ct)
    {
        if (model.SelectedPostalCodeId.HasValue)
        {
            var postal = await _db.PostalCode
                .Include(p => p.Country)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PostcodeId == model.SelectedPostalCodeId.Value, ct);

            if (postal != null)
            {
                model.PostalCode = postal.Postcode;
                model.City = postal.Gemeente;
                model.SelectedCountryId ??= postal.CountryId;
                model.CountryCode ??= postal.Country?.LandIsocode;
            }
        }
        else if (model.SelectedCountryId.HasValue && string.IsNullOrWhiteSpace(model.CountryCode))
        {
            model.CountryCode = await _db.Country
                .AsNoTracking()
                .Where(c => c.Id == model.SelectedCountryId.Value)
                .Select(c => c.LandIsocode)
                .FirstOrDefaultAsync(ct);
        }
    }

    private async Task<string?> ResolveLegalFormAbbreviation(int? legalFormId, CancellationToken ct)
    {
        if (!legalFormId.HasValue)
        {
            return null;
        }

        return await _db.CompanyLegalForm
            .AsNoTracking()
            .Where(l => l.Id == legalFormId.Value)
            .Select(l => l.Abbreviation)
            .FirstOrDefaultAsync(ct);
    }

    private static bool HasDepartmentData(DepartmentInputViewModel department)
    {
        return department != null && (
            !string.IsNullOrWhiteSpace(department.Name) ||
            !string.IsNullOrWhiteSpace(department.Street) ||
            !string.IsNullOrWhiteSpace(department.HouseNumber) ||
            !string.IsNullOrWhiteSpace(department.Bus) ||
            !string.IsNullOrWhiteSpace(department.Phone) ||
            !string.IsNullOrWhiteSpace(department.Mobile) ||
            !string.IsNullOrWhiteSpace(department.Email) ||
            department.PostalCodeId.HasValue);
    }

    private static bool HasContactData(ContactInputViewModel contact)
    {
        return contact != null && (
            !string.IsNullOrWhiteSpace(contact.FirstName) ||
            !string.IsNullOrWhiteSpace(contact.LastName) ||
            !string.IsNullOrWhiteSpace(contact.Function) ||
            !string.IsNullOrWhiteSpace(contact.Phone) ||
            !string.IsNullOrWhiteSpace(contact.Mobile) ||
            !string.IsNullOrWhiteSpace(contact.Email));
    }

    private async Task<Dictionary<string, CompanyDepartments>> PersistDepartmentsAsync(int companyId, IEnumerable<DepartmentInputViewModel> departments, CancellationToken ct)
    {
        var tracked = new Dictionary<string, CompanyDepartments>();

        foreach (var department in departments.Where(HasDepartmentData))
        {
            var entity = new CompanyDepartments
            {
                CompanyId = companyId,
                Naam = department.Name,
                Straat = department.Street,
                Huisnummer = department.HouseNumber,
                Bus = department.Bus,
                PostcodeId = department.PostalCodeId,
                Telefoon = department.Phone,
                Gsm = department.Mobile,
                Email = department.Email
            };

            _db.CompanyDepartments.Add(entity);
            tracked[department.Key] = entity;
        }

        await _db.SaveChangesAsync(ct);
        return tracked;
    }

    private async Task PersistContactsAsync(int companyId, IEnumerable<ContactInputViewModel> contacts, IReadOnlyDictionary<string, CompanyDepartments> departments, CancellationToken ct)
    {
        foreach (var contact in contacts.Where(HasContactData))
        {
            int? departmentId = null;

            if (!string.IsNullOrWhiteSpace(contact.DepartmentKey) && departments.TryGetValue(contact.DepartmentKey, out var department))
            {
                departmentId = department.DepartmentId;
            }

            var entity = new CompanyContacts
            {
                CompanyId = companyId,
                DepartmentId = departmentId,
                ContactNaam = contact.LastName,
                ContactVoornaam = contact.FirstName,
                Functie = contact.Function,
                Telefoon = contact.Phone,
                Gsm = contact.Mobile,
                Email = contact.Email
            };

            _db.CompanyContacts.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
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

    [HttpGet]
    public async Task<IActionResult> ValidateVat(string vatNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
        {
            return BadRequest(new { error = "Ongeldig ondernemingsnummer" });
        }

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://controleerbtwnummer.eu/api/validate/{Uri.EscapeDataString(vatNumber.Trim())}", ct);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
            }

            var payload = await response.Content.ReadFromJsonAsync<VatLookupResponse>(cancellationToken: ct);
            return Json(payload);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> FindPostalMatch(string postalCode, string city, string? countryIso, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(postalCode) || string.IsNullOrWhiteSpace(city))
        {
            return BadRequest();
        }

        var query = _db.PostalCode
            .Include(p => p.Country)
            .AsNoTracking()
            .Where(p => p.Postcode == postalCode.Trim() && p.Gemeente == city.Trim());

        if (!string.IsNullOrWhiteSpace(countryIso))
        {
            query = query.Where(p => p.Country != null && p.Country.LandIsocode == countryIso);
        }

        var match = await query.FirstOrDefaultAsync(ct);

        if (match == null)
        {
            return NotFound();
        }

        return Json(new { id = match.PostcodeId, text = $"{match.Postcode} - {match.Gemeente}", countryId = match.CountryId });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = await BuildFormAsync(new SupplierFormViewModel(), ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await BuildFormAsync(model, ct);
            return View(model);
        }

        await PopulateSelectionsAsync(model, ct);

        var entity = new CompanyInfo();
        MapToEntity(model, entity);

        entity.Type = await ResolveLegalFormAbbreviation(model.SelectedLegalFormId, ct);

        if (model.SelectedActivityIds != null && model.SelectedActivityIds.Any())
        {
            entity.Activity = await _db.Activity
                .Where(a => model.SelectedActivityIds.Contains(a.ActivityId))
                .ToListAsync(ct);
        }

        _db.CompanyInfo.Add(entity);
        await _db.SaveChangesAsync(ct);

        var departments = await PersistDepartmentsAsync(entity.CompanyId, model.Departments ?? Enumerable.Empty<DepartmentInputViewModel>(), ct);
        await PersistContactsAsync(entity.CompanyId, model.Contacts ?? Enumerable.Empty<ContactInputViewModel>(), departments, ct);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await _db.CompanyInfo
            .Include(c => c.Activity)
            .Include(c => c.CompanyDepartments)
            .ThenInclude(d => d.Postcode)
            .Include(c => c.CompanyContacts)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == id, ct);

        if (entity == null)
        {
            return NotFound();
        }

        var legalFormId = await _db.CompanyLegalForm
            .Where(l => l.Abbreviation == entity.Type || l.Name == entity.Type)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(ct);

        var vm = new SupplierFormViewModel
        {
            Id = entity.CompanyId,
            Name = entity.BedrijfsNaam,
            SelectedLegalFormId = legalFormId,
            EnterpriseNumber = entity.Ondernemingsnummer,
            Street = entity.Straat,
            HouseNumber = entity.Huisnummer,
            BusNumber = entity.Busnummer,
            PostalCode = entity.Postcode,
            City = entity.Gemeente,
            CountryCode = entity.LandCode,
            SelectedPostalCodeId = entity.PostCodeId,
            Phone = entity.Telefoon1,
            Mobile = entity.Gsm,
            Email = entity.Email,
            InvoiceEmail = entity.InvoiceEmail,
            RequiresDigitalInvoice = entity.RequiresDigitalInvoice,
            AttachUblByDefault = entity.AttachUblByDefault,
            SelectedActivityIds = entity.Activity.Select(a => a.ActivityId).ToList(),
            WebUrl = entity.Weburl,
            Departments = entity.CompanyDepartments
                .Select(d => new DepartmentInputViewModel
                {
                    Id = d.DepartmentId,
                    Key = $"dep-{d.DepartmentId}",
                    Name = d.Naam,
                    Street = d.Straat,
                    HouseNumber = d.Huisnummer,
                    Bus = d.Bus,
                    PostalCodeId = d.PostcodeId,
                    PostalDisplay = d.Postcode != null ? $"{d.Postcode.Postcode} - {d.Postcode.Gemeente}" : null,
                    Phone = d.Telefoon,
                    Mobile = d.Gsm,
                    Email = d.Email
                })
                .ToList(),
            Contacts = entity.CompanyContacts
                .Select(c => new ContactInputViewModel
                {
                    Id = c.ContactId,
                    Key = $"ct-{c.ContactId}",
                    LastName = c.ContactNaam,
                    FirstName = c.ContactVoornaam,
                    Function = c.Functie,
                    Phone = c.Telefoon,
                    Mobile = c.Gsm,
                    Email = c.Email,
                    DepartmentKey = c.DepartmentId.HasValue ? $"dep-{c.DepartmentId.Value}" : null
                })
                .ToList()
        };

        if (!vm.SelectedCountryId.HasValue && !string.IsNullOrWhiteSpace(entity.LandCode))
        {
            vm.SelectedCountryId = await _db.Country
                .AsNoTracking()
                .Where(c => c.LandIsocode == entity.LandCode)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        await BuildFormAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await BuildFormAsync(model, ct);
            return View(model);
        }

        var entity = await _db.CompanyInfo
            .Include(c => c.Activity)
            .Include(c => c.CompanyDepartments)
            .Include(c => c.CompanyContacts)
            .FirstOrDefaultAsync(c => c.CompanyId == id, ct);

        if (entity == null)
        {
            return NotFound();
        }

        await PopulateSelectionsAsync(model, ct);

        MapToEntity(model, entity);

        entity.Type = await ResolveLegalFormAbbreviation(model.SelectedLegalFormId, ct);

        entity.Activity.Clear();

        if (model.SelectedActivityIds != null && model.SelectedActivityIds.Any())
        {
            var activities = await _db.Activity
                .Where(a => model.SelectedActivityIds.Contains(a.ActivityId))
                .ToListAsync(ct);

            foreach (var activity in activities)
            {
                entity.Activity.Add(activity);
            }
        }

        if (entity.CompanyContacts.Any())
        {
            _db.CompanyContacts.RemoveRange(entity.CompanyContacts);
        }

        if (entity.CompanyDepartments.Any())
        {
            _db.CompanyDepartments.RemoveRange(entity.CompanyDepartments);
        }

        await _db.SaveChangesAsync(ct);

        var departments = await PersistDepartmentsAsync(entity.CompanyId, model.Departments ?? Enumerable.Empty<DepartmentInputViewModel>(), ct);
        await PersistContactsAsync(entity.CompanyId, model.Contacts ?? Enumerable.Empty<ContactInputViewModel>(), departments, ct);

        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<IActionResult> ModalDelete(int id, CancellationToken ct)
    {
        var supplier = await _db.CompanyInfo
            .AsNoTracking()
            .Select(c => new SupplierDeleteViewModel
            {
                Id = c.CompanyId,
                Name = c.BedrijfsNaam
            })
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (supplier == null)
        {
            return NotFound();
        }

        return PartialView("Modals/_ModalDeleteSupplier", supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Administrator")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.CompanyInfo.FindAsync(new object[] { id }, ct);

        if (entity == null)
        {
            return NotFound();
        }

        _db.CompanyInfo.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Index));
    }

    private class VatLookupResponse
    {
        public bool valid { get; set; }

        public string vatNumber { get; set; } = string.Empty;

        public string name { get; set; } = string.Empty;

        public string countryCode { get; set; } = string.Empty;

        public VatLookupAddress address { get; set; } = new();
    }

    private class VatLookupAddress
    {
        public string street { get; set; } = string.Empty;

        public string zip { get; set; } = string.Empty;

        public string city { get; set; } = string.Empty;
    }
}