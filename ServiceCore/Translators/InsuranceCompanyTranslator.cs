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
    public class InsuranceCompanyTranslator
    {
        internal static ErrorCode TranslateEntityToBO(InsuranceCompanies _entity, InsuranceCompanyBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            // Algemene gegevens
            bo.Id = _entity.Id;
            bo.Name = _entity.Name;
            if ((_entity.Postcode != null))
            {
                bo.Postalcode.Postcode = _entity.Postcode.Postcode;
                bo.Postalcode.Gemeente = _entity.Postcode.Gemeente;
                bo.Postalcode.PostcodeId = _entity.Postcode.PostcodeId;
                if (_entity.Postcode.Country != null)
                {
                    bo.Postalcode.Country.Name = _entity.Postcode.Country.LandNaam;
                    bo.Postalcode.Country.CountryId = _entity.Postcode.Country.Id;
                    bo.Postalcode.Country.ISOCode = _entity.Postcode.Country.LandIsocode;
                }
                if (_entity.Postcode.Provincie != null)
                {
                    bo.Postalcode.Provincie.Name = _entity.Postcode.Provincie.ProvincieName;
                    bo.Postalcode.Provincie.ProvincieId = _entity.Postcode.Provincie.ProvincieId;
                }
            }
            bo.Street = _entity.Straat;
            bo.HouseNumber = _entity.Huisnummer;
            bo.BusNumber = _entity.Busnummer;
            bo.Addition = _entity.Toevoeging;



            return ErrorCode.Success;
        }
        internal static ErrorCode TranslateBOToEntity(InsuranceCompanies _entity, InsuranceCompanyBO bo, UnitOfWork uow)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;

            _entity.Name = bo.Name;
            _entity.Straat = bo.Street;
            _entity.Huisnummer = bo.HouseNumber;
            _entity.Busnummer = bo.BusNumber;
            _entity.Toevoeging = bo.Addition;

            if ((bo.Postalcode != null & bo.Postalcode.PostcodeId != 0))
                _entity.PostcodeId = (int)bo.Postalcode.PostcodeId;

            return ErrorCode.Success;
        }
    }
}
