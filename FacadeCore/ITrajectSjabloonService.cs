using DALCore.Models;
using BOCore;

namespace FacadeCore;

/// <summary>Beheer van trajectsjablonen (admin-UI onder Instellingen/Trajectsjablonen).</summary>
public interface ITrajectSjabloonService
{
    Task<List<TrajectSjabloon>> GetAll(bool includeInactive = false);
    Task<TrajectSjabloon?> GetById(int id, bool includeDetails = true);

    /// <summary>Kiest het standaardsjabloon voor een projecttype (val terug op een type-onafhankelijk sjabloon).</summary>
    Task<TrajectSjabloon?> GetStandaardVoorProjectType(int? projectType);

    /// <summary>Maakt of werkt een sjabloon inclusief fases en mijlpalen bij vanuit het bewerkingsmodel.</summary>
    Task<TrajectSjabloon> Upsert(TrajectSjabloonBO dto, string? userId);

    Task<bool> Delete(int id, string? userId);
}
