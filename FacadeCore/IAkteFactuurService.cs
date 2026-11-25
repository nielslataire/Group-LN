using BOCore;

namespace FacadeCore
{
    /// <summary>Eén conceptfactuur van de aktefacturen (voor één eigenaar of mede-eigenaar).</summary>
    public class AkteFactuurVoorstel
    {
        public int ClientAccountId { get; set; }
        /// <summary>"Grond" (grondwaarde, zonder btw) of "Kosten" (aandeel akten, landmeter en eigen kosten, met btw).</summary>
        public string Soort { get; set; } = "";
        public string Eigenaar { get; set; } = "";
        public decimal Aandeel { get; set; }
        public bool IsHoofdeigenaar { get; set; }
        public InvoiceDraftBO Draft { get; set; } = new();
        public List<int> UnitIds { get; set; } = new();
    }

    public class AkteFacturenPlan
    {
        public List<AkteFactuurVoorstel> Facturen { get; set; } = new();
        /// <summary>Wat niet klaargezet kon worden en waarom (geen facturatiebedrijf, geen bedragen ingesteld, enz.).</summary>
        public List<string> Opmerkingen { get; set; } = new();
    }

    /// <summary>Aktefacturen klaarzetten (25/11/2026): per klant een grondfactuur (grondvennootschap, zonder btw) en een kostenfactuur
    /// (grondvennootschap, met btw uit de verkoopsinstellingen). De werkenfactuur loopt via de bestaande schijven. Zie PLANNING_VOORTGANG.md.</summary>
    public interface IAkteFactuurService
    {
        /// <summary>Berekent de voorstellen zonder iets te bewaren. Eenheden die al een (niet geannuleerde) akte-factuur van die soort hebben, worden overgeslagen.</summary>
        Task<AkteFacturenPlan> BerekenAsync(int projectId, IReadOnlyCollection<int> clientAccountIds, CancellationToken ct = default);

        /// <summary>Maakt de conceptfacturen aan (niet uitgegeven). Geeft het aantal aangemaakte facturen terug; fouten staan in de opmerkingen.</summary>
        Task<(int Aangemaakt, List<string> Opmerkingen)> AanmakenAsync(int projectId, IReadOnlyCollection<int> clientAccountIds, CancellationToken ct = default);
    }
}
