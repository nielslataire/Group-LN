using BOCore;

namespace DALCore.Models
{
    public partial class Mijlpaal
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.Id,
                Display = this.Naam ?? $"Mijlpaal {this.Id}",
                Group = this.ProjecttrajectFase?.Naam
            };
        }

        /// <summary>Effectieve streefdatum: handmatig gezet wint van berekend.</summary>
        public System.DateOnly? EffectieveDoeldatum => this.Doeldatum ?? this.DoeldatumBerekend;
    }
}
