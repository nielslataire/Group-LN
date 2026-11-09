using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FacadeCore
{
    /// <summary>Resultaat van een controle inhoudingsplicht (30bis/30ter) bij de RSZ-webservice BillRetainment, voor één onderneming.</summary>
    public class RetainmentCheckVm
    {
        public int Id { get; set; }
        public int? IncomingInvoiceId { get; set; }
        public Guid? BatchId { get; set; }
        public string SupplierName { get; set; }
        public string EnterpriseIdentifier { get; set; }
        public string EnterpriseNumber { get; set; }
        /// <summary>Simulation of Production.</summary>
        public string Environment { get; set; }
        public DateTime CheckedAt { get; set; }
        public string CheckedBy { get; set; }
        public string RetainmentCheckReference { get; set; }
        /// <summary>BelGov-Trace-Id van het RSZ-antwoord.</summary>
        public string TraceId { get; set; }
        public string FiscalSituation { get; set; }
        public int? FiscalWithholdingPercentage { get; set; }
        public DateTime? FiscalValidUntil { get; set; }
        public long? FiscalAttestId { get; set; }
        public string SocialSituation { get; set; }
        public int? SocialWithholdingPercentage { get; set; }
        public DateTime? SocialValidUntil { get; set; }
        /// <summary>Het ongewijzigde RSZ-resultaat (JSON) voor deze onderneming.</summary>
        public string RawResponse { get; set; }
        /// <summary>Ingevuld als de bevraging mislukte; er is dan geen uitspraak over de inhoudingsplicht.</summary>
        public string ErrorMessage { get; set; }
        public string BatchName { get; set; }

        public bool Failed => !string.IsNullOrEmpty(ErrorMessage);
        public bool FiscalObligation => FiscalSituation == "obligation";
        public bool SocialObligation => SocialSituation == "obligation";
        /// <summary>Rood: er moet (deels) ingehouden worden.</summary>
        public bool HasObligation => !Failed && (FiscalObligation || SocialObligation);

        /// <summary>
        /// Mag deze controle nog gebruikt worden in plaats van een nieuwe bevraging? Zonder inhoudingsplicht: tot de vermelde vervaldatum (in productie de laatste donderdag van de maand);
        /// met inhoudingsplicht of zonder datum: 24 uur, want zo'n toestand kan snel veranderen.
        /// </summary>
        public bool IsStillValid(DateTime utcNow)
        {
            if (Failed) return false;
            var until = new[] { SocialValidUntil, FiscalValidUntil }.Where(d => d.HasValue).Select(d => d.Value).DefaultIfEmpty().Min();
            if (!HasObligation && until != default) return until.Date.AddDays(1) > utcNow.ToLocalTime();   // de vervaldatum zelf telt nog mee
            return CheckedAt.AddHours(24) > utcNow;
        }
    }

    /// <summary>Eén onderneming om te controleren in een controlelijst.</summary>
    public class RetainmentCheckInput
    {
        public string SupplierName { get; set; }
        /// <summary>Btw-nummer, ondernemingsnummer of buitenlandse identificatie, in eender welke schrijfwijze.</summary>
        public string Identifier { get; set; }
    }

    /// <summary>Een controlelijst: meerdere ondernemingen die samen, handmatig, vóór een betaling gecontroleerd zijn.</summary>
    public class RetainmentBatchVm
    {
        public Guid BatchId { get; set; }
        public string Name { get; set; }
        public DateTime CheckedAt { get; set; }
        public string CheckedBy { get; set; }
        public string Environment { get; set; }
        public IReadOnlyList<RetainmentCheckVm> Items { get; set; } = Array.Empty<RetainmentCheckVm>();
    }

    public class RetainmentBatchSummaryVm
    {
        public Guid BatchId { get; set; }
        public string Name { get; set; }
        public DateTime CheckedAt { get; set; }
        public string CheckedBy { get; set; }
        public string Environment { get; set; }
        public int Total { get; set; }
        public int WithObligation { get; set; }
        public int Failed { get; set; }
    }

    public interface IRetainmentCheckService
    {
        /// <summary>True als de RSZ-koppeling ingesteld is (Client ID en private sleutel).</summary>
        bool IsConfigured { get; }

        /// <summary>True zolang de koppeling op de testomgeving van de RSZ staat (vaste testgegevens, geen bewijswaarde).</summary>
        bool IsSimulation { get; }

        /// <summary>De laatste controle voor een ontvangen factuur, of null als er nog geen is.</summary>
        Task<RetainmentCheckVm> GetLatestForInvoiceAsync(int incomingInvoiceId, CancellationToken ct = default);

        /// <summary>Vraagt de inhoudingsplicht van de leverancier van de factuur op, bewaart het resultaat en geeft het terug. Mislukt de bevraging, dan krijg je een resultaat met <see cref="RetainmentCheckVm.ErrorMessage"/>.</summary>
        Task<RetainmentCheckVm> CheckInvoiceAsync(int incomingInvoiceId, string checkedBy, CancellationToken ct = default);

        /// <summary>Controleert een lijst ondernemingen (max. 200) in één keer en bewaart ze als één controlelijst met bewijs.</summary>
        Task<RetainmentBatchVm> CheckBatchAsync(IReadOnlyList<RetainmentCheckInput> inputs, string checkedBy, CancellationToken ct = default, string batchName = null);

        Task<RetainmentBatchVm> GetBatchAsync(Guid batchId, CancellationToken ct = default);

        /// <summary>Geeft een controlelijst een (andere) naam; enkel de naam wijzigt, het bewijs blijft ongewijzigd. False als de lijst niet bestaat.</summary>
        Task<bool> RenameBatchAsync(Guid batchId, string name, CancellationToken ct = default);

        Task<IReadOnlyList<RetainmentBatchSummaryVm>> ListBatchesAsync(int take, CancellationToken ct = default);

        /// <summary>
        /// Geeft een nog geldige eerdere controle van deze onderneming terug (zelfde omgeving, geen fout, niet verlopen) en bevraagt de RSZ enkel als die er niet is.
        /// Gebruik dit overal waar een controle automatisch moet gebeuren: zo blijft het aantal bevragingen laag.
        /// </summary>
        Task<RetainmentCheckVm> EnsureCheckedAsync(RetainmentCheckInput input, int? incomingInvoiceId, string checkedBy, CancellationToken ct = default);

        /// <summary>Zelfde als <see cref="EnsureCheckedAsync"/> voor een lijst: één RSZ-aanroep voor wie nog geen geldige controle heeft, alles bewaard als één lijst (<see cref="RetainmentBatchVm.Items"/> bevat enkel de nieuwe controles; null als er niets te doen was).</summary>
        Task<RetainmentBatchVm> EnsureCheckedBatchAsync(IReadOnlyList<RetainmentCheckInput> inputs, string checkedBy, CancellationToken ct = default);

        /// <summary>De laatste bruikbare controle van een onderneming (op nummer, in eender welke schrijfwijze), of null.</summary>
        Task<RetainmentCheckVm> GetLatestForEnterpriseAsync(string identifier, CancellationToken ct = default);

        /// <summary>De laatste controle (ook een mislukte) per onderneming in de huidige omgeving, zonder het ruwe antwoord. Sleutel = genormaliseerd nummer.</summary>
        Task<IReadOnlyDictionary<string, RetainmentCheckVm>> GetLastChecksAsync(IReadOnlyCollection<string> identifiers, CancellationToken ct = default);

        /// <summary>Alle controles van een onderneming, nieuwste eerst.</summary>
        Task<IReadOnlyList<RetainmentCheckVm>> GetHistoryForEnterpriseAsync(string identifier, int take, CancellationToken ct = default);

        /// <summary>Ondernemingen waarvan de laatste (geslaagde) controle in de huidige omgeving een inhoudingsplicht toont.</summary>
        Task<IReadOnlyList<RetainmentCheckVm>> GetOpenObligationsAsync(CancellationToken ct = default);
    }
}
