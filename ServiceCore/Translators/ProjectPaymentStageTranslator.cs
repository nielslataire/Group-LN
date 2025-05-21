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
    public class ProjectPaymentStageTranslator
    {
        public static ErrorCode TranslateEntityToBO(InvoicingPaymentStages _entity, ProjectPaymentStageBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            bo.GroupId = _entity.GroupId;
            bo.Invoicable = _entity.Invoicable;
            bo.Percentage = _entity.Percentage;
            bo.VatPercentage = _entity.Group.VatPercentage;
            bo.InvoiceCount = _entity.InvoicesDetails.Count;
            bo.GroupName = _entity.Group.Name;
            if (_entity.Doc is not null)
            {
                ProjectDocBO doc = new ProjectDocBO();
                bo.Doc = doc;
                ProjectDocsTranslator.TranslateEntityToBO(_entity.Doc, bo.Doc);
            }
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(InvoicingPaymentStages _entity, ProjectPaymentStageBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            _entity.GroupId = bo.GroupId;
            _entity.Invoicable = bo.Invoicable;
            _entity.Percentage = bo.Percentage;
            if (bo.Doc is not null)
            {
                if (bo.Doc.Docid == 0)
                {
                    ProjectDocs doc = new ProjectDocs();
                    ProjectDocsTranslator.TranslateBOToEntity(doc, bo.Doc, uow);
                    _entity.Doc = doc;
                }
                else
                    _entity.DocId = bo.Doc.Docid;
            }
            else
                _entity.DocId = null;
            return ErrorCode.Success;
        }
    }
}
