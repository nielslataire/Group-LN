using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;
using DALCore;
using Microsoft.EntityFrameworkCore;


namespace ServiceCore.Translators
{
    public class ProjectPaymentGroupTranslator
    {
        public static ErrorCode TranslateEntityToBO(InvoicingPaymentGroup _entity, ProjectPaymentGroupBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            bo.ProjectId = _entity.ProjectId;
            bo.VatTypeId = _entity.VatTypeId;
            bo.VatPercentage = _entity.VatType?.BasePercentage ?? _entity.VatPercentage;
            foreach (var item in _entity.InvoicingPaymentStages)
            {
                ProjectPaymentStageBO stage = new ProjectPaymentStageBO();
                var err = ProjectPaymentStageTranslator.TranslateEntityToBO(item, stage);
                if (err != ErrorCode.Success)
                    return err;
                bo.PaymentStages.Add(stage);
            }
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(InvoicingPaymentGroup _entity, ProjectPaymentGroupBO bo, UnitOfWorkCore uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            _entity.ProjectId = bo.ProjectId;
            _entity.VatTypeId = bo.VatTypeId;
            if (bo.VatTypeId.HasValue)
            {
                var vatType = uow.VatTypes.GetNoTracking().FirstOrDefault(v => v.Id == bo.VatTypeId.Value);
                _entity.VatPercentage = vatType?.BasePercentage;
            }
            else
            {
                var project = uow.Projects.GetNoTracking()
                    .Include(p => p.IssuerCompanyIdBuilderNavigation)
                    .FirstOrDefault(p => p.ProjectId == _entity.ProjectId);
                var issuerId = project?.IssuerCompanyIdBuilder;
                var defaultVatTypeId = project?.IssuerCompanyIdBuilderNavigation?.DefaultVatTypeId
                    ?? (issuerId.HasValue
                        ? uow.IssuerCompanies.GetNoTracking()
                            .Where(i => i.Id == issuerId.Value)
                            .Select(i => i.DefaultVatTypeId)
                            .FirstOrDefault()
                        : null);

                var vatTypeLookup = defaultVatTypeId.HasValue
                    ? uow.VatTypes.GetNoTracking().FirstOrDefault(v => v.Id == defaultVatTypeId.Value)
                    : issuerId.HasValue
                        ? uow.VatTypes.GetNoTracking()
                            .Where(v => v.IssuerCompanyId == issuerId.Value)
                            .OrderByDescending(v => v.BasePercentage)
                            .FirstOrDefault()
                        : null;

                if (vatTypeLookup != null)
                {
                    _entity.VatTypeId = vatTypeLookup.Id;
                    _entity.VatPercentage = vatTypeLookup.BasePercentage;
                }
                else
                {
                    _entity.VatPercentage = bo.VatPercentage;
                }
            }
            var err = HandleStages(_entity, bo.PaymentStages, uow);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        private static ErrorCode HandleStages(InvoicingPaymentGroup _entity, List<ProjectPaymentStageBO> stages, UnitOfWorkCore uow)
        {
            if ((stages == null))
                return ErrorCode.Success;
            if ((stages.Count == 0))
                return ErrorCode.Success;
            foreach (var x in stages)
            {
                if ((x.Id == 0))
                {
                    // insert
                    InvoicingPaymentStages stage = new InvoicingPaymentStages();
                    var err = ProjectPaymentStageTranslator.TranslateBOToEntity(stage, x, uow);
                    if (err != ErrorCode.Success)
                        return err;
                    _entity.InvoicingPaymentStages.Add(stage);
                }
                else
                {
                    // update
                    var stage = _entity.InvoicingPaymentStages.FirstOrDefault(f => f.Id == x.Id);
                    if ((stage != null))
                    {
                        var err = ProjectPaymentStageTranslator.TranslateBOToEntity(stage, x, uow);
                        if (err != ErrorCode.Success)
                            return err;
                    }
                }
            }
            // delete
            List<InvoicingPaymentStages> delList = new List<InvoicingPaymentStages>();
            foreach (var x in _entity.InvoicingPaymentStages)
            {
                if ((!stages.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
            {
                uow.PaymentStages.DeleteObject(x.Id);
                _entity.InvoicingPaymentStages.Remove(x);
            }
            return ErrorCode.Success;
        }
    }
}
