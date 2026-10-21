#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Borgen en inhoudingen (Nacalculatie, design-handoff 42h-42l, migratie 105) — DbSets + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> (Seeding).</summary>
public partial class cpmRunningContext
{
    public virtual DbSet<IncommingInvoiceInhouding> IncommingInvoiceInhouding { get; set; }
    public virtual DbSet<IncommingInvoiceInhoudingAfhandeling> IncommingInvoiceInhoudingAfhandeling { get; set; }
    public virtual DbSet<ContractBorgVrijgave> ContractBorgVrijgave { get; set; }

    private void ConfigureBorgEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IncommingInvoices>(entity =>
        {
            entity.Property(e => e.BorgPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.BorgBedrag).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BorgAfwijkReden).HasMaxLength(500);
            entity.Property(e => e.BorgVoPercentage).HasColumnType("decimal(5, 2)");
        });
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.Property(e => e.GuaranteeReleaseVOPercentage).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<IncommingInvoiceInhouding>(entity =>
        {
            entity.ToTable("IncommingInvoiceInhouding");
            entity.Property(e => e.Bedrag).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Toelichting).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.IncommingInvoiceId);
            entity.HasOne(d => d.IncommingInvoice).WithMany(p => p.Inhoudingen)
                .HasForeignKey(d => d.IncommingInvoiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_IncommingInvoiceInhouding_Invoice");
        });

        modelBuilder.Entity<IncommingInvoiceInhoudingAfhandeling>(entity =>
        {
            entity.ToTable("IncommingInvoiceInhoudingAfhandeling");
            entity.Property(e => e.Datum).HasColumnType("date");
            entity.Property(e => e.Bedrag).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BorgBedrag).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Opmerking).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IngetrokkenOp).HasColumnType("datetime2(0)");
            entity.Property(e => e.IngetrokkenDoor).HasMaxLength(100);
            // Ingetrokken afhandelingen tellen nergens meer mee (Include, sommen, "creditnota al verrekend").
            entity.HasQueryFilter(e => e.IngetrokkenOp == null);
            entity.HasIndex(e => e.InhoudingId);
            entity.HasOne(d => d.Inhouding).WithMany(p => p.Afhandelingen)
                .HasForeignKey(d => d.InhoudingId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InhoudingAfhandeling_Inhouding");
            entity.HasOne(d => d.CreditnotaInvoice).WithMany()
                .HasForeignKey(d => d.CreditnotaInvoiceId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_InhoudingAfhandeling_Creditnota");
        });

        modelBuilder.Entity<ContractBorgVrijgave>(entity =>
        {
            entity.ToTable("ContractBorgVrijgave");
            entity.Property(e => e.Datum).HasColumnType("date");
            entity.Property(e => e.Bedrag).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Opmerking).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IngetrokkenOp).HasColumnType("datetime2(0)");
            entity.Property(e => e.IngetrokkenDoor).HasMaxLength(100);
            entity.HasQueryFilter(e => e.IngetrokkenOp == null);
            entity.HasIndex(e => e.ContractId);
            entity.HasOne(d => d.Contract).WithMany()
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ContractBorgVrijgave_Contract");
        });
    }
}
