using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators;

internal static class ConstructionIssueTranslator
{
    internal static ErrorCode TranslateBOToEntity(ConstructionIssue entity, ConstructionIssueUpsertBO bo)
    {
        if (entity == null)
            return ErrorCode.EntityNull;
        if (bo == null)
            return ErrorCode.BoNull;

        entity.Title = (bo.Title ?? string.Empty).Trim();
        entity.Description = bo.Description;
        entity.LocationText = (bo.LocationText ?? string.Empty).Trim();
        entity.CategoryId = bo.CategoryId;
        entity.BuildingPart = bo.BuildingPart;
        entity.RoomOrZone = bo.RoomOrZone;
        entity.UnitId = bo.UnitId;
        entity.IssueType = bo.IssueType;
        entity.IssuePhase = bo.IssuePhase;
        entity.Priority = bo.Priority;
        entity.Status = bo.Status;
        entity.ResponsiblePartyType = bo.ResponsiblePartyType;
        entity.ResponsiblePartyId = bo.ResponsiblePartyId;
        entity.ResponsibleOtherName = bo.ResponsibleOtherName;
        entity.ResponsibleOtherEmail = bo.ResponsibleOtherEmail;
        entity.DueDate = bo.DueDate;
        entity.PlannedDate = bo.PlannedDate;
        entity.PlanDocumentId = bo.PlanDocumentId;
        entity.PlanPageNumber = bo.PlanPageNumber;
        entity.PlanXnormalized = bo.PlanXNormalized;
        entity.PlanYnormalized = bo.PlanYNormalized;

        return ErrorCode.Success;
    }
}