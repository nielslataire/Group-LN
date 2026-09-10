using BOCore;

namespace DALCore.Models
{
    public partial class TrajectSjabloon
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.Id,
                Display = this.Naam ?? $"Sjabloon {this.Id}"
            };
        }
    }
}
