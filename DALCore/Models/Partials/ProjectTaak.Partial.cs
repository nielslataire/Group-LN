using BOCore;

namespace DALCore.Models
{
    public partial class ProjectTaak
    {
        public IdNameBO GetIdName()
        {
            return new IdNameBO
            {
                ID = this.Id,
                Display = this.Titel ?? $"Taak {this.Id}",
                Group = this.Project?.ProjectName
            };
        }
    }
}
