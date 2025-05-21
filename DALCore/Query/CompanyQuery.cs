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
    public class CompanyQuery
    {
        public static Expression<Func<CompanyInfo, bool>> GetFilterQeury(CompanyFilter filter)
        {
            Expression<Func<CompanyInfo, bool>> query = GetNameQuery(filter.CompanyName);
            query = query.And(GetActivitesAndQuery(filter.SelectedActivities));
            query = query.And(GetProvincesOrQuery(filter.SelectedProvince));
            return query;
        }

        public static Expression<Func<CompanyInfo, bool>> GetNameQuery(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null/* TODO Change to default(_) if this is not a reference type */;
            return w => w.BedrijfsNaam.Contains(name);
        }
        public static Expression<Func<CompanyInfo, bool>> GetActivitesAndQuery(List<int> activities)
        {
            if (activities == null || activities.Count == 0)
                return null/* TODO Change to default(_) if this is not a reference type */;
            Expression<Func<CompanyInfo, bool>> query = null/* TODO Change to default(_) if this is not a reference type */;
            foreach (var Activity in activities)
            {
                int id = Activity;
                query = query.And(w => w.Activity.Any(a => a.ActivityId == id));
            }
            return query;
        }
        public static Expression<Func<CompanyInfo, bool>> GetProvincesOrQuery(List<int> provinces)
        {
            if (provinces == null || provinces.Count == 0)
                return null/* TODO Change to default(_) if this is not a reference type */;
            return f => f.PostCode.ProvincieId.HasValue & provinces.Contains((int)f.PostCode.ProvincieId);
        }
    }
}
