﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using BOCore;

namespace DALCore.Models;

public partial class Contract
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int CompanyId { get; set; }

    public decimal? VatPercentage { get; set; }

    public decimal? PaymentTerm { get; set; }

    public decimal? CashDiscountPercentage { get; set; }

    public decimal? CashDiscountPaymentTerm { get; set; }

    public bool CashDiscount { get; set; }

    public int? GuaranteeType { get; set; }

    public decimal? GuaranteePercentage { get; set; }

    public bool? ContractSigned { get; set; }

    public virtual CompanyInfo Company { get; set; }

    public virtual ICollection<ContractActivity> ContractActivity { get; set; } = new List<ContractActivity>();

    public virtual ICollection<IncommingInvoices> IncommingInvoices { get; set; } = new List<IncommingInvoices>();

    public virtual Project Project { get; set; }

    public IdNameBO GetIdName()
    {
        IdNameBO bo = new IdNameBO();
        bo.ID = this.Id;
        bo.Display = this.Company.BedrijfsNaam;
        foreach (var act in this.ContractActivity)
            bo.Display = bo.Display + " - " + act.Activity.Omschrijving;
        return bo;
    }
}