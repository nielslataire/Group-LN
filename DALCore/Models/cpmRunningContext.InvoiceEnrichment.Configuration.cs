#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

// Fluent-configuratie voor de verrijkings- en leertabellen.
// Wordt samengebundeld met OnModelCreating via partial class.
public partial class cpmRunningContext
{
    private void ConfigureInvoiceEnrichmentEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IncomingInvoiceEnrichment>(entity =>
        {
            entity.ToTable("IncomingInvoiceEnrichment");

            entity.HasIndex(e => e.IncomingInvoiceId, "IX_IncomingInvoiceEnrichment_InvoiceId").IsUnique();

            entity.Property(e => e.DocumentClassification).HasMaxLength(50);
            entity.Property(e => e.SupplierMatchConfidence).HasMaxLength(20);
            entity.Property(e => e.SupplierMatchReason).HasMaxLength(500);
            entity.Property(e => e.SupplierIban).HasMaxLength(50);
            entity.Property(e => e.SuggestedProjectName).HasMaxLength(250);
            entity.Property(e => e.SuggestedProjectConfidence).HasMaxLength(20);
            entity.Property(e => e.SuggestedProjectMatchReason).HasMaxLength(500);
            entity.Property(e => e.SuggestedProjectMatchedFields).HasMaxLength(200);
            entity.Property(e => e.SuggestedContractName).HasMaxLength(250);
            entity.Property(e => e.SuggestedContractConfidence).HasMaxLength(20);
            entity.Property(e => e.SuggestedAccountingCode).HasMaxLength(100);
            entity.Property(e => e.SuggestedAccountingDescription).HasMaxLength(250);
            entity.Property(e => e.SuggestedAccountingSource).HasMaxLength(50);
            entity.Property(e => e.UserReviewedBy).HasMaxLength(200);
            entity.Property(e => e.UserConfirmedAccountingCode).HasMaxLength(100);
            entity.Property(e => e.UserNotes).HasMaxLength(2000);

            entity.HasOne(d => d.IncomingInvoice)
                .WithOne(p => p.Enrichment)
                .HasForeignKey<IncomingInvoiceEnrichment>(d => d.IncomingInvoiceId)
                .HasConstraintName("FK_IncomingInvoiceEnrichment_Invoice")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.SuggestedProject)
                .WithMany()
                .HasForeignKey(d => d.SuggestedProjectId)
                .HasConstraintName("FK_IncomingInvoiceEnrichment_SuggestedProject")
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.UserConfirmedProject)
                .WithMany()
                .HasForeignKey(d => d.UserConfirmedProjectId)
                .HasConstraintName("FK_IncomingInvoiceEnrichment_ConfirmedProject")
                .OnDelete(DeleteBehavior.SetNull);

            // Warnings zijn gekoppeld aan de factuur (IncommingInvoices), niet aan het enrichment-record.
            // Zonder deze Ignore probeert EF een shadow FK 'IncomingInvoiceEnrichmentId' aan te maken.
            entity.Ignore(e => e.Warnings);
        });

        modelBuilder.Entity<IncomingInvoiceWarning>(entity =>
        {
            entity.ToTable("IncomingInvoiceWarning");

            entity.HasIndex(e => e.IncomingInvoiceId, "IX_IncomingInvoiceWarning_InvoiceId");

            entity.Property(e => e.WarningCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WarningMessage).HasMaxLength(1000);
            entity.Property(e => e.Severity).HasMaxLength(20);
            entity.Property(e => e.Source).HasMaxLength(50);

            entity.HasOne(d => d.IncomingInvoice)
                .WithMany(p => p.Warnings)
                .HasForeignKey(d => d.IncomingInvoiceId)
                .HasConstraintName("FK_IncomingInvoiceWarning_Invoice")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceSupplierRule>(entity =>
        {
            entity.ToTable("InvoiceSupplierRule");

            entity.HasIndex(e => new { e.IssuerCompanyId, e.CompanyId }, "IX_InvoiceSupplierRule_Supplier");
            entity.HasIndex(e => e.SupplierVatNumber, "IX_InvoiceSupplierRule_Vat")
                .HasFilter("[SupplierVatNumber] IS NOT NULL");

            entity.Property(e => e.SupplierVatNumber).HasMaxLength(50);
            entity.Property(e => e.SupplierNameContains).HasMaxLength(250);
            entity.Property(e => e.RuleType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DefaultAccountingCode).HasMaxLength(100);
            entity.Property(e => e.DefaultVatCode).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("FK_InvoiceSupplierRule_Company")
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.IssuerCompany)
                .WithMany()
                .HasForeignKey(d => d.IssuerCompanyId)
                .HasConstraintName("FK_InvoiceSupplierRule_IssuerCompany")
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.DefaultProject)
                .WithMany()
                .HasForeignKey(d => d.DefaultProjectId)
                .HasConstraintName("FK_InvoiceSupplierRule_Project")
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InvoiceProjectAlias>(entity =>
        {
            entity.ToTable("InvoiceProjectAlias");

            entity.HasIndex(e => e.ProjectId, "IX_InvoiceProjectAlias_Project");

            entity.Property(e => e.Alias).IsRequired().HasMaxLength(250);
            entity.Property(e => e.AliasType).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);

            entity.HasOne(d => d.Project)
                .WithMany()
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_InvoiceProjectAlias_Project")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
