#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Verkoop (design-handoff 45, migratie 115): eigen kostenposten bij de kosten koper — DbSet + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<ProjectSalesExtraCost> ProjectSalesExtraCost { get; set; }

    private void ConfigureVerkoopEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectSalesExtraCost>(entity =>
        {
            entity.ToTable("ProjectSalesExtraCost");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder });
        });
    }
}
