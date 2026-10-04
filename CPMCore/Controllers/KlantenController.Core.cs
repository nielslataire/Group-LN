using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Instellingen;
using CPMCore.Models.Klanten;
using CPMCore.Models.Projecten;
using FacadeCore;
using CPMCore.Services.Octopus;
using CPMCore.Services.Security;
using DALCore.Models;
using DinkToPdf;
using FacadeCore;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Configuration;
using ServiceCore;
using ServiceCore.Signing;
using SmartBreadcrumbs.Attributes;
using SmartBreadcrumbs.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

namespace CPMCore.Controllers
{
    /// <summary>Klantenfiche: lijst, detail, aanmaken, bewerken, snel aanmaken, plus de formulier-/Octopus-helpers daarvoor. Opgesplitst uit KlantenController.cs (okt. 2026, structureren) - views in Views/Klanten/Core/. Zelfde partial class: alle private velden/services van KlantenController.cs blijven gewoon bruikbaar.</summary>
    public partial class KlantenController
    {

        [HttpGet]
        [Breadcrumb("Klanten")]
        public async Task<IActionResult> Index(int? issuerCompanyId, CancellationToken ct)
        {
            var readScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bekijken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            var writeScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            var deleteScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Delete, ct);


            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var KlantenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Klanten", "Klanten")
            {
                Parent = Index,
            };

            ViewData["BreadcrumbNode"] = KlantenIndex;
            var issuerCompanies = await _db.IssuerCompany
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

            var clientsQuery = _db.ClientAccount
                .Include(c => c.PostalCode)
                .Include(c => c.ClientContacts)
                 .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .AsNoTracking()
                .OrderBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? c.Name : c.CompanyName);

