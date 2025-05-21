using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;
using DALCore;

namespace ServiceCore.Translators
{
    public class ContactTranslator
    {
        public static ErrorCode TranslateEntityToBO(CompanyContacts _entity, ContactBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.ContactId;
            bo.CellPhone = _entity.Gsm;
            bo.Email = _entity.Email;
            bo.Firstname = _entity.ContactVoornaam;
            bo.JobFunction = _entity.Functie;
            bo.Name = _entity.ContactNaam;
            bo.Phone = _entity.Telefoon;
            bo.Salutation = _entity.Aanspreking;
            if ((_entity.Company != null))
                bo.Company = _entity.Company.GetIdName();
            if ((_entity.Department != null))
                bo.Department = _entity.Department.GetIdName();
            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(CompanyContacts _entity, ContactBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            if (bo.CellPhone is not null)
                _entity.Gsm = Regex.Replace(bo.CellPhone, "[^0-9]", "");
            _entity.Email = bo.Email;
            _entity.ContactVoornaam = bo.Firstname;
            _entity.Functie = bo.JobFunction;
            _entity.ContactNaam = bo.Name;
            if (bo.Phone is not null)
                _entity.Telefoon = Regex.Replace(bo.Phone, "[^0-9]", "");
            _entity.Aanspreking = bo.Salutation;

            //if (bo.Company != null && bo.Company.ID != 0)
                _entity.CompanyId = bo.Company.ID;
            //else
            //    _entity.CompanyId = null;
            if (bo.Department != null && bo.Department.ID != 0)
                _entity.DepartmentId = bo.Department.ID;
            else
                _entity.DepartmentId = null;

            return ErrorCode.Success;
        }
    }
}
