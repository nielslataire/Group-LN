﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using BOCore;

namespace DALCore.Models;

public partial class InsuranceCompanies
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int PostcodeId { get; set; }

    public string Straat { get; set; }

    public string Huisnummer { get; set; }

    public string Busnummer { get; set; }

    public string Toevoeging { get; set; }

    public virtual ICollection<Insurances> Insurances { get; set; } = new List<Insurances>();

    public virtual PostalCode Postcode { get; set; }
    public IdNameBO GetIdName()
    {
        IdNameBO bo = new IdNameBO();
        bo.ID = this.Id;
        bo.Display = this.Name;

        return bo;
    }

}