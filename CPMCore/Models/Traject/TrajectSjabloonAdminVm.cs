using DALCore.Models;

namespace CPMCore.Models.Traject;

public class TrajectSjabloonListVm
{
    public List<TrajectSjabloon> Sjablonen { get; set; } = new();
    public Dictionary<int, int> AantalTrajectenPerSjabloon { get; set; } = new();
}

public class TrajectSjabloonEditVm
{
    public TrajectSjabloon? Sjabloon { get; set; }
    public bool IsNieuw => Sjabloon == null || Sjabloon.Id == 0;
}
