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
            bo.InvoiceAddress = _entity.InvoiceAddress ?? false;
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

        internal static ErrorCode TranslateBOToEntity(ClientAccount entity, ClientAccountBO bo, UnitOfWorkCore uow)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;
            if (uow == null) return ErrorCode.UowNull;

            // 1) Eenvoudige velden
            entity.Name = bo.Name;
            entity.Salutation = bo.Salutation.ToString();
            entity.Street = bo.Street;
            entity.Housenumber = bo.Housenumber;
            entity.Busnumber = bo.Busnumber;
            entity.DateDeedOfSale = bo.DateDeedOfSale;
            entity.DeedOfSaleExpDate = bo.DateDeedOfSaleExpDate;
            entity.DateSalesAgreement = bo.DateSalesAgreement;
            entity.DeliveryDate = bo.DeliveryDate;
            entity.DeliveryDoc = bo.DeliveryDoc;
            entity.Vatnumber = bo.VATnumber;
            entity.CompanyName = bo.CompanyName;
            entity.ExecutionDays = bo.ExecutionDays;
            entity.StartDateConstruction = bo.StartDateConstruction;
            entity.BankAccountNumber = bo.BankAccountNumber;
            entity.InvoiceAddress = bo.InvoiceAddress;          // bool? of bool in jouw model
            entity.InvoiceStreet = bo.InvoiceStreet;
            entity.InvoiceHousenumber = bo.InvoiceHousenumber;
            entity.InvoiceBusnumber = bo.InvoiceBusnumber;
            entity.InvoiceExtra = bo.InvoiceExtra;

            // 2) OwnerType
            if (bo.OwnerType != null && bo.OwnerType.Id != 0)
                entity.OwnerTypeId = bo.OwnerType.Id;
            else
                entity.OwnerTypeId = null;

            entity.OwnerPercentage = bo.OwnerPercentage;

            // 3) Postcodes
            if (bo.Postalcode?.PostcodeId is int pcId && pcId != 0)
                entity.PostalCodeId = pcId;
            else
                entity.PostalCodeId = null;

            if (bo.InvoicePostalcode?.PostcodeId is int invPcId && invPcId != 0)
                entity.InvoicePostalCodeId = invPcId;
            else
                entity.InvoicePostalCodeId = null;

            // 4) Contacts & Co-owners
            var err = HandleContacts(entity, bo.Contacts, bo.CoOwners, uow);
            if (err != ErrorCode.Success) return err;

            err = HandleCoOwners(entity, bo.CoOwners, bo.Contacts, uow);
            if (err != ErrorCode.Success) return err;

            return ErrorCode.Success;
        }

        private static ErrorCode HandleContacts(
        ClientAccount entity,
        List<ClientContactBO> contacts,
        List<ClientContactBO> coowners,
        UnitOfWorkCore uow)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (uow == null) return ErrorCode.UowNull;

            // Niks te doen (=> verwijder niet-co-owners die niet meer bestaan)
            if (contacts == null || contacts.Count == 0)
            {
                var delList = entity.ClientContacts
                    .Where(x => !x.IsCoOwner)
                    .Where(x => coowners == null || !coowners.Any(c => c.Id == x.Id))
                    .ToList();

                foreach (var x in delList)
                    uow.Context.Remove(x); // <-- echt deleten, niet enkel uit de collectie

                return ErrorCode.Success;
            }

            // Insert/Update
            foreach (var bo in contacts)
            {
                if (bo.Id == 0)
                {
                    // INSERT
                    var contact = new ClientContacts();
                    var err = ClientContactTranslator.TranslateBOToEntity(contact, bo, uow);
                    if (err != ErrorCode.Success) return err;

                    contact.IsCoOwner = false;
                    contact.CoOwnerTypeId = null;
                    contact.CoOwnerType = null;
                    contact.PostalCode = null;
                    contact.PostalCodeId = bo.Postalcode?.PostcodeId ?? null;
                    contact.InvoicePostalCode = null;
                    contact.InvoicePostalCodeId = bo.InvoicePostalcode?.PostcodeId ?? null;

                    // >>> koppel aan parent (cruciaal om FK te zetten)
                    if (entity.Id == 0)
                        contact.ClientAccount = entity;         // nieuw: via navigatie
                    else
                        contact.ClientAccountId = entity.Id;    // bestaand: via FK

                    entity.ClientContacts.Add(contact);
                }
                else
                {
                    // UPDATE
                    var contact = entity.ClientContacts.FirstOrDefault(f => f.Id == bo.Id);
                    if (contact != null)
                    {
                        var err = ClientContactTranslator.TranslateBOToEntity(contact, bo, uow);
                        if (err != ErrorCode.Success) return err;

                        contact.IsCoOwner = false;
                        contact.CoOwnerTypeId = null;
                        contact.CoOwnerType = null;
                        contact.PostalCode = null;
                        contact.PostalCodeId = bo.Postalcode?.PostcodeId ?? null;
                        contact.InvoicePostalCode = null;
                        contact.InvoicePostalCodeId = bo.InvoicePostalcode?.PostcodeId ?? null;

                        // >>> enforce juiste parent-link bij update
                        if (entity.Id == 0)
                            contact.ClientAccount = entity;
                        else
                            contact.ClientAccountId = entity.Id;
                    }
                    // else: onbekende Id in POST → negeren of fout, jouw keuze
                }
            }

            // Delete contacts die niet meer in contacts/coowners zitten
            var delContacts = entity.ClientContacts
                .Where(x => !x.IsCoOwner)
                .Where(x => !contacts.Any(c => c.Id == x.Id) && (coowners == null || !coowners.Any(m => m.Id == x.Id)))
                .ToList();

            foreach (var x in delContacts)
                uow.Context.Remove(x); // <-- echt deleten

            return ErrorCode.Success;
        }


        private static ErrorCode HandleCoOwners(
            ClientAccount entity,
            List<ClientContactBO> coowners,
            List<ClientContactBO> contacts,
            UnitOfWorkCore uow)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (uow == null) return ErrorCode.UowNull;

            if (coowners == null || coowners.Count == 0)
            {
                // Verwijder co-owners die niet meer voorkomen
                var delList = entity.ClientContacts
                    .Where(x => x.IsCoOwner)
                    .Where(x => contacts == null || !contacts.Any(c => c.Id == x.Id))
                    .ToList();

                foreach (var x in delList)
                    uow.Context.Remove(x); // <-- echte delete i.p.v. uit collectie halen

                return ErrorCode.Success;
            }

            // Insert/Update
            foreach (var bo in coowners)
            {
                if (bo.Id == 0)
                {
                    var contact = new ClientContacts();
                    var err = ClientContactTranslator.TranslateBOToEntity(contact, bo, uow);
                    if (err != ErrorCode.Success) return err;

                    contact.IsCoOwner = true;
                    contact.CoOwnerTypeId = bo.CoOwnerType?.Id == 0 ? null : bo.CoOwnerType?.Id;

                    contact.PostalCode = null;
                    contact.PostalCodeId = bo.Postalcode?.PostcodeId ?? null;
                    contact.InvoicePostalCode = null;
                    contact.InvoicePostalCodeId = bo.InvoicePostalcode?.PostcodeId ?? null;

                    // >>> parent-link forceren
                    if (entity.Id == 0)
                        contact.ClientAccount = entity;      // nieuwe klant: via navigatie
                    else
                        contact.ClientAccountId = entity.Id; // bestaande klant: via FK

                    entity.ClientContacts.Add(contact);
                }
                else
                {
                    var contact = entity.ClientContacts.FirstOrDefault(f => f.Id == bo.Id);
                    if (contact != null)
                    {
                        var err = ClientContactTranslator.TranslateBOToEntity(contact, bo, uow);
                        if (err != ErrorCode.Success) return err;

                        contact.IsCoOwner = true;
                        contact.CoOwnerTypeId = bo.CoOwnerType?.Id == 0 ? null : bo.CoOwnerType?.Id;

                        contact.PostalCode = null;
                        contact.PostalCodeId = bo.Postalcode?.PostcodeId ?? null;
                        contact.InvoicePostalCode = null;
                        contact.InvoicePostalCodeId = bo.InvoicePostalcode?.PostcodeId ?? null;

                        // >>> parent-link forceren bij update
                        if (entity.Id == 0)
                            contact.ClientAccount = entity;
                        else
                            contact.ClientAccountId = entity.Id;
                    }
                    // else: onbekende Id in POST → negeren of fout (jij beslist)
                }
            }

            // Delete co-owners die niet meer voorkomen in coowners/contacts
            var delCoOwners = entity.ClientContacts
                .Where(x => x.IsCoOwner)
                .Where(x => !coowners.Any(c => c.Id == x.Id) && (contacts == null || !contacts.Any(m => m.Id == x.Id)))
                .ToList();

            foreach (var x in delCoOwners)
                uow.Context.Remove(x); // <-- echte delete

            return ErrorCode.Success;
        }

    }
}
