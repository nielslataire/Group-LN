using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class ClientAccount

    {
        public IdNameBO GetIdName()
        {
            var display = string.IsNullOrWhiteSpace(CompanyName) ? Name : CompanyName;

            return new IdNameBO
            {
                ID = Id,
                Display = display
            };
        }
    }
}
