﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using BOCore;

namespace DALCore.Models;

public partial class Activity
{
    public int ActivityId { get; set; }

    public string Omschrijving { get; set; }

    public int GroupId { get; set; }

    public virtual ICollection<ContractActivity> ContractActivity { get; set; } = new List<ContractActivity>();

    public virtual ActivityGroup Group { get; set; }

    public virtual ICollection<IncommingInvoiceDetail> IncommingInvoiceDetail { get; set; } = new List<IncommingInvoiceDetail>();

    public virtual ICollection<ProjectBudget> ProjectBudget { get; set; } = new List<ProjectBudget>();

    public virtual ICollection<CompanyInfo> Company { get; set; } = new List<CompanyInfo>();

    public virtual ICollection<ClientGift> Gift { get; set; } = new List<ClientGift>();

    public virtual ICollection<ClientPoa> Poa { get; set; } = new List<ClientPoa>();

    public IdNameBO GetIdName()
    {
        IdNameBO bo = new IdNameBO();
        bo.ID = this.ActivityId;
        bo.Display = this.Omschrijving;
        bo.Group = "Deel " + this.Group.Lot + " - " + this.Group.Name;
        return bo;
    }
}