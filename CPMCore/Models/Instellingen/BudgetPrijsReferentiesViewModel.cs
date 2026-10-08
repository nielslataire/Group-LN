using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;
using BOCore.Budget;

namespace CPMCore.Models.Instellingen
{
    /// <summary>Instellingen › Budget › Prijsreferenties verkoop (design-handoff 38a/38b): algemene en projectspecifieke €/m²-codes voor stap 8.</summary>
    public class BudgetPrijsReferentiesViewModel
    {
        public List<BudgetPrijsReferentieBO> Referenties { get; set; } = new();
        /// <summary>Projecten voor de optionele projectkoppeling van een code.</summary>
        public List<IdNameBO> Projecten { get; set; } = new();

        /// <summary>Invulvoorstel voor een nieuwe code per type (eerstvolgende vrije).</summary>
        public int VolgendeCodeBouw  { get; set; } = 1;
        public int VolgendeCodeGrond { get; set; } = 1;

        /// <summary>Gl-v2: welk tabblad open staat na een redirect ("Bouw"/"Grond").</summary>
        public string StartTab { get; set; } = "Bouw";

        public IEnumerable<BudgetPrijsReferentieBO> Bouw  => Referenties.Where(r => r.PrijsType == "Bouw");
        public IEnumerable<BudgetPrijsReferentieBO> Grond => Referenties.Where(r => r.PrijsType == "Grond");
    }

    /// <summary>Formulier (toevoegen én inline bewerken) van één prijsreferentie; veldnamen = de parameters die de POST-acties al kenden.</summary>
    public class PrijsReferentieFormVm
    {
        public int      Id           { get; set; }
        public string   PrijsType    { get; set; } = "Bouw";
        public int?     Code         { get; set; }
        public string   Omschrijving { get; set; }
        public decimal? PrijsPerM2   { get; set; }
        public DateTime? Datum       { get; set; }
        public string   Bron         { get; set; }
        public int?     ProjectId    { get; set; }
    }
}
