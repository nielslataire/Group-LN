using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;   

namespace ServiceCore.Translators
{
   public  class ProjectPictureTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ProjectPictures _entity, ProjectPictureBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id              = _entity.Id;
            bo.Name            = _entity.Name;
            bo.Caption         = _entity.Caption;
            bo.ProjectId       = (int)(_entity.ProjectId ?? 0);
            bo.Type            = (PictureType)(_entity.Type ?? 0);
            bo.DateTimeUploaded = _entity.Datetimeuploaded;
            bo.FacebookIdCopro = _entity.FacebookIdCopro;
            bo.SectionId       = _entity.SectionId;
            bo.IsPublic        = _entity.IsPublic;
            bo.SortOrder       = _entity.SortOrder;
            bo.MediaType       = _entity.MediaType;
            bo.FileSizeBytes   = _entity.FileSizeBytes;
            bo.WidthPx         = _entity.WidthPx;
            bo.HeightPx        = _entity.HeightPx;
            bo.DurationSeconds = _entity.DurationSeconds;
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(ProjectPictures _entity, ProjectPictureBO bo, UnitOfWorkCore uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name           = bo.Name;
            _entity.Caption        = bo.Caption;
            _entity.ProjectId      = bo.ProjectId;
            _entity.Type           = (int)bo.Type;
            _entity.Datetimeuploaded = bo.DateTimeUploaded;
            _entity.FacebookIdCopro = bo.FacebookIdCopro;
            _entity.SectionId      = bo.SectionId;
            _entity.IsPublic       = bo.IsPublic;
            _entity.SortOrder      = bo.SortOrder;
            _entity.MediaType      = bo.MediaType;
            _entity.FileSizeBytes  = bo.FileSizeBytes;
            _entity.WidthPx        = bo.WidthPx;
            _entity.HeightPx       = bo.HeightPx;
            _entity.DurationSeconds = bo.DurationSeconds;
            return ErrorCode.Success;
        }
    }
}
