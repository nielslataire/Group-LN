namespace CPMCore.Services.Octopus
{
    public class OctopusOptions
    {
        public string ApiBaseUrl { get; set; } = string.Empty;

        public string softwareHouseUuid { get; set; } = string.Empty;

        /// <summary>
        /// Base URL voor de Octopus File Sync API (andere service dan de REST API).
        /// Stel in via appsettings.json onder "Octopus:FileSyncApiBaseUrl".
        /// Standaard: https://service.inaras.be/octopus-filesync-api/v1
        /// </summary>
        public string FileSyncApiBaseUrl { get; set; } = "https://service.inaras.be/octopus-filesync-api/v1";

        /// <summary>
        /// Prefix van aankoopjournalen (dagboeken) die geïmporteerd worden als inkomende facturen.
        /// Stel in via appsettings.json onder "Octopus:PurchaseJournalKeyPrefix".
        /// Standaard "A" — past bij Belgische Octopus-conventie (A1, A2, ...).
        /// Leeg = alle journalen importeren (inclusief verkoopboekingen — niet aanbevolen).
        /// </summary>
        public string PurchaseJournalKeyPrefix { get; set; } = "A";
    }
}