﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using BOCore;

namespace DALCore.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; }

    public string Slug { get; set; }

    public int? PostalCodeId { get; set; }

    public string Street { get; set; }

    public string Number { get; set; }

    public int? StatusId { get; set; }

    public string CommercialTitleNl { get; set; }

    public string CommercialTextNl { get; set; }

    public DateOnly? ConstructionYear { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public DateOnly? DeliveryDateDef { get; set; }

    public DateOnly? StartDateConstruction { get; set; }

    public int? DeveloperId { get; set; }

    public int? BuilderId { get; set; }

    public int? ExecutionDays { get; set; }

    public int? WheaterStationId { get; set; }

    public int? DefaultPictureId { get; set; }

    public int? EngineerId { get; set; }

    public int? SecurityCoordinatorId { get; set; }

    public int? EpbReporterId { get; set; }

    public int? ArchitectId { get; set; }

    public decimal? FacebookAlbumId { get; set; }

    public string AspNetUserId { get; set; }

    public decimal? TotalLandShare { get; set; }

    public string FacebookPlaceId { get; set; }

    public string ProjectFolder { get; set; }

    public bool? UploadToFacebook { get; set; }

    public bool? DocPid { get; set; }

    public bool? DocElectricalInspection { get; set; }

    public bool? DocWaterInspection { get; set; }

    public bool? DocSewerInspection { get; set; }

    public bool? DocFireInspection { get; set; }

    public bool? DocDelivery { get; set; }

    public bool? DocDefDelivery { get; set; }

    public int ProjectType { get; set; }

    public virtual CompanyInfo Architect { get; set; }

    public virtual AspNetUsers1 AspNetUser { get; set; }

    public virtual CompanyInfo Builder { get; set; }

    public virtual ICollection<Contract> Contract { get; set; } = new List<Contract>();

    public virtual ProjectPictures DefaultPicture { get; set; }

    public virtual CompanyInfo Developer { get; set; }

    public virtual CompanyInfo Engineer { get; set; }

    public virtual CompanyInfo EpbReporter { get; set; }

    public virtual ICollection<IncommingInvoices> IncommingInvoices { get; set; } = new List<IncommingInvoices>();

    public virtual ICollection<InvoicingPaymentGroup> InvoicingPaymentGroup { get; set; } = new List<InvoicingPaymentGroup>();

    public virtual PostalCode PostalCode { get; set; }

    public virtual ICollection<ProjectBudget> ProjectBudget { get; set; } = new List<ProjectBudget>();

    public virtual ICollection<ProjectDocs> ProjectDocs { get; set; } = new List<ProjectDocs>();

    public virtual ICollection<ProjectLevels> ProjectLevels { get; set; } = new List<ProjectLevels>();

    public virtual ICollection<ProjectNews> ProjectNews { get; set; } = new List<ProjectNews>();

    public virtual ICollection<ProjectPictures> ProjectPictures { get; set; } = new List<ProjectPictures>();

    public virtual ICollection<ProjectSalesSettings> ProjectSalesSettings { get; set; } = new List<ProjectSalesSettings>();

    public virtual CompanyInfo SecurityCoordinator { get; set; }

    public virtual ProjectStatus Status { get; set; }

    public virtual ICollection<Units> Units { get; set; } = new List<Units>();

    public virtual ICollection<VacationDays> VacationDays { get; set; } = new List<VacationDays>();

    public virtual WheaterStations WheaterStation { get; set; }

    public virtual ICollection<CompanyInfo> Company { get; set; } = new List<CompanyInfo>();

    public IdNameBO GetIdName()
    {
        IdNameBO bo = new IdNameBO();
        bo.ID = this.ProjectId;
        bo.Display = this.ProjectName;
        return bo;
    }

}