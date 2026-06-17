using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<BlogArtikel> BlogArtikel { get; set; }
    public virtual DbSet<BlogArtikelBlok> BlogArtikelBlok { get; set; }
    public virtual DbSet<BlogArtikelFaq> BlogArtikelFaq { get; set; }
}
