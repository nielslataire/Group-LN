#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Outlook-sync (migratie 121) — DbSet + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<OutlookSyncStatus> OutlookSyncStatus { get; set; }

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
    }
}
