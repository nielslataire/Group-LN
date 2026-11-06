using DALCore.Models;

namespace FacadeCore
{
    /// <summary>Uitkomst van het aanmaken van één concept uit een schema.</summary>
    public sealed record RecurringGenerateResult(bool Ok, string? Error, int? InvoiceId, DateOnly? Occurrence, bool Held = false);

    /// <summary>Terugkerende facturen (design-handoff 49/50): schema's bewaren, concepten klaarzetten, pauzeren, hervatten en stopzetten.</summary>
    public interface IRecurringInvoiceService
    {
        /// <summary>De standaardinstellingen van een facturatiebedrijf; zonder bewaarde rij een nieuwe met de standaardwaarden (niet bewaard).</summary>
        Task<RecurringInvoiceSettings> GetSettingsAsync(int issuerCompanyId, CancellationToken ct = default);
        Task SaveSettingsAsync(RecurringInvoiceSettings settings, CancellationToken ct = default);

        /// <summary>Maakt een schema aan of wijzigt het (Id = 0 is nieuw). Status concept of actief. Gooit InvalidOperationException met een gebruiksvriendelijke tekst als iets niet klopt.</summary>
        Task<int> SaveScheduleAsync(RecurringInvoiceSchedule schedule, IList<RecurringInvoiceScheduleLine> lines, string? user, DateOnly today, CancellationToken ct = default);

        /// <summary>Zet nu al het concept voor de volgende factuurdatum klaar ("Nu factuur maken").</summary>
        Task<RecurringGenerateResult> GenerateNextAsync(int scheduleId, string? user, DateOnly today, CancellationToken ct = default);

        /// <summary>Zet voor alle actieve schema's de concepten klaar waarvan de factuurdatum binnen "concept klaar" valt. Geeft het aantal nieuwe concepten.</summary>
        Task<int> GenerateDueAsync(DateOnly today, CancellationToken ct = default);

        Task PauseAsync(int scheduleId, string? user, CancellationToken ct = default);
        /// <summary>Aantal factuurdatums van een gepauzeerd schema die intussen voorbij zijn.</summary>
        Task<int> CountMissedAsync(int scheduleId, DateOnly today, CancellationToken ct = default);
        /// <summary>Hervat een gepauzeerd schema. Met <paramref name="backfillMissed"/> worden gemiste periodes alsnog gefactureerd, anders overgeslagen.</summary>
        Task ResumeAsync(int scheduleId, bool backfillMissed, string? user, DateOnly today, CancellationToken ct = default);
        Task StopAsync(int scheduleId, string? user, CancellationToken ct = default);
        /// <summary>Verwijdert een schema in elke status. Aangemaakte facturen en concepten blijven staan als gewone facturen, zonder koppeling.</summary>
        Task DeleteAsync(int scheduleId, CancellationToken ct = default);
        /// <summary>Zet een stopgezet schema terug aan. Factuurdatums die intussen voorbij zijn worden overgeslagen.</summary>
        Task ReactivateAsync(int scheduleId, string? user, DateOnly today, CancellationToken ct = default);
        /// <summary>Een concept uit een schema werd verwijderd. Met <paramref name="restore"/> komt die factuurdatum terug in het schema (enkel als het de laatst aangemaakte is), anders wordt die periode definitief overgeslagen. Geeft true als ze teruggezet is.</summary>
        Task<bool> HandleDeletedConceptAsync(int scheduleId, DateOnly occurrence, string? periodText, bool restore, string? user, CancellationToken ct = default);
        /// <summary>Kan een verwijderd concept met deze factuurdatum nog terug in het schema? Enkel de laatst aangemaakte factuur van een actief, gepauzeerd of afgelopen schema.</summary>
        Task<bool> CanRestoreAsync(int scheduleId, DateOnly occurrence, CancellationToken ct = default);
        /// <summary>Bevestigt de eerstvolgende indexatie (met eventueel een nieuw percentage) zodat het concept klaargezet mag worden.</summary>
        Task ConfirmIndexationAsync(int scheduleId, decimal? percent, string? user, CancellationToken ct = default);
    }
}
