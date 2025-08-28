using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;              // of: using DALCore.BO;
namespace DALCore.Models
{
    public partial class Country
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.Id,
                Display = this.LandNaam ?? string.Empty,
                Group = this.LandIsocode
            };
        }
    }
}