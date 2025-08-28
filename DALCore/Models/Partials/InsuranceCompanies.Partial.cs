using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class InsuranceCompanies
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.Id,   // of this.Id, afhankelijk van scaffold
                Display = this.Name ?? string.Empty
            };
        }
    }
}
