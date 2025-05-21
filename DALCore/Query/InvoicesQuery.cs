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
    public class InvoicesQuery
    {
        public static Expression<Func<Invoices, bool>> GetUnitsQuery(List<int> units)
        {
            if (units == null || units.Count == 0)
                return null/* TODO Change to default(_) if this is not a reference type */;
            Expression<Func<Invoices, bool>> query = null/* TODO Change to default(_) if this is not a reference type */;
            foreach (var unit in units)
            {
                int id = unit;
                query = query.Or(w => w.InvoicesDetails.Any(a => a.UnitId == id));
            }
            return query;
        }

    }
}
