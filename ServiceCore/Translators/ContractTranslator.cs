using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class ContractTranslator
    {
        public static ErrorCode TranslateEntityToBO(Contract _entity, ContractBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.ProjectId = _entity.ProjectId;
            if (_entity.Company is not null)
            {
                bo.Company.ID = _entity.Company.CompanyId;
                bo.Company.Display = _entity.Company.BedrijfsNaam;
            }
            bo.CashDiscount = _entity.CashDiscount;
            bo.CashDiscountPaymentTerm = _entity.CashDiscountPaymentTerm;
            bo.CashDiscountPercentage = _entity.CashDiscountPercentage;
            bo.PaymentTerm = _entity.PaymentTerm;
            bo.VatPercentage = _entity.VatPercentage;
            bo.GuaranteeType = (ContractGuaranteeType)_entity.GuaranteeType;
            bo.GuaranteePercentage = _entity.GuaranteePercentage;
            bo.ContractSigned = _entity.ContractSigned ?? false;
            bo.ContractSentDate = _entity.ContractSentDate;
            bo.ContractSentNote = _entity.ContractSentNote;
            bo.SiteNotification = _entity.SiteNotification;
            bo.VgmCharter = _entity.VgmCharter;
            bo.PidAttest = _entity.PidAttest;
            bo.SiteManagerContactId = _entity.SiteManagerContactId;
            bo.ContractName = _entity.ContractName;
            if (_entity.ContractActivity != null)
            {
                foreach (var item in _entity.ContractActivity)
                {
                    ContractActivityBO activity = new ContractActivityBO();
                    var err = ContractActivityTranslator.TranslateEntityToBO(item, activity);
                    if (err != ErrorCode.Success)
                        return err;
                    bo.Activities.Add(activity);
                }
            }
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(Contract _entity, ContractBO bo, UnitOfWorkCore uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.ProjectId = bo.ProjectId;
            _entity.CashDiscount = bo.CashDiscount;
            _entity.CashDiscountPaymentTerm = bo.CashDiscountPaymentTerm;
            _entity.CashDiscountPercentage = bo.CashDiscountPercentage;
            _entity.CompanyId = bo.Company.ID;
            _entity.PaymentTerm = bo.PaymentTerm;
            _entity.VatPercentage = bo.VatPercentage;
            _entity.ContractSigned = bo.ContractSigned;
            _entity.ContractSentDate = bo.ContractSentDate;
            _entity.ContractSentNote = string.IsNullOrWhiteSpace(bo.ContractSentNote) ? null : bo.ContractSentNote.Trim();
            _entity.SiteNotification = bo.SiteNotification;
            _entity.VgmCharter = bo.VgmCharter;
            _entity.PidAttest = bo.PidAttest;
            _entity.GuaranteePercentage = bo.GuaranteePercentage;
            _entity.GuaranteeType = (int)bo.GuaranteeType;
            _entity.SiteManagerContactId = bo.SiteManagerContactId;
            _entity.ContractName = string.IsNullOrWhiteSpace(bo.ContractName) ? null : bo.ContractName.Trim();
            var Err = HandleActivities(_entity, bo.Activities, uow);
            if ((Err != ErrorCode.Success))
                return Err;
            return ErrorCode.Success;
        }
        private static ErrorCode HandleActivities(Contract _entity, List<ContractActivityBO> activities, UnitOfWorkCore uow)
        {
            if ((activities == null))
                return ErrorCode.Success;
            if ((activities.Count == 0))
                return ErrorCode.Success;
            foreach (var x in activities)
            {
                ErrorCode err;
                if ((x.ContractActivityId == 0))
                {
                    // insert
                    ContractActivity activity = new ContractActivity();
                    err = ContractActivityTranslator.TranslateBOToEntity(activity, x, uow);
                    if (err != ErrorCode.Success)
                        return err;
                    _entity.ContractActivity.Add(activity);
                }
                else
                {
                    // update
                    var activity = _entity.ContractActivity.FirstOrDefault(f => f.Id == x.ContractActivityId);
                    if ((activity != null))
                        err = ContractActivityTranslator.TranslateBOToEntity(activity, x, uow);
                }
            }
            // delete
            List<ContractActivity> delList = new List<ContractActivity>();
            foreach (var x in _entity.ContractActivity)
            {
                if ((!activities.Any(f => f.ContractActivityId == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.ContractActivity.Remove(x);
            return ErrorCode.Success;
        }
    }
}
