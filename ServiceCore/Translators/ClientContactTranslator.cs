using System;
using System.Linq;
using System.Text.RegularExpressions;
using BOCore;
using DALCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class ClientContactTranslator
    {
        internal static ErrorCode TranslateEntityToBO(ClientContacts entity, ClientContactBO bo)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;

            bo.Id = entity.Id;
            bo.AccountId = entity.ClientAccountId;
            bo.Name = entity.Name;
            bo.Street = entity.Street;
            bo.Housenumber = entity.Housenumber;
            bo.Busnumber = entity.Busnumber;
            bo.Email = entity.Email;
            bo.IsCoOwner = entity.IsCoOwner;
            bo.Phone = entity.Phone;
            bo.Cellphone = entity.Cellphone;
            bo.Firstname = entity.Forename;

            // Salutation komt als string uit de entity; veilig parsen
            if (!string.IsNullOrWhiteSpace(entity.Salutation) &&
                Enum.TryParse(typeof(Salutation), entity.Salutation, out var sal))
            {
                bo.Salutation = (Salutation)sal;
            }
            else
            {
                // optioneel: default zetten, of niets doen als je BO nullable enum gebruikt
                // bo.Salutation = Salutation.None;  // only if you have such value
            }

            bo.VATnumber = entity.Vatnumber;
            bo.CompanyName = entity.CompanyName;
            bo.InvoiceAddress = entity.InvoiceAddress;
            bo.InvoiceStreet = entity.InvoiceStreet;
            bo.InvoiceHousenumber = entity.InvoiceHousenumber;
            bo.InvoiceBusnumber = entity.InvoiceBusnumber;

            // PostalCode
            if (entity.PostalCode != null)
            {
                bo.Postalcode.Postcode = entity.PostalCode.Postcode;
                bo.Postalcode.Gemeente = entity.PostalCode.Gemeente;
                bo.Postalcode.PostcodeId = entity.PostalCode.PostcodeId;

                if (entity.PostalCode.Country != null)
                {
                    bo.Postalcode.Country.Name = entity.PostalCode.Country.LandNaam;
                    bo.Postalcode.Country.CountryId = entity.PostalCode.Country.Id;
                    bo.Postalcode.Country.ISOCode = entity.PostalCode.Country.LandIsocode;
                }
                if (entity.PostalCode.Provincie != null)
                {
                    bo.Postalcode.Provincie.Name = entity.PostalCode.Provincie.ProvincieName;
                    bo.Postalcode.Provincie.ProvincieId = entity.PostalCode.Provincie.ProvincieId;
                }
            }

            // InvoicePostalCode
            if (entity.InvoicePostalCode != null)
            {
                bo.InvoicePostalcode.Postcode = entity.InvoicePostalCode.Postcode;
                bo.InvoicePostalcode.Gemeente = entity.InvoicePostalCode.Gemeente;
                bo.InvoicePostalcode.PostcodeId = entity.InvoicePostalCode.PostcodeId;

                if (entity.InvoicePostalCode.Country != null)
                {
                    bo.InvoicePostalcode.Country.Name = entity.InvoicePostalCode.Country.LandNaam;
                    bo.InvoicePostalcode.Country.CountryId = entity.InvoicePostalCode.Country.Id;
                    bo.InvoicePostalcode.Country.ISOCode = entity.InvoicePostalCode.Country.LandIsocode;
                }
                if (entity.InvoicePostalCode.Provincie != null)
                {
                    bo.InvoicePostalcode.Provincie.Name = entity.InvoicePostalCode.Provincie.ProvincieName;
                    bo.InvoicePostalcode.Provincie.ProvincieId = entity.InvoicePostalCode.Provincie.ProvincieId;
                }
            }

            if (entity.CoOwnerPercentage is not null)
                bo.CoOwnerPercentage = entity.CoOwnerPercentage;

            if (entity.CoOwnerType is not null)
            {
                var err = ClientOwnerTypeTranslator.TranslateEntityToBO(entity.CoOwnerType, bo.CoOwnerType);
                if (err != ErrorCode.Success) return err;
            }

            return ErrorCode.Success;
        }

        internal static ErrorCode TranslateBOToEntity(ClientContacts entity, ClientContactBO bo, UnitOfWorkCore uow)
        {
            if (entity == null) return ErrorCode.EntityNull;
            if (bo == null) return ErrorCode.BoNull;
            // uow wordt hier niet gebruikt, maar blijft in de signature voor consistentie

            entity.ClientAccountId = bo.AccountId;
            entity.Name = bo.Name;
            entity.Street = bo.Street;
            entity.Housenumber = bo.Housenumber;
            entity.Busnumber = bo.Busnumber;
            entity.Email = bo.Email;
            entity.IsCoOwner = bo.IsCoOwner;

            if (!string.IsNullOrEmpty(bo.Cellphone))
                entity.Cellphone = Regex.Replace(bo.Cellphone, "[^0-9]", "");
            else
                entity.Cellphone = null;

            if (!string.IsNullOrEmpty(bo.Phone))
                entity.Phone = Regex.Replace(bo.Phone, "[^0-9]", "");
            else
                entity.Phone = null;

            // Salutation als string opslaan
            entity.Salutation = bo.Salutation.ToString();

            entity.Forename = bo.Firstname;
            entity.CompanyName = bo.CompanyName;
            entity.Vatnumber = bo.VATnumber;
            entity.InvoiceAddress = bo.InvoiceAddress;
            entity.InvoiceStreet = bo.InvoiceStreet;
            entity.InvoiceHousenumber = bo.InvoiceHousenumber;
            entity.InvoiceBusnumber = bo.InvoiceBusnumber;

            // PostcodeId’s (0 => null)
            if (bo.Postalcode != null && bo.Postalcode.PostcodeId.HasValue && bo.Postalcode.PostcodeId.Value != 0)
                entity.PostalCodeId = bo.Postalcode.PostcodeId.Value;
            else
                entity.PostalCodeId = null;

            if (bo.InvoicePostalcode != null && bo.InvoicePostalcode.PostcodeId.HasValue && bo.InvoicePostalcode.PostcodeId.Value != 0)
                entity.InvoicePostalCodeId = bo.InvoicePostalcode.PostcodeId.Value;
            else
                entity.InvoicePostalCodeId = null;

            entity.CoOwnerPercentage = bo.CoOwnerPercentage;

            // CoOwnerTypeId
            if (bo.CoOwnerType != null && bo.CoOwnerType.Id != 0)
                entity.CoOwnerTypeId = bo.CoOwnerType.Id;
            else
                entity.CoOwnerTypeId = null;

            return ErrorCode.Success;
        }
    }
}
