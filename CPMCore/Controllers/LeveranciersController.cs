using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Leveranciers;
using CPMCore.Services;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Suppliers)]
public class LeveranciersController : BaseController
{
    private const string SupplierCompanyPermissionPrefix = "Suppliers.Company.";
    private static readonly JsonSerializerOptions VatLookupSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    private readonly cpmRunningContext _db;
    private readonly IOctopusApiClient _octopusClient;
    private readonly IOctopusTokenManager _octopusTokens;
    private readonly IContractorInviteService _contractorInviteService;
    private readonly IEntraGuestInvitationService _guestInvitationService;

    public LeveranciersController(
        cpmRunningContext db,
        IOctopusApiClient octopusClient,
        IOctopusTokenManager octopusTokens,
        IContractorInviteService contractorInviteService,
        IEntraGuestInvitationService guestInvitationService)
    {
        _db = db;
        _octopusClient = octopusClient;
        _octopusTokens = octopusTokens;
        _contractorInviteService = contractorInviteService;
        _guestInvitationService = guestInvitationService;
    }

    private async Task<SupplierFormViewModel> BuildFormAsync(SupplierFormViewModel model, CancellationToken ct, SupplierIssuerScope? scope = null)
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

        if (string.IsNullOrWhiteSpace(model.EnterpriseNumberCountryCode))
        {
            model.EnterpriseNumberCountryCode = countries
                .FirstOrDefault(c => string.Equals(c.IsoCode, "BE", StringComparison.OrdinalIgnoreCase))?.IsoCode
                ?? countries.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.IsoCode))?.IsoCode
                ?? "BE";
        }
        else
        {
            model.EnterpriseNumberCountryCode = SanitizeVatCountry(model.EnterpriseNumberCountryCode) ?? model.EnterpriseNumberCountryCode;
        }

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

        var issuerCompanies = await _db.IssuerCompany
            .AsNoTracking()
            .Where(i => i.IsActive)
            .Where(i => scope == null || scope.HasAllIssuers || scope.AllowedIssuerIds.Contains(i.Id))
            .OrderBy(i => i.Name)
            .Select(i => new IssuerCompanyOptionViewModel
            {
                Id = i.Id,
                Name = i.Name
            })
            .ToListAsync(ct);

        model.IssuerCompanies = issuerCompanies;

        model.SelectedIssuerCompanyIds ??= new List<int>();
        if (scope != null && !scope.HasAllIssuers)
        {
            model.SelectedIssuerCompanyIds = model.SelectedIssuerCompanyIds
                .Where(scope.AllowedIssuerIds.Contains)
                .ToList();
        }



        if (!model.SelectedCountryId.HasValue && countries.Any())
        {
            var defaultCountry = countries
                .FirstOrDefault(c => string.Equals(c.IsoCode, "BE", StringComparison.OrdinalIgnoreCase))
                ?? countries.First();

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
        entity.LandCode = model.EnterpriseNumberCountryCode;
        entity.Telefoon1 = model.Phone;
        entity.Gsm = model.Mobile;
        entity.Email = model.Email;
        entity.InvoiceEmail = model.InvoiceEmail;
        entity.RequiresDigitalInvoice = model.RequiresDigitalInvoice;
        entity.AttachUblByDefault = model.AttachUblByDefault;
        entity.PostCodeId = model.SelectedPostalCodeId;
        entity.Weburl = model.WebUrl;
        entity.IsActive = model.IsActive;
        entity.IsCustomer = model.IsCustomer;
        entity.VatNumber = model.VatNumber;
        entity.VatStatus = model.VatStatus;
    }

    private static void NormalizeWebsiteInput(SupplierFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.WebUrl))
        {
            model.WebUrl = null;
            return;
        }

        model.WebUrl = model.WebUrl.Trim();
    }

    private async Task AssignIssuerCompaniesAsync(CompanyInfo entity, IEnumerable<int> selectedIssuerCompanyIds, CancellationToken ct)
    {
        entity.CompanyIssuerCompany.Clear();

        var issuerIds = selectedIssuerCompanyIds
            ?.Where(id => id > 0)
            .Distinct()
            .ToList();

        if (issuerIds == null || issuerIds.Count == 0)
        {
            return;
        }

        var issuers = await _db.IssuerCompany
            .Where(i => issuerIds.Contains(i.Id))
            .ToListAsync(ct);

        foreach (var issuer in issuers)
        {
            entity.CompanyIssuerCompany.Add(new CompanyIssuerCompany
            {
                Company = entity,
                IssuerCompany = issuer,
                IssuerCompanyId = issuer.Id,
                CompanyId = entity.CompanyId
            });
        }
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
    private async Task EnsurePostalSelectionAsync(SupplierFormViewModel model, CancellationToken ct)
    {
        if (model.SelectedPostalCodeId.HasValue)
        {
            return;
        }

        if (!model.SelectedCountryId.HasValue && !string.IsNullOrWhiteSpace(model.CountryCode))
        {
            var iso = model.CountryCode.Trim().ToUpperInvariant();
            model.SelectedCountryId = await _db.Country
                .AsNoTracking()
                .Where(c => c.LandIsocode == iso)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        if (!model.SelectedCountryId.HasValue || string.IsNullOrWhiteSpace(model.PostalCode) || string.IsNullOrWhiteSpace(model.City))
        {
            return;
        }

        var trimmedPostal = model.PostalCode.Trim();
        var trimmedCity = model.City.Trim();

        var existing = await _db.PostalCode
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Postcode == trimmedPostal &&
                     p.Gemeente == trimmedCity &&
                     p.CountryId == model.SelectedCountryId.Value,
                ct);

        if (existing != null)
        {
            model.SelectedPostalCodeId = existing.PostcodeId;
            return;
        }

        var newPostal = new PostalCode
        {
            Postcode = trimmedPostal,
            Gemeente = trimmedCity,
            CountryId = model.SelectedCountryId.Value
        };

        _db.PostalCode.Add(newPostal);
        await _db.SaveChangesAsync(ct);

        model.SelectedPostalCodeId = newPostal.PostcodeId;
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

    private async Task<Dictionary<string, CompanyDepartments>> UpsertDepartmentsAsync(
        int companyId,
        IReadOnlyCollection<CompanyDepartments> existingDepartments,
        IEnumerable<DepartmentInputViewModel> departments,
        CancellationToken ct)
    {
        var tracked = new Dictionary<string, CompanyDepartments>();
        var existingById = existingDepartments.ToDictionary(d => d.DepartmentId);

        foreach (var department in departments.Where(HasDepartmentData))
        {
            CompanyDepartments entity;
            if (department.Id.HasValue && existingById.TryGetValue(department.Id.Value, out var existing))
            {
                entity = existing;
                entity.Naam = department.Name;
                entity.Straat = department.Street;
                entity.Huisnummer = department.HouseNumber;
                entity.Bus = department.Bus;
                entity.PostcodeId = department.PostalCodeId;
                entity.Telefoon = department.Phone;
                entity.Gsm = department.Mobile;
                entity.Email = department.Email;
            }
            else
            {
                entity = new CompanyDepartments
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
            }

            tracked[department.Key] = entity;
        }

        await _db.SaveChangesAsync(ct);
        return tracked;
    }

    private async Task SyncContactsAsync(
        int companyId,
        IReadOnlyCollection<CompanyContacts> existingContacts,
        IEnumerable<ContactInputViewModel> contacts,
        IReadOnlyDictionary<string, CompanyDepartments> departments,
        CancellationToken ct)
    {
        var existingById = existingContacts.ToDictionary(c => c.ContactId);
        var postedContacts = contacts.Where(HasContactData).ToList();
        var postedIds = postedContacts
            .Where(c => c.Id.HasValue)
            .Select(c => c.Id!.Value)
            .ToHashSet();

        var contactsToRemove = existingContacts
            .Where(c => !postedIds.Contains(c.ContactId))
            .ToList();

        if (contactsToRemove.Count > 0)
        {
            var contactIdsToRemove = contactsToRemove.Select(c => c.ContactId).ToList();
            var contractsUsingContacts = await _db.Contract
                .Where(c => c.SiteManagerContactId.HasValue && contactIdsToRemove.Contains(c.SiteManagerContactId.Value))
                .ToListAsync(ct);

            foreach (var contract in contractsUsingContacts)
            {
                contract.SiteManagerContactId = null;
            }

            _db.CompanyContacts.RemoveRange(contactsToRemove);
        }

        foreach (var contact in postedContacts)
        {
            int? departmentId = null;
            if (!string.IsNullOrWhiteSpace(contact.DepartmentKey) && departments.TryGetValue(contact.DepartmentKey, out var dep))
            {
                departmentId = dep.DepartmentId;
            }

            if (contact.Id.HasValue && existingById.TryGetValue(contact.Id.Value, out var existing))
            {
                existing.DepartmentId = departmentId;
                existing.ContactNaam = contact.LastName;
                existing.ContactVoornaam = contact.FirstName;
                existing.Functie = contact.Function;
                existing.Telefoon = contact.Phone;
                existing.Gsm = contact.Mobile;
                existing.Email = contact.Email;
            }
            else
            {
                _db.CompanyContacts.Add(new CompanyContacts
                {
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    ContactNaam = contact.LastName,
                    ContactVoornaam = contact.FirstName,
                    Functie = contact.Function,
                    Telefoon = contact.Phone,
                    Gsm = contact.Mobile,
                    Email = contact.Email
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }


    private static string? CombineEnterpriseNumber(string? countryCode, string? number)
    {
        var sanitizedNumber = SanitizeDigits(number);
        if (string.IsNullOrWhiteSpace(sanitizedNumber))
        {
            return null;
        }

        var sanitizedCountry = SanitizeVatCountry(countryCode);
        return string.IsNullOrEmpty(sanitizedCountry) ? sanitizedNumber : sanitizedCountry + sanitizedNumber;
    }

    private static string? SanitizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private static string? SanitizeVatCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        var builder = new StringBuilder(2);
        foreach (var ch in countryCode.Trim())
        {
            if (char.IsLetter(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }

            if (builder.Length == 2)
            {
                break;
            }
        }

        return builder.Length == 0 ? null : builder.ToString();
    }
    private static void NormalizeVatInputs(SupplierFormViewModel model)
    {
        model.EnterpriseNumberCountryCode = SanitizeVatCountry(model.EnterpriseNumberCountryCode) ?? "BE";
        model.EnterpriseNumber = SanitizeDigits(model.EnterpriseNumber);
        model.VatNumber = SanitizeDigits(model.VatNumber);

        if (string.IsNullOrWhiteSpace(model.VatStatus))
        {
            model.VatStatus = SupplierFormViewModel.VatStatusBtwPlichtig;
        }
    }

    private async Task ValidateVatInputsAsync(SupplierFormViewModel model, int? existingCompanyId, CancellationToken ct)
    {
        var isBelgian = string.Equals(model.EnterpriseNumberCountryCode, "BE", StringComparison.OrdinalIgnoreCase);
        var enterpriseNumber = model.EnterpriseNumber ?? string.Empty;
        var vatNumber = model.VatNumber ?? string.Empty;

        if (string.IsNullOrWhiteSpace(enterpriseNumber))
        {
            if (isBelgian)
            {
                ModelState.AddModelError(nameof(SupplierFormViewModel.EnterpriseNumber), "Ondernemingsnummer is verplicht voor Belgische ondernemingen.");
            }
        }
        else
        {
            if (isBelgian && enterpriseNumber.Length != 10)
            {
                ModelState.AddModelError(nameof(SupplierFormViewModel.EnterpriseNumber), "Een Belgisch ondernemingsnummer moet 10 cijfers bevatten.");
            }

            if (!isBelgian && enterpriseNumber.Length > 20)
            {
                ModelState.AddModelError(nameof(SupplierFormViewModel.EnterpriseNumber), "Een buitenlands ondernemingsnummer mag maximaal 20 cijfers bevatten.");
            }

            var existing = await _db.CompanyInfo
                .AsNoTracking()
                .AnyAsync(c => c.Ondernemingsnummer == enterpriseNumber && (!existingCompanyId.HasValue || c.CompanyId != existingCompanyId.Value), ct);

            if (existing)
            {
                ModelState.AddModelError(nameof(SupplierFormViewModel.EnterpriseNumber), "Er bestaat al een leverancier met dit ondernemingsnummer.");
            }
        }

        if (isBelgian)
        {
            if (!string.IsNullOrWhiteSpace(enterpriseNumber))
            {
                if (string.IsNullOrWhiteSpace(vatNumber))
                {
                    model.VatNumber = enterpriseNumber;
                }
                else if (!string.Equals(vatNumber, enterpriseNumber, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(SupplierFormViewModel.VatNumber), "Voor Belgische ondernemingen moet het btw-nummer gelijk zijn aan het ondernemingsnummer.");
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(vatNumber) && vatNumber.Length > 20)
        {
            ModelState.AddModelError(nameof(SupplierFormViewModel.VatNumber), "Een buitenlands btw-nummer mag maximaal 20 cijfers bevatten.");
        }
    }


    [HttpGet]
    [Breadcrumb("Leveranciers")]
    public async Task<IActionResult> Index(int? issuerCompanyId, CancellationToken ct)
    {
        var readScope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Read, ct);
        if (!readScope.HasAccess)
        {
            AddMessage("error", "Je hebt geen rechten om leveranciers te bekijken.", "Geen toegang");
            return RedirectToAction("AccessDenied", "Account");
        }
        var writeScope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Write, ct);
        var deleteScope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Delete, ct);
        ViewData["Title"] = "Leveranciers";

        var suppliersQuery = _db.CompanyInfo
            .Include(c => c.CompanyIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
            .AsNoTracking();

        if (!readScope.HasAllIssuers)
        {
            suppliersQuery = suppliersQuery
                .Where(c => c.CompanyIssuerCompany.Any(ci => readScope.AllowedIssuerIds.Contains(ci.IssuerCompanyId)));
        }

        if (issuerCompanyId.HasValue)
        {
            if (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(issuerCompanyId.Value))
            {
                issuerCompanyId = null;
            }

            suppliersQuery = suppliersQuery.Where(c => c.CompanyIssuerCompany.Any(ci => ci.IssuerCompanyId == issuerCompanyId));
        }

        var suppliers = await suppliersQuery
            .Select(c => new SupplierListItemViewModel
            {
                Id = c.CompanyId,
                Name = c.BedrijfsNaam,
                IsActive = c.IsActive,
                EnterpriseNumber = c.Ondernemingsnummer,
                Email = c.Email,
                CountryCode = c.LandCode,
                Phone = c.Telefoon1,
                Mobile = c.Gsm,
                ContractCount = c.Contract.Count,
                TotalContractAmount = c.Contract
                    .SelectMany(contract => contract.ContractActivity)
                    .Sum(activity => (decimal?)activity.Price) ?? 0m,
                ActivityIds = c.Activity
                    .Select(a => a.ActivityId)
                    .ToList(),
                IssuerCompanyIds = c.CompanyIssuerCompany
                    .Select(ci => ci.IssuerCompanyId)
                    .ToList(),
                CanEdit = false,
                CanDelete = false
            })
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        bool HasAccessForSupplier(SupplierIssuerScope scope, IReadOnlyList<int> issuerIds)
        {
            if (!scope.HasAccess)
            {
                return false;
            }

            if (scope.HasAllIssuers)
            {
                return true;
            }

            return issuerIds.Any(scope.AllowedIssuerIds.Contains);
        }

        suppliers = suppliers
            .Select(s => new SupplierListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                IsActive = s.IsActive,
                EnterpriseNumber = s.EnterpriseNumber,
                Email = s.Email,
                CountryCode = s.CountryCode,
                Phone = s.Phone,
                Mobile = s.Mobile,
                ContractCount = s.ContractCount,
                TotalContractAmount = s.TotalContractAmount,
                ActivityIds = s.ActivityIds,
                IssuerCompanyIds = s.IssuerCompanyIds,
                CanEdit = HasAccessForSupplier(writeScope, s.IssuerCompanyIds),
                CanDelete = HasAccessForSupplier(deleteScope, s.IssuerCompanyIds)
            })
            .ToList();

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

        var issuers = await _db.IssuerCompany
            .AsNoTracking()
            .Where(i => i.IsActive)
             .Where(i => readScope.HasAllIssuers || readScope.AllowedIssuerIds.Contains(i.Id))
            .OrderBy(i => i.Name)
            .Select(i => new IssuerCompanyOptionViewModel
            {
                Id = i.Id,
                Name = i.Name
            })
            .ToListAsync(ct);

        var vm = new SupplierIndexViewModel
        {
            Suppliers = suppliers,
            Activities = activities,
            IssuerCompanies = issuers,
            SelectedIssuerCompanyId = issuerCompanyId,
            CanCreateSupplier = writeScope.HasAccess
                  && (!issuerCompanyId.HasValue
                      || writeScope.HasAllIssuers
                      || writeScope.AllowedIssuerIds.Contains(issuerCompanyId.Value)),
            WritableIssuerCompanyIds = writeScope.HasAllIssuers
                  ? issuers.Select(i => i.Id).ToList()
                  : writeScope.AllowedIssuerIds.ToList()
        };

        return View(vm);
    }


    [HttpGet]
    [Breadcrumb("Detail", FromAction = nameof(Index))]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var readScope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Read, ct);
        if (!readScope.HasAccess)
        {
            AddMessage("error", "Je hebt geen rechten om leveranciers te bekijken.", "Geen toegang");
            return RedirectToAction("AccessDenied", "Account");
        }

        var supplier = await _db.CompanyInfo
            .Include(c => c.PostCode)
                .ThenInclude(p => p.Country)
                .Include(c => c.CompanyIssuerCompany)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == id, ct);

        if (supplier == null)
        {
            return NotFound();
        }

        if (!readScope.HasAllIssuers && !supplier.CompanyIssuerCompany.Any(ci => readScope.AllowedIssuerIds.Contains(ci.IssuerCompanyId)))
        {
            AddMessage("error", "Je hebt geen rechten om deze leverancier te bekijken.", "Geen toegang");
            return RedirectToAction("Index","Leveranciers");
        }

        var writeScope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Write, ct);
        var deleteScope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Delete, ct);
        var canEdit = writeScope.HasAccess && (writeScope.HasAllIssuers || supplier.CompanyIssuerCompany.Any(ci => writeScope.AllowedIssuerIds.Contains(ci.IssuerCompanyId)));
        var canDelete = deleteScope.HasAccess && (deleteScope.HasAllIssuers || supplier.CompanyIssuerCompany.Any(ci => deleteScope.AllowedIssuerIds.Contains(ci.IssuerCompanyId)));


        var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var LeveranciersIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Leveranciers", "Leveranciers")
        {
            Parent = Index,
        };

        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Details), "Leveranciers", supplier.BedrijfsNaam)
        {
            Parent = LeveranciersIndex,
            RouteValues = new { id = supplier.CompanyId }
        };
        

        string? legalFormName = null;

        if (!string.IsNullOrWhiteSpace(supplier.Type))
        {
            legalFormName = await _db.CompanyLegalForm
                .Where(l => l.Abbreviation == supplier.Type || l.Name == supplier.Type)
                .Select(l => l.Name)
                .FirstOrDefaultAsync(ct);
        }

        var countryName = supplier.PostCode?.Country?.LandNaam;

        if (string.IsNullOrWhiteSpace(countryName) && !string.IsNullOrWhiteSpace(supplier.LandCode))
        {
            countryName = await _db.Country
                .Where(c => c.LandIsocode == supplier.LandCode)
                .Select(c => c.LandNaam)
                .FirstOrDefaultAsync(ct);
        }

        var activities = await _db.CompanyInfo
            .Where(c => c.CompanyId == id)
            .SelectMany(c => c.Activity)
            .OrderBy(a => a.Omschrijving)
            .Select(a => a.Omschrijving)
            .ToListAsync(ct);

        var departments = await _db.CompanyDepartments
            .Include(d => d.Postcode)
                .ThenInclude(p => p.Country)
            .Where(d => d.CompanyId == id)
            .AsNoTracking()
            .OrderBy(d => d.Naam)
            .Select(d => new SupplierDepartmentDetailViewModel
            {
                Name = d.Naam,
                Street = d.Straat,
                HouseNumber = d.Huisnummer,
                Bus = d.Bus,
                PostalCode = d.Postcode != null ? d.Postcode.Postcode : null,
                City = d.Postcode != null ? d.Postcode.Gemeente : null,
                CountryName = d.Postcode != null && d.Postcode.Country != null ? d.Postcode.Country.LandNaam : null,
                Phone = d.Telefoon,
                Mobile = d.Gsm,
                Email = d.Email
            })
            .ToListAsync(ct);

        var rawContacts = await _db.CompanyContacts
            .Include(c => c.Department)
            .Where(c => c.CompanyId == id)
            .AsNoTracking()
            .OrderBy(c => c.ContactNaam)
            .ThenBy(c => c.ContactVoornaam)
            .ToListAsync(ct);

        // Invite-status ophalen per e-mail
        var contactEmails = rawContacts
            .Where(c => !string.IsNullOrWhiteSpace(c.Email))
            .Select(c => c.Email!.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var linkedUsers = await _db.Users
            .AsNoTracking()
            .Where(u => u.Email != null && contactEmails.Contains(u.Email.Trim().ToLower()))
            .ToListAsync(ct);

        var linkedUserIds = linkedUsers.Select(u => u.Id).ToList();
        var invites = await _db.UserGuestInvitation
            .AsNoTracking()
            .Where(i => linkedUserIds.Contains(i.UserId))
            .ToListAsync(ct);
        var accessByUser = await _db.UserCompanyAccess
            .AsNoTracking()
            .Where(a => a.CompanyId == id && linkedUserIds.Contains(a.UserId))
            .ToListAsync(ct);

        var contacts = rawContacts.Select(c =>
        {
            var email = c.Email?.Trim().ToLowerInvariant();
            var user = email != null
                ? linkedUsers.FirstOrDefault(u => u.Email?.Trim().ToLower() == email)
                : null;
            var invite = user != null
                ? invites.FirstOrDefault(i => i.UserId == user.Id)
                : null;
            var access = user != null
                ? accessByUser.FirstOrDefault(a => a.UserId == user.Id)
                : null;
            return new SupplierContactDetailViewModel
            {
                ContactId       = c.ContactId,
                FirstName       = c.ContactVoornaam,
                LastName        = c.ContactNaam,
                Function        = c.Functie,
                Phone           = c.Telefoon,
                Mobile          = c.Gsm,
                Email           = c.Email,
                DepartmentName  = c.Department?.Naam,
                LinkedUserId    = user?.Id,
                PortaalStatus   = invite?.InvitationStatus,
                IsFullCompanyAdmin = access?.Role == CPMCore.Services.ContractorAccessRole.VolledigVerantwoordelijk,
                LastLoginAt     = invite?.LastLoginAt
            };
        }).ToList();

        var contracts = await _db.Contract
            .Where(c => c.CompanyId == id)
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new SupplierContractDetailViewModel
            {
                Id = c.Id,
                ProjectId = c.ProjectId,
                ProjectName = c.Project != null ? c.Project.ProjectName : string.Empty,
                TotalAmount = c.ContractActivity.Sum(a => (decimal?)a.Price) ?? 0m,
                ActivityCount = c.ContractActivity.Count
            })
            .ToListAsync(ct);

        var invoices = await _db.IncommingInvoices
          .AsNoTracking()
          .Where(i => i.CompanyId == id || (i.Contract != null && i.Contract.CompanyId == id))
          .Include(i => i.Project)
          .Include(i => i.Contract)
          .OrderByDescending(i => i.Date)
          .Select(i => new SupplierInvoiceDetailViewModel
          {
              Id = i.Id,
              InvoiceDate = i.Date,
              ExternalInvoiceId = i.ExternalId,
              Amount = i.Price,
              ProjectId = i.ProjectId,
              ProjectName = i.Project != null ? i.Project.ProjectName : string.Empty
          })
          .ToListAsync(ct);


        var detailModel = new SupplierDetailViewModel
        {
            Id = supplier.CompanyId,
            CanEdit = canEdit,
            CanDelete = canDelete,
            Name = supplier.BedrijfsNaam,
            EnterpriseNumber = supplier.Ondernemingsnummer,
            LegalForm = legalFormName ?? supplier.Type,
            Street = supplier.Straat,
            HouseNumber = supplier.Huisnummer,
            BusNumber = supplier.Busnummer,
            PostalCode = supplier.Postcode,
            City = supplier.Gemeente,
            CountryName = countryName,
            CountryCode = supplier.LandCode,
            Phone = supplier.Telefoon1,
            Mobile = supplier.Gsm,
            Email = supplier.Email,
            InvoiceEmail = supplier.InvoiceEmail,
            WebUrl = supplier.Weburl,
            RequiresDigitalInvoice = supplier.RequiresDigitalInvoice,
            AttachUblByDefault = supplier.AttachUblByDefault,
            Activities = activities,
            Departments = departments,
            Contacts = contacts,
            Contracts = contracts,
            Invoices = invoices
        };

        ViewData["Title"] = detailModel.Name;
        return View(detailModel);
    }

    // ── Portaal: contact uitnodigen ──────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> InviteContact(int contactId, bool isFullAdmin, CancellationToken ct)
    {
        var contact = await _db.CompanyContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContactId == contactId, ct);

        if (contact == null)
        {
            TempData["Error"] = "Contact niet gevonden.";
            return RedirectToAction(nameof(Details), new { id = 0 });
        }

        if (string.IsNullOrWhiteSpace(contact.Email))
        {
            TempData["Error"] = "Dit contact heeft geen e-mailadres. Voeg eerst een e-mailadres toe.";
            return RedirectToAction(nameof(Details), new { id = contact.CompanyId });
        }

        var appBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var invitedBy  = User.GetCpmUserId();

        var request = new ContractorInviteRequest(
            CompanyId:          contact.CompanyId,
            Email:              contact.Email,
            FirstName:          contact.ContactVoornaam ?? string.Empty,
            LastName:           contact.ContactNaam ?? string.Empty,
            IsFullCompanyAdmin: isFullAdmin,
            JobFunction:        contact.Functie,
            Phone:              contact.Gsm ?? contact.Telefoon,
            ContactId:          isFullAdmin ? null : contact.ContactId);

        var result = await _contractorInviteService.InviteResponsibleAsync(request, appBaseUrl, invitedBy, ct);

        TempData[result.Success ? "Message" : "Error"] = result.Success
            ? $"Uitnodiging verstuurd naar {contact.Email}."
            : result.ErrorMessage ?? "Uitnodiging mislukt.";

        return RedirectToAction(nameof(Details), new { id = contact.CompanyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> RevokeContactAccess(int contactId, CancellationToken ct)
    {
        var contact = await _db.CompanyContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContactId == contactId, ct);

        if (contact == null)
            return RedirectToAction(nameof(Details), new { id = 0 });

        var email = contact.Email?.Trim().ToLowerInvariant();
        if (email != null)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.Trim().ToLower() == email, ct);
            if (user != null)
            {
                var access = await _db.UserCompanyAccess
                    .FirstOrDefaultAsync(a => a.UserId == user.Id && a.CompanyId == contact.CompanyId, ct);
                if (access != null)
                    _db.UserCompanyAccess.Remove(access);

                await _db.SaveChangesAsync(ct);

                // Entra-koppeling en OID wissen zodat de gebruiker niet meer kan inloggen
                var performedBy = User.GetCpmUserId();
                await _guestInvitationService.ResetRedemptionAsync(user.Id, performedBy, ct);
            }
        }

        TempData["Message"] = "Portaaltoegang ingetrokken.";
        return RedirectToAction(nameof(Details), new { id = contact.CompanyId });
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
    public async Task<IActionResult> ValidateVat(string vatNumber, string vatCountryCode, CancellationToken ct)
    {
        var normalized = CombineEnterpriseNumber(vatCountryCode, vatNumber);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return BadRequest(new { error = "Ongeldig ondernemingsnummer" });
        }

        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            using var response = await httpClient.GetAsync($"https://controleerbtwnummer.eu/api/validate/{Uri.EscapeDataString(normalized)}.json", ct);
            var rawContent = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = string.IsNullOrWhiteSpace(rawContent)
                    ? "De controle van het btw-nummer is mislukt."
                    : rawContent;

                return StatusCode((int)response.StatusCode, new { error = errorMessage });
            }

            VatLookupResponse? payload;

            try
            {
                payload = JsonSerializer.Deserialize<VatLookupResponse>(rawContent, VatLookupSerializerOptions);
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(new { error = $"Het antwoord van de btw-service kon niet worden gelezen: {jsonEx.Message}" });
            }

            if (payload == null)
            {
                return BadRequest(new { error = "Ontving een leeg antwoord van de btw-service." });
            }

            payload.address ??= new VatLookupAddress();

            if (string.IsNullOrWhiteSpace(payload.countryCode))
            {
                payload.countryCode = payload.address.countryCode;
            }

            return Json(payload);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new { error = "De btw-service heeft niet tijdig geantwoord. Probeer het later opnieuw." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> FindPostalMatch(string postalCode, string city, string? countryIso, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            return BadRequest();
        }

        var trimmedPostal = postalCode.Trim();
        var trimmedCity = city?.Trim();
        var normalizedIso = countryIso?.Trim().ToUpperInvariant();

        var baseQuery = _db.PostalCode
            .Include(p => p.Country)
            .AsNoTracking()
            .Where(p => p.Postcode == trimmedPostal);

        if (!string.IsNullOrWhiteSpace(normalizedIso))
        {
            baseQuery = baseQuery.Where(p => p.Country != null && p.Country.LandIsocode != null && p.Country.LandIsocode.ToUpper() == normalizedIso);
        }

        var candidates = await baseQuery
            .OrderBy(p => p.Gemeente)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return NotFound();
        }

        PostalCode? match = null;

        if (!string.IsNullOrWhiteSpace(trimmedCity))
        {
            match = candidates.FirstOrDefault(p => string.Equals(p.Gemeente?.Trim(), trimmedCity, StringComparison.OrdinalIgnoreCase));
        }

        match ??= candidates.First();

        return Json(new { id = match.PostcodeId, text = $"{match.Postcode} - {match.Gemeente}", countryId = match.CountryId });
    }

    [HttpGet]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> Create(int? issuerCompanyId, CancellationToken ct)
    {
        var scope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Write, ct);
        if (!scope.HasAccess)
        {
            AddMessage("error", "Je hebt geen rechten om leveranciers toe te voegen.", "Geen toegang");
            return RedirectToAction("AccessDenied", "Account");
        }

        if (issuerCompanyId.HasValue && !scope.HasAllIssuers && !scope.AllowedIssuerIds.Contains(issuerCompanyId.Value))
        {
            issuerCompanyId = null;
        }

        var vm = await BuildFormAsync(new SupplierFormViewModel
        {
            SelectedIssuerCompanyIds = issuerCompanyId.HasValue
                ? new List<int> { issuerCompanyId.Value }
                : new List<int>()
        }, ct, scope);
        return View(vm);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> Create(SupplierFormViewModel model, CancellationToken ct)
    {
        var scope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Write, ct);
        if (!scope.HasAccess)
        {
            AddMessage("error", "Je hebt geen rechten om leveranciers toe te voegen.", "Geen toegang");
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!scope.HasAllIssuers)
        {
            model.SelectedIssuerCompanyIds = (model.SelectedIssuerCompanyIds ?? new List<int>())
                .Where(scope.AllowedIssuerIds.Contains)
                .ToList();
        }

        NormalizeVatInputs(model);
        NormalizeWebsiteInput(model);
        await ValidateVatInputsAsync(model, null, ct);

        if (!ModelState.IsValid)
        {
            await BuildFormAsync(model, ct, scope);
            return View(model);
        }

        await PopulateSelectionsAsync(model, ct);
        await EnsurePostalSelectionAsync(model, ct);

        var entity = new CompanyInfo();
        MapToEntity(model, entity);

        entity.Type = await ResolveLegalFormAbbreviation(model.SelectedLegalFormId, ct);

        _db.CompanyInfo.Add(entity);

        if (model.SelectedActivityIds != null && model.SelectedActivityIds.Any())
        {
            entity.Activity = await _db.Activity
                .Where(a => model.SelectedActivityIds.Contains(a.ActivityId))
                .ToListAsync(ct);
        }

        await AssignIssuerCompaniesAsync(entity, model.SelectedIssuerCompanyIds, ct);

        await _db.SaveChangesAsync(ct);

        var departments = await PersistDepartmentsAsync(entity.CompanyId, model.Departments ?? Enumerable.Empty<DepartmentInputViewModel>(), ct);
        await PersistContactsAsync(entity.CompanyId, model.Contacts ?? Enumerable.Empty<ContactInputViewModel>(), departments, ct);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var scope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Write, ct);
        if (!scope.HasAccess)
        {
            AddMessage("error", "Je hebt geen rechten om leveranciers te bewerken.", "Geen toegang");
            return RedirectToAction("AccessDenied", "Account");
        }

        var entity = await _db.CompanyInfo
            .Include(c => c.Activity)
            .Include(c => c.CompanyIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
            .Include(c => c.CompanyDepartments)
            .ThenInclude(d => d.Postcode)
            .Include(c => c.CompanyContacts)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == id, ct);

        if (entity == null)
        {
            return NotFound();
        }

        if (!scope.HasAllIssuers && !entity.CompanyIssuerCompany.Any(ci => scope.AllowedIssuerIds.Contains(ci.IssuerCompanyId)))
        {
            AddMessage("error", "Je hebt geen rechten om deze leverancier te bewerken.", "Geen toegang");
            return RedirectToAction("Index","Leveranciers");
        }


        var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
        var LeveranciersIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Leveranciers", "Leveranciers")
        {
            Parent = Index,
        };

        var LeveranciersDetail = new MvcBreadcrumbNode(nameof(Details), "Leveranciers", entity.BedrijfsNaam)
        {
            Parent = LeveranciersIndex,
            RouteValues = new { id = entity.CompanyId }
        };
        ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Edit), "Leveranciers", "Bewerken")
        {
            Parent = LeveranciersDetail,
            RouteValues = new { id = entity.CompanyId }
        };



        var legalFormId = await _db.CompanyLegalForm
            .Where(l => l.Abbreviation == entity.Type || l.Name == entity.Type)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(ct);


        var vm = new SupplierFormViewModel
        {
            Id = entity.CompanyId,
            Name = entity.BedrijfsNaam,
            SelectedLegalFormId = legalFormId,
            EnterpriseNumber = SanitizeDigits(entity.Ondernemingsnummer),
            EnterpriseNumberCountryCode = entity.LandCode ?? "BE",
            VatNumber = entity.VatNumber,
            VatStatus = string.IsNullOrWhiteSpace(entity.VatStatus) ? SupplierFormViewModel.VatStatusBtwPlichtig : entity.VatStatus,
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
            IsCustomer = entity.IsCustomer,
            RequiresDigitalInvoice = entity.RequiresDigitalInvoice,
            AttachUblByDefault = entity.AttachUblByDefault,
            SelectedActivityIds = entity.Activity.Select(a => a.ActivityId).ToList(),
            SelectedIssuerCompanyIds = entity.CompanyIssuerCompany.Select(ci => ci.IssuerCompanyId).ToList(),
            WebUrl = entity.Weburl,
            IsActive = entity.IsActive,
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

        await BuildFormAsync(vm, ct, scope);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionWrite(PermissionCodes.Suppliers)]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model, CancellationToken ct)
    {
        var scope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Write, ct);
        if (!scope.HasAccess)
        {
            AddMessage("error", "Je hebt geen rechten om leveranciers te bewerken.", "Geen toegang");
            return RedirectToAction("AccessDenied", "Account");
        }


        if (id != model.Id)
        {
            return BadRequest();
        }

        NormalizeVatInputs(model);
        NormalizeWebsiteInput(model);
        await ValidateVatInputsAsync(model, id, ct);

        if (!ModelState.IsValid)
        {
            await BuildFormAsync(model, ct, scope);
            return View(model);
        }

        var entity = await _db.CompanyInfo
            .Include(c => c.Activity)
            .Include(c => c.CompanyIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
            .Include(c => c.CompanyDepartments)
            .Include(c => c.CompanyContacts)
            .FirstOrDefaultAsync(c => c.CompanyId == id, ct);

        if (entity == null)
        {
            return NotFound();
        }
        if (!scope.HasAllIssuers && !entity.CompanyIssuerCompany.Any(ci => scope.AllowedIssuerIds.Contains(ci.IssuerCompanyId)))
        {
            AddMessage("error", "Je hebt geen rechten om deze leverancier te bewerken.", "Geen toegang");
            return RedirectToAction(nameof(Index));
        }

        if (!scope.HasAllIssuers)
        {
            model.SelectedIssuerCompanyIds = (model.SelectedIssuerCompanyIds ?? new List<int>())
                .Where(scope.AllowedIssuerIds.Contains)
                .ToList();
        }

        await PopulateSelectionsAsync(model, ct);
        await EnsurePostalSelectionAsync(model, ct);

        var requiresOctopusSync = (entity.OctopusRelationId ?? 0) > 0
            && model.IsCustomer
            && HasSupplierDataChanged(entity, model);

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
        
        await AssignIssuerCompaniesAsync(entity, model.SelectedIssuerCompanyIds, ct);

        var departments = await UpsertDepartmentsAsync(
               entity.CompanyId,
               entity.CompanyDepartments.ToList(),
               model.Departments ?? Enumerable.Empty<DepartmentInputViewModel>(),
               ct);

        await SyncContactsAsync(
            entity.CompanyId,
            entity.CompanyContacts.ToList(),
            model.Contacts ?? Enumerable.Empty<ContactInputViewModel>(),
            departments,
            ct);

        await _db.SaveChangesAsync(ct);

        if (requiresOctopusSync)
        {
            await TrySyncSupplierRelationAsync(entity.CompanyId, ct);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.Suppliers)]
    public async Task<IActionResult> ModalDelete(int id, CancellationToken ct)
    {
        var scope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Delete, ct);
        if (!scope.HasAccess)
        {
            return StatusCode(403, new { redirectUrl = Url.Action("AccessDenied", "Account") });
        }

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

        if (!scope.HasAllIssuers)
        {
            var supplierIssuerIds = await _db.CompanyIssuerCompany
                .AsNoTracking()
                .Where(ci => ci.CompanyId == id)
                .Select(ci => ci.IssuerCompanyId)
                .ToListAsync(ct);

            if (!supplierIssuerIds.Any(scope.AllowedIssuerIds.Contains))
            {
                return StatusCode(403, new { redirectUrl = Url.Action("AccessDenied", "Account") });
            }
        }

        return PartialView("Modals/_ModalDeleteSupplier", supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CPMCore.Filters.PermissionDelete(PermissionCodes.Suppliers)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var scope = await ResolveSupplierIssuerScopeAsync(PermissionAccessType.Delete, ct);
        if (!scope.HasAccess)
        {
            AddMessage("error", "Je hebt geen rechten om leveranciers te verwijderen.", "Geen toegang");
            return RedirectToAction("AccessDenied", "Account");
        }

        var entity = await _db.CompanyInfo.FindAsync(new object[] { id }, ct);

        if (entity == null)
        {
            return NotFound();
        }

        if (!scope.HasAllIssuers)
        {
            var supplierIssuerIds = await _db.CompanyIssuerCompany
                .AsNoTracking()
                .Where(ci => ci.CompanyId == id)
                .Select(ci => ci.IssuerCompanyId)
                .ToListAsync(ct);

            if (!supplierIssuerIds.Any(scope.AllowedIssuerIds.Contains))
            {
                AddMessage("error", "Je hebt geen rechten om deze leverancier te verwijderen.", "Geen toegang");
                return RedirectToAction(nameof(Index));
            }
        }

        _db.CompanyInfo.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public PartialViewResult BlankDepartmentRow()
    {
        return PartialView("Partials/_SupplierDepartmentRow", new DepartmentInputViewModel());
    }

    [HttpGet]
    public PartialViewResult BlankContactRow()
    {
        return PartialView("Partials/_SupplierContactRow", new ContactInputViewModel());
    }
    private async Task<SupplierIssuerScope> ResolveSupplierIssuerScopeAsync(PermissionAccessType accessType, CancellationToken ct)
    {
        var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        await permissionService.EnsureLoadedAsync(ct);

        var hasMainAccess = accessType switch
        {
            PermissionAccessType.Read => permissionService.HasRead(PermissionCodes.Suppliers),
            PermissionAccessType.Write => permissionService.HasWrite(PermissionCodes.Suppliers),
            PermissionAccessType.Delete => permissionService.HasDelete(PermissionCodes.Suppliers),
            _ => false
        };

        if (!hasMainAccess)
        {
            return new SupplierIssuerScope(false, false, new HashSet<int>());
        }

        var scopedIds = permissionService.EffectivePermissions
            .Where(x => x.Key.StartsWith(SupplierCompanyPermissionPrefix, StringComparison.OrdinalIgnoreCase))
            .Where(x => accessType switch
            {
                PermissionAccessType.Read => x.Value.Read,
                PermissionAccessType.Write => x.Value.Write,
                PermissionAccessType.Delete => x.Value.Delete,
                _ => false
            })
            .Select(x =>
            {
                var suffix = x.Key[SupplierCompanyPermissionPrefix.Length..];
                return int.TryParse(suffix, out var companyId) ? (int?)companyId : null;
            })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

        if (scopedIds.Count == 0)
        {
            return new SupplierIssuerScope(true, true, new HashSet<int>());
        }

        return new SupplierIssuerScope(true, false, scopedIds.ToHashSet());
    }

    private sealed record SupplierIssuerScope(bool HasAccess, bool HasAllIssuers, HashSet<int> AllowedIssuerIds);

    private static bool HasSupplierDataChanged(CompanyInfo entity, SupplierFormViewModel model)
    {
        static bool Different(string? a, string? b)
            => !string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

        var newVat = model.EnterpriseNumber;

        return Different(entity.Straat, model.Street)
            || Different(entity.Huisnummer, model.HouseNumber)
            || Different(entity.Busnummer, model.BusNumber)
            || entity.PostCodeId != model.SelectedPostalCodeId
            || Different(entity.Gemeente, model.City)
            || Different(entity.Postcode, model.PostalCode)
            || Different(entity.Ondernemingsnummer, newVat)
            || Different(entity.Email, model.Email)
            || Different(entity.InvoiceEmail, model.InvoiceEmail)
            || Different(entity.LandCode, model.EnterpriseNumberCountryCode)
            || Different(entity.VatNumber, model.VatNumber)
            || Different(entity.VatStatus, model.VatStatus);
    }

    private async Task TrySyncSupplierRelationAsync(int companyId, CancellationToken ct)
    {
        var company = await _db.CompanyInfo
            .Include(c => c.PostCode)!
                .ThenInclude(pc => pc.Country)
            .Include(c => c.CompanyIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
            .FirstOrDefaultAsync(c => c.CompanyId == companyId, ct);

        if (company?.OctopusRelationId is not int relationId || relationId <= 0 || !company.IsCustomer)
        {
            return;
        }

        var request = BuildOctopusRelationRequest(company);

        var issuers = company.CompanyIssuerCompany
            .Select(ci => ci.IssuerCompany)
            .Where(i => i != null && !string.IsNullOrWhiteSpace(i.OctopusDossierNumber))
            .Cast<IssuerCompany>();

        foreach (var issuer in issuers)
        {
            var dossierToken = await _octopusTokens.RefreshDossierTokenAsync(issuer.Id, issuer.OctopusDossierNumber, ct);
            await _octopusClient.UpsertRelationAsync(dossierToken.Token, issuer.OctopusDossierNumber, request, ct);
        }
    }

    private static OctopusRelationRequest BuildOctopusRelationRequest(CompanyInfo company)
    {
        var countryCode = company.PostCode?.Country?.LandIsocode ?? company.LandCode;

        var name = company.BedrijfsNaam;

        var request = new OctopusRelationRequest
        {
            RelationIdentificationServiceData = new OctopusRelationIdentificationData
            {
                RelationKey = new OctopusRelationKey { Id = company.OctopusRelationId ?? 0 },
                ExternalRelationId = company.CompanyId
            },
            Name = name,
            Firstname = name,
            Client = true,
            Supplier = true,
            Active = true,
            StreetAndNr = BuildStreetAndNumber(company.Straat, company.Huisnummer, company.Busnummer, company.Straat),
            PostalCode = company.Postcode,
            City = company.Gemeente,
            Country = countryCode ?? "BE",
            VatNr = FormatVatNumberForOctopus(company.VatNumber ?? company.Ondernemingsnummer, countryCode),
            CurrencyCode = "EUR",
            Contactperson = name,
            DefaultBookingAccountClient = 0,
            DefaultBookingAccountSupplier = 0,
            SupplierPaymentMethod = 0,
            VatType = DetermineVatType(!string.IsNullOrWhiteSpace(company.VatNumber ?? company.Ondernemingsnummer), countryCode)
        };

        return request;
    }

    private static string? BuildStreetAndNumber(string? street, string? houseNumber, string? busNumber, string? fallback)
    {
        var parts = new[] { street, houseNumber, string.IsNullOrWhiteSpace(busNumber) ? null : busNumber }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        var result = string.Join(" ", parts);

        if (string.IsNullOrWhiteSpace(result))
            return fallback;

        return result;
    }

    private static string? FormatVatNumberForOctopus(string? vatNumber, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
            return null;

        var cleanedDigits = new string(vatNumber.Where(char.IsDigit).ToArray());
        if (cleanedDigits.Length >= 10)
        {
            var digits = cleanedDigits[^10..];
            var prefix = string.IsNullOrWhiteSpace(countryCode) ? "BE" : countryCode.Trim().ToUpperInvariant();
            return $"{prefix}{digits[..4]}.{digits.Substring(4, 3)}.{digits.Substring(7, 3)}";
        }

        return vatNumber;
    }

    private static int? DetermineVatType(bool isCompany, string? countryCode)
    {
        var isBelgian = string.Equals(countryCode, "BE", StringComparison.OrdinalIgnoreCase);

        if (isCompany)
            return isBelgian ? 1 : 4;

        return isBelgian ? 7 : 8;
    }
    private sealed class VatLookupResponse
    {
        public bool valid { get; set; }

        public string vatNumber { get; set; } = string.Empty;

        public string name { get; set; } = string.Empty;

        public string countryCode { get; set; } = string.Empty;

        public VatLookupAddress? address { get; set; }

        [JsonPropertyName("strAddress")]
        public string? strAddress { get; set; }
    }

    private sealed class VatLookupAddress
    {
        public string street { get; set; } = string.Empty;

        public string number { get; set; } = string.Empty;

        [JsonPropertyName("zip_code")]
        public string zip { get; set; } = string.Empty;

        public string city { get; set; } = string.Empty;

        public string country { get; set; } = string.Empty;

        public string countryCode { get; set; } = string.Empty;
    }
}