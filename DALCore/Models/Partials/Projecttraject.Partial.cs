using BOCore;

namespace DALCore.Models
{
    public partial class Projecttraject
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.Id,
                Display = this.Naam ?? $"Traject {this.Id}"
            };
        }
    }
}
