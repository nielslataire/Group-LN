namespace CPMCore.Models.Contacten;

/// <summary>Een rij in de contactenlijst (design-handoff 44a): alles wat de client-side filters en sortering nodig hebben staat erin.</summary>
public class CrmContactRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Source { get; set; } = "Website";
    public string? BrokerName { get; set; }
    public string? SeenVia { get; set; }
    public int Status { get; set; }
    public string? StatusReason { get; set; }
    public List<string> UnitNames { get; set; } = new();
    public List<int> UnitIds { get; set; } = new();
    public string? Budget { get; set; }
    public string? LastTitle { get; set; }
    public DateTime? LastAt { get; set; }
    public string? LastBy { get; set; }
    public bool LastAuto { get; set; }
    public string? NextText { get; set; }
    public DateOnly? NextDate { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedCompanyId { get; set; }
    public string AssignedKey => AssignedUserId.HasValue ? "u" + AssignedUserId : AssignedCompanyId.HasValue ? "c" + AssignedCompanyId : "";
    public string? AssignedName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool ConsentContact { get; set; }
    public bool ConsentNewsletter { get; set; }
    public DateTime? VisitAt { get; set; }
    public bool Overdue { get; set; }
}

public class CrmContactsIndexVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public List<CrmContactRow> Rows { get; set; } = new();
    public List<KeyValuePair<int, string>> Units { get; set; } = new();
    public List<KeyValuePair<int, string>> Users { get; set; } = new();
    public List<AssigneeOption> Assignees { get; set; } = new();
    public bool CanExport { get; set; }
    public DateOnly Today { get; set; }
    public int CurrentUserId { get; set; }
    public List<CrmListVm> Lists { get; set; } = new();
}

public record CrmListVm(int Id, string Name, string? FiltersJson, string? CreatedBy);

public class CrmEventVm
{
    public int Id { get; set; }
    public string Kind { get; set; } = "note";
    public DateTime OccurredAt { get; set; }
    public string? UserName { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public int? DurationMin { get; set; }
    public bool IsAuto { get; set; }
}

public class CrmContactDetailVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Initials { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PartnerName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Source { get; set; } = "Website";
    public string? BrokerName { get; set; }
    public string? SeenVia { get; set; }
    public int Status { get; set; }
    public string? StatusReason { get; set; }
    public int? AssignedUserId { get; set; }
    public int? AssignedCompanyId { get; set; }
    public string AssignedKey => AssignedUserId.HasValue ? "u" + AssignedUserId : AssignedCompanyId.HasValue ? "c" + AssignedCompanyId : "";
    public string? AssignedName { get; set; }
    public int? BrokerCompanyId { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public int? PostcodeId { get; set; }
    public bool CanExport { get; set; }
    public List<AssigneeOption> Assignees { get; set; } = new();
    public List<string> UnitNames { get; set; } = new();
    public string? Budget { get; set; }
    public string? Financing { get; set; }
    public string? OwnHome { get; set; }
    public string? Wish { get; set; }
    public bool ConsentContact { get; set; }
    public bool ConsentNewsletter { get; set; }
    public DateTime? ConsentAt { get; set; }
    public string? NextText { get; set; }
    public DateOnly? NextDate { get; set; }
    public string? NextName { get; set; }
    public int? NextUserId { get; set; }
    public DateTime? VisitAt { get; set; }
    public string? VisitUnit { get; set; }
    public DateOnly? OptionUntil { get; set; }
    public string? OptionUnit { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CrmEventVm> Events { get; set; } = new();
    public List<KeyValuePair<int, string>> Users { get; set; } = new();
    public List<KeyValuePair<int, string>> Units { get; set; } = new();
    public List<int> UnitIds { get; set; } = new();
    public int CurrentUserId { get; set; }
    public DateOnly Today { get; set; }
    public List<EmailTemplateItem> Templates { get; set; } = new();
    public string? Notice { get; set; }
}

public record EmailTemplateItem(int Id, string Name);

/// <summary>Iemand aan wie een contact toegewezen kan worden: Key is "u12" (medewerker) of "c34" (makelaar), Kind "Medewerker" of "Makelaar".</summary>
public record AssigneeOption(string Key, string Text, string Kind);

public class CrmContactFormVm
{
    /// <summary>Waarde voor de editortemplates GlV2Telefoon en GlV2Email (voorvoegsel, validatie).</summary>
    [System.ComponentModel.DataAnnotations.Display(Name = "Telefoon")] public string? Phone { get; set; }
    [System.ComponentModel.DataAnnotations.Display(Name = "E-mail")] public string? Email { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public List<KeyValuePair<int, string>> Users { get; set; } = new();
    public List<AssigneeOption> Assignees { get; set; } = new();
    public List<KeyValuePair<int, string>> Brokers { get; set; } = new();
    public int BelgiumCountryId { get; set; }
    public List<KeyValuePair<int, string>> Units { get; set; } = new();
    public int CurrentUserId { get; set; }
    public DateOnly Today { get; set; }
}
