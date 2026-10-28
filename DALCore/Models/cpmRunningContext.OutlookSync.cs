#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Outlook-sync (migraties 121 en 122) — DbSet + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<OutlookSyncStatus> OutlookSyncStatus { get; set; }
    public virtual DbSet<OutlookMail> OutlookMail { get; set; }
    public virtual DbSet<OutlookGeblokkeerdeAfzender> OutlookGeblokkeerdeAfzender { get; set; }

    private void ConfigureOutlookSyncEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutlookSyncStatus>(entity =>
        {
            entity.ToTable("OutlookSyncStatus");
            entity.HasIndex(e => e.UserId, "UQ_OutlookSyncStatus_UserId").IsUnique();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.LaatsteFout).HasMaxLength(500);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_OutlookSyncStatus_Users");
        });

        modelBuilder.Entity<OutlookMail>(entity =>
        {
            entity.ToTable("OutlookMail");
            entity.HasIndex(e => new { e.UserId, e.GraphId }, "UQ_OutlookMail_User_GraphId").IsUnique();
            entity.HasIndex(e => new { e.UserId, e.InternetMessageId }, "UQ_OutlookMail_User_InternetMessageId").IsUnique().HasFilter("([InternetMessageId] IS NOT NULL)");
            entity.Property(e => e.Richting).IsRequired().HasMaxLength(3);
            entity.Property(e => e.GraphId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.InternetMessageId).HasMaxLength(500);
            entity.Property(e => e.ConversationId).HasMaxLength(200);
            entity.Property(e => e.Afzender).HasMaxLength(320);
            entity.Property(e => e.AfzenderNaam).HasMaxLength(200);
            entity.Property(e => e.Ontvangers).HasMaxLength(2000);
            entity.Property(e => e.Onderwerp).HasMaxLength(500);
            entity.Property(e => e.Voorvertoning).HasMaxLength(300);
            entity.Property(e => e.WebLink).HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_OutlookMail_Users");
            entity.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_OutlookMail_Project");
        });

        modelBuilder.Entity<OutlookGeblokkeerdeAfzender>(entity =>
        {
            entity.ToTable("OutlookGeblokkeerdeAfzender");
            entity.HasIndex(e => new { e.UserId, e.Email }, "UQ_OutlookGeblokkeerdeAfzender_User_Email").IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
        });
    }
}
