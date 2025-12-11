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
    public class ProjectSalesSettingsTranslator
    {
        public static ErrorCode TranslateEntityToBO(ProjectSalesSettings _entity, ProjectSalesSettingsBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.SettingsId = _entity.Id;
            bo.ProjectId = _entity.Projectid;
            bo.MixedVatRegistration = _entity.MixedVatregistration;
            bo.SaleVisible = _entity.SaleVisible;
            if (_entity.ConnectionFees is not null)
                bo.ConnectionFees = _entity.ConnectionFees;
            if (_entity.Vatpercentage is not null)
                bo.VatPercentage = _entity.Vatpercentage;
            if (_entity.RegistrationPercentage is not null)
                bo.RegistrationPercentage = _entity.RegistrationPercentage;
            if (_entity.BaseCertificateCost is not null)
                bo.BaseCertificateCost = _entity.BaseCertificateCost;
            if (_entity.FixedCertificateCost is not null)
                bo.FixedCertificateCost = _entity.FixedCertificateCost;
            if (_entity.MortageRegistrationCost is not null)
                bo.MortageRegistrationCost = _entity.MortageRegistrationCost;
            if (_entity.BankAccountId is not null)
                bo.BankAccountId = _entity.BankAccountId;

            var iban = _entity.BankAccountNumber ?? _entity.BankAccount?.Iban;
            if (!string.IsNullOrWhiteSpace(iban))
                bo.BankAccountNumber = iban;
            bo.RegistrationType = (RegistrationType)_entity.RegistrationType;
            if (_entity.SurveyorCost is not null)
                bo.SurveyorCost = _entity.SurveyorCost;
            if (_entity.ParcelCost is not null)
                bo.ParcelCost = _entity.ParcelCost;

            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ProjectSalesSettings _entity, ProjectSalesSettingsBO bo, UnitOfWorkCore uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Projectid = bo.ProjectId;
            _entity.MixedVatregistration = bo.MixedVatRegistration;
            _entity.Vatpercentage = bo.VatPercentage;
            _entity.ConnectionFees = bo.ConnectionFees;
            _entity.RegistrationPercentage = bo.RegistrationPercentage;
            _entity.BaseCertificateCost = bo.BaseCertificateCost;
            _entity.FixedCertificateCost = bo.FixedCertificateCost;
            _entity.MortageRegistrationCost = bo.MortageRegistrationCost;
            _entity.BankAccountNumber = bo.BankAccountNumber;
            _entity.BankAccountId = bo.BankAccountId;
            _entity.SaleVisible = bo.SaleVisible;
            _entity.RegistrationType = (int)bo.RegistrationType;
            _entity.SurveyorCost = bo.SurveyorCost;
            _entity.ParcelCost = bo.ParcelCost;

            return ErrorCode.Success;
        }
    }
}
