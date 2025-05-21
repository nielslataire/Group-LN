using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DALCore.Models;   

namespace DALCore.Query
{
   public class CountryQuery
    {
        public static Expression<Func<Country, bool>> GetVisibleQuery(bool visible)
        {
            return f => f.Selectable == visible;
        }

    }
}
