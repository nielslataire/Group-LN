using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<Vacature> Vacature { get; set; }
}
