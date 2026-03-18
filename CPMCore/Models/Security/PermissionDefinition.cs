namespace CPMCore.Models.Security;

public record PermissionDefinition(string Code, string Name, string? ParentCode, int SortOrder);