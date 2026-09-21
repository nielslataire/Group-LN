#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// gl-v2 dashboard-meldingenscherm (design-handoff 7b) — server-side snooze-opslag. DbSet + fluent
/// config, aangeroepen vanuit <c>OnModelCreatingPartial</c> in <c>cpmRunningContext.Seeding.cs</c>.
/// </summary>
public partial class cpmRunningContext
{
    public virtual DbSet<MeldingSnooze> MeldingSnooze { get; set; }

    private void ConfigureMeldingSnoozeEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeldingSnooze>(entity =>
        {
            entity.ToTable("MeldingSnooze");
            entity.Property(e => e.MeldingKey).IsRequired().HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasIndex(e => new { e.UserId, e.MeldingKey }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.SnoozedUntil });
        });
    }
}
