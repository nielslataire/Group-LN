// Opgesplitst uit het vroegere Models/Projecten/ProjectModel.cs (okt. 2026, "views/controllers/models structureren") - groep "Budget". Zelfde namespace, dus geen enkele @@using CPMCore.Models.Projecten elders hoeft te wijzigen.
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
    public class BudgetGevelsDakModel
    {
        public int VersieId { get; set; }
        public int MasterId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string VersieLabel { get; set; }
        public string MasterNaam { get; set; }

        [ValidateNever]
        public Dictionary<string, List<BudgetGevelElementBO>> Elementen { get; set; } = new();

        [ValidateNever]
        public BudgetGevelTotaalBO Totaal { get; set; } = new();

        public int? AantalVeluxen { get; set; }
    }

    public class BudgetPageHeaderModel
    {
        public int    StepNum         { get; set; }
        public int    TotalSteps      { get; set; } = 9;
        public string PageTitle       { get; set; }
        public string PageDescription { get; set; }
        public string MasterNaam      { get; set; }
        public string VersieLabel     { get; set; }
        public string VersieStatus    { get; set; }
        public string PrevUrl         { get; set; }
        public string NextUrl         { get; set; }
        public bool   ShowSaveButton  { get; set; }
        public string SaveFormId      { get; set; }
    }

    public class BudgetWizardTabsModel
    {
        public int  VersieId        { get; set; }
        public int  ActiveStep      { get; set; }
        public bool ShowCalcToggle  { get; set; } = true;
    }

    public class DakSectieVM
    {
        public string ElementType { get; set; }
        public List<BudgetGevelElementBO> Rijen { get; set; } = new();
        public int VersieId { get; set; }
        public decimal Subtotaal { get; set; }
    }

    public class BudgetGevelElementModel
    {
        public int ElementId { get; set; }
        public int VersieId { get; set; }
        public string ElementType { get; set; }
        public string EenheidNaam { get; set; }
        public string Beschrijving { get; set; }
        public decimal Aantal { get; set; }
        public decimal? Breedte { get; set; }
        public decimal? Hoogte { get; set; }
        public decimal? Lengte { get; set; }
    }

    public class BudgetIndexModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public List<BudgetMasterBO> BudgetMasters { get; set; } = new();
    }

    public class BudgetMasterAanmakenModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Naam is verplicht.")]
        public string Naam { get; set; }

        public string Omschrijving { get; set; }
    }

    public class BudgetOppervlaktesModel
    {
        public int VersieId { get; set; }
        public int MasterId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string VersieLabel { get; set; }
        public string MasterNaam { get; set; }

        [ValidateNever]
        public List<BudgetOppervlaktesBO> Rijen { get; set; } = new();

        [ValidateNever]
        public BudgetOppervlaktesTotaalBO Totalen { get; set; } = new();

        [ValidateNever]
        public List<SelectListItem> GroupTypes { get; set; } = new();

        [ValidateNever]
        public List<UnitTypeBO> AllTypes { get; set; } = new();
    }

    public class BudgetOppervlaktesRijModel
    {
        public int RijId { get; set; }
        public int VersieId { get; set; }
        public string EenheidNaam { get; set; }
        public int? UnitGroupTypeId { get; set; }
        public int? UnitTypeId { get; set; }
        public decimal BewoonbareOpp { get; set; }
        public decimal Tuin { get; set; }
        public decimal TerrasPrefab { get; set; }
        public decimal TerrasGelijkvloers { get; set; }
        public decimal Dakterras { get; set; }
        public decimal GaragesParkingsBovenGr { get; set; }
        public decimal GarBergOndergronds { get; set; }
        public decimal BergGelijkvloers { get; set; }
        public decimal Carports { get; set; }
        public decimal DoorritGVL { get; set; }
        public decimal Zolder { get; set; }
        public decimal GemeenschappelijkeDelen { get; set; }
        public decimal Wegenis { get; set; }
        public decimal Grondopp { get; set; }
    }

    public class BudgetSanitairModel
    {
        public int VersieId { get; set; }
        public int MasterId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string VersieLabel { get; set; }
        public string MasterNaam { get; set; }

        [ValidateNever]
        public List<BudgetSanitairBO> Rijen { get; set; } = new();

        [ValidateNever]
        public BudgetSanitairTotaalBO Totaal { get; set; } = new();
    }

    public class BudgetSanitairRijModel
    {
        public int RijId { get; set; }
        public int VersieId { get; set; }
        public string EenheidNaam { get; set; }
        public int? UnitTypeId { get; set; }
        public int Badkamer { get; set; }
        public int ToiletInBadkamer { get; set; }
        public int AfzonderlijkToilet { get; set; }
        public int DoucheInBadkamer { get; set; }
        public int Douchekamer { get; set; }
    }

    public class BudgetGegevensModel
    {
        public int VersieId { get; set; }
        public int MasterId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string VersieLabel { get; set; }
        public string VersieStatus { get; set; }
        public string MasterNaam { get; set; }
        public DateTime VersieCreatedAt { get; set; }

        [ValidateNever]
        public BudgetGegevensBO Gegevens { get; set; } = new();

        [ValidateNever]
        public List<SelectListItem> BouwheerOptions { get; set; } = new();

        [ValidateNever]
        public List<SelectListItem> TypeDakOptions { get; set; } = new()
        {
            new SelectListItem("Plat dak",             "Plat dak"),
            new SelectListItem("Hellend dak",          "Hellend dak"),
            new SelectListItem("Combinatie plat/hellend", "Combinatie plat/hellend"),
        };

        [ValidateNever]
        public List<SelectListItem> TypePoortenOptions { get; set; } = new()
        {
            new SelectListItem("Sectionaalpoort", "Sectionaalpoort"),
            new SelectListItem("Kantelpoort",     "Kantelpoort"),
            new SelectListItem("Geen",            "Geen"),
        };

        // Formule-voorstellen: sleutel → berekend resultaat (null = geen koppeling of formule)
        [ValidateNever]
        public Dictionary<string, ServiceCore.Budget.FormulaResultaat> FormulaVoorstellingen { get; set; } = new();
    }
}
