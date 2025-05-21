using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Translators
{
    public class PostalcodeTranslator
    {
        internal static ErrorCode TranslateEntityToBO(PostalCode _entity, PostalCodeBO bo)
        {
            if (_entity == null)
                return ErrorCode.EntityNull;
            if (bo == null)
                return ErrorCode.BoNull;
            bo.PostcodeId = _entity.PostcodeId;
            bo.Postcode = _entity.Postcode;
            bo.Gemeente = _entity.Gemeente;
            if ((_entity.Country != null))
            {
                bo.Country.CountryId = _entity.Country.Id;
                bo.Country.Name = _entity.Country.LandNaam;
                bo.Country.ISOCode = _entity.Country.LandIsocode;
            }
            if ((_entity.Provincie != null))
            {
                bo.Provincie.Name = _entity.Provincie.ProvincieName;
                bo.Provincie.ProvincieId = _entity.Provincie.ProvincieId;
            }
            return ErrorCode.Success;
        }
    }
}
