using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace FacadeCore
{
   public interface ICountryService
    {
        GetResponse<CountryBO> GetCountrys();
        GetResponse<CountryBO> GetCountryById(int id);
        GetResponse<IdNameBO> GetVisibleCountriesForSelect();
    }
}
