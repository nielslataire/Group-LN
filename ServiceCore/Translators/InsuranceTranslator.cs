using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;
using DALCore;

namespace ServiceCore.Translators
{
    public class InsuranceTranslator
    {
        internal static ErrorCode TranslateEntityToBO(Insurances _entity, InsuranceBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            // Algemene gegevens
            bo.Id = _entity.Id;
            bo.ExtensionPeriod = _entity.ExtensionPeriod;
            bo.GuaranteePeriod = _entity.GuaranteePeriod;
            bo.ContractActivityID = _entity.ContractActivityId;
            bo.InsuranceBrokerName = _entity.ContractActivity.Contract.Company.BedrijfsNaam;
            if (_entity.InsuranceCompany is not null)
            {
                var err = InsuranceCompanyTranslator.TranslateEntityToBO(_entity.InsuranceCompany, bo.InsuranceCompany);
                if ((err != ErrorCode.Success))
                    return err;
            }
            bo.Period = _entity.Period;
            bo.ProjectID = _entity.ContractActivity.Contract.ProjectId;
            bo.Startdate = _entity.Startdate;
            bo.Type = (InsuranceType)_entity.Type;
            if (_entity.Enddate is not null)
                bo.Enddate = _entity.Enddate;

            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(Insurances _entity, InsuranceBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            if (bo.InsuranceCompany.Id != 0)
                _entity.InsuranceCompanyId = bo.InsuranceCompany.Id;
            if (bo.ContractActivityID != 0)
                _entity.ContractActivityId = bo.ContractActivityID;
            _entity.ExtensionPeriod = bo.ExtensionPeriod;
            _entity.GuaranteePeriod = bo.GuaranteePeriod;
            _entity.Period = bo.Period;
            _entity.Startdate = bo.Startdate;
            if (bo.Type != 0)
                _entity.Type = (int)bo.Type;
            _entity.Enddate = bo.Enddate;


            return ErrorCode.Success;
        }
    }
}
