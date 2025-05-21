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
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            bo.Caption = _entity.Caption;
            bo.ProjectId = (int)_entity.ProjectId;
            bo.Type = (PictureType)_entity.Type;
            bo.DateTimeUploaded = _entity.Datetimeuploaded;
            bo.FacebookIdCopro = _entity.FacebookIdCopro;
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(ProjectPictures _entity, ProjectPictureBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            _entity.Caption = bo.Caption;
            _entity.ProjectId = bo.ProjectId;
            _entity.Type = (int)bo.Type;
            _entity.Datetimeuploaded = bo.DateTimeUploaded;
            _entity.FacebookIdCopro = bo.FacebookIdCopro;
            return ErrorCode.Success;
        }
    }
}
