using System;
using System.Collections.Generic;
using BOCore;
using BOCore.Budget;

namespace CPMCore.Models.Instellingen
{
    /// <summary>Instellingen › Budget › Referentieprojecten (nacalc, design-handoff 38c): afgewerkte projecten met hun werkelijke kost per activiteit.</summary>
    public class BudgetReferentieProjectenViewModel
    {
        public List<BudgetReferentieProjectBO> Referenties { get; set; } = new();
        /// <summary>Projecten in de app waaruit een referentieproject gemaakt kan worden.</summary>
        public List<IdNameBO> Projecten { get; set; } = new();

        /// <summary>Huidige (actieve) S- en I2021-index, voor de kolom "Index S / I" = factor peildatum → nu.</summary>
        public decimal HuidigeSIndex { get; set; }
        public decimal HuidigeIIndex { get; set; }

        public ReferentieExcelFormVm   Excel   { get; set; } = new();
        public ReferentieProjectFormVm Project { get; set; } = new();
    }

    /// <summary>38c "Uit Excel inladen" — kopgegevens; het bestand zelf komt als IFormFile.</summary>
    public class ReferentieExcelFormVm
    {
        public string    Naam           { get; set; }
        public DateTime? Datum          { get; set; }
        public int?      AantalEenheden { get; set; }
        public decimal?  OppervlakteGBA { get; set; }
        public string    Opmerking      { get; set; }
    }

    /// <summary>38c "Uit een project van de app".</summary>
    public class ReferentieProjectFormVm
    {
        public int?      ProjectId { get; set; }
        public string    Naam      { get; set; }
        public DateTime? Datum     { get; set; }
        public string    Opmerking { get; set; }
        /// <summary>Eigen GBA (bewoonbare oppervlakte, m²) als het project geen budget heeft; leeg = uit het laatste budget.</summary>
        public decimal?  OppervlakteGBA { get; set; }
        /// <summary>Eigen aantal eenheden als het project geen units heeft; leeg = woon-/commerciële units van het project.</summary>
        public int?      AantalEenheden { get; set; }
    }

    /// <summary>38d "Controle na Excel-import": de ingelezen regels, niet-gematcht bovenaan, met per regel een keuzelijst; opslaan post dezelfde regels terug.</summary>
    public class ReferentieImportControleVm
    {
        public string    Bestandsnaam   { get; set; }
        public string    Naam           { get; set; }
        public DateTime? Datum          { get; set; }
        public int       AantalEenheden { get; set; }
        public decimal?  OppervlakteGBA { get; set; }
        public string    Opmerking      { get; set; }
        public List<ReferentieImportRijVm> Rijen { get; set; } = new();

        /// <summary>Enkel voor de weergave (keuzelijst per niet-gematchte regel); wordt niet teruggepost.</summary>
        public List<BudgetReferentieLijnBO> Activiteiten { get; set; } = new();
    }

    public class ReferentieImportRijVm
    {
        public int?    ExcelId              { get; set; }
        public string  ExcelNaam            { get; set; }
        public decimal Bedrag               { get; set; }
        public int?    ActivityId           { get; set; }
        public string  ActivityOmschrijving { get; set; }
        /// <summary>"id", "naam", "manueel" of leeg.</summary>
        public string  Match                { get; set; }
    }
}
