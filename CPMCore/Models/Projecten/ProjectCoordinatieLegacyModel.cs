// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "CoordinatieLegacy". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
using BOCore;
using CPMCore.Models.Leveranciers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System;

namespace CPMCore.Models.Projecten
{
    public class ProjectCoordinatieModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public BOCore.CoordinationContractType? ContractType { get; set; }
        public decimal? ProjectDistanceKm { get; set; }
        public decimal? KmAllowance { get; set; }
        public int? CoordinationIssuerCompanyId { get; set; }
        public decimal? ContractPrice { get; set; }
        public decimal InvoicedAmount { get; set; }
        public string ProjectManagerUserId { get; set; }
        public List<ProjectContractSliceVM> ContractSlices { get; set; } = new();
        public List<ProjectHourlyRateVM> HourlyRates { get; set; } = new();
        public List<ProjectRegieUurVM> RegieUren { get; set; } = new();

        /// <summary>gl-v2 (Projecten/DetailCoordinatieV2, design-handoff punt 18) — enkel gevuld wanneer die
        /// pagina gerenderd wordt.</summary>
        public DetailCoordinatieV2Vm? GlV2 { get; set; }
    }

    public class ProjectRegieUurVM
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserFullName { get; set; }
        public decimal HourlyRate { get; set; }
        public DateOnly Date { get; set; }
        public decimal Hours { get; set; }
        public bool WithTravel { get; set; }
        public decimal? TravelKm { get; set; }
        public string Description { get; set; }
        public int? InvoiceId { get; set; }
        public string InvoicePublicId { get; set; }
        public bool IsInvoiced => InvoiceId.HasValue;
    }

    public class CoordinatieInstellingenVM
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        [Display(Name = "Coördinatiebedrijf")]
        public int? CoordinationIssuerCompanyId { get; set; }
        [Display(Name = "Facturatiewijze")]
        public BOCore.CoordinationContractType? ContractType { get; set; }
        [Display(Name = "Prijs per km")]
        public decimal? KmAllowance { get; set; }
        [Display(Name = "Referentie")]
        [MaxLength(200)]
        public string CoordinationReference { get; set; }
        [Display(Name = "Contractprijs")]
        public decimal? ContractPrice { get; set; }
        public List<ProjectContractSliceVM> ContractSlices { get; set; } = new();
        public List<ProjectHourlyRateVM> HourlyRates { get; set; } = new();
        // Alleen voor weergave, niet gepost
        public decimal? ProjectDistanceKm { get; set; }
        public int? RouteDurationSeconds { get; set; }
        public List<ProjectIssuerCompanyOptionVM> IssuerCompanies { get; set; } = new();
        public List<IdNameBO> AvailableUsers { get; set; } = new();
    }

    // ── Budget Wizard ViewModels ─────────────────────────────────────────────
}
