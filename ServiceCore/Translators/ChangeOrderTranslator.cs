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
    public class ChangeOrderTranslator
    {
        public static ErrorCode TranslateEntityToBO(ChangeOrder _entity, ChangeOrderBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.ClientAccountID = _entity.ClientAccount.Id;
            bo.Description = _entity.Description;
            bo.ChangeOrderDate = _entity.Date;
            bo.ExpirationDate = _entity.ExpirationDate;
            bo.Comment = _entity.Comment;
            bo.DateSendToClient = _entity.DateSendToClient;
            bo.DateAgreement = _entity.DateAgreement;
            bo.Invoiceable = _entity.Invoiceable;
            bo.ContractActivityID = _entity.ContractActivity.Id;
            bo.ProjectId = _entity.ContractActivity.Contract.ProjectId;
            bo.ChangeOrderConditions = _entity.ChangeOrderConditions;
            if (_entity.ClientAccount.Name != null)
                bo.ClientName = _entity.ClientAccount.Name;
            else
                bo.ClientName = _entity.ClientAccount.CompanyName;
            if (_entity.ChangeOrderDetail != null)
            {
                foreach (var item in _entity.ChangeOrderDetail)
                {
                    ChangeOrderDetailBO detail = new ChangeOrderDetailBO();
                    var err = TranslateDetailEntityToBO(item, detail);
                    if (err != ErrorCode.Success)
                        return err;
                    bo.Details.Add(detail);
                }
            }


            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(ChangeOrder _entity, ChangeOrderBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.ClientAccountId = bo.ClientAccountID;
            _entity.Description = bo.Description;
            _entity.Date = bo.ChangeOrderDate;
            _entity.ExpirationDate = bo.ExpirationDate;
            _entity.Comment = bo.Comment;
            _entity.DateSendToClient = bo.DateSendToClient;
            _entity.DateAgreement = bo.DateAgreement;
            _entity.Invoiceable = bo.Invoiceable;
            _entity.ContractActivityId = bo.ContractActivityID;
            _entity.ChangeOrderConditions = bo.ChangeOrderConditions;

            var err = HandleDetails(_entity, bo.Details, uow);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }
        public static ErrorCode TranslateDetailEntityToBO(ChangeOrderDetail _entity, ChangeOrderDetailBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.ChangeOrderID = _entity.ChangeOrder.Id;
            bo.Description = _entity.Description;
            bo.MeasurementType = (MeasurementType)_entity.MeasurementType;
            bo.MeasurementUnit = (MeasurementUnit)_entity.MeasurementUnit;
            bo.Number = _entity.Number;
            bo.Price = _entity.Price;
            bo.Commision = _entity.Commission;
            if (!_entity.Invoicable == null)
                bo.Invoicable = _entity.Invoicable;
            if (!_entity.Invoiced == null)
                bo.Invoiced = _entity.Invoiced;
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateDetailBOToEntity(ChangeOrderDetail _entity, ChangeOrderDetailBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.ChangeOrderId = bo.ChangeOrderID;
            _entity.Description = bo.Description;
            _entity.MeasurementType = (int)bo.MeasurementType;
            _entity.MeasurementUnit = (int)bo.MeasurementUnit;
            _entity.Number = bo.Number;
            _entity.Price = bo.Price;
            _entity.Commission = bo.Commision;
            _entity.Invoicable = bo.Invoicable;
            _entity.Invoiced = bo.Invoiced;
            return ErrorCode.Success;
        }

        private static ErrorCode HandleDetails(ChangeOrder _entity, List<ChangeOrderDetailBO> details, UnitOfWork uow)
        {
            if ((details == null))
                return ErrorCode.Success;
            if ((details.Count == 0))
                return ErrorCode.Success;
            foreach (var x in details)
            {
                ErrorCode err;
                if ((x.Id == 0))
                {
                    // insert
                    ChangeOrderDetail detail = new ChangeOrderDetail();
                    err = TranslateDetailBOToEntity(detail, x, uow);
                    if (err != ErrorCode.Success)
                        return err;
                    _entity.ChangeOrderDetail.Add(detail);
                }
                else
                {
                    // update
                    var detail = _entity.ChangeOrderDetail.FirstOrDefault(f => f.Id == x.Id);
                    if ((detail != null))
                        err = TranslateDetailBOToEntity(detail, x, uow);
                }
            }
            // delete
            List<ChangeOrderDetail> delList = new List<ChangeOrderDetail>();
            foreach (var x in _entity.ChangeOrderDetail)
            {
                if ((!details.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.ChangeOrderDetail.Remove(x);
            return ErrorCode.Success;
        }
    }
}