            if (issuerCompanyId.HasValue)
            {
                if (!readScope.HasAllIssuers && !readScope.AllowedIssuerIds.Contains(issuerCompanyId.Value))
                {
                    issuerCompanyId = null;
                }

                clientsQuery = clientsQuery
                    .Where(c => c.ClientAccountIssuerCompany.Any(i => i.IssuerCompanyId == issuerCompanyId))
                    .OrderBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? c.Name : c.CompanyName);
            }

            if (!readScope.HasAllIssuers)
            {
                clientsQuery = clientsQuery
                    .Where(c => c.ClientAccountIssuerCompany.Any(i => readScope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
                    .OrderBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? c.Name : c.CompanyName);
            }

            var clients = await clientsQuery
                .Select(c => new ClientListItemViewModel
                {
                    Id = c.Id,
                    DisplayName = !string.IsNullOrWhiteSpace(c.CompanyName)
                        ? c.CompanyName
                        : (string.IsNullOrWhiteSpace(c.Forename) ? c.Name : c.Name + " " + c.Forename),
                    EnterpriseNumber = c.Vatnumber,
                    City = c.PostalCode != null
                        ? c.PostalCode.Postcode + " " + c.PostalCode.Gemeente
                        : null,

                    // Primair contact (indien aangeduid) wint van de bestaande "eerste bij Id"-
                    // terugval — OrderByDescending(IsPrimaryContact) zet 'm vooraan ongeacht Id,
                    // zonder de bestaande volgorde/uitkomst te wijzigen wanneer niemand primair is.
                    Email = !string.IsNullOrWhiteSpace(c.Email)
                        ? c.Email
                        : c.ClientContacts
                            .OrderByDescending(cc => cc.IsPrimaryContact)
                            .ThenBy(cc => cc.Id)
                            .Select(cc => cc.Email)
                            .FirstOrDefault(),

                    // Migratie 058: eigenaar 1 se eigen Phone/Cellphone wint nu, zelfde voorrangsregel
                    // als Email hierboven (eigen waarde eerst, anders de eerste/primaire contactpersoon).
                    Phone = !string.IsNullOrWhiteSpace(c.Phone)
                        ? c.Phone
                        : !string.IsNullOrWhiteSpace(c.Cellphone)
                            ? c.Cellphone
                            : c.ClientContacts
                                .OrderByDescending(cc => cc.IsPrimaryContact)
                                .ThenBy(cc => cc.Id)
                                .Select(cc => cc.Phone != null ? cc.Phone : cc.Cellphone)
                                .FirstOrDefault(),

                    IssuerCompanies = c.ClientAccountIssuerCompany
                        .Select(i => i.IssuerCompany.Name)
                        .ToList(),
                    IssuerCompanyIds = c.ClientAccountIssuerCompany
                        .Select(i => i.IssuerCompanyId)
                        .ToList(),

                    ContactCount = c.ClientContacts.Count,
                    CanEdit = false,
                    CanDelete = false
                })
                .ToListAsync(ct);


            bool HasAccessForClient(CustomerIssuerScope scope, IReadOnlyList<int> issuerIds)
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

            var clientsWithPermissions = clients
                .Select(client => new ClientListItemViewModel
                {
                    Id = client.Id,
                    DisplayName = client.DisplayName,
                    EnterpriseNumber = client.EnterpriseNumber,
                    City = client.City,
                    Email = client.Email,
                    Phone = client.Phone,
                    IssuerCompanies = client.IssuerCompanies,
                    IssuerCompanyIds = client.IssuerCompanyIds,
                    ContactCount = client.ContactCount,
                    CanEdit = HasAccessForClient(writeScope, client.IssuerCompanyIds),
                    CanDelete = HasAccessForClient(deleteScope, client.IssuerCompanyIds)
                })
                .ToList();

            var canCreateClient = writeScope.HasAccess
                && (!issuerCompanyId.HasValue
                    || writeScope.HasAllIssuers
                    || writeScope.AllowedIssuerIds.Contains(issuerCompanyId.Value));

            var model = new ClientIndexViewModel
            {
                Clients = clientsWithPermissions,
                IssuerCompanies = issuerCompanies,
                SelectedIssuerCompanyId = issuerCompanyId,
                CanCreateClient = canCreateClient,
                WritableIssuerCompanyIds = writeScope.HasAllIssuers
                   ? issuerCompanies.Select(i => i.Id).ToList()
                   : writeScope.AllowedIssuerIds.ToList()
            };

            // Facturatiebedrijf-filter actief? Titel/breadcrumb tonen dan dat bedrijf i.p.v. de
            // generieke "Klanten" — zelfde MvcBreadcrumbNode-aanpak als Details verderop (die
            // expliciet Home > Klanten > <naam> opbouwt i.p.v. het statische [Breadcrumb]-attribuut
            // te hergebruiken, want dat kan geen route-afhankelijke waarde tonen).
            var selectedIssuerName = issuerCompanyId.HasValue
                ? issuerCompanies.FirstOrDefault(i => i.Id == issuerCompanyId.Value)?.Name
                : null;

            if (!string.IsNullOrWhiteSpace(selectedIssuerName))
            {
                SetPageHeader("bx bx-group", $"Klanten {selectedIssuerName}");
                ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Index), "Klanten", selectedIssuerName)
                {
                    Parent = KlantenIndex,
                    RouteValues = new { issuerCompanyId = issuerCompanyId!.Value }
                };
            }
            else
            {
                SetPageHeader("bx bx-group", "Klanten");
            }

            return View(ViewData["UseGlV2Layout"] as bool? == true ? "IndexV2" : "Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> Lookup(string? term, int take = 20, CancellationToken ct = default)
        {
            var query = _db.ClientAccount.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var like = $"%{term.Trim()}%";
                query = query.Where(c => EF.Functions.Like(c.Name, like));
            }

            var results = await query
                .OrderBy(c => c.Name)
                .Take(take)
                .Select(c => new { id = c.Id, text = c.Name ?? "" })
                .ToListAsync(ct);

            return Json(new { results });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {

            var readScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Read, ct);
            if (!readScope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bekijken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            var client = await _db.ClientAccount
                .Include(c => c.PostalCode)
                .Include(c => c.InvoicePostalCode)
                .Include(c => c.ClientContacts)
               .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (client == null)
            {
                return NotFound();
            }

            if (!readScope.HasAllIssuers && !client.ClientAccountIssuerCompany.Any(i => readScope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om deze klant te bekijken.", "Geen toegang");
                return RedirectToAction("Index","Klanten");
            }

            var writeScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            var canEdit = writeScope.HasAccess
                && (writeScope.HasAllIssuers || client.ClientAccountIssuerCompany.Any(i => writeScope.AllowedIssuerIds.Contains(i.IssuerCompanyId)));
            var deleteScope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Delete, ct);
            var canDelete = deleteScope.HasAccess
                && (deleteScope.HasAllIssuers || client.ClientAccountIssuerCompany.Any(i => deleteScope.AllowedIssuerIds.Contains(i.IssuerCompanyId)));

            var clientDisplayName = DisplayNameOf(client);
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var KlantenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Klanten", "Klanten")
            {
                Parent = Index,
            };

            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Details), "Klanten", clientDisplayName)
            {
                Parent = KlantenIndex,
                RouteValues = new { id }
            };



            var isCompany = !string.IsNullOrWhiteSpace(client.CompanyName) || !string.IsNullOrWhiteSpace(client.Vatnumber);
            Salutation? salutation = Enum.TryParse(client.Salutation, out Salutation parsed)
                    ? parsed
                    : null;
            var (vatCountryCode, vatNumber) = SplitEnterpriseNumber(client.Vatnumber);

            var model = new ClientFormViewModel
            {
                Id = client.Id,
                IsCompany = isCompany,
                CompanyName = isCompany ? client.CompanyName ?? client.Name : null,
                Name = client.Name,
                Forename = client.Forename,
                Salutation = isCompany ? null : salutation,
                EnterpriseNumber = isCompany ? vatNumber : null,
                EnterpriseNumberCountryCode = isCompany ? vatCountryCode ?? "BE" : "BE",
                Street = client.Street,
                HouseNumber = client.Housenumber,
                BusNumber = client.Busnumber,
                SelectedPostalCodeId = client.PostalCodeId,
                PostalCode = client.PostalCode?.Postcode,
                City = client.PostalCode?.Gemeente,
                SelectedCountryId = client.PostalCode?.CountryId,
                UseInvoiceAddress = client.InvoiceAddress ?? false,
                InvoiceStreet = client.InvoiceStreet,
                InvoiceHouseNumber = client.InvoiceHousenumber,
                InvoiceBusNumber = client.InvoiceBusnumber,
                SelectedInvoicePostalCodeId = client.InvoicePostalCodeId,
                InvoicePostalCode = client.InvoicePostalCode?.Postcode,
                InvoiceCity = client.InvoicePostalCode?.Gemeente,
                SelectedInvoiceCountryId = client.InvoicePostalCode?.CountryId,
                Email = client.Email,
                Phone = client.Phone,
                Cellphone = client.Cellphone,
                InvoiceEmail = client.InvoiceEmail,
                RequiresDigitalInvoice = client.RequiresDigitalInvoice,
                AttachUblByDefault = client.AttachUblByDefault,
                SelectedIssuerCompanyIds = client.ClientAccountIssuerCompany
                    .Select(i => i.IssuerCompanyId)
                    .ToList(),
                // Mede-eigenaars (IsCoOwner) horen hier niet tussen — dit is de contactenlijst van het
                // globale (projectloze) scherm, en ContactInputViewModel heeft geen aandeel/type/adres
                // om ze correct te tonen. Ze zouden hier als "gewoon contact" verschijnen, zonder hun
                // eigenaarsrol — zelfde reden als de Edit-actie hieronder ze uitsluit.
                Contacts = client.ClientContacts
                    .Where(c => !c.IsCoOwner)
                    .OrderBy(c => c.Id)
                    .Select(c => new ContactInputViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Forename = c.Forename,
                        Email = c.Email,
                        Phone = c.Phone,
                        Mobile = c.Cellphone,
                        RequiresDigitalInvoice = c.RequiresDigitalInvoice,
                        AttachUblByDefault = c.AttachUblByDefault
                    }).ToList(),
                CanEdit = canEdit,
                CanDelete = canDelete
            };

            await BuildFormAsync(model, ct);
            ViewBag.ReadOnly = true;
            ViewData["BackUrl"] = Url.Action("Index", "Klanten");
            SetPageHeader("bx bx-group", string.IsNullOrWhiteSpace(model.DisplayLabel) ? model.Title : model.DisplayLabel);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailsV2" : "Form", model);
        }

        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Create(int? issuerCompanyId, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten toe te voegen.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            if (issuerCompanyId.HasValue && !scope.HasAllIssuers && !scope.AllowedIssuerIds.Contains(issuerCompanyId.Value))
            {
                issuerCompanyId = null;
            }

            // Zelfde Referrer-patroon als Edit: Annuleren en (bij succes) Opslaan gaan terug naar de
            // pagina waar de gebruiker vandaan kwam i.p.v. altijd naar Index.
            TempData["Referrer"] = Request.Headers["Referer"].ToString();

            var home = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var klantenIndex = new MvcBreadcrumbNode("Index", "Klanten", "Klanten")
            {
                Parent = home
            };
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Create), "Klanten", "Nieuw klant")
            {
                Parent = klantenIndex
            };

            var model = new ClientFormViewModel
            {
                SelectedIssuerCompanyIds = issuerCompanyId.HasValue
                    ? new List<int> { issuerCompanyId.Value }
                    : new List<int>()
            };
            await BuildFormAsync(model, ct, scope);
            SetPageHeader("bx bx-group", model.Title);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "CreateV2" : "Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Create(ClientFormViewModel model, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten toe te voegen.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!scope.HasAllIssuers)
            {
                model.SelectedIssuerCompanyIds = (model.SelectedIssuerCompanyIds ?? new List<int>())
                    .Where(scope.AllowedIssuerIds.Contains)
                    .ToList();
            }

            RemoveEmptyContacts(model);
            await BuildFormAsync(model, ct, scope);
            ValidateClientType(model);
            ValidateIssuerCompanies(model);

            if (!ModelState.IsValid)
            {
                SetPageHeader("bx bx-group", model.Title);
                return View(ViewData["UseGlV2Layout"] as bool? == true ? "CreateV2" : "Create", model);
            }

            var entity = new ClientAccount();
            MapToEntity(model, entity);
            AttachIssuerCompany(model, entity);
            AttachContacts(model, entity);

            _db.ClientAccount.Add(entity);
            await _db.SaveChangesAsync(ct);

            AddMessage("success", $"Klant {model.DisplayLabel} is toegevoegd", "Geslaagd!");
            var referrer = TempData["Referrer"] as string;
            return !string.IsNullOrWhiteSpace(referrer) ? Redirect(referrer) : RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> QuickCreateModal(int issuerCompanyId, CancellationToken ct)
        {
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

            var issuers = await _db.IssuerCompany
                .AsNoTracking()
                .OrderBy(i => i.Name)
                .Select(i => new IssuerCompanyOptionViewModel { Id = i.Id, Name = i.Name })
                .ToListAsync(ct);

            var vm = new QuickCreateClientModalViewModel
            {
                IssuerCompanyId = issuerCompanyId,
                Countries = countries,
                IssuerCompanies = issuers,
                DefaultCountryCode = "BE"
            };

            return PartialView("Partials/_QuickCreateModal", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> QuickCreate([FromForm] QuickCreateClientDto dto, CancellationToken ct)
        {
            var entityName = dto.IsCompany
                ? (dto.CompanyName ?? "").Trim()
                : string.Join(" ", new[] { dto.Forename, dto.Name }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var displayName = dto.IsCompany
                ? entityName
                : string.Join(" ", new[] { dto.Salutation, dto.Forename, dto.Name }.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrWhiteSpace(entityName))
                return Json(new { success = false, error = "Naam is verplicht." });

            if (dto.IsSupplier)
            {
                // Save as CompanyInfo (leverancier)
                var supplier = new CompanyInfo
                {
                    BedrijfsNaam = displayName,
                    Ondernemingsnummer = dto.IsCompany ? NormalizeEnterpriseNumber(dto.EnterpriseNumber) : null,
                    Email = dto.Email,
                    InvoiceEmail = dto.InvoiceEmail,
                    RequiresDigitalInvoice = dto.RequiresDigitalInvoice,
                    Straat = dto.Street,
                    Huisnummer = dto.HouseNumber,
                    Busnummer = dto.BusNumber,
                    PostCodeId = dto.PostalCodeId > 0 ? dto.PostalCodeId : null,
                    IsActive = true
                };

                if (dto.IssuerCompanyId > 0)
                {
                    var issuer = await _db.IssuerCompany.FindAsync(new object[] { dto.IssuerCompanyId }, ct);
                    if (issuer != null)
                    {
                        supplier.CompanyIssuerCompany.Add(new CompanyIssuerCompany
                        {
                            Company = supplier,
                            IssuerCompany = issuer,
                            IssuerCompanyId = issuer.Id
                        });
                    }
                }

                _db.CompanyInfo.Add(supplier);
                await _db.SaveChangesAsync(ct);

                return Json(new
                {
                    success = true,
                    id = $"su:{supplier.CompanyId}",
                    display = displayName,
                    text = displayName,
                    name = displayName,
                    type = "Supplier"
                });
            }
            else
            {
                // Save as ClientAccount (klant) — Naam en Voornaam apart bewaard (migratie 057) i.p.v.
                // samengevoegd; enkel als Naam zelf leeg bleef (snel toegevoegd met enkel een voornaam)
                // valt dit terug op de samengevoegde entityName, zoals voorheen.
                var entity = new ClientAccount
                {
                    Name = dto.IsCompany
                        ? dto.CompanyName
                        : (!string.IsNullOrWhiteSpace(dto.Name) ? dto.Name.Trim() : entityName),
                    Forename = dto.IsCompany || string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Forename?.Trim(),
                    CompanyName = dto.IsCompany ? dto.CompanyName : null,
                    Salutation = dto.IsCompany ? null : dto.Salutation,
                    Vatnumber = dto.IsCompany ? NormalizeEnterpriseNumber(dto.EnterpriseNumber) : null,
                    Email = dto.Email,
                    InvoiceEmail = dto.InvoiceEmail,
                    RequiresDigitalInvoice = dto.RequiresDigitalInvoice,
                    Street = dto.Street,
                    Housenumber = dto.HouseNumber,
                    Busnumber = dto.BusNumber,
                    PostalCodeId = dto.PostalCodeId > 0 ? dto.PostalCodeId : null,
                    InvoiceAddress = dto.UseInvoiceAddress,
                    InvoiceStreet = dto.UseInvoiceAddress ? dto.InvoiceStreet : null,
                    InvoiceHousenumber = dto.UseInvoiceAddress ? dto.InvoiceHouseNumber : null,
                    InvoiceBusnumber = dto.UseInvoiceAddress ? dto.InvoiceBusNumber : null,
                    InvoicePostalCodeId = dto.UseInvoiceAddress && dto.InvoicePostalCodeId > 0 ? dto.InvoicePostalCodeId : null,
                    OwnerPercentage = 100,
                    OwnerTypeId = 1
                };

                if (dto.IssuerCompanyId > 0)
                {
                    var issuer = await _db.IssuerCompany.FindAsync(new object[] { dto.IssuerCompanyId }, ct);
                    if (issuer != null)
                    {
                        entity.ClientAccountIssuerCompany.Add(new ClientAccountIssuerCompany
                        {
                            IssuerCompany = issuer,
                            IssuerCompanyId = issuer.Id
                        });
                    }
                }

                _db.ClientAccount.Add(entity);
                await _db.SaveChangesAsync(ct);

                return Json(new
                {
                    success = true,
                    id = $"ca:{entity.Id}",
                    display = displayName,
                    text = displayName,
                    name = displayName,
                    type = "ClientAccount"
                });
            }
        }

        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bewerken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }

            var client = await _db.ClientAccount
                .Include(c => c.ClientContacts)
                 .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .Include(c => c.PostalCode)
                .Include(c => c.InvoicePostalCode)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (client == null)
            {
                return NotFound();
            }

            if (!scope.HasAllIssuers && !client.ClientAccountIssuerCompany.Any(i => scope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om deze klant te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(Index));
            }

            TempData["Referrer"] = Request.Headers["Referer"].ToString();

            var clientDisplayName = DisplayNameOf(client);
            var klantenNode = new MvcBreadcrumbNode(nameof(Index), "Klanten", "Klanten")
            {
                Parent = new MvcBreadcrumbNode("Index", "Home", "Home")
            };
            var detailsNode = new MvcBreadcrumbNode(nameof(Details), "Klanten", clientDisplayName)
            {
                Parent = klantenNode,
                RouteValues = new { id = client.Id }
            };
            ViewData["BreadcrumbNode"] = new MvcBreadcrumbNode(nameof(Edit), "Klanten", "Bewerken")
            {
                Parent = detailsNode,
                RouteValues = new { id = client.Id }
            };

            var isCompany = !string.IsNullOrWhiteSpace(client.CompanyName) || !string.IsNullOrWhiteSpace(client.Vatnumber);
            Salutation? salutation = Enum.TryParse(client.Salutation, out Salutation parsed)
                ? parsed
                : null;
            var (vatCountryCode, vatNumber) = SplitEnterpriseNumber(client.Vatnumber);

            var model = new ClientFormViewModel
            {
                Id = client.Id,
                IsCompany = isCompany,
                CompanyName = isCompany ? client.CompanyName ?? client.Name : null,
                Name = client.Name,
                Forename = client.Forename,
                Salutation = isCompany ? null : salutation,
                EnterpriseNumber = isCompany ? vatNumber : null,
                EnterpriseNumberCountryCode = isCompany ? vatCountryCode ?? "BE" : "BE",
                Street = client.Street,
                HouseNumber = client.Housenumber,
                BusNumber = client.Busnumber,
                SelectedPostalCodeId = client.PostalCodeId,
                PostalCode = client.PostalCode?.Postcode,
                City = client.PostalCode?.Gemeente,
                SelectedCountryId = client.PostalCode?.CountryId,
                UseInvoiceAddress = client.InvoiceAddress ?? false,
                InvoiceStreet = client.InvoiceStreet,
                InvoiceHouseNumber = client.InvoiceHousenumber,
                InvoiceBusNumber = client.InvoiceBusnumber,
                SelectedInvoicePostalCodeId = client.InvoicePostalCodeId,
                InvoicePostalCode = client.InvoicePostalCode?.Postcode,
                InvoiceCity = client.InvoicePostalCode?.Gemeente,
                SelectedInvoiceCountryId = client.InvoicePostalCode?.CountryId,
                Email = client.Email,
                Phone = client.Phone,
                Cellphone = client.Cellphone,
                InvoiceEmail = client.InvoiceEmail,
                RequiresDigitalInvoice = client.RequiresDigitalInvoice,
                AttachUblByDefault = client.AttachUblByDefault,
                SelectedIssuerCompanyIds = client.ClientAccountIssuerCompany
                    .Select(i => i.IssuerCompanyId)
                    .ToList(),
                // Mede-eigenaars (IsCoOwner) worden hier bewust NIET getoond — dit globale scherm kent
                // hun aandeel/type/adres niet (ContactInputViewModel heeft die velden niet), en zou ze
                // via UpdateContacts hieronder blind als "gewoon contact" overschrijven of, als de rij
                // niet terugkomt, zelfs verwijderen. Mede-eigenaars blijven uitsluitend beheerd via het
                // projectscherm (Klanten/EditProject), waar ze wél met hun volledige context staan.
                Contacts = client.ClientContacts
                    .Where(c => !c.IsCoOwner)
                    .OrderBy(c => c.Id)
                    .Select(c => new ContactInputViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Forename = c.Forename,
                        Email = c.Email,
                        Phone = c.Phone,
                        Mobile = c.Cellphone,
                        RequiresDigitalInvoice = c.RequiresDigitalInvoice,
                        AttachUblByDefault = c.AttachUblByDefault
                    }).ToList()
            };

            await BuildFormAsync(model, ct, scope);
            SetPageHeader("bx bx-group", model.Title);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditV2" : "Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Customers)]
        public async Task<IActionResult> Edit(int id, ClientFormViewModel model, CancellationToken ct)
        {
            var scope = await ResolveCustomerIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!scope.HasAccess)
            {
                AddMessage("error", "Je hebt geen rechten om klanten te bewerken.", "Geen toegang");
                return RedirectToAction("AccessDenied", "Account");
            }
            if (id != model.Id)
            {
                return BadRequest();
            }

            RemoveEmptyContacts(model);
            var client = await _db.ClientAccount
                .Include(c => c.ClientContacts)
                .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (client == null)
            {
                return NotFound();
            }
            if (!scope.HasAllIssuers && !client.ClientAccountIssuerCompany.Any(i => scope.AllowedIssuerIds.Contains(i.IssuerCompanyId)))
            {
                AddMessage("error", "Je hebt geen rechten om deze klant te bewerken.", "Geen toegang");
                return RedirectToAction(nameof(Index));
            }

            if (!scope.HasAllIssuers)
            {
                model.SelectedIssuerCompanyIds = (model.SelectedIssuerCompanyIds ?? new List<int>())
                    .Where(scope.AllowedIssuerIds.Contains)
                    .ToList();
            }

            await BuildFormAsync(model, ct, scope);
            ValidateClientType(model);
            ValidateIssuerCompanies(model);

            if (!ModelState.IsValid)
            {
                SetPageHeader("bx bx-group", model.Title);
                return View(ViewData["UseGlV2Layout"] as bool? == true ? "EditV2" : "Edit", model);
            }

            var requiresOctopusSync = client.ClientAccountIssuerCompany.Any(l => (l.OctopusRelationId ?? 0) > 0)
               && HasClientDataChanged(client, model);

            // Signingmodule §6.3: e-mail/gsm zijn de OTP-bestemming bij het ondertekenen — een wijziging
            // hier moet in ClientContactChangeLog komen (gemaskeerd) vóór MapToEntity/UpdateContacts de
            // oude waarden overschrijven. ClientAccount.Cellphone (migratie 058) mee sinds
            // SmsOtpMethod.ResolveDestinationAsync die kolom als eerste bestemming voor eigenaar 1 gebruikt.
            var oldEmail = client.Email;
            var oldCellphone = client.Cellphone;
            var oldContacts = client.ClientContacts.ToDictionary(c => c.Id, c => (c.Email, c.Cellphone));

            MapToEntity(model, client);
            UpdateIssuerCompany(model, client);
            UpdateContacts(model, client);
            LogContactChanges(client, oldEmail, oldCellphone, oldContacts);

            await _db.SaveChangesAsync(ct);

            if (requiresOctopusSync)
            {
                await TrySyncOctopusRelationAsync(client.Id, ct);
            }

            AddMessage("success", $"Klant {model.DisplayLabel} is bijgewerkt", "Geslaagd!");
            var referrer = TempData["Referrer"] as string;
            return !string.IsNullOrWhiteSpace(referrer) ? Redirect(referrer) : RedirectToAction(nameof(Index));
        }



        // KLANTEN - PROJECT
        [Breadcrumb("Klanten", FromController = typeof(ProjectenController), FromAction = nameof(ProjectenController.Detail))]
        public async Task<ActionResult> Detail(int clientId, int projectId = 0, CancellationToken ct = default)
        {
            var referrer = Request.Headers["Referer"].ToString();
            var model = new ClientModel();
            var clientService = _clientService;
            var unitService = _unitService;
            var projectService = _projectService;

            // gl-v2 (Klanten/DetailV2): zelfde permissie/vlag als Projecten.DetailClients — de
            // "Bewerken"-knop in de topbar hoort achter dezelfde ProjectsCustomers-schrijfrechten.
            var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectCustomers = permissionService.HasWrite(PermissionCodes.ProjectsCustomers);

            // 1. Get Client
            var clientResponse = clientService.GetClientAccountById(clientId);
            if (clientResponse.Success)
            {
                model.Client = clientResponse.Values.FirstOrDefault();
            }

            // 2. Get Units for Client
            model.UnitsGrouped = unitService.GetGroupedUnitsByAccountId(clientId)?.Values;

            // 3. Get Units with Payment Stages
            model.UnitsWithStages = unitService.GetClientUnitsWithStages(clientId)?.Values;

            // 4. Get Invoices for those Units
            var unitIds = model.UnitsWithStages?.Select(m => m.Unit.Id).ToList() ?? new List<int>();
            model.Invoices = projectService.GetInvoicesByUnitIds(unitIds)?.Values;

            // 5. Determine Project ID
            model.ProjectId = projectId != 0
                ? projectId
                : model.UnitsGrouped?.FirstOrDefault()?.Units?.FirstOrDefault()?.ProjectId ?? 0;

            // 6. Project Folder Path
            var deliveryDocPath = Configuration["URL:DeliveryDocLocalURL"];
            model.Folder = projectService.GetProjectFolderById(model.ProjectId) + deliveryDocPath;
            var imageUrl = Configuration["URL:ImageWebURL"];
            ViewBag.ImageWebURL = imageUrl;

            // 7. Get Gifts and PoAs
            model.Gifts = clientService.GetClientGiftByAccountId(clientId)?.Values;
            model.Poas = clientService.GetClientPoaByAccountId(clientId)?.Values;

            // 9. Execution Days
            model.ExecutionDays = model.Client.ExecutionDays.HasValue && model.Client.ExecutionDays.Value != 0
                ? model.Client.ExecutionDays.Value
                : projectService.GetProjectExecutionDays(model.ProjectId);

            // 10. Start Date
            model.StartDate = model.Client.StartDateConstruction != null
                ? model.Client.StartDateConstruction.Value
                : projectService.GetProjectStartDateConstruction(model.ProjectId);

            // 11. Final Construction Date & Working Days Left
            model.WorkingDaysLeft = -9999;
            if (model.ExecutionDays > 0 && model.StartDate != DateOnly.FromDateTime(DateTime.MinValue))
            {
                model.FinalConstructionDate = projectService.GetFinalConstructionDay(model.ProjectId, model.StartDate, model.ExecutionDays);
                if (model.FinalConstructionDate != DateOnly.FromDateTime(DateTime.MinValue))
                {
                    model.WorkingDaysLeft = projectService.GetWorkingDaysLeft(model.FinalConstructionDate, model.ProjectId);
                }
            }

            // 12. Latest Documents
            var latestDocsResponse = projectService.GetLatestClientDocs(4, clientId);
            if (latestDocsResponse.Success)
            {
                model.LatestDocs = latestDocsResponse.Values;
            }

            // 13. Change Orders
            var changeOrderResponse = projectService.GetClientChangeOrders(4, clientId);
            if (changeOrderResponse.Success)
            {
                model.ChangeOrders = changeOrderResponse.Values;
            }

            // gl-v2 (Klanten/DetailV2, design-handoff 12c): "Wijzigingsopdrachten"-kaart en de
            // WIJZIGINGSOPDRACHTEN-KPI hebben de VOLLEDIGE (ongecapte) lijst voor deze klant nodig — de
            // hierboven al opgehaalde model.ChangeOrders capt op 4 en is dat sinds jaar en dag voor de
            // legacy Detail.cshtml, dus die laten we ongemoeid en filteren hier apart, projectbreed,
            // op ClientAccountID. GetProjectChangeOrders is dezelfde methode als Projecten/DetailV2 al
            // gebruikt voor het hele project — dit is gewoon diezelfde lijst geherbruikt maar dan per klant.
            if (model.ProjectId > 0)
            {
                var projectChangeOrdersResponse = projectService.GetProjectChangeOrders(model.ProjectId);
                if (projectChangeOrdersResponse.Success)
                {
                    model.ClientChangeOrders = projectChangeOrdersResponse.Values
                        .Where(co => co.ClientAccountID == clientId)
                        .OrderByDescending(co => co.ChangeOrderDate)
                        .ToList();
                }

                // gl-v2: GEFACTUREERD/OPENSTAAND-KPI's op deze klant, binnen dit project — dezelfde
                // service/methode als Invoices/DetailV2 en Projecten/DetailV2 (RecentInvoices), hier
                // client-side gefilterd op ClientId omdat er geen per-klant variant van
                // GetByProjectAsync bestaat.
                var projectInvoices = await _invoiceQueryService.GetByProjectAsync(model.ProjectId, ct);
                model.ClientInvoices = projectInvoices.Where(i => i.ClientId == clientId).ToList();

                var projectResponse = projectService.GetProjectByID(model.ProjectId);
                model.IsCoordinationProject = projectResponse.Success && projectResponse.Value?.IsOnlyCoordinationProject == true;

                // gl-v2: zelfde teller als Projecten/DetailClientsV2's ItemCounts["Klanten"] — het
                // inner menu moet hier exact hetzelfde aantal tonen als op de klantenlijst zelf.
                var projectClientsResponse = clientService.GetClientAccountsByProjectId(model.ProjectId);
                model.ProjectClientCount = projectClientsResponse.Success ? projectClientsResponse.Values.Count : 0;

                var projectName = projectService.GetProjectNameById(model.ProjectId);
                var clientName = model.Client?.DisplayName ?? clientService.GetClientAccountNameById(clientId);

                var dashboard = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Home");
                var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
                {
                    Parent = dashboard
                };
                var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName)
                {
                    Parent = projectenIndex,
                    RouteValues = new { projectid = model.ProjectId }
                };
                var klanten = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailClients", "Projecten", "Klanten")
                {
                    Parent = projectDetail,
                    RouteValues = new { projectid = model.ProjectId }
                };
                // Kruimelpad stopt bij "Klanten" (wáár dit zit) i.p.v. nog een knoop met clientName toe
                // te voegen — DetailV2.cshtml zet de klantnaam al als paginatitel (regel 30 verderop),
                // dus een laatste kruimelitem met (vrijwel) dezelfde naam zou 'm herhalen. Design-
                // handoff punt 13, regel 2 — zelfde fix als Projecten/Detail en Projecten/DetailClients.
                ViewData["BreadcrumbNode"] = klanten;
            }



            SetPageHeader("bx bx-group", "Klant - " + model.Client?.DisplayName);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailV2" : "Detail", model);
        }


        private async Task<ClientFormViewModel> BuildFormAsync(ClientFormViewModel model, CancellationToken ct, CustomerIssuerScope? scope = null)
        {
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

            if (!model.SelectedCountryId.HasValue && countries.Any())
            {
                var defaultCountry = countries.FirstOrDefault(c => c.IsoCode.Equals("BE", StringComparison.OrdinalIgnoreCase)) ?? countries.First();
                model.SelectedCountryId = defaultCountry.Id;
                model.CountryIsoCode = defaultCountry.IsoCode;
            }

            if (!model.SelectedInvoiceCountryId.HasValue && countries.Any())
            {
                model.SelectedInvoiceCountryId = model.SelectedCountryId;
            }

            var issuers = await _db.IssuerCompany
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

            model.SelectedIssuerCompanyIds ??= new List<int>();
            if (scope != null && !scope.HasAllIssuers)
            {
                model.SelectedIssuerCompanyIds = model.SelectedIssuerCompanyIds
                    .Where(scope.AllowedIssuerIds.Contains)
                    .ToList();
            }
            model.IssuerCompanies = issuers;

            if (!model.SelectedIssuerCompanyIds.Any() && issuers.Any())
            {
                model.SelectedIssuerCompanyIds = issuers
                    .Take(1)
                    .Select(i => i.Id)
                    .ToList();
            }


            if (model.Contacts == null || model.Contacts.Count == 0)
            {
                model.Contacts = new List<ContactInputViewModel> { new ContactInputViewModel() };
            }

            return model;
        }
        private void ValidateClientType(ClientFormViewModel model)
        {
            if (model.IsCompany)
            {
                if (string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    ModelState.AddModelError(nameof(model.CompanyName), "Bedrijfsnaam is verplicht voor een onderneming.");
                }

                return;
            }

            if (!model.Salutation.HasValue)
            {
                ModelState.AddModelError(nameof(model.Salutation), "Kies een aanspreking.");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Naam is verplicht voor een particulier.");
            }
        }

        private void ValidateIssuerCompanies(ClientFormViewModel model)
        {
            if (model.SelectedIssuerCompanyIds == null || !model.SelectedIssuerCompanyIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedIssuerCompanyIds), "Kies minstens één facturatiebedrijf.");
            }
        }

        private async Task<CustomerIssuerScope> ResolveCustomerIssuerScopeAsync(PermissionAccessType accessType, CancellationToken ct)
        {
            var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            await permissionService.EnsureLoadedAsync(ct);

            var hasMainAccess = accessType switch
            {
                PermissionAccessType.Read => permissionService.HasRead(PermissionCodes.Customers),
                PermissionAccessType.Write => permissionService.HasWrite(PermissionCodes.Customers),
                PermissionAccessType.Delete => permissionService.HasDelete(PermissionCodes.Customers),
                _ => false
            };

            if (!hasMainAccess)
            {
                return new CustomerIssuerScope(false, false, new HashSet<int>());
            }

            var scopedIds = permissionService.EffectivePermissions
                .Where(x => x.Key.StartsWith(CustomerCompanyPermissionPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(x => accessType switch
                {
                    PermissionAccessType.Read => x.Value.Read,
                    PermissionAccessType.Write => x.Value.Write,
                    PermissionAccessType.Delete => x.Value.Delete,
                    _ => false
                })
                .Select(x =>
                {
                    var suffix = x.Key[CustomerCompanyPermissionPrefix.Length..];
                    return int.TryParse(suffix, out var companyId) ? (int?)companyId : null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
            if (scopedIds.Count == 0)
            {
                return new CustomerIssuerScope(true, true, new HashSet<int>());
            }

            return new CustomerIssuerScope(true, false, scopedIds.ToHashSet());
        }

        private sealed record CustomerIssuerScope(bool HasAccess, bool HasAllIssuers, HashSet<int> AllowedIssuerIds);


        private static void MapToEntity(ClientFormViewModel model, ClientAccount entity)
        {
            entity.Name = model.IsCompany ? model.CompanyName : model.Name;
            entity.Forename = model.IsCompany ? null : model.Forename;
            entity.CompanyName = model.IsCompany ? model.CompanyName : null;
            entity.Salutation = model.IsCompany ? null : model.Salutation?.ToString();
            entity.Vatnumber = model.IsCompany
                 ? NormalizeEnterpriseNumber(model.EnterpriseNumber)
               : null;
            entity.Street = model.Street;
            entity.Housenumber = model.HouseNumber;
            entity.Busnumber = model.BusNumber;
            entity.PostalCodeId = model.SelectedPostalCodeId;
            entity.InvoiceAddress = model.UseInvoiceAddress;
            entity.InvoiceStreet = model.UseInvoiceAddress ? model.InvoiceStreet : null;
            entity.InvoiceHousenumber = model.UseInvoiceAddress ? model.InvoiceHouseNumber : null;
            entity.InvoiceBusnumber = model.UseInvoiceAddress ? model.InvoiceBusNumber : null;
            entity.InvoicePostalCodeId = model.UseInvoiceAddress ? model.SelectedInvoicePostalCodeId : null;
            entity.Email = model.Email;
            entity.Phone = model.Phone;
            entity.Cellphone = model.Cellphone;
            entity.InvoiceEmail = model.InvoiceEmail;
            entity.RequiresDigitalInvoice = model.RequiresDigitalInvoice;
            entity.AttachUblByDefault = model.AttachUblByDefault;
            entity.OwnerPercentage ??= 100;
            entity.OwnerTypeId ??= 1;
        }

        private void AttachIssuerCompany(ClientFormViewModel model, ClientAccount entity)
        {
            if (model.SelectedIssuerCompanyIds == null || model.SelectedIssuerCompanyIds.Count == 0)
            {
                return;
            }

            var issuerIds = model.SelectedIssuerCompanyIds.Distinct().ToList();
            var issuers = _db.IssuerCompany
                .Where(i => issuerIds.Contains(i.Id))
                .ToList();

            foreach (var issuer in issuers)
            {
                if (entity.ClientAccountIssuerCompany.Any(link => link.IssuerCompanyId == issuer.Id))
                {
                    continue;
                }

                entity.ClientAccountIssuerCompany.Add(new ClientAccountIssuerCompany
                {
                    ClientAccount = entity,
                    ClientAccountId = entity.Id,
                    IssuerCompany = issuer,
                    IssuerCompanyId = issuer.Id
                });
            }
        }

        private void UpdateIssuerCompany(ClientFormViewModel model, ClientAccount entity)
        {
            entity.ClientAccountIssuerCompany.Clear();
            AttachIssuerCompany(model, entity);
        }

        // Zie de aanroep in AddClientAccount (POST) hierboven voor de volledige uitleg: koppelt een
        // net aangemaakte klant automatisch aan het/de facturatiebedrijf/bedrijven van het project
        // (grondeigenaar en/of aannemer), als die ingesteld zijn. Werkt rechtstreeks op _db (zelfde
        // stijl als AttachIssuerCompany hierboven) omdat ClientAccountBO/ClientService geen begrip
        // van facturatiebedrijven kent — dat leeft enkel op ClientFormViewModel-niveau.
        private void LinkProjectIssuerCompanies(int clientAccountId, int projectId)
        {
            var projectResponse = _projectService.GetProjectByID(projectId);
            if (!projectResponse.Success || projectResponse.Value == null) return;

            var issuerIds = new[] { projectResponse.Value.IssuerCompanyIdLandOwner, projectResponse.Value.IssuerCompanyIdBuilder }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            if (!issuerIds.Any()) return;

            var alreadyLinked = _db.ClientAccountIssuerCompany
                .Where(l => l.ClientAccountId == clientAccountId)
                .Select(l => l.IssuerCompanyId)
                .ToList();

            foreach (var issuerId in issuerIds.Except(alreadyLinked))
            {
                _db.ClientAccountIssuerCompany.Add(new ClientAccountIssuerCompany
                {
                    ClientAccountId = clientAccountId,
                    IssuerCompanyId = issuerId
                });
            }
            _db.SaveChanges();
        }

        // Defensief, niet enkel cosmetisch: de JS laat visueel maar één rij tegelijk "Primair
        // contact" aanvinken (uncheckt de andere bij een klik), maar dat is bypasbaar (JS uit,
        // bewerkte request, …) — dus hier hetzelfde normaliseren vóór het opslaan: enkel de EERSTE
        // aangevinkte rij (in postvolgorde) blijft primair, de rest wordt hier expliciet false.
        private static void NormalizePrimaryContact(List<ContactInputViewModel> contacts)
        {
            var keepPrimary = contacts.FirstOrDefault(c => c.IsPrimaryContact);
            foreach (var contact in contacts)
                contact.IsPrimaryContact = contact == keepPrimary;
        }

        private static void AttachContacts(ClientFormViewModel model, ClientAccount entity)
        {
            NormalizePrimaryContact(model.Contacts);
            foreach (var contact in model.Contacts.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
            {
                entity.ClientContacts.Add(new ClientContacts
                {
                    Name = contact.Name,
                    Forename = contact.Forename,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Cellphone = contact.Mobile,
                    RequiresDigitalInvoice = contact.RequiresDigitalInvoice,
                    AttachUblByDefault = contact.AttachUblByDefault,
                    IsPrimaryContact = contact.IsPrimaryContact
                });
            }
        }

        private static void UpdateContacts(ClientFormViewModel model, ClientAccount entity)
        {
            NormalizePrimaryContact(model.Contacts);
            var incomingIds = model.Contacts.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToList();
            // Mede-eigenaars (IsCoOwner) komen nooit in model.Contacts terecht (zie de Edit-actie
            // hierboven, die ze bewust niet laadt) — hun Id staat dus nooit in incomingIds. Zonder deze
            // filter zou de "verwijder wat niet terugkomt"-opruiming hieronder ELKE mede-eigenaar van
            // deze klant stilzwijgend verwijderen bij elke keer opslaan vanuit dit globale scherm. Enkel
            // de niet-mede-eigenaar-rijen zijn hier beheerbaar; mede-eigenaars blijven ongemoeid.
            var manageableContacts = entity.ClientContacts.Where(c => !c.IsCoOwner).ToList();
            var toRemove = manageableContacts.Where(c => !incomingIds.Contains(c.Id)).ToList();

            foreach (var removal in toRemove)
            {
                entity.ClientContacts.Remove(removal);
            }

            foreach (var contactModel in model.Contacts)
            {
                if (contactModel.Id is int contactId)
                {
                    var existing = manageableContacts.FirstOrDefault(c => c.Id == contactId);
                    if (existing != null)
                    {
                        existing.Name = contactModel.Name;
                        existing.Forename = contactModel.Forename;
                        existing.Email = contactModel.Email;
                        existing.Phone = contactModel.Phone;
                        existing.Cellphone = contactModel.Mobile;
                        existing.RequiresDigitalInvoice = contactModel.RequiresDigitalInvoice;
                        existing.AttachUblByDefault = contactModel.AttachUblByDefault;
                        existing.IsPrimaryContact = contactModel.IsPrimaryContact;
                        continue;
                    }
                }

                entity.ClientContacts.Add(new ClientContacts
                {
                    Name = contactModel.Name,
                    Forename = contactModel.Forename,
                    Email = contactModel.Email,
                    Phone = contactModel.Phone,
                    Cellphone = contactModel.Mobile,
                    RequiresDigitalInvoice = contactModel.RequiresDigitalInvoice,
                    AttachUblByDefault = contactModel.AttachUblByDefault,
                    IsPrimaryContact = contactModel.IsPrimaryContact
                });
            }
        }

        /// <summary>ClientContactChangeLog vullen (fase 3, signingmodule §6.3 — e-mail/gsm zijn de
        /// OTP-bestemming bij het ondertekenen) — enkel gemaskeerde oude/nieuwe waarden, nooit de echte
        /// e-mail/gsm zelf. Nieuwe contacten (geen oude waarde om mee te vergelijken) en verwijderde
        /// contacten worden niet gelogd — buiten scope, zie ONDERTEKENEN_VOORTGANG.md.</summary>
        private void LogContactChanges(ClientAccount client, string oldEmail, string oldCellphone, Dictionary<int, (string Email, string Cellphone)> oldContacts)
        {
            var userId = User.GetCpmUserId();
            var now = DateTime.UtcNow;

            if (!string.Equals(oldEmail, client.Email, StringComparison.Ordinal))
            {
                _db.ClientContactChangeLog.Add(new ClientContactChangeLog
                {
                    EntityType = "ClientAccount", EntityId = client.Id, ClientAccountId = client.Id, Field = "Email",
                    OldValueMasked = SigningCrypto.MaskEmail(oldEmail), NewValueMasked = SigningCrypto.MaskEmail(client.Email),
                    ChangedByUserId = userId, ChangedAt = now,
                });
            }
            if (!string.Equals(oldCellphone, client.Cellphone, StringComparison.Ordinal))
            {
                _db.ClientContactChangeLog.Add(new ClientContactChangeLog
                {
                    EntityType = "ClientAccount", EntityId = client.Id, ClientAccountId = client.Id, Field = "Cellphone",
                    OldValueMasked = SigningCrypto.MaskPhone(oldCellphone), NewValueMasked = SigningCrypto.MaskPhone(client.Cellphone),
                    ChangedByUserId = userId, ChangedAt = now,
                });
            }

            foreach (var contact in client.ClientContacts)
            {
                if (!oldContacts.TryGetValue(contact.Id, out var old)) continue;

                if (!string.Equals(old.Email, contact.Email, StringComparison.Ordinal))
                {
                    _db.ClientContactChangeLog.Add(new ClientContactChangeLog
                    {
                        EntityType = "ClientContact", EntityId = contact.Id, ClientAccountId = client.Id, Field = "Email",
                        OldValueMasked = SigningCrypto.MaskEmail(old.Email), NewValueMasked = SigningCrypto.MaskEmail(contact.Email),
                        ChangedByUserId = userId, ChangedAt = now,
                    });
                }
                if (!string.Equals(old.Cellphone, contact.Cellphone, StringComparison.Ordinal))
                {
                    _db.ClientContactChangeLog.Add(new ClientContactChangeLog
                    {
                        EntityType = "ClientContact", EntityId = contact.Id, ClientAccountId = client.Id, Field = "Cellphone",
                        OldValueMasked = SigningCrypto.MaskPhone(old.Cellphone), NewValueMasked = SigningCrypto.MaskPhone(contact.Cellphone),
                        ChangedByUserId = userId, ChangedAt = now,
                    });
                }
            }
        }

        private void RemoveEmptyContacts(ClientFormViewModel model)
        {
            if (model.Contacts == null)
            {
                model.Contacts = new List<ContactInputViewModel>();
                return;
            }

            var cleaned = new List<ContactInputViewModel>();
            for (var i = 0; i < model.Contacts.Count; i++)
            {
                var contact = model.Contacts[i];
                if (contact == null)
                {
                    continue;
                }

                var isEmpty = string.IsNullOrWhiteSpace(contact.Name)
                    && string.IsNullOrWhiteSpace(contact.Forename)
                    && string.IsNullOrWhiteSpace(contact.Email)
                    && string.IsNullOrWhiteSpace(contact.Phone)
                    && string.IsNullOrWhiteSpace(contact.Mobile);

                if (isEmpty)
                {
                    var prefix = $"Contacts[{i}].";
                    var keysToRemove = ModelState.Keys
                        .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var key in keysToRemove)
                    {
                        ModelState.Remove(key);
                    }
                    continue;
                }

                cleaned.Add(contact);
            }

            model.Contacts = cleaned;
        }
        private static bool HasClientDataChanged(ClientAccount entity, ClientFormViewModel model)
        {
            static bool Different(string? a, string? b)
                => !string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

            var newVat = model.IsCompany
                ? NormalizeEnterpriseNumber(model.EnterpriseNumber)
                : null;

            return Different(entity.Street, model.Street)
                || Different(entity.Housenumber, model.HouseNumber)
                || Different(entity.Busnumber, model.BusNumber)
                || entity.PostalCodeId != model.SelectedPostalCodeId
                || Different(entity.InvoiceStreet, model.UseInvoiceAddress ? model.InvoiceStreet : null)
                || Different(entity.InvoiceHousenumber, model.UseInvoiceAddress ? model.InvoiceHouseNumber : null)
                || Different(entity.InvoiceBusnumber, model.UseInvoiceAddress ? model.InvoiceBusNumber : null)
                || entity.InvoicePostalCodeId != (model.UseInvoiceAddress ? model.SelectedInvoicePostalCodeId : null)
                || Different(entity.Vatnumber, newVat)
                || Different(entity.Email, model.Email)
                || Different(entity.InvoiceEmail, model.InvoiceEmail);
        }

        private async Task TrySyncOctopusRelationAsync(int clientId, CancellationToken ct)
        {
            var client = await _db.ClientAccount
                .Include(c => c.PostalCode)!
                    .ThenInclude(pc => pc.Country)
                .Include(c => c.InvoicePostalCode)!
                    .ThenInclude(pc => pc.Country)
                 .Include(c => c.ClientAccountIssuerCompany)
                    .ThenInclude(link => link.IssuerCompany)
                .FirstOrDefaultAsync(c => c.Id == clientId, ct);

            if (client == null)
                return;

            foreach (var link in client.ClientAccountIssuerCompany
                .Where(l => (l.OctopusRelationId ?? 0) > 0
                            && l.IssuerCompany != null
                            && !string.IsNullOrWhiteSpace(l.IssuerCompany.OctopusDossierNumber)))
            {
                var request = BuildOctopusRelationRequest(client, link.OctopusRelationId!.Value);
                var dossierToken = await _octopusTokens.RefreshDossierTokenAsync(link.IssuerCompanyId, link.IssuerCompany.OctopusDossierNumber, ct);
                await _octopusClient.UpsertRelationAsync(dossierToken.Token, link.IssuerCompany.OctopusDossierNumber, request, ct);
            }
        }

        private static OctopusRelationRequest BuildOctopusRelationRequest(ClientAccount client, int octopusRelationId)
        {
            var countryCode = client.InvoicePostalCode?.Country?.LandIsocode
                ?? client.PostalCode?.Country?.LandIsocode;

            var isCompany = !string.IsNullOrWhiteSpace(client.CompanyName) || !string.IsNullOrWhiteSpace(client.Vatnumber);

            var name = string.IsNullOrWhiteSpace(client.CompanyName) ? client.Name : client.CompanyName;

            var request = new OctopusRelationRequest
            {
                RelationIdentificationServiceData = new OctopusRelationIdentificationData
                {
                    RelationKey = new OctopusRelationKey { Id = octopusRelationId },
                    ExternalRelationId = client.Id
                },
                Name = name,
                // Echte voornaam (migratie 057) wint; zonder Forename blijft dit het bestaande
                // surrogaat (heel Name als "voornaam") voor niet-gemigreerde accounts.
                Firstname = client.Forename ?? client.Name,
                Client = true,
                Supplier = false,
                Active = true,
                StreetAndNr = BuildStreetAndNumber(
                    client.InvoiceStreet ?? client.Street,
                    client.InvoiceHousenumber ?? client.Housenumber,
                    client.InvoiceBusnumber ?? client.Busnumber,
                    client.Street),
                PostalCode = client.InvoicePostalCode?.Postcode ?? client.PostalCode?.Postcode,
                City = client.InvoicePostalCode?.Gemeente ?? client.PostalCode?.Gemeente,
                Country = countryCode ?? "BE",
                VatNr = FormatVatNumberForOctopus(client.Vatnumber, countryCode),
                CurrencyCode = "EUR",
                Contactperson = name,
                DefaultBookingAccountClient = 0,
                DefaultBookingAccountSupplier = 0,
                SupplierPaymentMethod = 0,
                VatType = DetermineVatType(isCompany, countryCode)
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

        //WIJZIGNGSOPDRACHTEN
        public ActionResult DetailCO(int projectid, int clientid)
        {
            return RedirectToAction("DetailsChangeOrder", "Projecten", new { projectid, clientid });
        }

    }
}
