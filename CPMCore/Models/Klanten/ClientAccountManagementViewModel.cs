using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BOCore;

namespace CPMCore.Models.Klanten;

public class ClientIndexViewModel
{
    public IReadOnlyList<ClientListItemViewModel> Clients { get; init; } = new List<ClientListItemViewModel>();
}

public class ClientListItemViewModel
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? EnterpriseNumber { get; init; }
    public string? City { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public IReadOnlyList<string> IssuerCompanies { get; init; } = new List<string>();
    public int ContactCount { get; init; }
}

public class ClientFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Bedrijf of particulier")]
    public bool IsCompany { get; set; } = true;

    [Display(Name = "Bedrijfsnaam")]
    public string? CompanyName { get; set; }

    [Display(Name = "Naam")]
    public string? Name { get; set; }

    [Display(Name = "Aanspreking")]
    public Salutation? Salutation { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Ondernemingsnummer")]
    public string? EnterpriseNumber { get; set; }

    [Display(Name = "BTW landcode")]
    public string EnterpriseNumberCountryCode { get; set; } = "BE";

    [Display(Name = "Straat")]
    public string? Street { get; set; }

    [Display(Name = "Huisnummer")]
    public string? HouseNumber { get; set; }

    [Display(Name = "Busnummer")]
    public string? BusNumber { get; set; }

    [Display(Name = "Postcode keuze")]
    public int? SelectedPostalCodeId { get; set; }

    [Display(Name = "Postcode")]
    public string? PostalCode { get; set; }

    [Display(Name = "Gemeente")]
    public string? City { get; set; }

    [Display(Name = "Land")]
    public int? SelectedCountryId { get; set; }

    public string? CountryIsoCode { get; set; }

    [Display(Name = "Facturatie-adres gebruiken")]
    public bool UseInvoiceAddress { get; set; }

    [Display(Name = "Straat facturatie")]
    public string? InvoiceStreet { get; set; }

    [Display(Name = "Huisnummer facturatie")]
    public string? InvoiceHouseNumber { get; set; }

    [Display(Name = "Busnummer facturatie")]
    public string? InvoiceBusNumber { get; set; }

    [Display(Name = "Postcode facturatie")]
    public int? SelectedInvoicePostalCodeId { get; set; }

    [Display(Name = "Postcode facturatie")]
    public string? InvoicePostalCode { get; set; }

    [Display(Name = "Gemeente facturatie")]
    public string? InvoiceCity { get; set; }

    [Display(Name = "Land facturatie")]
    public int? SelectedInvoiceCountryId { get; set; }

    [Display(Name = "Facturatiebedrijven")]
    [Required(ErrorMessage = "Kies minstens één facturatiebedrijf")]
    public List<int> SelectedIssuerCompanyIds { get; set; } = new();

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessage = "Ongeldige e-mail")]
    public string? Email { get; set; }

    [Display(Name = "Facturatie e-mail")]
    [EmailAddress(ErrorMessage = "Ongeldige e-mail")]
    public string? InvoiceEmail { get; set; }

    [Display(Name = "Digitale factuur vereist")]
    public bool RequiresDigitalInvoice { get; set; }

    [Display(Name = "UBL standaard meesturen")]
    public bool AttachUblByDefault { get; set; }

    public List<CountryOptionViewModel> Countries { get; set; } = new();
    public List<IssuerCompanyOptionViewModel> IssuerCompanies { get; set; } = new();
    public List<ContactInputViewModel> Contacts { get; set; } = new() { new ContactInputViewModel() };

    public string Title => Id.HasValue ? "Klant bewerken" : "Klant toevoegen";
    public string Subtitle => Id.HasValue ? "Werk de klantgegevens bij" : "Maak een nieuwe klant aan";

    public string DisplayLabel => IsCompany
        ? CompanyName ?? string.Empty
        : string.Join(" ", new[] { Salutation?.GetDisplayName(), Name }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public class ContactInputViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Naam is verplicht")]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Voornaam")]
    public string? Forename { get; set; }

    [Display(Name = "E-mail")]
    [EmailAddress(ErrorMessage = "Ongeldige e-mail")]
    public string? Email { get; set; }

    [Display(Name = "Telefoon")]
    public string? Phone { get; set; }

    [Display(Name = "GSM")]
    public string? Mobile { get; set; }

    [Display(Name = "Digitale factuur vereist")]
    public bool RequiresDigitalInvoice { get; set; }

    [Display(Name = "UBL standaard meesturen")]
    public bool AttachUblByDefault { get; set; }
}

public class CountryOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string IsoCode { get; init; } = string.Empty;
}

public class IssuerCompanyOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}