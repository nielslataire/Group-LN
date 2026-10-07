using System;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Budget
{
    /// <summary>
    /// Pure rekenregels voor één verkooplijn van stap 8 (geen I/O, unit-getest). Dezelfde regels staan in JavaScript
    /// op BudgetVerkoop.cshtml voor de live-herberekening; deze klasse is de server-kant (opslaan, kopiëren, tests).
    ///
    ///   bouwwaarde  = BouwPrijsPerM2  × gereduceerde oppervlakte      (bron referentiecode / manueel €/m²)
    ///   grondwaarde = GrondPrijsPerM2 × grondoppervlakte               (idem; bij ruil altijd 0)
    ///   anders (voorstel / markt / manueel bedrag): bedrag blijft, €/m² wordt afgeleid = bedrag / oppervlakte
    ///   vraagprijs  = bouwwaarde + grondwaarde + extra forfait          (behalve manueel bedrag met eigen vraagprijs)
    /// </summary>
    public static class VerkoopLijnRekenregel
    {
        /// <summary>Herrekent de afgeleide velden van de lijn volgens haar bron. Oppervlaktes komen uit stap 2 (BudgetOppervlaktesBO).</summary>
        public static void Herbereken(BudgetVerkoopLijn l, decimal oppGereduceerd, decimal grondopp)
        {
            if (l is null) return;
            var bron = (VerkoopPrijsBron?)l.PrijsBron;
            var perM2 = bron is VerkoopPrijsBron.Referentiecode or VerkoopPrijsBron.ManueelPerM2;

            if (l.IsRuil)
            {
                // Grondruil: de grond wordt niet verkocht; de compensatie zit in het extra forfait.
                l.Grondwaarde     = 0m;
                l.GrondPrijsPerM2 = null;
            }
            else if (perM2)
            {
                if (l.GrondPrijsPerM2.HasValue && grondopp > 0m)
                    l.Grondwaarde = Math.Round(l.GrondPrijsPerM2.Value * grondopp, 2);
            }
            else
            {
                l.GrondPrijsPerM2 = Afgeleid(l.Grondwaarde, grondopp);
            }

            if (perM2)
            {
                if (l.BouwPrijsPerM2.HasValue && oppGereduceerd > 0m)
                    l.Bouwwaarde = Math.Round(l.BouwPrijsPerM2.Value * oppGereduceerd, 2);
            }
            else
            {
                l.BouwPrijsPerM2 = Afgeleid(l.Bouwwaarde, oppGereduceerd);
            }

            var eigenVraagprijs = bron == VerkoopPrijsBron.ManueelBedrag && l.Vraagprijs is > 0m;
            if (!eigenVraagprijs)
                l.Vraagprijs = Vraagprijs(l.Bouwwaarde, l.Grondwaarde, l.ExtraForfait);
        }

        /// <summary>bouw + grond + forfait; null als er geen enkel bedrag is.</summary>
        public static decimal? Vraagprijs(decimal? bouw, decimal? grond, decimal? forfait)
        {
            if (!bouw.HasValue && !grond.HasValue && !forfait.HasValue) return null;
            return Math.Round((bouw ?? 0m) + (grond ?? 0m) + (forfait ?? 0m), 2);
        }

        /// <summary>€/m² uit een bedrag en een oppervlakte; null zonder bedrag of oppervlakte.</summary>
        public static decimal? Afgeleid(decimal? bedrag, decimal opp)
            => bedrag.HasValue && opp > 0m ? Math.Round(bedrag.Value / opp, 2) : null;

        /// <summary>Ligt de vastgelegde vraagprijs onder de minimale verkoopprijs uit het voorstel (kost + marge)?</summary>
        public static bool OnderMinimum(decimal? vraagprijs, decimal minimum)
            => vraagprijs is > 0m && minimum > 0m && vraagprijs.Value < minimum;

        public static string BronLabel(byte? bron) => (VerkoopPrijsBron?)bron switch
        {
            VerkoopPrijsBron.Voorstel       => "voorstel",
            VerkoopPrijsBron.Markt          => "markt",
            VerkoopPrijsBron.Referentiecode => "code",
            VerkoopPrijsBron.ManueelPerM2   => "manueel €/m²",
            VerkoopPrijsBron.ManueelBedrag  => "manueel bedrag",
            _                               => "—"
        };
    }
}
