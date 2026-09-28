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
            // _entity.Type is int? (documenten-module 17a-e: generieke ProjectDocs-rijen hebben vaak
            // geen classificatie) — bo.Type is een niet-nullable enum, dus rechtstreeks casten crashte
            // met "Nullable object must have a value" zodra Type niet ingevuld is (bv. Klanten/Detail's
            // GetLatestClientDocs). 0 is geen benoemde ProjectDocType-waarde, maar wel exact de impliciete
            // standaardwaarde van het VB-veld zelf (Private _type As ProjectDocType, geen initializer).
            bo.Type = (ProjectDocType)(_entity.Type ?? 0);
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
