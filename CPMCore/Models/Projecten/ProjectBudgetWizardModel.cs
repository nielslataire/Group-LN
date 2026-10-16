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

        // gl-v2 overzicht (design-handoff 39a)
        /// <summary>De ene definitieve versie van het project, of null.</summary>
        public BudgetVersieBO DefinitiefVersie { get; set; }
        /// <summary>Totale kostprijs per versie (enkel berekend voor afgeronde/definitieve versies en de huidige).</summary>
        public Dictionary<int, decimal?> Totalen { get; set; } = new();
        /// <summary>Marge op kostprijs (resultaat / totale kostprijs) en resultaat in euro per versie waarvan alle stappen doorlopen zijn; null = niet te berekenen (geen vraagprijzen).</summary>
        public Dictionary<int, BudgetMargeVm> Marges { get; set; } = new();
        /// <summary>Aantal eenheden (stap 2) van de huidige versie per budget.</summary>
        public Dictionary<int, int> EenhedenPerMaster { get; set; } = new();
        /// <summary>Versies van alle projecten om een nieuw budget als kopie van te starten (ID = versieId, Display, Group = project · budget).</summary>
        public List<IdNameBO> KopieerOpties { get; set; } = new();
        /// <summary>Budgetten (met hun versies) waarvan een nieuw budget een kopie kan zijn: eerst die van dit project, dan van andere projecten.</summary>
        public List<BudgetKopieBronVm> KopieBronnen { get; set; } = new();
        /// <summary>Aantal eenheden van het project (zonder links): de bron voor een leeg budget.</summary>
        public int AantalProjectEenheden { get; set; }
        /// <summary>"S- en I-index 10/2026": de index die een nieuw budget standaard krijgt.</summary>
        public string ActieveIndexTekst { get; set; } = "";
        /// <summary>Het definitieve budget tegenover contracten en facturen (tegels in de kaart Definitief budget); null zonder definitief budget.</summary>
        public BudgetKoppelingVm Koppeling { get; set; }
    }

    /// <summary>Het definitieve budget (enkel activiteiten) tegenover contracten (+ bijbestellingen) en facturen (incl. meerwerk, excl. meerwerk voor klanten).</summary>
    public class BudgetKoppelingVm
    {
        public decimal Begroot { get; set; }
        public decimal Gecontracteerd { get; set; }
        public decimal Gefactureerd { get; set; }
        /// <summary>Per activiteit: het hoogste van gecontracteerd en gefactureerd, of het budget zolang er nog niets is vastgelegd.</summary>
        public decimal VerwachteEindkost { get; set; }
        /// <summary>Begroot min verwachte eindkost; positief = onder budget.</summary>
        public decimal VerwachtVerschil => Begroot - VerwachteEindkost;
        public int AantalActiviteiten { get; set; }
        public int AantalOverBudget { get; set; }
    }

    public class BudgetKopieBronVm
    {
        public int MasterId { get; set; }
        public string Naam { get; set; }
        public string ProjectNaam { get; set; }
        public bool EigenProject { get; set; }
        public List<IdNameBO> Versies { get; set; } = new();
    }

    /// <summary>Pagina "Budgetten vergelijken" (39n): de vergelijking plus de overzichtsgegevens voor de modal "Versie toevoegen".</summary>
    public class BudgetVergelijkPaginaModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public ServiceCore.Budget.BudgetVergelijking Vergelijking { get; set; }
        public BudgetIndexModel Index { get; set; }
        public string Modus { get; set; } = "alles";
        public List<int> VersieIds { get; set; } = new();
    }

    public class BudgetMargeVm
    {
        public decimal Percentage { get; set; }
        public decimal Bedrag { get; set; }
    }

    public class BudgetMasterAanmakenModel
    {
        public int ProjectId { get; set; }
        // Nullable: zonder ? telt een niet-ingevuld veld als "verplicht" (implicit [Required]); het venster Nieuw budget heeft geen omschrijving meer.
        public string? ProjectName { get; set; }

        [Required(ErrorMessage = "Naam is verplicht.")]
        public string Naam { get; set; } = "";

        public string? Omschrijving { get; set; }

        /// <summary>gl-v2 39a: versie waarvan de inhoud gekopieerd wordt (leeg = leeg budget).</summary>
        public int? KopieVanVersieId { get; set; }

        /// <summary>39l "Overnemen": de stappen die uit de kopie meekomen (gegevens, oppervlaktes, sanitair, gevels, dak, activiteiten, parameters, verkoop, opmerkingen).</summary>
        public List<string> Overnemen { get; set; } = new();
        /// <summary>De nieuwe modal stuurt altijd true mee: dan telt de keuze in <see cref="Overnemen"/> (ook als niets aangevinkt is); zonder is het een klassiek formulier en wordt alles gekopieerd.</summary>
        public bool OvernemenIngevuld { get; set; }
        /// <summary>39l "Meteen actief budget maken".</summary>
        public bool ActiefMaken { get; set; }
        /// <summary>39l EENHEDEN: "project" (eenheden en oppervlaktes uit het project) of "leeg".</summary>
        public string EenhedenBron { get; set; } = "project";
        /// <summary>39l INDEX: "actueel" (de nieuwste S- en I-index) of "bron" (die van de gekopieerde versie).</summary>
        public string IndexBron { get; set; } = "actueel";
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
        /// <summary>Aantal garagepoorten (geheel getal; als decimal binnengekomen zodat JSON met komma/punt nooit faalt).</summary>
        public decimal Poorten { get; set; }
        public decimal DoorritGVL { get; set; }
        public decimal Zolder { get; set; }
        public decimal GemeenschappelijkeDelen { get; set; }
        public decimal Wegenis { get; set; }
        public decimal Grondopp { get; set; }
        public decimal GrondoppAgrarisch { get; set; }
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
