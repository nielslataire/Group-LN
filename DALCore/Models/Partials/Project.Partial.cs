using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class Project
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.ProjectId,
                Display = this.ProjectName ?? string.Empty
            };
        }
    }
}
