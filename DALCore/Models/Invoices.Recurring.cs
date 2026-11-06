#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Terugkerende facturen (migratie 133): welke factuur uit welk schema komt. De rest van <see cref="Invoices"/> staat in het gegenereerde bestand.</summary>
public partial class Invoices
{
    public int? RecurringScheduleId { get; set; }
    /// <summary>De geplande factuurdatum in het schema waarvoor deze factuur gemaakt werd.</summary>
    public DateOnly? RecurringOccurrence { get; set; }
    /// <summary>Het kleine label in de lijst met facturen, bv. "Coördinatie · dec".</summary>
    public string RecurringLabel { get; set; }
    /// <summary>De periode waarop de factuur slaat, bv. "December 2026".</summary>
    public string RecurringPeriodText { get; set; }
}
