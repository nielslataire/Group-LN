using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class ProjectStatus

    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.StatusId,
                Display = this.StatusName ?? string.Empty
            };
        }
    }
}
