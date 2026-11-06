using DALCore.Models;

namespace CPMCore.Models.Invoicing
{
    /// <summary>Een rij in het Terugkerend-overzicht (design-handoff 50b).</summary>
    public class RecurringRowVM
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "";
        /// <summary>De omschrijving van de eerste lijn, met de variabelen nog in de tekst.</summary>
        public string Text { get; set; } = "";
        public string Rhythm { get; set; } = "";
        public DateOnly? Next { get; set; }
        public decimal Net { get; set; }
        public string OnCreateText { get; set; } = "";
        public int? LastInvoiceId { get; set; }
        public string? LastInvoiceNumber { get; set; }
        public byte Status { get; set; }
        /// <summary>active, pending (nog niet gestart), paused, concept, stopped, finished.</summary>
        public string StatusKey { get; set; } = "active";
        public string StatusLabel { get; set; } = "";
        public int FirstYear { get; set; }
        public int? LastYear { get; set; }
    }

    public class RecurringIndexVM
    {
        public int IssuerCompanyId { get; set; }
        public List<RecurringRowVM> Rows { get; set; } = new();
        public int InvoiceCount { get; set; }
        public int ScheduleCount { get; set; }
        public int ReviewCount { get; set; }
        public DateOnly? ReviewDate { get; set; }
        public decimal MonthlyAverage { get; set; }
        public bool CanWrite { get; set; }
        public List<string> Years { get; set; } = new();
        public string DefaultYear { get; set; } = "";
    }

    public class RecurringUpcomingVM
    {
        public DateOnly Date { get; set; }
        public decimal Net { get; set; }
        public string Note { get; set; } = "";
        public bool IsDue { get; set; }
    }

    public class RecurringCreatedInvoiceVM
    {
        public int Id { get; set; }
        public string? PublicId { get; set; }
        public DateOnly Date { get; set; }
        public string Period { get; set; } = "";
        public decimal Net { get; set; }
        public bool IsDraft { get; set; }
        public string StatusLabel { get; set; } = "";
        /// <summary>review (Ter controle), open, paid, other</summary>
        public string StatusKey { get; set; } = "other";
    }

    public class RecurringEventVM
    {
        public DateTime At { get; set; }
        public string Text { get; set; } = "";
        public string? Detail { get; set; }
    }

    public class RecurringLineVM
    {
        public string Text { get; set; } = "";
        public decimal Quantity { get; set; } = 1m;
        public decimal UnitPrice { get; set; }
        public int? VatTypeId { get; set; }
        public decimal VatPercentage { get; set; }
        public bool FirstOnly { get; set; }
        public bool Indexable { get; set; }
    }

    /// <summary>Schemadetail (design-handoff 50d).</summary>
    public class RecurringDetailVM
    {
        public RecurringInvoiceSchedule Schedule { get; set; } = new();
        public int IssuerCompanyId { get; set; }
        public string ClientName { get; set; } = "";
        public string? ClientEmail { get; set; }
        public string? ClientPeppolText { get; set; }
        public string? ProjectName { get; set; }
        public string? PaymentTermText { get; set; }
        public string StatusKey { get; set; } = "active";
        public string StatusLabel { get; set; } = "";
        public string Rhythm { get; set; } = "";
        public string RhythmLong { get; set; } = "";
        public decimal LinesNet { get; set; }
        public decimal LinesVat { get; set; }
        public string ExampleSentence { get; set; } = "";
        public List<RecurringLineVM> Lines { get; set; } = new();
        public List<RecurringUpcomingVM> Upcoming { get; set; } = new();
        public List<RecurringCreatedInvoiceVM> Invoices { get; set; } = new();
        public int InvoiceTotal { get; set; }
        public decimal InvoiceTotalNet { get; set; }
        public List<RecurringEventVM> Events { get; set; } = new();
        public bool CanWrite { get; set; }
        /// <summary>Wie verwijderrechten heeft op facturen mag een schema verwijderen (de facturen blijven staan).</summary>
        public bool CanDelete { get; set; }
        public int? DraftInvoiceId { get; set; }
        public DateOnly? DraftOccurrence { get; set; }
        public string? DraftPeriod { get; set; }
        public bool IndexationPending { get; set; }
        public decimal? IndexationOldNet { get; set; }
        public decimal? IndexationNewNet { get; set; }
        /// <summary>Uitleg bij de indexatie: welke gezondheidsindex gebruikt wordt, of waarop er gewacht wordt.</summary>
        public string? IndexationExplain { get; set; }
        public bool IndexationWaiting { get; set; }
        public bool IndexationIsHealth { get; set; }
        public int MissedCount { get; set; }
        public string IndexationText { get; set; } = "Geen";
        public string StopText { get; set; } = "Nooit";
    }

    /// <summary>Het zijpaneel Instellingen terugkerende facturen (design-handoff 50c).</summary>
    public class RecurringSettingsVM
    {
        public int IssuerCompanyId { get; set; }
        public string CompanyName { get; set; } = "";
        public byte OnCreateMode { get; set; } = 1;
        public byte DraftLeadDays { get; set; } = 3;
        public byte WeekendRule { get; set; } = 1;
        public string? NotifyUsers { get; set; }
        public byte IndexationType { get; set; } = 1;
        public byte IndexationWhen { get; set; } = 1;
        public bool IndexationConfirm { get; set; } = true;
        public byte ResumeMode { get; set; } = 1;
        public bool WarnUnpaid { get; set; } = true;
    }

    /// <summary>Wat het formulier "Nieuwe terugkerende factuur" (CreateV2, stap 1 t/m 4) naar de server stuurt.</summary>
    public class RecurringScheduleFormVM
    {
        public int ScheduleId { get; set; }
        /// <summary>0 = bewaren als concept, 1 = schema activeren.</summary>
        public byte Status { get; set; }
        public int IssuerCompanyId { get; set; }
        public int? PartyId { get; set; }
        public BOCore.InvoicePartyType? PartyType { get; set; }
        public int? ProjectId { get; set; }
        public int? SupplierContractId { get; set; }
        public int? IssuerBankAccountId { get; set; }
        public int? PaymentTermId { get; set; }
        public int? VatTypeId { get; set; }
        public bool IsPrepaid { get; set; }
        public string? HeaderDescription { get; set; }
        public string? DetailDescription { get; set; }
        public string? FooterDescription { get; set; }

        public byte Frequency { get; set; } = 2;
        public byte DayMode { get; set; } = 1;
        public byte DayOfMonth { get; set; } = 1;
        public string? FirstDate { get; set; }
        public byte PeriodMode { get; set; } = 1;
        public byte EndMode { get; set; }
        public int? EndCount { get; set; }
        public string? EndDate { get; set; }
        public byte DraftLeadDays { get; set; } = 3;
        public byte WeekendRule { get; set; } = 1;
        public byte ResumeMode { get; set; } = 1;
        public bool WarnUnpaid { get; set; } = true;

        public bool IndexationEnabled { get; set; }
        public byte IndexationType { get; set; } = 1;
        public byte IndexationWhen { get; set; } = 1;
        public string? IndexationBaseDate { get; set; }
        public string? IndexationDate { get; set; }
        public string? IndexationPercent { get; set; }
        public string? IndexationBaseIndex { get; set; }
        public bool IndexationConfirm { get; set; } = true;

        public List<RecurringLineVM> Lines { get; set; } = new();
    }
}
