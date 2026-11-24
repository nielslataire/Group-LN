#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Bouwstaten (migratie 158). DbSet + mapping, aangeroepen vanuit <c>OnModelCreatingPartial</c>.</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<Bouwstaat> Bouwstaat { get; set; }

    private void ConfigureBouwstaatEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bouwstaat>(entity =>
        {
            entity.ToTable("Bouwstaat");
            entity.Property(e => e.Code).IsRequired().HasMaxLength(60);
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Groep).HasMaxLength(100);
            entity.HasIndex(e => e.Code, "IX_Bouwstaat_Code");
        });
    }
}
