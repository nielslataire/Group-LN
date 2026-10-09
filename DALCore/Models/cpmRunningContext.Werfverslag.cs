#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Werfverslagen (Punten, design-handoff 40i/40l) — DbSets + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<Werfverslag> Werfverslag { get; set; }
    public virtual DbSet<WerfverslagPunt> WerfverslagPunt { get; set; }

    private void ConfigureWerfverslagEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Werfverslag>(entity =>
        {
            entity.ToTable("Werfverslag");
            // De kolommen zijn TINYINT (migratie 082); in code blijven het gewone ints.
            entity.Property(e => e.VerslagType).HasConversion<byte>();
            entity.Property(e => e.Status).HasConversion<byte>();
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Datum).HasColumnType("date");
            entity.Property(e => e.Uur).HasColumnType("time(0)");
            entity.Property(e => e.Weer).HasMaxLength(100);
            entity.Property(e => e.VolgendBezoek).HasColumnType("datetime2(0)");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.VerzondenDoor).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => new { e.ProjectId, e.Datum });
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Werfverslag_Project");
        });

        modelBuilder.Entity<WerfverslagPunt>(entity =>
        {
            entity.ToTable("WerfverslagPunt");
            entity.Property(e => e.TerPlaatse).HasConversion<byte?>();
            entity.Property(e => e.Opmerking).HasMaxLength(300);
            entity.HasIndex(e => new { e.VerslagId, e.IssueId }).IsUnique();
            entity.HasOne(d => d.Verslag).WithMany(p => p.Punten)
                .HasForeignKey(d => d.VerslagId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WerfverslagPunt_Verslag");
            entity.HasOne(d => d.Issue).WithMany()
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_WerfverslagPunt_Issue");
        });
    }
}
