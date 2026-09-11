#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// Trajectopvolging — generieke dossier-/workstreamlaag (increment 4). DbSets + fluent config,
/// aangeroepen vanuit <c>OnModelCreatingPartial</c> in <c>cpmRunningContext.Seeding.cs</c>.
/// </summary>
public partial class cpmRunningContext
{
    public virtual DbSet<ProjectDossier> ProjectDossier { get; set; }
    public virtual DbSet<ProjectDossierGebeurtenis> ProjectDossierGebeurtenis { get; set; }
    public virtual DbSet<ProjectDossierDocument> ProjectDossierDocument { get; set; }
    public virtual DbSet<ProjectDossierMijlpaal> ProjectDossierMijlpaal { get; set; }
    public virtual DbSet<ProjectNutsAansluiting> ProjectNutsAansluiting { get; set; }
    public virtual DbSet<ProjectDossierSubstap> ProjectDossierSubstap { get; set; }

    private void ConfigureDossierEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectDossier>(entity =>
        {
            entity.ToTable("ProjectDossier");
            entity.Property(e => e.Titel).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Referentie).HasMaxLength(100);
            entity.Property(e => e.VerantwoordelijkeUserId).HasMaxLength(128);
            entity.Property(e => e.ExterneContactNaam).HasMaxLength(150);
            entity.Property(e => e.ExterneContactEmail).HasMaxLength(254);
            entity.Property(e => e.Bedrag).HasColumnType("decimal(19,4)");
            entity.Property(e => e.AanvraagDatum).HasColumnType("date");
            entity.Property(e => e.VerwachteAfhandelingDatum).HasColumnType("date");
            entity.Property(e => e.AfgehandeldDatum).HasColumnType("date");
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(128);
            entity.Property(e => e.DossierKind).HasDefaultValue(0);
            entity.Property(e => e.Status).HasDefaultValue(0);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.ProjectId, e.DossierKind });
            entity.HasIndex(e => e.UnitId);
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectDossier_Project");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectDossier_Units");
        });

        modelBuilder.Entity<ProjectDossierGebeurtenis>(entity =>
        {
            entity.ToTable("ProjectDossierGebeurtenis");
            entity.Property(e => e.Titel).HasMaxLength(200);
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Type).HasDefaultValue(0);
            entity.Property(e => e.Datum).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ProjectDossierId);
            entity.HasOne(d => d.ProjectDossier).WithMany(p => p.Gebeurtenissen)
                .HasForeignKey(d => d.ProjectDossierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectDossierGebeurtenis_Dossier");
        });

        modelBuilder.Entity<ProjectDossierDocument>(entity =>
        {
            entity.ToTable("ProjectDossierDocument");
            entity.Property(e => e.FileId).HasMaxLength(200);
            entity.Property(e => e.Naam).HasMaxLength(200);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ProjectDossierId);
            entity.HasOne(d => d.ProjectDossier).WithMany(p => p.Documenten)
                .HasForeignKey(d => d.ProjectDossierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectDossierDocument_Dossier");
            entity.HasOne(d => d.ProjectDoc).WithMany()
                .HasForeignKey(d => d.ProjectDocId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectDossierDocument_ProjectDoc");
        });

        modelBuilder.Entity<ProjectDossierMijlpaal>(entity =>
        {
            entity.ToTable("ProjectDossierMijlpaal");
            entity.HasIndex(e => new { e.ProjectDossierId, e.MijlpaalId }).IsUnique();
            entity.HasOne(d => d.ProjectDossier).WithMany(p => p.DossierMijlpalen)
                .HasForeignKey(d => d.ProjectDossierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectDossierMijlpaal_Dossier");
            // NO ACTION: Mijlpaal cascadet al via Projecttraject -> Project; een tweede pad hierheen
            // via Project -> ProjectDossier -> ProjectDossierMijlpaal geeft SQL Server Msg 1785.
            entity.HasOne(d => d.Mijlpaal).WithMany()
                .HasForeignKey(d => d.MijlpaalId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectDossierMijlpaal_Mijlpaal");
        });

        modelBuilder.Entity<ProjectNutsAansluiting>(entity =>
        {
            entity.ToTable("ProjectNutsAansluiting");
            entity.Property(e => e.Ean).HasMaxLength(50);
            entity.Property(e => e.Meternummer).HasMaxLength(50);
            entity.Property(e => e.GevraagdVermogen).HasMaxLength(50);
            entity.Property(e => e.AansluitkostRaming).HasColumnType("decimal(19,4)");
            entity.Property(e => e.AansluitkostDefinitief).HasColumnType("decimal(19,4)");
            entity.Property(e => e.AanvraagVerstuurdOp).HasColumnType("date");
            entity.Property(e => e.NutsType).HasDefaultValue(0);
            entity.HasIndex(e => e.ProjectDossierId).IsUnique();
            entity.HasOne(d => d.ProjectDossier).WithOne(p => p.NutsAansluiting)
                .HasForeignKey<ProjectNutsAansluiting>(d => d.ProjectDossierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectNutsAansluiting_Dossier");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectNutsAansluiting_Units");
            entity.HasOne(d => d.NetbeheerderCompany).WithMany()
                .HasForeignKey(d => d.NetbeheerderCompanyId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectNutsAansluiting_Netbeheerder");
            entity.HasOne(d => d.KeuringDoc).WithMany()
                .HasForeignKey(d => d.KeuringDocId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectNutsAansluiting_KeuringDoc");
            entity.HasOne(d => d.Afrekening).WithMany()
                .HasForeignKey(d => d.AfrekeningLink)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectNutsAansluiting_Afrekening");
        });

        modelBuilder.Entity<ProjectDossierSubstap>(entity =>
        {
            entity.ToTable("ProjectDossierSubstap");
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(128);
            entity.Property(e => e.Status).HasDefaultValue(0);
            entity.Property(e => e.Volgorde).HasDefaultValue(0);
            entity.Property(e => e.Datum).HasColumnType("date");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ProjectDossierId);
            entity.HasIndex(e => new { e.ProjectDossierId, e.Code }).IsUnique();
            entity.HasOne(d => d.ProjectDossier).WithMany(p => p.Substappen)
                .HasForeignKey(d => d.ProjectDossierId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectDossierSubstap_Dossier");
        });

        modelBuilder.Entity<Mijlpaal>(entity =>
        {
            entity.HasOne<ProjectDossier>()
                .WithMany()
                .HasForeignKey(d => d.DossierId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mijlpaal_ProjectDossier");
        });
    }
}
