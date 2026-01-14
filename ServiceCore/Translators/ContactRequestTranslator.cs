using System;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators;

internal static class ContactRequestTranslator
{
    internal static ErrorCode TranslateEntityToBO(ContactRequests entity, ContactRequestBO bo)
    {
        if (entity == null || bo == null) return ErrorCode.EntityNotFound;

        bo.Id = entity.Id;
        bo.CreatedAt = entity.CreatedAt;
        bo.SourceSite = entity.SourceSite;
        bo.Origin = entity.Origin;
        bo.RequestType = entity.RequestType;
        bo.ProjectId = entity.ProjectId;
        bo.UnitId = entity.UnitId;
        bo.DocumentId = entity.DocumentId;
        bo.DocumentName = entity.DocumentName;
        bo.DocumentFileName = entity.DocumentFileName;
        bo.DocumentType = entity.DocumentType;
        bo.Firstname = entity.Firstname;
        bo.Lastname = entity.Lastname;
        bo.Fullname = entity.Fullname;
        bo.Email = entity.Email;
        bo.Phone = entity.Phone;
        bo.Subject = entity.Subject;
        bo.Question = entity.Question;
        bo.ExternalMailStatus = entity.ExternalMailStatus;
        bo.InternalMailStatus = entity.InternalMailStatus;

        return ErrorCode.Success;
    }

    internal static ErrorCode TranslateBOToEntity(ContactRequests entity, ContactRequestBO bo)
    {
        if (entity == null || bo == null) return ErrorCode.EntityNotFound;

        entity.CreatedAt = bo.CreatedAt == default ? DateTime.UtcNow : bo.CreatedAt;
        entity.SourceSite = bo.SourceSite;
        entity.Origin = bo.Origin;
        entity.RequestType = bo.RequestType;
        entity.ProjectId = bo.ProjectId;
        entity.UnitId = bo.UnitId;
        entity.DocumentId = bo.DocumentId;
        entity.DocumentName = bo.DocumentName;
        entity.DocumentFileName = bo.DocumentFileName;
        entity.DocumentType = bo.DocumentType;
        entity.Firstname = bo.Firstname;
        entity.Lastname = bo.Lastname;
        entity.Fullname = bo.Fullname;
        entity.Email = bo.Email;
        entity.Phone = bo.Phone;
        entity.Subject = bo.Subject;
        entity.Question = bo.Question;
        entity.ExternalMailStatus = bo.ExternalMailStatus;
        entity.InternalMailStatus = bo.InternalMailStatus;
        return ErrorCode.Success;
    }
}