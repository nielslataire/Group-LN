using System.Collections.Generic;
using CPMCore.Models.GlV2;
using DALCore.Models;

namespace CPMCore.Models.Budget
{
    /// <summary>Uitklapbare detailregel van een eenheid op stap 8 (gl-v2, design-handoff 39i): code → €/m² → bedrag voor grond en bouw, bron, ruil, forfait, unit.</summary>
    public class BwVerkoopDetailVm
    {
        public string Naam { get; set; } = "";
        public BudgetVerkoopLijn Lijn { get; set; }
        public List<GlV2SelectItem> RefBouw { get; set; } = new();
        public List<GlV2SelectItem> RefGrond { get; set; } = new();
        public List<GlV2SelectItem> Units { get; set; } = new();
        public int Rij { get; set; }
    }
}
