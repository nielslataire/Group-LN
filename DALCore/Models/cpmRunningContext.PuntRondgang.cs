#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Rondgangen van Punten (migratie 100) — DbSets + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<PuntRondgang> PuntRondgang { get; set; }
    public virtual DbSet<PuntRondgangFoto> PuntRondgangFoto { get; set; }

    private void ConfigurePuntRondgangEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PuntRondgang>(entity =>
        {
            entity.ToTable("PuntRondgang");
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.StartedAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.Step).HasMaxLength(60);
            entity.Property(e => e.AudioFile).HasMaxLength(300);
            entity.Property(e => e.AudioType).HasMaxLength(100);
            entity.Property(e => e.Error).HasMaxLength(500);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => new { e.ProjectId, e.UserId });
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PuntRondgang_Project");
        });

        modelBuilder.Entity<PuntRondgangFoto>(entity =>
        {
            entity.ToTable("PuntRondgangFoto");
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(300);
            entity.HasIndex(e => new { e.RondgangId, e.Volgnr }).IsUnique();
            entity.HasOne(d => d.Rondgang).WithMany(p => p.Fotos)
                .HasForeignKey(d => d.RondgangId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PuntRondgangFoto_Rondgang");
        });
    }
}
