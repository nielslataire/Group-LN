using System;
using System.Collections.Generic;
using System.Linq;
using BOCore.Budget;

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

        // ── Nacalc: referentieprojecten (okt. 2026) ──────────────────────────
        /// <summary>Alle referentieprojecten (Instellingen › Budget › Referentieprojecten) om uit te kiezen.</summary>
        public List<BudgetReferentieProjectBO> Referenties { get; set; } = new();
        /// <summary>Referentieprojecten die deze versie vergelijkt.</summary>
        public List<int> GeselecteerdeReferentieIds { get; set; } = new();

        /// <summary>Er is minstens één gekozen referentieproject én minstens één activiteit met een referentieprijs.</summary>
        public bool HeeftReferenties => GeselecteerdeReferentieIds.Count > 0 && LotGroepen.SelectMany(g => g.Lijnen).Any(l => l.HeeftReferentie);

        /// <summary>Referentiekost over de activiteiten die in dit budget een bedrag hebben (dezelfde selectie als de tabel).</summary>
        public decimal TotaalReferentie =>
            LotGroepen.SelectMany(g => g.Lijnen).Where(l => l.TotaalAlternatief > 0 && l.HeeftReferentie).Sum(l => l.ReferentieTotaal ?? 0m);

        /// <summary>Activiteiten met bedrag in dit budget waarvoor geen enkel referentieproject een bedrag heeft.</summary>
        public int AantalZonderReferentie =>
            LotGroepen.SelectMany(g => g.Lijnen).Count(l => l.TotaalAlternatief > 0 && !l.HeeftReferentie);
    }
}
