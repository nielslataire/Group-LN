#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// Trajectopvolging — sjabloon- en instantie-laag. DbSets + fluent config, gescheiden van de
/// gescaffolde <c>cpmRunningContext.cs</c>. Aangeroepen vanuit <c>OnModelCreatingPartial</c> in
/// <c>cpmRunningContext.Seeding.cs</c> via <see cref="ConfigureTrajectEntities"/>.
/// </summary>
public partial class cpmRunningContext
{
    // --- Sjabloon-laag ---
    public virtual DbSet<TrajectSjabloon> TrajectSjabloon { get; set; }
    public virtual DbSet<TrajectSjabloonFase> TrajectSjabloonFase { get; set; }
    public virtual DbSet<TrajectSjabloonMijlpaal> TrajectSjabloonMijlpaal { get; set; }
    public virtual DbSet<TrajectSjabloonMijlpaalAfhankelijkheid> TrajectSjabloonMijlpaalAfhankelijkheid { get; set; }
    public virtual DbSet<TrajectSjabloonMijlpaalTrigger> TrajectSjabloonMijlpaalTrigger { get; set; }

    // --- Instantie-laag ---
    public virtual DbSet<Projecttraject> Projecttraject { get; set; }
    public virtual DbSet<ProjecttrajectFase> ProjecttrajectFase { get; set; }
    public virtual DbSet<Mijlpaal> Mijlpaal { get; set; }
    public virtual DbSet<MijlpaalHistoriek> MijlpaalHistoriek { get; set; }
    public virtual DbSet<MijlpaalAfhankelijkheid> MijlpaalAfhankelijkheid { get; set; }
    public virtual DbSet<MijlpaalTrigger> MijlpaalTrigger { get; set; }
    public virtual DbSet<MijlpaalTriggerRun> MijlpaalTriggerRun { get; set; }

