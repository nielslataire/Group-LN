using BOCore;
using DALCore.Models;

namespace CPMCore.Models.Traject;

public class DeadlinesIndexVm
{
    public List<Mijlpaal> Mijlpalen { get; set; } = new();
    public MijlpaalFilterBO Filter { get; set; } = new();
    public List<IdNameBO> Projecten { get; set; } = new();

    public int AantalAchterstallig
    {
        get
        {
            var vandaag = DateOnly.FromDateTime(DateTime.Today);
            return Mijlpalen.Count(m => m.Status != (int)MijlpaalStatus.Bereikt && m.Status != (int)MijlpaalStatus.NietVanToepassing
                && (m.Doeldatum ?? m.DoeldatumBerekend) != null && (m.Doeldatum ?? m.DoeldatumBerekend) < vandaag);
        }
    }

    public int AantalDeze30Dagen
    {
        get
        {
            var vandaag = DateOnly.FromDateTime(DateTime.Today);
            var grens = vandaag.AddDays(30);
            return Mijlpalen.Count(m => m.Status != (int)MijlpaalStatus.Bereikt && m.Status != (int)MijlpaalStatus.NietVanToepassing
                && (m.Doeldatum ?? m.DoeldatumBerekend) is DateOnly d && d >= vandaag && d <= grens);
        }
    }
}
