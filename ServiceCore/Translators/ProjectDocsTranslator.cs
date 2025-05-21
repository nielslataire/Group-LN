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
    public class ProjectDocsTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ProjectDocs _entity, ProjectDocBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Docid = _entity.Id;
            bo.Name = _entity.Name;
            bo.ProjectId = _entity.ProjectId;
            bo.ClientAccountId = _entity.ClientAccountId;
            bo.Filename = _entity.Filename;
            bo.SortOrder = _entity.SortOrder;
            bo.Type = (ProjectDocType)_entity.Type;
            bo.DocDate = _entity.Date;

            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(ProjectDocs _entity, ProjectDocBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            _entity.Filename = bo.Filename;
            _entity.ProjectId = bo.ProjectId;
            _entity.ClientAccountId = bo.ClientAccountId;
            _entity.SortOrder = bo.SortOrder;
            _entity.Type = (int)bo.Type;
            _entity.Date = bo.DocDate;
            return ErrorCode.Success;
        }
    }
}
