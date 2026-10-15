using System;
using System.Collections.Generic;
using System.Linq;
using BOCore.Budget;

namespace ServiceCore.Budget
{
    /// <summary>
    /// Stap 9, "Verkoop t.o.v. kostprijs": verkoopwaarde (vastgelegde vraagprijzen, anders de aanbevolen), min verkoopkosten, min kostprijs = resultaat.
    /// Verkoopkosten = makelaarscommissie op de verkoopprijs per eenheid + overige verkoopkosten. De btw op de commissie (21 %) telt enkel mee op het
    /// grondaandeel (niet aftrekbaar); op het bouwaandeel is ze aftrekbaar en dus geen kost. Het grond-/bouwaandeel komt uit de verkooplijnen van stap 8.
    /// </summary>
    public class VerkoopResultaat
    {
        public const decimal BtwCommissie = 0.21m;

        public bool HeeftVerkoop { get; set; }
        public int AantalLijnen { get; set; }
        public string Bron { get; set; }
        public decimal Verkoopwaarde { get; set; }
        public decimal GrondDeel { get; set; }
        public decimal BouwDeel { get; set; }
        public decimal CommissiePerc { get; set; }
        public decimal CommissieGrond { get; set; }
        public decimal BtwGrond { get; set; }
        public decimal CommissieBouw { get; set; }
        public decimal BtwBouwAftrekbaar { get; set; }
        public decimal Overige { get; set; }
        public decimal Kostprijs { get; set; }

        public decimal GrondKostInclBtw => CommissieGrond + BtwGrond;
        public decimal Verkoopkosten => CommissieGrond + BtwGrond + CommissieBouw + Overige;
        public decimal NettoOpbrengst => Verkoopwaarde - Verkoopkosten;
        public decimal Resultaat => NettoOpbrengst - Kostprijs;
        public decimal MargeVoorKosten => Kostprijs > 0m ? (Verkoopwaarde - Kostprijs) / Kostprijs : 0m;
        public decimal MargeOpKostprijs => Kostprijs > 0m ? Resultaat / Kostprijs : 0m;
        public decimal MargeOpVerkoop => Verkoopwaarde > 0m ? Resultaat / Verkoopwaarde : 0m;
        public decimal ResultaatPerLijn => AantalLijnen > 0 ? Resultaat / AantalLijnen : 0m;

        /// <summary>Verkoopwaarde waarbij het resultaat precies 0 is, bij hetzelfde grond-/bouwaandeel.</summary>
        public decimal BreakEven
        {
            get
            {
                var grondAandeel = Verkoopwaarde > 0m ? GrondDeel / Verkoopwaarde : 0m;
                var f = CommissiePerc * (1m + BtwCommissie * grondAandeel);   // verkoopkosten per euro verkoopwaarde
                return f < 1m ? Math.Round((Kostprijs + Overige) / (1m - f), 0) : 0m;
            }
        }
        /// <summary>Hoeveel de verkoopwaarde mag zakken voor het resultaat 0 is (negatief = ruimte); t.o.v. de huidige verkoopwaarde.</summary>
        public decimal RuimteTovBreakEven => Verkoopwaarde > 0m && BreakEven > 0m ? (BreakEven - Verkoopwaarde) / Verkoopwaarde : 0m;

        public static VerkoopResultaat Bereken(BudgetVerkoopSamenvattingBO sam, BudgetVerkoopVoorstelBO voorstel, IReadOnlyList<DALCore.Models.BudgetVerkoopLijn> lijnen,
                                               decimal commissiePerc, decimal overige, decimal kostprijs)
        {
            var metPrijs = (lijnen ?? new List<DALCore.Models.BudgetVerkoopLijn>()).Where(l => l.Vraagprijs is > 0m).ToList();
            var r = new VerkoopResultaat
            {
                Verkoopwaarde = sam.Opbrengst,
                Bron = sam.OpbrengstBron,
                AantalLijnen = metPrijs.Count > 0 ? metPrijs.Count : sam.AantalEenheden,
                CommissiePerc = commissiePerc,
                Overige = Math.Max(0m, overige),
                Kostprijs = kostprijs
            };
            r.HeeftVerkoop = r.Verkoopwaarde > 0m;
            if (!r.HeeftVerkoop) return r;

            var vsom = voorstel.Grondwaarde + voorstel.Bouwwaarde;
            var voorstelShare = vsom > 0m ? voorstel.Grondwaarde / vsom : 0m;
            decimal grond = 0m;
            if (metPrijs.Count > 0)
                foreach (var l in metPrijs)
                {
                    decimal g = (l.Grondwaarde ?? 0m) + (l.GrondwaardeAgrarisch ?? 0m), b = l.Bouwwaarde ?? 0m;
                    grond += l.Vraagprijs!.Value * (g + b > 0m ? g / (g + b) : voorstelShare);
                }
            else grond = r.Verkoopwaarde * voorstelShare;

            r.GrondDeel = Math.Round(grond, 2);
            r.BouwDeel = r.Verkoopwaarde - r.GrondDeel;
            r.CommissieGrond = Math.Round(r.GrondDeel * commissiePerc, 2);
            r.BtwGrond = Math.Round(r.CommissieGrond * BtwCommissie, 2);
            r.CommissieBouw = Math.Round(r.BouwDeel * commissiePerc, 2);
            r.BtwBouwAftrekbaar = Math.Round(r.CommissieBouw * BtwCommissie, 2);
            return r;
        }
    }
}
