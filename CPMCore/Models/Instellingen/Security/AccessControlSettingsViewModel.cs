using DALCore.Models;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen.Security;

public class AccessControlSettingsViewModel
{
    public List<ProjectUserAccessRow> ProjectAccessRows { get; set; } = new();
    public List<UserCompanyAccessRow> CompanyAccessRows { get; set; } = new();
    public List<UserOption> Users { get; set; } = new();
    public List<ProjectOption> Projects { get; set; } = new();
    public List<CompanyOption> Companies { get; set; } = new();
    public List<AccessScopeRole> Roles { get; set; } = Enum.GetValues<AccessScopeRole>().ToList();

    public CreateProjectAccessInput NewProjectAccess { get; set; } = new();
    public CreateUserCompanyAccessInput NewCompanyAccess { get; set; } = new();
}

public class CreateProjectAccessInput
{
    [Required] public int UserId { get; set; }
    [Required] public int ProjectId { get; set; }
    [Required] public AccessScopeRole Role { get; set; }
}

public class CreateUserCompanyAccessInput
{
    [Required] public int UserId { get; set; }
    [Required] public int CompanyId { get; set; }
    [Required] public AccessScopeRole Role { get; set; }
}

public record ProjectUserAccessRow(int Id, int UserId, string UserName, int ProjectId, string ProjectName, AccessScopeRole Role);
public record UserCompanyAccessRow(int Id, int UserId, string UserName, int CompanyId, string CompanyName, AccessScopeRole Role);
public record UserOption(int Id, string Name);
public record ProjectOption(int Id, string Name);
public record CompanyOption(int Id, string Name);