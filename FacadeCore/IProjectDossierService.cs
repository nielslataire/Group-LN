using BOCore;
using DALCore.Models;

namespace FacadeCore;

/// <summary>Generiek dossierbeheer (spiegelt IConstructionIssueService/IMijlpaalService).</summary>
public interface IProjectDossierService
{
    Task<ProjectDossier?> GetById(int projectId, int id);
    Task<List<ProjectDossier>> Search(int projectId, DossierFilterBO filters);
    Task<ProjectDossier> Create(DossierUpsertBO dto, string? userId);
    Task<ProjectDossier?> Update(int id, DossierUpsertBO dto, string? userId);
    Task<bool> ChangeStatus(int projectId, int id, int newStatus, string? userId, string? opmerking = null);
    Task<bool> Delete(int projectId, int id, string? userId);

    Task<ProjectDossierGebeurtenis> AddGebeurtenis(DossierGebeurtenisBO dto, string? userId);
    Task<List<ProjectDossierGebeurtenis>> GetGebeurtenissen(int projectDossierId);

    Task<bool> LinkDoc(int projectDossierId, int? projectDocId, string? fileId, string? naam, string? userId);

    /// <summary>
    /// Koppelt een mijlpaal aan het dossier. Voor een Omgevingsvergunning-dossier: als de mijlpaal
    /// (op Code) een bekende vergunningsstap is en nog geen eigen binding heeft, wordt automatisch
    /// <c>BronBinding = DossierSubstap</c> + de bijbehorende stap-Code ingesteld.
    /// </summary>
    Task<bool> LinkMijlpaal(int projectDossierId, int mijlpaalId);
    Task<bool> UnlinkMijlpaal(int projectDossierId, int mijlpaalId);

    /// <summary>
    /// Herkoppelt projectniveau-mijlpalen (VERGUNNING_INGEDIEND/VOLLEDIG/OPENBAAR_ONDERZOEK/VERLEEND/DEFINITIEF)
    /// die nog aan geen enkel dossier hangen, aan het (oudste) actieve Omgevingsvergunning-dossier van dit
    /// project. Dekt het geval dat die mijlpalen pas ná het aanmaken van het dossier zijn ontstaan (bv. traject
    /// pas later geïnstantieerd, of sjabloon-synchronisatie voegt ze pas nadien toe). Idempotent.
    /// </summary>
    Task<int> RelinkVergunningMijlpalen(int projectId, string? userId);

    /// <summary>Aantal dossiers per project die nog niet afgehandeld/geannuleerd zijn (voor KPI's).</summary>
    Task<int> CountOpen(IEnumerable<int> projectIds);

    // --- Checklist-stappen (bv. omgevingsvergunning) ---
    Task<List<ProjectDossierSubstap>> GetSubstappen(int projectDossierId);
    Task<bool> ChangeSubstapStatus(int projectDossierId, int substapId, int status, DateOnly? datum, string? userId);
}
