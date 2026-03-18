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
        public string? EntraObjectId { get; set; }
        public bool IsActive { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class EntraUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserPrincipalName { get; set; }
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

    public class UserAdminIndexViewModel
    {
        public List<UserListItemViewModel> LocalUsers { get; set; } = new();
        public List<EntraUserListItemViewModel> EntraUsers { get; set; } = new();
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