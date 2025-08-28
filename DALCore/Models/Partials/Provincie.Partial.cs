using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class Provincie

    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.ProvincieId,
                Display = this.ProvincieName ?? string.Empty,
                Group = this.Country.LandNaam
            };
        }
    }
}
