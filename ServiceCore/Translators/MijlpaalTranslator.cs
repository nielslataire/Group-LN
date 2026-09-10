using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators;

internal static class MijlpaalTranslator
{
    /// <summary>Zet een <see cref="MijlpaalUpsertBO"/> op een (nieuwe of bestaande) <see cref="Mijlpaal"/>-entity.</summary>
    internal static ErrorCode TranslateBOToEntity(Mijlpaal entity, MijlpaalUpsertBO bo)
    {
        if (entity == null)
            return ErrorCode.EntityNull;
        if (bo == null)
            return ErrorCode.BoNull;

        entity.Naam = (bo.Naam ?? string.Empty).Trim();
        entity.Code = string.IsNullOrWhiteSpace(bo.Code) ? entity.Code : bo.Code.Trim();
        entity.ProjecttrajectFaseId = bo.ProjecttrajectFaseId;
        entity.UnitId = bo.UnitId;
        entity.Volgorde = bo.Volgorde;
        entity.MijlpaalType = bo.MijlpaalType;
        entity.Status = bo.Status;
        entity.Doeldatum = bo.Doeldatum;
        entity.WerkelijkeDatum = bo.WerkelijkeDatum;
        entity.VerantwoordelijkeRol = bo.VerantwoordelijkeRol;
        entity.VerantwoordelijkePartijType = bo.VerantwoordelijkePartijType;
        entity.VerantwoordelijkePartijId = bo.VerantwoordelijkePartijId;
        entity.VerantwoordelijkeUserId = bo.VerantwoordelijkeUserId;
        entity.IsVerplicht = bo.IsVerplicht;
        entity.Opmerking = bo.Opmerking;

        return ErrorCode.Success;
    }
}
