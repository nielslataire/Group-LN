using System;
using System.Collections.Generic;
using System.Linq;
using BOCore.Budget;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CPMCore.Models.Budget
{
    public class BudgetActivityLijnenModel
    {
        public int    BudgetVersieId { get; set; }
        public int    ProjectId      { get; set; }
        public string ProjectName    { get; set; }
        public string BudgetNaam     { get; set; }
        public int    Versienummer   { get; set; }

        public List<BudgetLotGroepBO> LotGroepen { get; set; } = new();

        public decimal TotaalAlternatief => LotGroepen.Sum(g => g.TotaalAlternatief);
        public decimal TotaalNacalc      => LotGroepen.Sum(g => g.TotaalNacalc);
        public decimal Verschil          => TotaalAlternatief - TotaalNacalc;

        public decimal OppervlakteGBA  { get; set; }
        public int     AantalEenheden  { get; set; }

        public decimal PrijsPerM2GBA =>
            OppervlakteGBA == 0 ? 0m : TotaalAlternatief / OppervlakteGBA;

        public decimal ABEXBasis  { get; set; }
        public decimal ABEXHuidig { get; set; }

        public decimal ABEXFactor =>
            ABEXBasis == 0 ? 1m : ABEXHuidig / ABEXBasis;

        public IEnumerable<SelectListItem> BeschikbareProjecten { get; set; } =
            new List<SelectListItem>();

        public List<string> WizardSteps { get; set; } = new List<string>
        {
            "Gegevens", "Oppervlaktes", "Sanitair", "Gevels",
            "Dak & Afbraak", "Activiteiten", "Parameters", "Verkoop", "Resultaat"
        };

        public int HuidigeStap { get; set; } = 6;
    }
}
