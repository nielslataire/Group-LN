using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class CompanyInfo

    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.CompanyId,
                Display = this.BedrijfsNaam ?? string.Empty,
            };
        }
        public SelectBO GetIdNameForSearch()
        {
            return new SelectBO
            {
                id = this.CompanyId,
                text = this.BedrijfsNaam ?? string.Empty,
                extra = "Company"
            };
        }
    }
}
