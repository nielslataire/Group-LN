using System.ComponentModel.DataAnnotations;
using CPMCore.Services;

namespace CPMCore.Models.ContractorInvite;

public class ContractorInviteVm
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "E-mailadres")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Voornaam")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Naam")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Functie")]
    public string? JobFunction { get; set; }

    [Display(Name = "Telefoon")]
    public string? Phone { get; set; }

    [Display(Name = "Volledig verantwoordelijk voor alle puntmeldingen")]
    public bool IsFullCompanyAdmin { get; set; } = true;
}

public class ContractorAccessRow
{
    public int     UserId             { get; set; }
    public string  DisplayName        { get; set; } = string.Empty;
    public string  Email              { get; set; } = string.Empty;
    public bool    IsFullCompanyAdmin { get; set; }
    public bool    IsActive           { get; set; }
    public string? InvitationStatus   { get; set; }
    public DateTime? LastLoginAt      { get; set; }
}

public class ContractorOverzichtVm
{
    public int    CompanyId   { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public List<ContractorAccessRow> Rows { get; set; } = new();
}
