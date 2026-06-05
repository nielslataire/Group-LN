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
        public string VersieLabel    { get; set; }
        public string VersieStatus   { get; set; }

        public List<BudgetLotGroepBO> LotGroepen { get; set; } = new();

        public decimal TotaalAlternatief => LotGroepen.Sum(g => g.TotaalAlternatief);
        public decimal TotaalNacalc      => LotGroepen.Sum(g => g.TotaalNacalc);
        public decimal Verschil          => TotaalAlternatief - TotaalNacalc;

        public decimal OppervlakteGBA  { get; set; }
        public int     AantalEenheden  { get; set; }

        public decimal PrijsPerM2GBA =>
            OppervlakteGBA == 0 ? 0m : TotaalAlternatief / OppervlakteGBA;

        public decimal SIndexStart  { get; set; }
        public decimal SIndexHuidig { get; set; }
        public decimal IIndexStart  { get; set; }
        public decimal IIndexHuidig { get; set; }

        public decimal GewogenFactor
        {
            get
            {
                var s = SIndexStart > 0 ? SIndexHuidig / SIndexStart : 1m;
                var i = IIndexStart > 0 ? IIndexHuidig / IIndexStart : 1m;
                return i * 0.40m + s * 0.40m + 0.20m;
            }
        }

        public IEnumerable<SelectListItem> BeschikbareProjecten { get; set; } =
            new List<SelectListItem>();
    }
}
