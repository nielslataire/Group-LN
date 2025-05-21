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
    public class CompanyTranslator
    {
        internal static ErrorCode TranslateEntityToBO(CompanyInfo _entity, CompanyBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Bedrijfsnaam = _entity.BedrijfsNaam;
            bo.Busnummer = _entity.Busnummer;
            bo.CompanyId = _entity.CompanyId;
            bo.Email = _entity.Email;
            bo.GSM = _entity.Gsm;
            bo.Huisnummer = _entity.Huisnummer;
            bo.Straat = _entity.Straat;
            bo.Telefoon1 = _entity.Telefoon1;
            bo.Telefoon2 = _entity.Telefoon2;
            bo.Toevoeging = _entity.Toevoeging;
            bo.Opmerking = _entity.Opmerkingen;
            bo.URL = _entity.Weburl;
            bo.OndNr = _entity.Ondernemingsnummer;
            if ((_entity.PostCode != null))
            {
                bo.Postcode.Postcode = _entity.PostCode.Postcode;
                bo.Postcode.Gemeente = _entity.PostCode.Gemeente;
                bo.Postcode.PostcodeId = _entity.PostCode.PostcodeId;
                if (_entity.PostCode.Country != null)
                {
                    bo.Postcode.Country.Name = _entity.PostCode.Country.LandNaam;
                    bo.Postcode.Country.CountryId = _entity.PostCode.Country.Id;
                    bo.Postcode.Country.ISOCode = _entity.PostCode.Country.LandIsocode;
                }
                if (_entity.PostCode.Provincie != null)
                {
                    bo.Postcode.Provincie.Name = _entity.PostCode.Provincie.ProvincieName;
                    bo.Postcode.Provincie.ProvincieId = _entity.PostCode.Provincie.ProvincieId;
                }
            }

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
                if ((x.Department != null))
                    contact.Department = x.Department.GetIdName();
                bo.Contacts.Add(contact);
            }
            foreach (var x in _entity.Activity)
            {
                ActivityBO activity = new ActivityBO();
                activity.ID = x.ActivityId;
                activity.Name = x.Omschrijving;
                // activity.Group.Name = x.ActivityGroup.Name
                // activity.Group.ID = x.ActivityGroup.GroupID
                // activity.Group.Lot = x.ActivityGroup.Lot
                bo.Activities.Add(activity);
            }
            foreach (var x in _entity.CompanyDepartments)
            {
                DepartmentBO department = new DepartmentBO();
                department.ID = x.DepartmentId;
                department.Name = x.Naam;
                department.Street = x.Straat;
                department.Phone = x.Telefoon;
                department.CellPhone = x.Gsm;
                department.Email = x.Email;
                department.Housenumber = x.Huisnummer;
                department.Postalcode.PostcodeId = x.PostcodeId;
                department.Postalcode.Postcode = x.Postcode.Postcode;
                department.Postalcode.Country.Name = x.Postcode.Country.LandNaam;
                department.Postalcode.Gemeente = x.Postcode.Gemeente;
                bo.Departments.Add(department);
            }

            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(CompanyInfo _entity, CompanyBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            _entity.BedrijfsNaam = bo.Bedrijfsnaam;
            _entity.Busnummer = bo.Busnummer;
            // _entity.CompanyID = bo.CompanyId
            _entity.Email = bo.Email;
            if (bo.GSM is not null)
                _entity.Gsm = Regex.Replace(bo.GSM, "[^0-9]", "");
            _entity.Huisnummer = bo.Huisnummer;
            _entity.Straat = bo.Straat;
            if (bo.Telefoon1 is not null)
                _entity.Telefoon1 = Regex.Replace(bo.Telefoon1, "[^0-9]", "");
            if (bo.Telefoon2 is not null)
                _entity.Telefoon2 = Regex.Replace(bo.Telefoon2, "[^0-9]", "");
            _entity.Toevoeging = bo.Toevoeging;
            _entity.Opmerkingen = bo.Opmerking;
            _entity.Weburl = bo.URL;
            if (bo.OndNr is not null)
                _entity.Ondernemingsnummer = Regex.Replace(bo.OndNr, "[^0-9]", "");
            if (bo.RegNr is not null)
                _entity.RegistratieNr = Regex.Replace(bo.RegNr, "[^0-9]", "");

            if ((bo.Postcode != null))
                _entity.PostCodeId = bo.Postcode.PostcodeId;
            var err = HandleContacts(_entity, bo.Contacts);
            if ((err != ErrorCode.Success))
                return err;
            err = HandleDepartmens(_entity, bo.Departments);
            if ((err != ErrorCode.Success))
                return err;
            err = HandleActivities(_entity, bo.Activities, uow);
            if ((err != ErrorCode.Success))
                return err;

            return ErrorCode.Success;
        }

        private static ErrorCode HandleContacts(CompanyInfo _entity, List<ContactBO> contacts)
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
                    if ((x.Department != null & x.Department.ID > 0))
                        contact.DepartmentId = x.Department.ID;

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
                        if ((x.Department != null))
                            contact.DepartmentId = x.Department.ID;
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
        private static ErrorCode HandleDepartmens(CompanyInfo _entity, List<DepartmentBO> departments)
        {
            if ((departments.Count == 0))
                return ErrorCode.Success;
            foreach (var x in departments)
            {
                if ((x.ID == 0))
                {
                    // insert
                    CompanyDepartments department = new CompanyDepartments();
                    var err = DepartmentTranslator.TranslateBOToEntity(department, x);
                    if ((err != ErrorCode.Success))
                        return err;
                    _entity.CompanyDepartments.Add(department);
                }
                else
                {
                    // update
                    var department = _entity.CompanyDepartments.FirstOrDefault(f => f.DepartmentId == x.ID);
                    if ((department != null))
                    {
                        var err = DepartmentTranslator.TranslateBOToEntity(department, x);
                        if ((err != ErrorCode.Success))
                            return err;
                    }
                }
            }
            // delete
            List<CompanyDepartments> delList = new List<CompanyDepartments>();
            foreach (var x in _entity.CompanyDepartments)
            {
                if ((!departments.Any(f => f.ID == x.DepartmentId)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.CompanyDepartments.Remove(x);
            return ErrorCode.Success;
        }
        private static ErrorCode HandleActivities(CompanyInfo _entity, List<ActivityBO> activities, UnitOfWork uow)
        {
            if ((activities.Count == 0))
                return ErrorCode.Success;
            foreach (var x in activities)
            {
                if ((x.ID == 0))
                {
                }
                else
                   // add the activity to the company
                   if ((!_entity.Activity.Any(m => m.ActivityId == x.ID)))
                {
                    var act = uow.GetActivityDAO().GetById(x.ID);
                    _entity.Activity.Add(act);
                }
            }
            // delete
            List<Activity> delList = new List<Activity>();
            foreach (var x in _entity.Activity)
            {
                if ((!activities.Any(f => f.ID == x.ActivityId)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.Activity.Remove(x);
            return ErrorCode.Success;
        }
    }
}
