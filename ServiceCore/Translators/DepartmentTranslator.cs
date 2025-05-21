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
    public class DepartmentTranslator
    {
        public static ErrorCode TranslateEntityToBO(CompanyDepartments _entity, DepartmentBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.ID = _entity.DepartmentId;
            bo.Name = _entity.Naam;
            bo.Busnumber = _entity.Bus;
            bo.CellPhone = _entity.Gsm;
            bo.Email = _entity.Email;
            bo.Housenumber = _entity.Huisnummer;
            bo.Phone = _entity.Telefoon;
            bo.Street = _entity.Straat;
            if (_entity.Postcode != null)
            {
                var err = PostalcodeTranslator.TranslateEntityToBO(_entity.Postcode, bo.Postalcode);
                if (err != ErrorCode.Success)
                    return err;
            }

            if ((_entity.Company != null))
                bo.Company = _entity.Company.GetIdName();
            foreach (var x in _entity.CompanyContacts)
            {
                ContactBO contact = new ContactBO();
                contact.Id = x.ContactId;
                if (x.ContactNaam == "")
                    contact.Weergavenaam1 = x.ContactVoornaam;
                else if (x.ContactVoornaam == "")
                    contact.Weergavenaam1 = x.ContactNaam;
                else
                    contact.Weergavenaam1 = x.ContactNaam + " " + x.ContactVoornaam;
                contact.Weergavenaam2 = x.Company.BedrijfsNaam;
                contact.Salutation = x.Aanspreking;

                contact.JobFunction = x.Functie;
                contact.Phone = x.Telefoon;
                contact.CellPhone = x.Gsm;
                contact.Email = x.Email;
                contact.Firstname = x.ContactVoornaam;
                contact.Name = x.ContactNaam;
                bo.Contacts.Add(contact);
            }

            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(CompanyDepartments _entity, DepartmentBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.Naam = bo.Name;
            _entity.Bus = bo.Busnumber;
            if (bo.CellPhone is not null)
                _entity.Gsm = Regex.Replace(bo.CellPhone, "[^0-9]", "");
            _entity.Email = bo.Email;
            _entity.Huisnummer = bo.Housenumber;
            if (bo.Phone is not null)
                _entity.Telefoon = Regex.Replace(bo.Phone, "[^0-9]", "");
            _entity.Straat = bo.Street;
            if (bo.Postalcode != null && bo.Postalcode.PostcodeId != 0)
                _entity.PostcodeId = bo.Postalcode.PostcodeId;
            else
                _entity.PostcodeId = null;
            //if (bo.Company != null && bo.Company.ID != 0)
                _entity.CompanyId = bo.Company.ID;
            //else
            //    _entity.CompanyId = 0;

            return ErrorCode.Success;
            var err = HandleContacts(_entity, bo.Contacts);
            if ((err != ErrorCode.Success))
                return err;
        }
        private static ErrorCode HandleContacts(CompanyDepartments _entity, List<ContactBO> contacts)
        {
            if ((contacts.Count == 0))
                return ErrorCode.Success;
            foreach (var x in contacts)
            {
                if ((x.Id == 0))
                {
                    // insert
                    CompanyContacts contact = new CompanyContacts();
                    contact.ContactNaam = x.Name;
                    contact.Aanspreking = x.Salutation;
                    contact.ContactVoornaam = x.Firstname;
                    contact.Functie = x.JobFunction;
                    contact.Telefoon = x.Phone;
                    contact.Gsm = x.CellPhone;
                    contact.Email = x.Email;
                    contact.CompanyId = x.Company.ID;
                    _entity.CompanyContacts.Add(contact);
                }
                else
                {
                    // update
                    var contact = _entity.CompanyContacts.FirstOrDefault(f => f.ContactId == x.Id);
                    if ((contact != null))
                    {
                        contact.ContactNaam = x.Name;
                        contact.Aanspreking = x.Salutation;
                        contact.ContactVoornaam = x.Firstname;
                        contact.Functie = x.JobFunction;
                        contact.Telefoon = x.Phone;
                        contact.Gsm = x.CellPhone;
                        contact.Email = x.Email;
                    }
                }
            }
            // delete
            List<CompanyContacts> delList = new List<CompanyContacts>();
            foreach (var x in _entity.CompanyContacts)
            {
                if ((!contacts.Any(f => f.Id == x.ContactId)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.CompanyContacts.Remove(x);
            return ErrorCode.Success;
        }
    }
}
