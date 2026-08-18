using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<Vacature> Vacature { get; set; }
    public virtual DbSet<VacatureTaak> VacatureTaak { get; set; }
    public virtual DbSet<VacatureVereiste> VacatureVereiste { get; set; }
    public virtual DbSet<VacatureVoordeel> VacatureVoordeel { get; set; }
    public virtual DbSet<VacatureSollicitatieStap> VacatureSollicitatieStap { get; set; }
}
