using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class Activity

    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.ActivityId,
                Display = this.Omschrijving ?? string.Empty,
                Group = "Deel " + this.Group.Lot + " - " + this.Group.Name,
                GroupId = this.Group.GroupId
            };
        }
    }
}
