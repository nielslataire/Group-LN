using BOCore;
using DALCore.Models;

namespace FacadeCore;

/// <summary>Beheer van nutsaansluitingsdossiers (ProjectDossier + ProjectNutsAansluiting samen).</summary>
public interface INutsAansluitingService
{
    Task<ProjectNutsAansluiting?> GetById(int projectId, int id);
    Task<List<ProjectNutsAansluiting>> GetForProject(int projectId);
    Task<ProjectNutsAansluiting> Create(NutsAansluitingUpsertBO dto, string? userId);
    Task<ProjectNutsAansluiting?> Update(int id, NutsAansluitingUpsertBO dto, string? userId);

    /// <summary>Voorvult EAN/watermeternummer uit de eenheid, voor het aanmaakformulier.</summary>
    Task<(string? EanGas, string? EanElektriciteit, string? Watermeter)> GetUnitMeterData(int unitId);
}
