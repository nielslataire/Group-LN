#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Schijvensjablonen (migratie 155). DbSets + mapping, aangeroepen vanuit <c>OnModelCreatingPartial</c>.</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<SchijfSjabloon> SchijfSjabloon { get; set; }

    public virtual DbSet<SchijfSjabloonSchijf> SchijfSjabloonSchijf { get; set; }

    private void ConfigureSchijfSjabloonEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchijfSjabloon>(entity =>
        {
            entity.ToTable("SchijfSjabloon");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Beschrijving).HasMaxLength(1000);
            entity.Property(e => e.MinimumPercentage).HasColumnType("decimal(9, 4)");
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(450);
        });
        modelBuilder.Entity<SchijfSjabloonSchijf>(entity =>
        {
            entity.ToTable("SchijfSjabloonSchijf");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(250);
            entity.Property(e => e.PlanningCode).HasMaxLength(100);
            entity.Property(e => e.VastPercentage).HasColumnType("decimal(9, 4)");
            entity.Property(e => e.Gewicht).HasColumnType("decimal(9, 4)");
            entity.HasIndex(e => e.SjabloonId, "IX_SchijfSjabloonSchijf_Sjabloon");
            entity.HasOne(d => d.Sjabloon).WithMany(p => p.Schijven).HasForeignKey(d => d.SjabloonId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_SchijfSjabloonSchijf_Sjabloon");
        });
    }
}
