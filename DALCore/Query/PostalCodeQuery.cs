using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DALCore.Models;
using BOCore;   

namespace DALCore.Query
{
  public  class PostalCodeQuery
    {
        public static Expression<Func<PostalCode, bool>> GetCityContainsQuery(string search)
        {
            return f => f.Gemeente.ToLower().Contains(search);
        }
        public static Expression<Func<PostalCode, bool>> GetPostalCodeContainsQuery(string search)
        {
            return f => f.Postcode.Contains(search);
        }
        public static Expression<Func<PostalCode, bool>> GetCountryQuery(int countryID)
        {
            return f => f.CountryId == countryID;
        }
        public static Expression<Func<PostalCode, bool>> GetPostalCodeStartsWithQuery(string search)
        {
            return f => f.Postcode.StartsWith(search);
        }
    }
}
