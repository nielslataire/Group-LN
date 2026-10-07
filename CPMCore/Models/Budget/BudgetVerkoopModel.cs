using System.Collections.Generic;
using System.Linq;
using DALCore.Models;

namespace CPMCore.Models.Budget
{
    public class BudgetVerkoopModel
    {
        public int    BudgetVersieId { get; set; }
        public int    ProjectId      { get; set; }
        public string ProjectName    { get; set; }
        public string BudgetNaam     { get; set; }
        public int    Versienummer   { get; set; }
        public string VersieLabel    { get; set; }
        public string VersieStatus   { get; set; }

        public List<BudgetVerkoopLijn>     Lijnen                { get; set; } = new();
        public List<BudgetPrijsReferentie> PrijsReferentiesBouw  { get; set; } = new();
        public List<BudgetPrijsReferentie> PrijsReferentiesGrond { get; set; } = new();
        public List<string>                BeschikbareEenheden   { get; set; } = new();

        /// <summary>Bottom-up verkoopvoorstel (kostprijs + marge → grond- en bouwwaarde per eenheid).</summary>
        public BOCore.Budget.BudgetVerkoopVoorstelBO Voorstel { get; set; }

        /// <summary>Units van het project, om een verkooplijn aan een Unit te koppelen (doorzetten).</summary>
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> UnitOptions { get; set; } = new();

        /// <summary>Per eenheid (naam uit stap 2) de oppervlaktes en voorstelbedragen waarmee de verkooplijnen rekenen (JS op stap 8).</summary>
        public Dictionary<string, VerkoopEenheidInfo> EenhedenInfo { get; set; } = new();

        public decimal? VraagprijzenLijnen =>
            Lijnen.Any(l => l.Vraagprijs is > 0m) ? Lijnen.Where(l => l.Vraagprijs is > 0m).Sum(l => l.Vraagprijs.Value) : null;
    }

    /// <summary>Rekenbasis van één eenheid voor de verkooplijnen: bouw op gereduceerde oppervlakte, grond op grondoppervlakte.</summary>
    public class VerkoopEenheidInfo
    {
        public decimal  OppGereduceerd  { get; set; }
        public decimal  Grondopp        { get; set; }
        public decimal  BewoonbareOpp   { get; set; }
        public decimal  VoorstelGrond   { get; set; }
        public decimal  VoorstelBouw    { get; set; }
        /// <summary>Minimale verkoopprijs (kost + marge) = VoorstelGrond + VoorstelBouw.</summary>
        public decimal  Minimum         { get; set; }
        /// <summary>Marktprijs (mediaan €/m² × bewoonbare opp.), null zonder referentie.</summary>
        public decimal? Markt           { get; set; }
        public decimal? MarktPerM2      { get; set; }
    }
}
