#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Terugkerende facturen (design-handoff 49/50, migratie 133): DbSets + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<RecurringInvoiceSchedule> RecurringInvoiceSchedule { get; set; }
    public virtual DbSet<RecurringInvoiceScheduleLine> RecurringInvoiceScheduleLine { get; set; }
    public virtual DbSet<RecurringInvoiceEvent> RecurringInvoiceEvent { get; set; }
    public virtual DbSet<RecurringInvoiceSettings> RecurringInvoiceSettings { get; set; }
    public virtual DbSet<HealthIndexValue> HealthIndexValue { get; set; }

    private void ConfigureRecurringInvoiceEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecurringInvoiceSchedule>(entity =>
        {
            entity.ToTable("RecurringInvoiceSchedule");
            entity.Property(e => e.IndexationPercent).HasColumnType("decimal(9, 4)");
            entity.Property(e => e.IndexationBaseIndex).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.HasIndex(e => new { e.IssuerCompanyId, e.Status });
            entity.HasMany(e => e.Lines).WithOne(l => l.Schedule).HasForeignKey(l => l.ScheduleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Events).WithOne(l => l.Schedule).HasForeignKey(l => l.ScheduleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecurringInvoiceScheduleLine>(entity =>
        {
            entity.ToTable("RecurringInvoiceScheduleLine");
            entity.Property(e => e.Text).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.VatPercentage).HasColumnType("decimal(9, 4)");
        });

        modelBuilder.Entity<RecurringInvoiceEvent>(entity =>
        {
            entity.ToTable("RecurringInvoiceEvent");
            entity.Property(e => e.Text).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Detail).HasMaxLength(300);
        });

        modelBuilder.Entity<RecurringInvoiceSettings>(entity =>
        {
            entity.ToTable("RecurringInvoiceSettings");
            entity.HasKey(e => e.IssuerCompanyId);
            entity.Property(e => e.IssuerCompanyId).ValueGeneratedNever();
            entity.Property(e => e.NotifyUsers).HasMaxLength(1000);
        });

        modelBuilder.Entity<HealthIndexValue>(entity =>
        {
            entity.ToTable("HealthIndexValue");
            entity.HasKey(e => new { e.Month, e.BaseYear });
            entity.Property(e => e.Value).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Invoices>(entity =>
        {
            entity.Property(e => e.RecurringLabel).HasMaxLength(80);
            entity.Property(e => e.RecurringPeriodText).HasMaxLength(40);
        });
    }
}
