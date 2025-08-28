using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOCore;

namespace DALCore.Models
{
    public partial class Units

    {
        public IdNameBO GetIdName()
        {
            var display = (Type != null && Type.Id == 11)
                            ? $"{Type.Name} {Name}"
                            : Name;

            return new IdNameBO
            {
                ID = Id,
                Display = display,
                Group = Type?.Group?.Name
            };
        }
    }
}
