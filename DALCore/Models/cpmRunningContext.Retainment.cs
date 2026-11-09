#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Controle inhoudingsplicht leveranciers (migratie 137): DbSet + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<SupplierRetainmentCheck> SupplierRetainmentCheck { get; set; }

    private void ConfigureRetainmentEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupplierRetainmentCheck>(entity =>
        {
            entity.ToTable("SupplierRetainmentCheck");
            entity.Property(e => e.SupplierName).HasMaxLength(200);
            entity.Property(e => e.TraceId).HasMaxLength(100);
            entity.HasIndex(e => e.BatchId);
            entity.Property(e => e.EnterpriseIdentifier).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EnterpriseNumber).HasMaxLength(20);
            entity.Property(e => e.Environment).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CheckedBy).HasMaxLength(200);
            entity.Property(e => e.RetainmentCheckReference).HasMaxLength(50);
            entity.Property(e => e.FiscalSituation).HasMaxLength(20);
            entity.Property(e => e.SocialSituation).HasMaxLength(20);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.BatchName).HasMaxLength(200);
            entity.HasIndex(e => new { e.IncomingInvoiceId, e.CheckedAt });
            entity.HasIndex(e => new { e.EnterpriseIdentifier, e.CheckedAt });
        });
    }
}
