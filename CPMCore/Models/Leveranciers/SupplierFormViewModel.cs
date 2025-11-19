using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace CPMCore.Models.Leveranciers;

public class SupplierFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Bedrijfsnaam is verplicht")]
    [Display(Name = "Bedrijfsnaam")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Bedrijfsvorm")]
    public int? SelectedLegalFormId { get; set; }

    [Display(Name = "Ondernemingsnummer")]
    public string? EnterpriseNumber { get; set; }

    [Display(Name = "Straat")]
    public string? Street { get; set; }

    [Display(Name = "Nr")]
    public string? HouseNumber { get; set; }

    [Display(Name = "Bus")]
    public string? BusNumber { get; set; }

    [Display(Name = "Postcode")]
    public string? PostalCode { get; set; }

    [Display(Name = "Gemeente")]
    public string? City { get; set; }

    [Display(Name = "Postcode Id")]
    public int? SelectedPostalCodeId { get; set; }

    [Display(Name = "Landcode")]
    public string? CountryCode { get; set; }

    [Display(Name = "Land")]
    public int? SelectedCountryId { get; set; }

    [Display(Name = "Telefoon")]
    public string? Phone { get; set; }

    [Display(Name = "GSM")]
    public string? Mobile { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [EmailAddress]
    [Display(Name = "Facturatie email")]
    public string? InvoiceEmail { get; set; }

    [Display(Name = "Website")]
    public string? WebUrl { get; set; }

    [Display(Name = "Digitale factuur nodig")]
    public bool RequiresDigitalInvoice { get; set; }

    [Display(Name = "UBL standaard toevoegen")]
    public bool AttachUblByDefault { get; set; }

    [Display(Name = "Activiteiten")]
    public List<int> SelectedActivityIds { get; set; } = new();

    public IReadOnlyList<ActivityFilterItemViewModel> Activities { get; set; } = new List<ActivityFilterItemViewModel>();

    public IReadOnlyList<CountryOptionViewModel> Countries { get; set; } = new List<CountryOptionViewModel>();

    public IReadOnlyList<LegalFormOptionViewModel> LegalForms { get; set; } = new List<LegalFormOptionViewModel>();

    public List<DepartmentInputViewModel> Departments { get; set; } = new();

    public List<ContactInputViewModel> Contacts { get; set; } = new();

    public string Title => Id.HasValue ? "Leverancier bewerken" : "Leverancier toevoegen";

    public string Subtitle => Id.HasValue ? "Pas de leverancier aan" : "Voeg een nieuwe leverancier toe";
}

public class CountryOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string IsoCode { get; init; } = string.Empty;
}

public class LegalFormOptionViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Abbreviation { get; init; } = string.Empty;
}

public class DepartmentInputViewModel
{
    public int? Id { get; set; }
    public string Key { get; set; } = Guid.NewGuid().ToString("N");

    [Display(Name = "Naam")]
    public string? Name { get; set; }

    [Display(Name = "Straat")]
    public string? Street { get; set; }

    [Display(Name = "Nr")]
    public string? HouseNumber { get; set; }

    [Display(Name = "Bus")]
    public string? Bus { get; set; }

    [Display(Name = "Postcode Id")]
    public int? PostalCodeId { get; set; }

    public string? PostalDisplay { get; set; }

    [Display(Name = "Telefoon")]
    public string? Phone { get; set; }

    [Display(Name = "GSM")]
    public string? Mobile { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }
}

public class ContactInputViewModel
{
    public int? Id { get; set; }
    public string Key { get; set; } = Guid.NewGuid().ToString("N");

    [Display(Name = "Naam")]
    public string? LastName { get; set; }

    [Display(Name = "Voornaam")]
    public string? FirstName { get; set; }

    [Display(Name = "Functie")]
    public string? Function { get; set; }

    [Display(Name = "Telefoon")]
    public string? Phone { get; set; }

    [Display(Name = "GSM")]
    public string? Mobile { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Afdeling")]
    public string? DepartmentKey { get; set; }
}