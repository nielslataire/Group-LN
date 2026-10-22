#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Een contact (persoon of koppel) van een project, met opvolging (design-handoff punt 44). Migratie 105.</summary>
public partial class CrmContact
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PartnerName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Street { get; set; }
    public string PostalCode { get; set; }
    public string City { get; set; }
    /// <summary>Website, Telefoon, E-mail, Makelaar of Werfbord.</summary>
    public string Source { get; set; } = "Website";
    public string BrokerName { get; set; }
    /// <summary>De makelaar als bedrijf (migratie 113); BrokerName blijft de weergavenaam.</summary>
    public int? BrokerCompanyId { get; set; }
    public string SeenVia { get; set; }
    /// <summary>0 Nieuw, 1 Contact, 2 Bezichtiging, 3 Optie, 4 Verkocht, 5 Afgevallen.</summary>
    public int Status { get; set; }
    public string StatusReason { get; set; }
    public int? AssignedUserId { get; set; }
    public string AssignedName { get; set; }
    /// <summary>Toegewezen aan een makelaar (bedrijf) i.p.v. een medewerker.</summary>
    public int? AssignedCompanyId { get; set; }
    public int? PostcodeId { get; set; }
    public string Budget { get; set; }
    public string Financing { get; set; }
    public string OwnHome { get; set; }
    public string Wish { get; set; }
    public bool ConsentContact { get; set; } = true;
    public bool ConsentNewsletter { get; set; }
    public DateTime? ConsentAt { get; set; }
    public string NextActionText { get; set; }
    public DateOnly? NextActionDate { get; set; }
    public int? NextActionUserId { get; set; }
    public string NextActionName { get; set; }
    public DateTime? VisitAt { get; set; }
    public string VisitUnit { get; set; }
    public DateOnly? OptionUntil { get; set; }
    public string OptionUnit { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastActivityAt { get; set; }

    public virtual Project Project { get; set; }
    public virtual ICollection<CrmContactUnit> Units { get; set; } = new List<CrmContactUnit>();
    public virtual ICollection<CrmContactEvent> Events { get; set; } = new List<CrmContactEvent>();
}

public partial class CrmContactUnit
{
    public int ContactId { get; set; }
    public int UnitId { get; set; }

    public virtual CrmContact Contact { get; set; }
    public virtual DALCore.Models.Units Unit { get; set; }
}

public partial class CrmContactEvent
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    /// <summary>form, note, call, mail, visit, option, status.</summary>
    public string Kind { get; set; }
    public DateTime OccurredAt { get; set; }
    public string UserName { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public int? DurationMin { get; set; }
    public bool IsAuto { get; set; }
    /// <summary>Id van de rij in ContactRequests waaruit dit item is overgenomen (voorkomt dubbel importeren).</summary>
    public int? LegacyRequestId { get; set; }

    public virtual CrmContact Contact { get; set; }
}

/// <summary>Een opgeslagen lijst (naam + filters) van de contacten van een project (design-handoff 44d).</summary>
public partial class CrmContactList
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; }
    public string FiltersJson { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Project Project { get; set; }
}

/// <summary>Elke export van contacten wordt gelogd: wie, wanneer, welke lijst.</summary>
public partial class CrmExportLog
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string UserName { get; set; }
    public string ListName { get; set; }
    public string Format { get; set; }
    public int ContactCount { get; set; }
    public int Skipped { get; set; }
    public DateTime ExportedAt { get; set; }

    public virtual Project Project { get; set; }
}

/// <summary>Een makelaar (bedrijf uit Leveranciers) gekoppeld aan een project. Migratie 113.</summary>
public partial class ProjectBroker
{
    public int ProjectId { get; set; }
    public int CompanyId { get; set; }

    public virtual CompanyInfo Company { get; set; }
}

/// <summary>Een bewust verwijderde oude contactaanvraag: wordt niet opnieuw als contact ingelezen. Migratie 114.</summary>
public partial class CrmContactLegacySkip
{
    public int LegacyRequestId { get; set; }
    public int ProjectId { get; set; }
    public DateTime DeletedAt { get; set; }
}
