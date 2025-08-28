using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;              // of: using DALCore.BO;
namespace DALCore.Models
{
    public partial class ContractActivity
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.Id,
                Display = this.Activity.Omschrijving + " - " + this.Contract.Company.BedrijfsNaam ?? string.Empty,
            };
        }
    }
}