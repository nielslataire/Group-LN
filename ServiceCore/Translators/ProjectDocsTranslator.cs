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
        internal static ErrorCode TranslateBOToEntity(ProjectDocs _entity, ProjectDocBO bo, UnitOfWorkCore uow)
        {
            if (_entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;

            _entity.Name = bo.Name;
            _entity.ProjectId = bo.ProjectId;
            // FK nullable maken als er geen echte waarde is
            if (bo.ClientAccountId is int v && v > 0)
                _entity.ClientAccountId = v;
            else
                _entity.ClientAccountId = null;
            _entity.SortOrder = bo.SortOrder;
            _entity.Type = (int?)bo.Type;        // entity.Type is int? in jouw screenshot
            _entity.Date = bo.DocDate;

            // >>> Alleen zetten als we een waarde hebben
            if (!string.IsNullOrWhiteSpace(bo.Filename))
                _entity.Filename = bo.Filename;

            return ErrorCode.Success;
        }
    }
}
