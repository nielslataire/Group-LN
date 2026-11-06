namespace FacadeCore
{
    public sealed record HealthIndexRefreshResult(bool Ok, string? Error, int Rows, DateOnly? Latest);

    /// <summary>De gezondheidsindex van Statbel (basis 2013 = 100), bewaard per maand, voor het indexeren van terugkerende facturen.</summary>
    public interface IHealthIndexService
    {
        /// <summary>Alle bewaarde indexen, basis 2013 = 100, per eerste dag van de maand.</summary>
        Task<IReadOnlyDictionary<DateOnly, decimal>> GetAsync(CancellationToken ct = default);
        /// <summary>Haalt de laatste 13 maanden op bij Statbel en voegt nieuwe of gewijzigde maanden toe. Oudere maanden blijven bewaard.</summary>
        Task<HealthIndexRefreshResult> RefreshAsync(CancellationToken ct = default);
        /// <summary>De laatst bekende maand en wanneer er voor het laatst opgehaald werd.</summary>
        Task<(DateOnly? Latest, DateTime? FetchedAt)> StatusAsync(CancellationToken ct = default);
    }
}
