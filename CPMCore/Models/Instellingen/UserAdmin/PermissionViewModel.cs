using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models;

public class PermissionListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PermissionEditViewModel
{
    [Required]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;
}