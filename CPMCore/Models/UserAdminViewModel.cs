using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models
{
    public class UserListItemViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Cellphone { get; set; }
        public string? EntraObjectId { get; set; }
        public bool IsActive { get; set; }
        public int? CurrentRoleId { get; set; }
        public string? CurrentRoleName { get; set; }
        public List<string> Permissions { get; set; } = new();

        // Gastuitnodiging (null = geen uitnodiging aangemaakt)
        public int? GuestInvitationId { get; set; }
        public string? GuestInvitationStatus { get; set; }
        public string? GuestUserType { get; set; }
        public DateTime? GuestInvitationSentAt { get; set; }
        public DateTime? GuestLastLoginAt { get; set; }
        public string? GuestExternalObjectId { get; set; }
        public string? GuestExternalTenantId { get; set; }
        public string? GuestInviteRedeemUrl { get; set; }

        public bool HasGuestInvitation => GuestInvitationId.HasValue;

        public DashboardType? DashboardType { get; set; }
    }

    public class EntraUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserPrincipalName { get; set; }
        /// <summary>"Member" voor interne gebruikers, "Guest" voor B2B-gasten.</summary>
        public string? UserType { get; set; }
        public bool IsGuest => string.Equals(UserType, "Guest", StringComparison.OrdinalIgnoreCase)
            || (UserPrincipalName?.Contains("#EXT#", StringComparison.OrdinalIgnoreCase) ?? false);
    }


    public class PermissionMatrixItemViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public int SortOrder { get; set; }
    }

    public class RolePermissionAssignmentViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class ContractorUserItem
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int Role { get; set; } // ContractorAccessRole
        public string? InvitationStatus { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UserAdminIndexViewModel
    {
        public List<UserListItemViewModel> LocalUsers { get; set; } = new();
        public List<ContractorUserItem> ContractorUsers { get; set; } = new();
        /// <summary>Alle Entra-gebruikers (voor legacy-gebruik in dropdowns).</summary>
        public List<EntraUserListItemViewModel> EntraUsers { get; set; } = new();
        /// <summary>Interne Entra-leden (Member, geen #EXT#).</summary>
        public List<EntraUserListItemViewModel> EntraInternalUsers { get; set; } = new();
        /// <summary>Externe Entra-gasten (Guest / #EXT#).</summary>
        public List<EntraUserListItemViewModel> EntraExternalUsers { get; set; } = new();
        public List<RolePermissionAssignmentViewModel> Roles { get; set; } = new();
        public List<PermissionMatrixItemViewModel> PermissionDefinitions { get; set; } = new();
    }


    public class CreateUserViewModel
    {
        [Required]
        [Display(Name = "Gebruikersnaam")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Naam")]
        public string? Name { get; set; }

        [Display(Name = "Voornaam")]
        public string? Forename { get; set; }

        [Display(Name = "Functie")]
        public string? JobFunction { get; set; }

        [Display(Name = "GSM")]
        public string? Cellphone { get; set; }

        [Display(Name = "Actief")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Entra gebruiker")]
        public string? SelectedEntraObjectId { get; set; }

        public List<EntraUserListItemViewModel> EntraUsers { get; set; } = new();
        public List<RolePermissionAssignmentViewModel> Roles { get; set; } = new();
        public List<PermissionMatrixItemViewModel> PermissionDefinitions { get; set; } = new();

        public List<string> AvailablePermissions { get; set; } = new();

        [Display(Name = "Dashboard type")]
        public DashboardType? DashboardType { get; set; }

        [Display(Name = "Rollen")]
        public List<string> SelectedPermissions { get; set; } = new();
    }


    public class EditUserViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Gebruikersnaam")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Naam")]
        public string? Name { get; set; }

        [Display(Name = "Voornaam")]
        public string? Forename { get; set; }

        [Display(Name = "Functie")]
        public string? JobFunction { get; set; }

        [Display(Name = "GSM")]
        public string? Cellphone { get; set; }

        [Display(Name = "Entra Object ID")]
        public string? EntraObjectId { get; set; }

        [Display(Name = "Actief")]
        public bool IsActive { get; set; }

        [Display(Name = "Nieuwe Entra Object ID")]
        public string? LinkEntraObjectId { get; set; }
        public List<EntraUserListItemViewModel> EntraUsers { get; set; } = new();
        public List<RolePermissionAssignmentViewModel> Roles { get; set; } = new();
        public List<PermissionMatrixItemViewModel> PermissionDefinitions { get; set; } = new();


        public List<string> AvailablePermissions { get; set; } = new();

        [Display(Name = "Dashboard type")]
        public DashboardType? DashboardType { get; set; }

        [Display(Name = "Rollen")]
        public List<string> SelectedPermissions { get; set; } = new();
    }

    public class UserDeleteViewModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}