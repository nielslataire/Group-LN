#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Extra inhouding op een inkomende factuur (design-handoff 42c/42j/42l): een bedrag dat tijdelijk niet wordt uitbetaald.</summary>
public partial class IncommingInvoiceInhouding
{
    public const byte RedenCreditnota = 1;
    public const byte RedenWerkNietVolledig = 2;
    public const byte RedenAndere = 3;

    public int Id { get; set; }
    public int IncommingInvoiceId { get; set; }
    public decimal Bedrag { get; set; }
    /// <summary>1 = creditnota te ontvangen, 2 = werk niet volledig uitgevoerd, 3 = andere.</summary>
    public byte Reden { get; set; }
    public string Toelichting { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }

    public virtual IncommingInvoices IncommingInvoice { get; set; }
    public virtual ICollection<IncommingInvoiceInhoudingAfhandeling> Afhandelingen { get; set; } = new List<IncommingInvoiceInhoudingAfhandeling>();
}

/// <summary>Eén stap waarmee een extra inhouding (deels) wordt afgehandeld.</summary>
public partial class IncommingInvoiceInhoudingAfhandeling
{
    public const byte SoortVrijgegeven = 1;
    public const byte SoortVerrekend = 2;
    public const byte SoortAfgeboekt = 3;

    public int Id { get; set; }
    public int InhoudingId { get; set; }
    public DateOnly Datum { get; set; }
    /// <summary>1 = vrijgegeven (betalen), 2 = verrekend met creditnota, 3 = afgeboekt.</summary>
    public byte Soort { get; set; }
    /// <summary>Deel van de inhouding dat met deze stap is afgehandeld (bij vrijgeven inclusief het deel dat als borg blijft).</summary>
    public decimal Bedrag { get; set; }
    /// <summary>Bij vrijgeven: het deel dat als borg ingehouden blijft.</summary>
    public decimal BorgBedrag { get; set; }
    public int? CreditnotaInvoiceId { get; set; }
    public string Opmerking { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    /// <summary>Ingetrokken (foute afhandeling gecorrigeerd, migratie 108): telt niet meer mee, de rij blijft als spoor staan.</summary>
    public DateTime? IngetrokkenOp { get; set; }
    public string IngetrokkenDoor { get; set; }

    public virtual IncommingInvoiceInhouding Inhouding { get; set; }
    public virtual IncommingInvoices CreditnotaInvoice { get; set; }
}

/// <summary>Vrijgave van (een deel van) de ingehouden borg van een contract.</summary>
public partial class ContractBorgVrijgave
{
    public const byte FaseVoorlopig = 1;
    public const byte FaseDefinitief = 2;
    public const byte FaseAndere = 3;

    public int Id { get; set; }
    /// <summary>Contract waarvan de borg vrijgegeven wordt; NULL voor borg op facturen zonder contract (dan CompanyId + ProjectId, migratie 106).</summary>
    public int? ContractId { get; set; }
    public int? CompanyId { get; set; }
    public int? ProjectId { get; set; }
    /// <summary>1 = voorlopige oplevering, 2 = definitieve oplevering, 3 = andere.</summary>
    public byte Fase { get; set; }
    public DateOnly Datum { get; set; }
    public decimal Bedrag { get; set; }
    public string Opmerking { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    /// <summary>Ingetrokken (foute vrijgave gecorrigeerd, migratie 108): telt niet meer mee, de rij blijft als spoor staan.</summary>
    public DateTime? IngetrokkenOp { get; set; }
    public string IngetrokkenDoor { get; set; }

    public virtual Contract Contract { get; set; }
}
