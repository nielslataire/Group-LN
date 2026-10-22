#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Contacten met opvolging (migratie 105) — DbSets + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<CrmContact> CrmContact { get; set; }
    public virtual DbSet<CrmContactUnit> CrmContactUnit { get; set; }
    public virtual DbSet<CrmContactEvent> CrmContactEvent { get; set; }
    public virtual DbSet<CrmContactList> CrmContactList { get; set; }
    public virtual DbSet<CrmExportLog> CrmExportLog { get; set; }
    public virtual DbSet<ProjectBroker> ProjectBroker { get; set; }
    public virtual DbSet<CrmContactLegacySkip> CrmContactLegacySkip { get; set; }

    private void ConfigureCrmContactEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CrmContact>(entity =>
        {
            entity.ToTable("CrmContact");
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(150);
            entity.Property(e => e.PartnerName).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Street).HasMaxLength(200);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(20);
            entity.Property(e => e.BrokerName).HasMaxLength(150);
            entity.Property(e => e.SeenVia).HasMaxLength(200);
            entity.Property(e => e.StatusReason).HasMaxLength(300);
            entity.Property(e => e.AssignedName).HasMaxLength(150);
            entity.Property(e => e.Budget).HasMaxLength(100);
            entity.Property(e => e.Financing).HasMaxLength(300);
            entity.Property(e => e.OwnHome).HasMaxLength(300);
            entity.Property(e => e.Wish).HasMaxLength(300);
            entity.Property(e => e.ConsentAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.NextActionText).HasMaxLength(200);
            entity.Property(e => e.NextActionDate).HasColumnType("date");
            entity.Property(e => e.NextActionName).HasMaxLength(150);
            entity.Property(e => e.VisitAt).HasColumnType("datetime2(0)");
            entity.Property(e => e.VisitUnit).HasMaxLength(100);
            entity.Property(e => e.OptionUntil).HasColumnType("date");
            entity.Property(e => e.OptionUnit).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(150);
            entity.Property(e => e.LastActivityAt).HasColumnType("datetime2(0)");
            entity.HasIndex(e => new { e.ProjectId, e.Status });
            entity.HasIndex(e => new { e.ProjectId, e.Email });
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CrmContact_Project");
        });

        modelBuilder.Entity<CrmContactLegacySkip>(entity =>
        {
            entity.ToTable("CrmContactLegacySkip");
            entity.HasKey(e => e.LegacyRequestId);
            entity.Property(e => e.LegacyRequestId).ValueGeneratedNever();
            entity.Property(e => e.DeletedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<ProjectBroker>(entity =>
        {
            entity.ToTable("ProjectBroker");
            entity.HasKey(e => new { e.ProjectId, e.CompanyId });
            entity.HasOne(d => d.Company).WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_ProjectBroker_Company");
        });

        modelBuilder.Entity<CrmContactUnit>(entity =>
        {
            entity.ToTable("CrmContactUnit");
            entity.HasKey(e => new { e.ContactId, e.UnitId });
            entity.HasOne(d => d.Contact).WithMany(p => p.Units)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CrmContactUnit_Contact");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_CrmContactUnit_Unit");
        });

        modelBuilder.Entity<CrmContactEvent>(entity =>
        {
            entity.ToTable("CrmContactEvent");
            entity.Property(e => e.Kind).IsRequired().HasMaxLength(20);
            entity.Property(e => e.OccurredAt).HasColumnType("datetime2(0)").HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserName).HasMaxLength(150);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.HasIndex(e => new { e.ContactId, e.OccurredAt });
            entity.HasOne(d => d.Contact).WithMany(p => p.Events)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CrmContactEvent_Contact");
        });

        modelBuilder.Entity<CrmContactList>(entity =>
        {
            entity.ToTable("CrmContactList");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.CreatedBy).HasMaxLength(150);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ProjectId);
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_CrmContactList_Project");
        });

        modelBuilder.Entity<CrmExportLog>(entity =>
        {
            entity.ToTable("CrmExportLog");
            entity.Property(e => e.UserName).HasMaxLength(150);
            entity.Property(e => e.ListName).HasMaxLength(150);
            entity.Property(e => e.Format).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ExportedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => new { e.ProjectId, e.ExportedAt });
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_CrmExportLog_Project");
        });
    }
}
