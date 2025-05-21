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
    public class ProjectQuery
    {
        public static Expression<Func<Project, bool>> GetNameQuery(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null/* TODO Change to default(_) if this is not a reference type */;
            return w => w.ProjectName.Contains(name);
        }

    }
}