    private void ConfigureTrajectEntities(ModelBuilder modelBuilder)
    {
        // ---------------------------------------------------------------
        // Sjabloon-laag
        // ---------------------------------------------------------------
        modelBuilder.Entity<TrajectSjabloon>(entity =>
        {
            entity.ToTable("TrajectSjabloon");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(128);
            entity.Property(e => e.IsStandaard).HasDefaultValue(false);
            entity.Property(e => e.IsActief).HasDefaultValue(true);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<TrajectSjabloonFase>(entity =>
        {
            entity.ToTable("TrajectSjabloonFase");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.KleurCode).HasMaxLength(20);
            entity.Property(e => e.Volgorde).HasDefaultValue(0);
            entity.HasIndex(e => e.TrajectSjabloonId);
            entity.HasOne(d => d.TrajectSjabloon).WithMany(p => p.Fases)
                .HasForeignKey(d => d.TrajectSjabloonId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TrajectSjabloonFase_Sjabloon");
        });

        modelBuilder.Entity<TrajectSjabloonMijlpaal>(entity =>
        {
            entity.ToTable("TrajectSjabloonMijlpaal");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DoeldatumAnkerCode).HasMaxLength(50);
            entity.Property(e => e.BronParam).HasMaxLength(100);
            entity.Property(e => e.Volgorde).HasDefaultValue(0);
            entity.Property(e => e.MijlpaalType).HasDefaultValue(0);
            entity.Property(e => e.Scope).HasDefaultValue(0);
            entity.Property(e => e.IsVerplicht).HasDefaultValue(true);
            entity.HasIndex(e => e.TrajectSjabloonFaseId);
            entity.HasOne(d => d.TrajectSjabloonFase).WithMany(p => p.Mijlpalen)
                .HasForeignKey(d => d.TrajectSjabloonFaseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TrajectSjabloonMijlpaal_Fase");
        });

        modelBuilder.Entity<TrajectSjabloonMijlpaalAfhankelijkheid>(entity =>
        {
            entity.ToTable("TrajectSjabloonMijlpaalAfhankelijkheid");
            entity.Property(e => e.Type).HasDefaultValue(0);
            entity.HasIndex(e => new { e.MijlpaalId, e.VereistMijlpaalId }).IsUnique();
            entity.HasOne(d => d.Mijlpaal).WithMany()
                .HasForeignKey(d => d.MijlpaalId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SjabloonMijlpaalAfh_Mijlpaal");
            entity.HasOne(d => d.VereistMijlpaal).WithMany()
                .HasForeignKey(d => d.VereistMijlpaalId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SjabloonMijlpaalAfh_Vereist");
        });

        modelBuilder.Entity<TrajectSjabloonMijlpaalTrigger>(entity =>
        {
            entity.ToTable("TrajectSjabloonMijlpaalTrigger");
            entity.Property(e => e.Omschrijving).HasMaxLength(300);
            entity.Property(e => e.TriggerEvent).HasDefaultValue(0);
            entity.Property(e => e.TriggerActie).HasDefaultValue(0);
            entity.Property(e => e.MagProjectWijzigen).HasDefaultValue(false);
            entity.Property(e => e.IsActief).HasDefaultValue(true);
            entity.HasIndex(e => e.TrajectSjabloonMijlpaalId);
            entity.HasOne(d => d.TrajectSjabloonMijlpaal).WithMany(p => p.Triggers)
                .HasForeignKey(d => d.TrajectSjabloonMijlpaalId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SjabloonMijlpaalTrigger_Mijlpaal");
        });

        // ---------------------------------------------------------------
        // Instantie-laag
        // ---------------------------------------------------------------
        modelBuilder.Entity<Projecttraject>(entity =>
        {
            entity.ToTable("Projecttraject");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(128);
            entity.Property(e => e.GestartOp).HasColumnType("date");
            entity.Property(e => e.AfgerondOp).HasColumnType("date");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ProjectId).IsUnique();
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Projecttraject_Project");
            entity.HasOne(d => d.TrajectSjabloon).WithMany(p => p.Projecttrajecten)
                .HasForeignKey(d => d.TrajectSjabloonId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Projecttraject_Sjabloon");
        });

        modelBuilder.Entity<ProjecttrajectFase>(entity =>
        {
            entity.ToTable("ProjecttrajectFase");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.KleurCode).HasMaxLength(20);
            entity.Property(e => e.Volgorde).HasDefaultValue(0);
            entity.Property(e => e.StartGepland).HasColumnType("date");
            entity.Property(e => e.StartWerkelijk).HasColumnType("date");
            entity.Property(e => e.EindGepland).HasColumnType("date");
            entity.Property(e => e.EindWerkelijk).HasColumnType("date");
            entity.HasIndex(e => e.ProjecttrajectId);
            entity.HasOne(d => d.Projecttraject).WithMany(p => p.Fases)
                .HasForeignKey(d => d.ProjecttrajectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjecttrajectFase_Projecttraject");
        });

        modelBuilder.Entity<Mijlpaal>(entity =>
        {
            entity.ToTable("Mijlpaal");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.BronParam).HasMaxLength(100);
            entity.Property(e => e.VerantwoordelijkeUserId).HasMaxLength(128);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(128);
            entity.Property(e => e.Volgorde).HasDefaultValue(0);
            entity.Property(e => e.MijlpaalType).HasDefaultValue(0);
            entity.Property(e => e.IsVerplicht).HasDefaultValue(true);
            entity.Property(e => e.Doeldatum).HasColumnType("date");
            entity.Property(e => e.DoeldatumBerekend).HasColumnType("date");
            entity.Property(e => e.WerkelijkeDatum).HasColumnType("date");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ProjecttrajectId);
            entity.HasIndex(e => new { e.ProjecttrajectId, e.Status });
            entity.HasIndex(e => new { e.ProjecttrajectId, e.UnitId });
            entity.HasIndex(e => e.ProjecttrajectFaseId);
            entity.HasIndex(e => e.Doeldatum);
            entity.HasOne(d => d.Projecttraject).WithMany(p => p.Mijlpalen)
                .HasForeignKey(d => d.ProjecttrajectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Mijlpaal_Projecttraject");
            entity.HasOne(d => d.ProjecttrajectFase).WithMany(p => p.Mijlpalen)
                .HasForeignKey(d => d.ProjecttrajectFaseId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mijlpaal_ProjecttrajectFase");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mijlpaal_Units");
        });

        modelBuilder.Entity<MijlpaalHistoriek>(entity =>
        {
            entity.ToTable("MijlpaalHistoriek");
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.MijlpaalId);
            entity.HasOne(d => d.Mijlpaal).WithMany(p => p.Historiek)
                .HasForeignKey(d => d.MijlpaalId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MijlpaalHistoriek_Mijlpaal");
        });

        modelBuilder.Entity<MijlpaalAfhankelijkheid>(entity =>
        {
            entity.ToTable("MijlpaalAfhankelijkheid");
            entity.Property(e => e.Type).HasDefaultValue(0);
            entity.HasIndex(e => new { e.MijlpaalId, e.VereistMijlpaalId }).IsUnique();
            entity.HasOne(d => d.Mijlpaal).WithMany(p => p.Afhankelijkheden)
                .HasForeignKey(d => d.MijlpaalId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MijlpaalAfhankelijkheid_Mijlpaal");
            entity.HasOne(d => d.VereistMijlpaal).WithMany()
                .HasForeignKey(d => d.VereistMijlpaalId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_MijlpaalAfhankelijkheid_Vereist");
        });

        modelBuilder.Entity<MijlpaalTrigger>(entity =>
        {
            entity.ToTable("MijlpaalTrigger");
            entity.Property(e => e.TriggerEvent).HasDefaultValue(0);
            entity.Property(e => e.TriggerActie).HasDefaultValue(0);
            entity.Property(e => e.MagProjectWijzigen).HasDefaultValue(false);
            entity.Property(e => e.IsActief).HasDefaultValue(true);
            entity.HasIndex(e => e.MijlpaalId);
            entity.HasOne(d => d.Mijlpaal).WithMany(p => p.Triggers)
                .HasForeignKey(d => d.MijlpaalId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MijlpaalTrigger_Mijlpaal");
        });

        modelBuilder.Entity<MijlpaalTriggerRun>(entity =>
        {
            entity.ToTable("MijlpaalTriggerRun");
            entity.Property(e => e.Status).HasDefaultValue(0);
            entity.Property(e => e.Uitgevoerd).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.MijlpaalTriggerId);
            entity.HasIndex(e => e.Uitgevoerd);
            entity.HasOne(d => d.MijlpaalTrigger).WithMany(p => p.Runs)
                .HasForeignKey(d => d.MijlpaalTriggerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MijlpaalTriggerRun_Trigger");
        });
    }
}
