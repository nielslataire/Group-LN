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
    public class ProjectNewsTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ProjectNews _entity, ProjectNewsBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.NewsDate = _entity.Date;
            bo.ProjectId = _entity.ProjectId;
            bo.TextNL = _entity.TextNl;
            bo.TitleNL = _entity.TitleNl;
            bo.Author = _entity.Author;
            if (_entity.Picture is not null)
            {
                ProjectPictureBO picture = new ProjectPictureBO();
                bo.Picture = picture;
                ProjectPictureTranslator.TranslateEntityToBO(_entity.Picture, bo.Picture);
            }
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(ProjectNews _entity, ProjectNewsBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.TitleNl = bo.TitleNL;
            _entity.TextNl = bo.TextNL;
            _entity.ProjectId = bo.ProjectId;
            _entity.Date = bo.NewsDate;
            _entity.Author = bo.Author;
            if (bo.Picture is not null)
                ProjectPictureTranslator.TranslateBOToEntity(_entity.Picture, bo.Picture, uow);
            return ErrorCode.Success;
        }
    }
}
