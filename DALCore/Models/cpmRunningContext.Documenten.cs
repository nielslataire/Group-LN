#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// Documentenmodule (migratie 049). DbSets + fluent config, aangeroepen vanuit
/// <c>OnModelCreatingPartial</c> in <c>cpmRunningContext.Seeding.cs</c>.
/// </summary>
public partial class cpmRunningContext
{
    public virtual DbSet<DocumentFolder> DocumentFolders { get; set; }
    public virtual DbSet<DocumentRevision> DocumentRevisions { get; set; }
    public virtual DbSet<DocumentLink> DocumentLinks { get; set; }
    public virtual DbSet<DocumentSignature> DocumentSignatures { get; set; }
    public virtual DbSet<DocumentTemplate> DocumentTemplates { get; set; }
    public virtual DbSet<DocumentRequest> DocumentRequests { get; set; }

    private void ConfigureDocumentenEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectDocs>(entity =>
        {
            entity.Property(e => e.DocumentNumber).HasMaxLength(50);
            entity.Property(e => e.AuthoredBy).HasMaxLength(150);
            entity.Property(e => e.Perceel).HasMaxLength(100);
            entity.Property(e => e.ExpiresOn).HasColumnType("date");
            // Bewust GEEN HasDefaultValue: een CLR-waarde 0 (Concept) zou door EF als "niet ingesteld" behandeld en door het
            // database-default (2 = Goedgekeurd) vervangen worden. Elke insert stuurt de status expliciet mee.
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128);

            entity.HasOne(d => d.Folder).WithMany()
                .HasForeignKey(d => d.FolderId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectDocs_DocumentFolders");
            entity.HasOne(d => d.CurrentRevision).WithMany()
                .HasForeignKey(d => d.CurrentRevisionId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectDocs_CurrentRevision");
            entity.HasOne<ChangeOrder>().WithMany()
                .HasForeignKey(d => d.ChangeOrderId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectDocs_ChangeOrder");
            entity.HasOne<ProjectDocs>().WithMany()
                .HasForeignKey(d => d.RelatedDocumentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProjectDocs_RelatedDocument");
        });

        modelBuilder.Entity<DocumentFolder>(entity =>
        {
            entity.ToTable("DocumentFolders");
            entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(40);
            entity.Property(e => e.ViewKind).IsRequired().HasMaxLength(20).HasDefaultValue("default");
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<DocumentRevision>(entity =>
        {
            entity.ToTable("DocumentRevisions");
            entity.Property(e => e.Filename).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OriginalFilename).HasMaxLength(260);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UploadedByUserId).HasMaxLength(128);
            entity.Property(e => e.UploadedByName).HasMaxLength(150);
            entity.Property(e => e.ApprovedByUserId).HasMaxLength(128);
            entity.Property(e => e.ApprovedByName).HasMaxLength(150);
            entity.Property(e => e.UploadedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => new { e.DocumentId, e.RevisionNo });
            entity.HasOne(d => d.Document).WithMany(p => p.Revisions)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DocumentRevisions_ProjectDocs");
        });

        modelBuilder.Entity<DocumentLink>(entity =>
        {
            entity.ToTable("DocumentLinks");
            entity.Property(e => e.SeenByName).HasMaxLength(150);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.DocumentId);
            entity.HasOne(d => d.Document).WithMany(p => p.Links)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DocumentLinks_ProjectDocs");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentLinks_Units");
            entity.HasOne(d => d.ClientAccount).WithMany()
                .HasForeignKey(d => d.ClientAccountId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentLinks_ClientAccount");
            entity.HasOne(d => d.Company).WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentLinks_CompanyInfo");
        });

        modelBuilder.Entity<DocumentSignature>(entity =>
        {
            entity.ToTable("DocumentSignatures");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(128);
            entity.Property(e => e.Method).HasMaxLength(20);
            entity.Property(e => e.SignerEmail).HasMaxLength(254);
            entity.Property(e => e.NotifyEmail).HasMaxLength(254);
            entity.Property(e => e.TokenHash).HasMaxLength(64);
            entity.Property(e => e.CodeHash).HasMaxLength(64);
            entity.Property(e => e.SignedName).HasMaxLength(150);
            entity.Property(e => e.SignedIp).HasMaxLength(64);
            entity.Property(e => e.SignedUserAgent).HasMaxLength(400);
            entity.Property(e => e.ConsentText).HasMaxLength(1000);
            entity.Property(e => e.DocumentHash).HasMaxLength(64);
            entity.Property(e => e.EvidenceRef).HasMaxLength(40);
            entity.HasIndex(e => e.TokenHash).IsUnique().HasFilter("[TokenHash] IS NOT NULL");
            entity.HasIndex(e => new { e.DocumentId, e.SignOrder });
            entity.HasOne(d => d.Document).WithMany(p => p.Signatures)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DocumentSignatures_ProjectDocs");
            entity.HasOne<DocumentRevision>().WithMany()
                .HasForeignKey(d => d.RevisionId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentSignatures_Revision");
            entity.HasOne<ClientAccount>().WithMany()
                .HasForeignKey(d => d.ClientAccountId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentSignatures_ClientAccount");
        });

        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.ToTable("DocumentTemplates");
            entity.Property(e => e.Code).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ResponsibleRole).HasMaxLength(100);
            entity.Property(e => e.DueLabel).HasMaxLength(100);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(d => d.Folder).WithMany()
                .HasForeignKey(d => d.FolderId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentTemplates_Folder");
        });

        modelBuilder.Entity<DocumentRequest>(entity =>
        {
            entity.ToTable("DocumentRequests");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Perceel).HasMaxLength(100);
            entity.Property(e => e.ResponsibleName).HasMaxLength(150);
            entity.Property(e => e.ResponsibleRole).HasMaxLength(100);
            entity.Property(e => e.DueLabel).HasMaxLength(100);
            entity.Property(e => e.DueDate).HasColumnType("date");
            entity.Property(e => e.RequestedByUserId).HasMaxLength(128);
            entity.Property(e => e.RequestedByName).HasMaxLength(150);
            entity.Property(e => e.SeenByName).HasMaxLength(150);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => new { e.ProjectId, e.Status });
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DocumentRequests_Project");
            entity.HasOne(d => d.Folder).WithMany()
                .HasForeignKey(d => d.FolderId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentRequests_Folder");
            entity.HasOne(d => d.Template).WithMany()
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentRequests_Template");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentRequests_Units");
            entity.HasOne(d => d.ResponsibleCompany).WithMany()
                .HasForeignKey(d => d.ResponsibleCompanyId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentRequests_Company");
            entity.HasOne(d => d.ResponsibleClientAccount).WithMany()
                .HasForeignKey(d => d.ResponsibleClientAccountId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentRequests_ClientAccount");
            entity.HasOne(d => d.FulfilledDocument).WithMany()
                .HasForeignKey(d => d.FulfilledDocumentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_DocumentRequests_FulfilledDoc");
        });
    }
}
