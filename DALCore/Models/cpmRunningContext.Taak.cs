#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// Trajectopvolging — persoonlijke/interne takenlaag ("Mijn taken", increment 6). DbSet + fluent
/// config, aangeroepen vanuit <c>OnModelCreatingPartial</c> in <c>cpmRunningContext.Seeding.cs</c>.
/// </summary>
public partial class cpmRunningContext
{
    public virtual DbSet<ProjectTaak> ProjectTaak { get; set; }

    private void ConfigureTaakEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectTaak>(entity =>
        {
            entity.ToTable("ProjectTaak");
            entity.Property(e => e.Titel).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ToegewezenAanUserId).HasMaxLength(128);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(128);
            entity.Property(e => e.Status).HasDefaultValue(0);
            entity.Property(e => e.Prioriteit).HasDefaultValue(1);
            entity.Property(e => e.Herkomst).HasDefaultValue(0);
            entity.Property(e => e.Vervaldatum).HasColumnType("date");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ToegewezenAanUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Vervaldatum);
            entity.HasIndex(e => e.MijlpaalId);
            entity.HasIndex(e => e.ProjectDossierId);
            entity.HasIndex(e => e.ConstructionIssueId);

            // Enkel ProjectId -> Project cascadet. Unit/Mijlpaal/ProjectDossier/ConstructionIssue
            // zijn allen (indirect) ook via Project bereikbaar -> NO ACTION om SQL Server Msg 1785
            // (meerdere cascade-paden) te vermijden, zelfde patroon als ProjectDossierMijlpaal.
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectTaak_Project");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectTaak_Units");
            entity.HasOne(d => d.Mijlpaal).WithMany()
                .HasForeignKey(d => d.MijlpaalId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectTaak_Mijlpaal");
            entity.HasOne(d => d.ProjectDossier).WithMany()
                .HasForeignKey(d => d.ProjectDossierId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectTaak_ProjectDossier");
            entity.HasOne(d => d.ConstructionIssue).WithMany()
                .HasForeignKey(d => d.ConstructionIssueId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectTaak_ConstructionIssue");
        });
    }
}
