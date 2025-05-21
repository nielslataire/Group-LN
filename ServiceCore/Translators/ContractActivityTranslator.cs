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
    public class ContractActivityTranslator
    {
        public static ErrorCode TranslateEntityToBO(ContractActivity _entity, ContractActivityBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.ContractActivityId = _entity.Id;
            bo.ContractId = _entity.ContractId;
            ActivityBO activity = new ActivityBO();
            var err = ActivityTranslator.TranslateEntityToBO(_entity.Activity, activity);
            if (err != ErrorCode.Success)
                return err;
            bo.Activity = activity;
            bo.Price = _entity.Price;
            if (_entity.Insurances != null)
            {
                InsuranceBO insurance = new InsuranceBO();
                var err2 = InsuranceTranslator.TranslateEntityToBO(_entity.Insurances, insurance);
                if (err2 != ErrorCode.Success)
                    return err2;
                bo.InsuranceData = insurance;
            }
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ContractActivity _entity, ContractActivityBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.ContractId = bo.ContractId;
            _entity.ActivityId = bo.Activity.ID;
            _entity.Price = bo.Price;
            if (bo.Activity.ID == 142 && bo.InsuranceData != null)
            {
                Insurances insurance = new Insurances();
                var err = InsuranceTranslator.TranslateBOToEntity(insurance, bo.InsuranceData, uow);
                if (err != ErrorCode.Success)
                    return err;
                _entity.Insurances = insurance;
            }
            return ErrorCode.Success;
        }
    }
}
