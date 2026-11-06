#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Een schema voor terugkerende facturen (design-handoff punt 49/50, migratie 133): wie, hoe vaak, welke lijnen en wat het CRM bij elke factuur doet.</summary>
public partial class RecurringInvoiceSchedule
{
    public const byte StatusConcept = 0;
    public const byte StatusActive = 1;
    public const byte StatusPaused = 2;
    public const byte StatusStopped = 3;
    public const byte StatusFinished = 4;

    public int Id { get; set; }
    public int IssuerCompanyId { get; set; }
    public byte Status { get; set; }

    /// <summary>1 ClientAccount, 2 ClientContact, 3 Supplier (zoals InvoicePartyType).</summary>
    public byte PartyType { get; set; }
    public int PartyId { get; set; }
    public int? ProjectId { get; set; }
    public int? SupplierContractId { get; set; }
    public int? IssuerBankAccountId { get; set; }
    public int? PaymentTermId { get; set; }
    public int? VatTypeId { get; set; }
    public bool IsPrepaid { get; set; }
    public string HeaderDescription { get; set; }
    public string DetailDescription { get; set; }
    public string FooterDescription { get; set; }

    /// <summary>1 week, 2 maand, 3 kwartaal, 4 jaar.</summary>
    public byte Frequency { get; set; } = 2;
    /// <summary>1 vaste dag, 2 laatste dag van de maand.</summary>
    public byte DayMode { get; set; } = 1;
    /// <summary>1-31; bij een wekelijks schema 1 = maandag tot 5 = vrijdag.</summary>
    public byte DayOfMonth { get; set; } = 1;
    public DateOnly FirstDate { get; set; }
    /// <summary>1 lopende periode op de factuur, 2 vorige periode.</summary>
    public byte PeriodMode { get; set; } = 1;
    /// <summary>0 nooit, 1 na aantal facturen, 2 op datum.</summary>
    public byte EndMode { get; set; }
    public int? EndCount { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>1 klaarzetten ter controle (Proforma), 2 automatisch nummeren en versturen.</summary>
    public byte OnCreateMode { get; set; } = 1;
    public byte DraftLeadDays { get; set; } = 3;
    /// <summary>1 volgende werkdag, 2 vorige werkdag, 3 ongewijzigd.</summary>
    public byte WeekendRule { get; set; } = 1;
    /// <summary>1 telkens vragen na hervatten, 2 gemiste periodes overslaan.</summary>
    public byte ResumeMode { get; set; } = 1;
    public bool WarnUnpaid { get; set; } = true;

    public bool IndexationEnabled { get; set; }
    /// <summary>1 gezondheidsindex, 2 consumptieprijzen, 3 vast percentage.</summary>
    public byte IndexationType { get; set; } = 1;
    /// <summary>1 op 1 januari, 2 op de verjaardag van de start, 3 op een andere datum (IndexationDay/-Month).</summary>
    public byte IndexationWhen { get; set; } = 1;
    public DateOnly? IndexationBaseDate { get; set; }
    public byte? IndexationDay { get; set; }
    public byte? IndexationMonth { get; set; }
    public decimal? IndexationPercent { get; set; }
    /// <summary>Basisindex (basis 2013 = 100) voor de gezondheidsindex als de startdatum ouder is dan de bewaarde historiek; leeg = de index van de maand vóór de startdatum.</summary>
    public decimal? IndexationBaseIndex { get; set; }
    public bool IndexationConfirm { get; set; } = true;
    public int IndexationAckCount { get; set; }

    public int OccurrenceIndex { get; set; }
    public DateOnly? NextOccurrence { get; set; }
    public DateOnly? LastOccurrence { get; set; }
    public int CreatedCount { get; set; }
    public int? LastInvoiceId { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RecurringInvoiceScheduleLine> Lines { get; set; } = new List<RecurringInvoiceScheduleLine>();
    public virtual ICollection<RecurringInvoiceEvent> Events { get; set; } = new List<RecurringInvoiceEvent>();
}

/// <summary>Een lijn van een schema: de omschrijving bevat variabelen als {maand} en {jaar} die bij elke factuur ingevuld worden.</summary>
public partial class RecurringInvoiceScheduleLine
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int SortOrder { get; set; }
    public string Text { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public int? VatTypeId { get; set; }
    public decimal VatPercentage { get; set; }
    /// <summary>Enkel op de eerste factuur (bv. een waarborg).</summary>
    public bool FirstOnly { get; set; }
    public bool Indexable { get; set; }

    public virtual RecurringInvoiceSchedule Schedule { get; set; }
}

/// <summary>Eén regel in de geschiedenis van een schema.</summary>
public partial class RecurringInvoiceEvent
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public DateTime At { get; set; }
    public string Text { get; set; }
    public string Detail { get; set; }

    public virtual RecurringInvoiceSchedule Schedule { get; set; }
}

/// <summary>Een gezondheidsindex van Statbel voor één maand en basisjaar (migratie 135).</summary>
public partial class HealthIndexValue
{
    /// <summary>De basis waarmee het CRM rekent: 2013 = 100.</summary>
    public const short CalcBase = 2013;
    public DateOnly Month { get; set; }
    public short BaseYear { get; set; }
    public decimal Value { get; set; }
    public DateTime FetchedAt { get; set; }
}

/// <summary>Standaardinstellingen voor nieuwe schema's van één facturatiebedrijf (zijpaneel "Instellingen terugkerende facturen").</summary>
public partial class RecurringInvoiceSettings
{
    public int IssuerCompanyId { get; set; }
    public byte OnCreateMode { get; set; } = 1;
    public byte DraftLeadDays { get; set; } = 3;
    public byte WeekendRule { get; set; } = 1;
    /// <summary>Kommagescheiden e-mailadressen van wie een melding krijgt als een concept klaarstaat.</summary>
    public string NotifyUsers { get; set; }
    public byte IndexationType { get; set; } = 1;
    public byte IndexationWhen { get; set; } = 1;
    public bool IndexationConfirm { get; set; } = true;
    public byte ResumeMode { get; set; } = 1;
    public bool WarnUnpaid { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
}
