using BOCore;
using DALCore.Models;

namespace CPMCore.Models.Traject;

public class MijnTakenIndexVm
{
    public List<ProjectTaak> Taken { get; set; } = new();
    public TaakFilterBO Filter { get; set; } = new();
    public List<IdNameBO> Projecten { get; set; } = new();

    /// <summary>UserId -> weergavenaam voor elke ToegewezenAanUserId in Taken — ProjectTaak heeft
    /// geen EF-navigatie naar Users, dus de controller vult dit apart in (GetGebruikersnamen).</summary>
    public Dictionary<string, string> GebruikersNamen { get; set; } = new();

    public int AantalOpen => Taken.Count(t => t.Status != (int)TaakStatus.Afgerond && t.Status != (int)TaakStatus.Geannuleerd);
    public int AantalAchterstallig
    {
        get
        {
            var vandaag = DateOnly.FromDateTime(DateTime.Today);
            return Taken.Count(t => t.Vervaldatum != null && t.Vervaldatum.Value < vandaag
                && t.Status != (int)TaakStatus.Afgerond && t.Status != (int)TaakStatus.Geannuleerd);
        }
    }
}
