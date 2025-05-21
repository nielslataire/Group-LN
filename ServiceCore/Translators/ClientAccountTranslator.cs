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
    public class ClientAccountTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ClientAccount _entity, ClientAccountBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            bo.Salutation = (Salutation)Enum.Parse(typeof(Salutation), _entity.Salutation);
            bo.Street = _entity.Street;
            bo.Housenumber = _entity.Housenumber;
            bo.Busnumber = _entity.Busnumber;
            if (_entity.DateDeedOfSale is not null)
                bo.DateDeedOfSale = _entity.DateDeedOfSale;
            if (_entity.DeedOfSaleExpDate is not null)
                bo.DateDeedOfSaleExpDate = _entity.DeedOfSaleExpDate;
            bo.DateSalesAgreement = _entity.DateSalesAgreement;
            bo.DeliveryDate = _entity.DeliveryDate;
            bo.DeliveryDoc = _entity.DeliveryDoc;
            bo.VATnumber = _entity.Vatnumber;
            bo.CompanyName = _entity.CompanyName;
            bo.StartDateConstruction = _entity.StartDateConstruction;
            bo.BankAccountNumber = _entity.BankAccountNumber;
            bo.InvoiceAddress = _entity.InvoiceAddress;
            bo.InvoiceStreet = _entity.InvoiceStreet;
            bo.InvoiceHousenumber = _entity.InvoiceHousenumber;
            bo.InvoiceBusnumber = _entity.InvoiceBusnumber;
            bo.InvoiceExtra = _entity.InvoiceExtra;

            if (_entity.ExecutionDays is not null)
                bo.ExecutionDays = _entity.ExecutionDays;

            if (_entity.OwnerPercentage is not null)
                bo.OwnerPercentage = _entity.OwnerPercentage;
            if ((_entity.OwnerType is not null))
            {
                var err = ClientOwnerTypeTranslator.TranslateEntityToBO(_entity.OwnerType, bo.OwnerType);
                if (err != ErrorCode.Success)
                    return err;
            }
            if ((_entity.PostalCode != null))
            {
                bo.Postalcode.Postcode = _entity.PostalCode.Postcode;
                bo.Postalcode.Gemeente = _entity.PostalCode.Gemeente;
                bo.Postalcode.PostcodeId = _entity.PostalCode.PostcodeId;
                if (_entity.PostalCode.Country != null)
                {
                    bo.Postalcode.Country.Name = _entity.PostalCode.Country.LandNaam;
                    bo.Postalcode.Country.CountryId = _entity.PostalCode.Country.Id;
                    bo.Postalcode.Country.ISOCode = _entity.PostalCode.Country.LandIsocode;
                }
                if (_entity.PostalCode.Provincie != null)
                {
                    bo.Postalcode.Provincie.Name = _entity.PostalCode.Provincie.ProvincieName;
                    bo.Postalcode.Provincie.ProvincieId = _entity.PostalCode.Provincie.ProvincieId;
                }
            }
            if ((_entity.InvoicePostalCode != null))
            {
                bo.InvoicePostalcode.Postcode = _entity.InvoicePostalCode.Postcode;
                bo.InvoicePostalcode.Gemeente = _entity.InvoicePostalCode.Gemeente;
                bo.InvoicePostalcode.PostcodeId = _entity.InvoicePostalCode.PostcodeId;
                if (_entity.InvoicePostalCode.Country != null)
                {
                    bo.InvoicePostalcode.Country.Name = _entity.InvoicePostalCode.Country.LandNaam;
                    bo.InvoicePostalcode.Country.CountryId = _entity.InvoicePostalCode.Country.Id;
                    bo.InvoicePostalcode.Country.ISOCode = _entity.InvoicePostalCode.Country.LandIsocode;
                }
                if (_entity.InvoicePostalCode.Provincie != null)
                {
                    bo.InvoicePostalcode.Provincie.Name = _entity.InvoicePostalCode.Provincie.ProvincieName;
                    bo.InvoicePostalcode.Provincie.ProvincieId = _entity.InvoicePostalCode.Provincie.ProvincieId;
                }
            }
            if (_entity.ClientContacts != null)
            {
                foreach (var item in _entity.ClientContacts)
                {
                    if (item.IsCoOwner == false)
                    {
                        ClientContactBO contact = new ClientContactBO();
                        var err = ClientContactTranslator.TranslateEntityToBO(item, contact);
                        if (err != ErrorCode.Success)
                            return err;
                        bo.Contacts.Add(contact);
                    }
                    else
                    {
                        ClientContactBO coowner = new ClientContactBO();
                        var err = ClientContactTranslator.TranslateEntityToBO(item, coowner);
                        if (err != ErrorCode.Success)
                            return err;
                        bo.CoOwners.Add(coowner);
                    }
                }
            }

            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateEntityToBO_WithUnits(ClientAccount _entity, ClientAccountWithUnitsBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            var err = TranslateEntityToBO(_entity, bo.Client);
            if (err != ErrorCode.Success)
                return err;

            foreach (var item in _entity.Units)
            {
                UnitBO unit = new UnitBO();
                err = UnitTranslator.TranslateEntityToBO(item, unit);
                if (err != ErrorCode.Success)
                    return err;
                bo.Units.Add(unit);
            }

            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ClientAccount _entity, ClientAccountBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;


            _entity.Name = bo.Name;
            _entity.Salutation = bo.Salutation.ToString();
            _entity.Street = bo.Street;
            _entity.Housenumber = bo.Housenumber;
            _entity.Busnumber = bo.Busnumber;
            _entity.DateDeedOfSale = bo.DateDeedOfSale;
            _entity.DeedOfSaleExpDate = bo.DateDeedOfSaleExpDate;
            _entity.DateSalesAgreement = bo.DateSalesAgreement;
            _entity.DeliveryDate = bo.DeliveryDate;
            _entity.DeliveryDoc = bo.DeliveryDoc;
            _entity.Vatnumber = bo.VATnumber;
            _entity.CompanyName = bo.CompanyName;
            _entity.ExecutionDays = bo.ExecutionDays;
            _entity.StartDateConstruction = bo.StartDateConstruction;
            _entity.BankAccountNumber = bo.BankAccountNumber;
            _entity.InvoiceAddress = bo.InvoiceAddress;
            _entity.InvoiceStreet = bo.InvoiceStreet;
            _entity.InvoiceHousenumber = bo.InvoiceHousenumber;
            _entity.InvoiceBusnumber = bo.InvoiceBusnumber;
            _entity.InvoiceExtra = bo.InvoiceExtra;


            if ((bo.OwnerType != null | bo.OwnerType.Id != 0))
                _entity.OwnerTypeId = bo.OwnerType.Id;
            _entity.OwnerPercentage = bo.OwnerPercentage;


            if ((bo.Postalcode != null))
                _entity.PostalCodeId = bo.Postalcode.PostcodeId;
            if ((bo.InvoicePostalcode != null) && bo.InvoicePostalcode.PostcodeId != 0)
                _entity.InvoicePostalCodeId = bo.InvoicePostalcode.PostcodeId;
            var err = HandleContacts(_entity, bo.Contacts, bo.CoOwners, uow);
            if ((err != ErrorCode.Success))
                return err;
            err = HandleCoOwners(_entity, bo.CoOwners, bo.Contacts, uow);
            if ((err != ErrorCode.Success))
                return err;
            return ErrorCode.Success;
        }

        private static ErrorCode HandleContacts(ClientAccount _entity, List<ClientContactBO> contacts, List<ClientContactBO> coowners, UnitOfWork uow)
        {
            if ((contacts == null))
                return ErrorCode.Success;
            if ((contacts.Count == 0))
                return ErrorCode.Success;
            foreach (var x in contacts)
            {
                if ((x.Id == 0))
                {
                    // insert
                    ClientContacts contact = new ClientContacts();
                    var err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow);
                    contact.IsCoOwner = false;
                    contact.CoOwnerTypeId = null;
                    contact.CoOwnerType = null;
                    contact.PostalCode = null;
                    contact.PostalCodeId = null;
                    contact.InvoicePostalCode = null;
                    contact.InvoicePostalCodeId = null;
                    if (err != ErrorCode.Success)
                        return err;

                    _entity.ClientContacts.Add(contact);
                }
                else
                {
                    // update
                    var contact = _entity.ClientContacts.FirstOrDefault(f => f.Id == x.Id);
                    if ((contact != null))
                    {
                        var err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow);
                        contact.IsCoOwner = false;
                        contact.CoOwnerTypeId = null;
                        contact.CoOwnerType = null;
                        contact.PostalCode = null;
                        contact.PostalCodeId = null;
                        contact.InvoicePostalCode = null;
                        contact.InvoicePostalCodeId = null;
                    }
                }
            }
            // delete
            List<ClientContacts> delList = new List<ClientContacts>();
            foreach (var x in _entity.ClientContacts)
            {
                if ((!contacts.Any(f => f.Id == x.Id) && !coowners.Any(m => m.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.ClientContacts.Remove(x);
            return ErrorCode.Success;
        }
        private static ErrorCode HandleCoOwners(ClientAccount _entity, List<ClientContactBO> coowners, List<ClientContactBO> contacts, UnitOfWork uow)
        {
            if ((coowners == null))
                return ErrorCode.Success;
            if ((coowners.Count == 0))
                return ErrorCode.Success;
            foreach (var x in coowners)
            {
                if ((x.Id == 0))
                {
                    // insert
                    ClientContacts contact = new ClientContacts();
                    var err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow);
                    contact.IsCoOwner = true;
                    if (err != ErrorCode.Success)
                        return err;

                    _entity.ClientContacts.Add(contact);
                }
                else
                {
                    // update
                    var contact = _entity.ClientContacts.FirstOrDefault(f => f.Id == x.Id);
                    if ((contact != null))
                    {
                        var err = ClientContactTranslator.TranslateBOToEntity(contact, x, uow);
                        contact.IsCoOwner = true;
                    }
                }
            }
            // delete
            List<ClientContacts> delList = new List<ClientContacts>();
            foreach (var x in _entity.ClientContacts)
            {
                if ((!coowners.Any(f => f.Id == x.Id) && !contacts.Any(m => m.Id == x.Id)))
                    delList.Add(x);
            }
            foreach (var x in delList)
                _entity.ClientContacts.Remove(x);
            return ErrorCode.Success;
        }
    }
}
