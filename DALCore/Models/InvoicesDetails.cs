﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

public partial class InvoicesDetails
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int? PaymentStageId { get; set; }

    public int? UnitId { get; set; }

    public int? ConstructionValueId { get; set; }

    public string Text { get; set; }

    public decimal? VatPercentage { get; set; }

    public decimal? Price { get; set; }

    public string GroupName { get; set; }

    public int? ChangeOrderDetailId { get; set; }

    public bool? UtilityCost { get; set; }

    public virtual Invoices Invoice { get; set; }

    public virtual InvoicingPaymentStages PaymentStage { get; set; }

    public virtual Units Unit { get; set; }
}