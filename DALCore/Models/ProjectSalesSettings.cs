﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

public partial class ProjectSalesSettings
{
    public int Id { get; set; }

    public int Projectid { get; set; }

    public decimal? Vatpercentage { get; set; }

    public int? RegistrationType { get; set; }

    public decimal? RegistrationPercentage { get; set; }

    public bool? MixedVatregistration { get; set; }

    public decimal? ConnectionFees { get; set; }

    public decimal? BaseCertificateCost { get; set; }

    public decimal? FixedCertificateCost { get; set; }

    public decimal? MortageRegistrationCost { get; set; }

    public string BankAccountNumber { get; set; }

    public bool? SaleVisible { get; set; }

    public decimal? SurveyorCost { get; set; }

    public decimal? ParcelCost { get; set; }

    public virtual Project Project { get; set; }
}