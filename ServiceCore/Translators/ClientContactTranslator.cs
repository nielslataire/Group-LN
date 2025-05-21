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
   public class ClientContactTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ClientContacts _entity, ClientContactBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.Id = _entity.Id;
            bo.AccountId = _entity.ClientAccountId;
            bo.Name = _entity.Name;
            bo.Street = _entity.Street;
            bo.Housenumber = _entity.Housenumber;
            bo.Busnumber = _entity.Busnumber;
            bo.Email = _entity.Email;
            bo.IsCoOwner = _entity.IsCoOwner;
            bo.Phone = _entity.Phone;
            bo.Cellphone = _entity.Cellphone;
            bo.Firstname = _entity.Forename;
            bo.Salutation = (Salutation)Enum.Parse(typeof(Salutation), _entity.Salutation);
            bo.VATnumber = _entity.Vatnumber;
            bo.CompanyName = _entity.CompanyName;
            bo.InvoiceAddress = _entity.InvoiceAddress;
            bo.InvoiceStreet = _entity.InvoiceStreet;
            bo.InvoiceHousenumber = _entity.InvoiceHousenumber;
            bo.InvoiceBusnumber = _entity.InvoiceBusnumber;


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
            if (_entity.CoOwnerPercentage is not null)
                bo.CoOwnerPercentage = _entity.CoOwnerPercentage;
            if ((_entity.CoOwnerType is not null))
            {
                var err = ClientOwnerTypeTranslator.TranslateEntityToBO(_entity.CoOwnerType, bo.CoOwnerType);
                if (err != ErrorCode.Success)
                    return err;
            }

            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ClientContacts _entity, ClientContactBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            _entity.ClientAccountId = bo.AccountId;
            _entity.Name = bo.Name;
            _entity.Street = bo.Street;
            _entity.Housenumber = bo.Housenumber;
            _entity.Busnumber = bo.Busnumber;
            _entity.Email = bo.Email;
            _entity.IsCoOwner = bo.IsCoOwner;
            if (bo.Cellphone is not null)
                _entity.Cellphone = Regex.Replace(bo.Cellphone, "[^0-9]", "");
            if (bo.Phone is not null)
                _entity.Phone = Regex.Replace(bo.Phone, "[^0-9]", "");
            _entity.Salutation = bo.Salutation.ToString();
            _entity.Forename = bo.Firstname;
            _entity.CompanyName = bo.CompanyName;
            _entity.Vatnumber = bo.VATnumber;
            _entity.InvoiceAddress = bo.InvoiceAddress;
            _entity.InvoiceStreet = bo.InvoiceStreet;
            _entity.InvoiceHousenumber = bo.InvoiceHousenumber;
            _entity.InvoiceBusnumber = bo.InvoiceBusnumber;

            if ((bo.Postalcode != null))
            {
                if (bo.Postalcode.PostcodeId != 0)
                    _entity.PostalCodeId = bo.Postalcode.PostcodeId;
            }
            if ((bo.InvoicePostalcode != null))
            {
                if (bo.InvoicePostalcode.PostcodeId != 0)
                    _entity.InvoicePostalCodeId = bo.InvoicePostalcode.PostcodeId;
            }
            _entity.CoOwnerPercentage = bo.CoOwnerPercentage;
            if ((bo.CoOwnerType != null))
            {
                if (bo.CoOwnerType.Id != 0)
                    _entity.CoOwnerTypeId = bo.CoOwnerType.Id;
            }

            return ErrorCode.Success;
        }
    }
}
