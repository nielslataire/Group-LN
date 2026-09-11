using BOCore;
using DALCore.Models;

namespace FacadeCore;

/// <summary>Persoonlijke/interne takenlaag ("Mijn taken"). Spiegelt IConstructionIssueService/IProjectDossierService.</summary>
public interface IProjectTaakService
{
    Task<ProjectTaak?> GetById(int id);

    /// <summary>Taken binnen één project (bv. op een mijlpaal-/dossier-/punt-detail).</summary>
    Task<List<ProjectTaak>> SearchProject(int projectId, TaakFilterBO filters);

    /// <summary>
    /// Cross-project "Mijn taken": taken toegewezen aan <paramref name="userId"/> (of aan één van
    /// <paramref name="rollen"/>), beperkt tot <paramref name="zichtbareProjectIds"/> + taken zonder
    /// project. Gebruikt dezelfde projectzichtbaarheid-scoping als de rest van de app.
    /// </summary>
    Task<List<ProjectTaak>> SearchMijnTaken(string userId, IEnumerable<int> rollen, IEnumerable<int> zichtbareProjectIds, TaakFilterBO filters);

    Task<ProjectTaak> Create(TaakUpsertBO dto, string? userId);
    Task<ProjectTaak?> Update(int id, TaakUpsertBO dto, string? userId);
    Task<bool> ChangeStatus(int id, int newStatus, string? userId);
    Task<bool> Reassign(int id, string? toegewezenAanUserId, int? toegewezenAanRol, string? userId);
    Task<bool> Delete(int id, string? userId);

    /// <summary>Aantal open taken toegewezen aan de gebruiker (voor badges/widgets).</summary>
    Task<int> CountOpenVoorGebruiker(string userId);

    /// <summary>Top N open taken toegewezen aan de gebruiker, dichtstbijzijnde vervaldatum eerst.</summary>
    Task<List<ProjectTaak>> GetTopOpenVoorGebruiker(string userId, int aantal = 5);

    /// <summary>Maakt een taak aan vanuit de trigger-engine (Herkomst = Trigger), gekoppeld aan de mijlpaal.</summary>
    Task<ProjectTaak> CreateVanuitTrigger(int? projectId, int mijlpaalId, string titel, string? omschrijving,
        string? toegewezenAanUserId, int? toegewezenAanRol, DateOnly? vervaldatum, string? triggeredBy);
}
