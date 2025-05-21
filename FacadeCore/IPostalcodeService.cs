using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
    public interface IPostalcodeService
    {
        GetResponse<PostalCodeBO> GetPostalcodeById(int PostalcodeId);
        GetResponse<PostalCodeBO> GetPostalcodeByCountry(int CountryId);
        GetResponse<PostalCodeBO> GetPostalcodeByCountryAndSearchstring(int CountryId, string Searchstring);
        //GetResponse<PostalCodeBO> GetPostalcodeByCountryCodeAndPostalcode(string Countrycode, string Postalcode);
    }
}
