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
    public class UnitTranslator
    {
        public static ErrorCode TranslateEntityToBO(Units _entity, UnitBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            bo.ProjectId = _entity.ProjectId;
            if(_entity.Project is not null)
            {
                bo.ProjectName = _entity.Project.ProjectName;
            }
            bo.Street = _entity.Street;
            bo.HouseNumber = _entity.Housenumber;
            bo.BusNumber = _entity.Busnumber;
            bo.PreKad = _entity.PreKad;
            bo.IsLink = _entity.IsLink;
            bo.Plan = _entity.Plan;
            if (_entity.Surface is not null)
                bo.Surface = _entity.Surface;
            if (_entity.GroundSurface is not null)
                bo.GroundSurface = _entity.GroundSurface;
            if (_entity.Level is not null)
                bo.Level = _entity.Level;
            if (_entity.ClientAccount is not null)
                bo.ClientAccountId = _entity.ClientAccountId;
            if (_entity.ConstructionValueSold is not null)
                bo.ConstructionValueSold = _entity.ConstructionValueSold;
            if (_entity.LandValueSold is not null)
                bo.LandValueSold = _entity.LandValueSold;
            if (_entity.ConstructionValue is not null)
                bo.ConstructionValue = _entity.ConstructionValue;
            if (_entity.LandValue is not null)
                bo.LandValue = _entity.LandValue;
            if (_entity.AttachedUnit is not null)
                bo.AttachedUnitsId = _entity.AttachedUnit.Id;
            if (_entity.PaymentGroup is not null)
                bo.PaymentGroupId = _entity.PaymentGroup.Id;
            if (_entity.Landshare is not null)
                bo.Landshare = _entity.Landshare;
            if ((_entity.Type is not null))
            {
                UnitTypeBO UnitType = new UnitTypeBO();
                var err = UnitTypeTranslator.TranslateEntityToBO(_entity.Type, UnitType);
                if ((err != ErrorCode.Success))
                    return err;
                bo.Type = UnitType;
            }

            foreach (var x in _entity.InverseLinkedUnit)
            {
                UnitBO bou = new UnitBO();
                var err = TranslateEntityToBO(x, bou);
                bo.LinkedUnits.Add(bou);
            }
            bo.LinkedUnits = bo.LinkedUnits.OrderBy(m => m.Name).ToList();
            foreach (var x in _entity.UnitConstructionValue)
            {
                UnitConstructionValueBO bou = new UnitConstructionValueBO();
                var err = UnitConstructionValueTranslator.TranslateEntityToBO(x, bou);
                bo.ConstructionValues.Add(bou);
            }
            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(Units _entity, UnitBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Name = bo.Name;
            _entity.ProjectId = bo.ProjectId;
            _entity.Landshare = bo.Landshare;
            _entity.Street = bo.Street;
            _entity.Housenumber = bo.HouseNumber;
            _entity.Busnumber = bo.BusNumber;
            _entity.PreKad = bo.PreKad;
            _entity.ConstructionValueSold = bo.ConstructionValueSold;
            _entity.LandValueSold = bo.LandValueSold;
            _entity.ConstructionValue = bo.ConstructionValue;
            _entity.LandValue = bo.LandValue;
            _entity.IsLink = bo.IsLink;
            _entity.Surface = bo.Surface;
            _entity.GroundSurface = bo.GroundSurface;
            _entity.Level = bo.Level;
            _entity.Plan = bo.Plan;
            // _entity.AttachedUnit_attachedunit.Id = bo.AttachedUnitsId
            if (bo.ClientAccountId is not 0)
                _entity.ClientAccountId = bo.ClientAccountId;

            if ((bo.Type != null))
            {
                if (bo.Type.Id != 0)
                    _entity.TypeId = bo.Type.Id;
            }

            // handle attached unit
            if ((bo.AttachedUnitsId == 0 | bo.AttachedUnitsId == null))
                // should never happen
                _entity.AttachedUnitId = null;
            else
                // add the activity to the company
                if ((_entity.AttachedUnit == null))
            {
                var unit = uow.GetUnitsDAO().GetById(bo.AttachedUnitsId.Value);
                _entity.AttachedUnit = unit;
            }
            else if ((_entity.AttachedUnit.Id != bo.AttachedUnitsId))
            {
                var unit = uow.GetUnitsDAO().GetById(bo.AttachedUnitsId.Value);
                _entity.AttachedUnit = unit;
            }
            // Handle Payment Group
            if ((bo.PaymentGroupId == 0 | bo.PaymentGroupId == null))
                // should never happen
                _entity.PaymentGroupId = null;
            else
                // add paymentgroup to unit
                if ((_entity.PaymentGroup == null))
            {
                var paymentgroup = uow.GetProjectPaymentGroupsDAO().GetById(bo.PaymentGroupId.Value);
                _entity.PaymentGroup = paymentgroup;
            }
            else if ((_entity.PaymentGroup.Id != bo.PaymentGroupId))
            {
                var paymentgroup = uow.GetProjectPaymentGroupsDAO().GetById(bo.PaymentGroupId.Value);
                _entity.PaymentGroup = paymentgroup;
            }
            var Err = HandleLinkedUnits(_entity, bo.LinkedUnits, uow);
            if ((Err != ErrorCode.Success))
                return Err;
            _entity.InverseLinkedUnit = _entity.InverseLinkedUnit.OrderBy(m => m.Name).ToList();
            if (_entity.InverseLinkedUnit.Count != 0)
            {
                _entity.Name = "";
                _entity.Landshare = _entity.InverseLinkedUnit.Sum(m => m.Landshare);
                _entity.TypeId = _entity.InverseLinkedUnit.First().TypeId;
                _entity.LevelId = _entity.InverseLinkedUnit.First().LevelId;
                foreach (var unit in _entity.InverseLinkedUnit)
                {
                    _entity.Name = _entity.Name + unit.Name;
                    if (unit != _entity.InverseLinkedUnit.Last())
                        _entity.Name = _entity.Name + " - ";
                }
            }
            // Dim Err2 = HandleConstructionValues(_entity, bo.ConstructionValues, uow)
            // If (Err2 <> ErrorCode.Success) Then Return Err
            return ErrorCode.Success;
        }
        private static ErrorCode HandleAttachedUnit(Units _entity, int unitid, UnitOfWork uow)
        {
            // If (unitids Is Nothing) Then Return ErrorCode.Success
            // If (unitids.Count = 0) Then Return ErrorCode.Success

            if ((unitid == 0))
                // should never happen
                _entity.AttachedUnit = null;
            else
                // add the activity to the company
                if ((_entity.AttachedUnit == null))
            {
                var unit = uow.GetUnitsDAO().GetById(unitid);
                _entity.AttachedUnit = unit;
            }
            else if ((_entity.AttachedUnit.Id != unitid))
            {
                var unit = uow.GetUnitsDAO().GetById(unitid);
                _entity.AttachedUnit = unit;
            }

            // delete
            // Dim delList As New List(Of Units)
            // For Each x In _entity.AttachedUnits_Attachedunit
            // If (Not unitids.Any(Function(f) f = x.Id)) Then
            // delList.Add(x)
            // End If
            // Next
            // For Each x In delList
            // _entity.AttachedUnits_Attachedunit.Remove(x)
            // Next
            return ErrorCode.Success;
        }
        private static ErrorCode HandleLinkedUnits(Units _entity, List<UnitBO> linkedunits, UnitOfWork uow)
        {
            if ((linkedunits == null))
                return ErrorCode.Success;
            // If (unitids.Count = 0) Then Return ErrorCode.Success
            foreach (var x in linkedunits)
            {
                if ((x.Id == 0))
                {
                }
                else
                   // add the activity to the company
                   if ((!_entity.InverseLinkedUnit.Any(m => m.Id == x.Id)))
                {
                    var unit = uow.GetUnitsDAO().GetById(x.Id);
                    _entity.InverseLinkedUnit.Add(unit);
                }
            }
            // delete
            List<Units> delList = new List<Units>();
            foreach (var x in _entity.InverseLinkedUnit)
            {
                if ((!linkedunits.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.InverseLinkedUnit.Remove(x);
            return ErrorCode.Success;
        }
        private static ErrorCode HandleConstructionValues(Units _entity, List<UnitConstructionValueBO> constructionvalues, UnitOfWork uow)
        {
            if ((constructionvalues == null))
                return ErrorCode.Success;
            // If (unitids.Count = 0) Then Return ErrorCode.Success
            foreach (var x in constructionvalues)
            {
                if ((x.Id == 0))
                {
                }
                else
                   // add the constructionvalue to the unit
                   if ((!_entity.UnitConstructionValue.Any(m => m.Id == x.Id)))
                {
                    var constructionvalue = uow.GetUnitConstructionValuesDAO().GetById(x.Id);
                    _entity.UnitConstructionValue.Add(constructionvalue);
                }
            }
            // delete
            List<UnitConstructionValue> delList = new List<UnitConstructionValue>();
            foreach (var x in _entity.UnitConstructionValue)
            {
                if ((!constructionvalues.Any(f => f.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.UnitConstructionValue.Remove(x);
            return ErrorCode.Success;
        }
    }
}
