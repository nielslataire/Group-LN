#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// Elektronisch ondertekenen (ONDERTEKENEN_VOORSTEL.md §3) — DbSets + fluent config, aangeroepen
/// vanuit <c>OnModelCreatingPartial</c> in <c>cpmRunningContext.Seeding.cs</c>, zelfde recept als
/// Taak/Dossier/Traject. Tabellen: migratie <c>_migrations/047_Signing.sql</c>.
/// </summary>
public partial class cpmRunningContext
{
    public virtual DbSet<SigningPolicy> SigningPolicy { get; set; }
    public virtual DbSet<SigningCase> SigningCase { get; set; }
    public virtual DbSet<SigningDocument> SigningDocument { get; set; }
    public virtual DbSet<SigningParty> SigningParty { get; set; }
    public virtual DbSet<SigningAccessToken> SigningAccessToken { get; set; }
    public virtual DbSet<SigningVerification> SigningVerification { get; set; }
    public virtual DbSet<SigningEvent> SigningEvent { get; set; }
    public virtual DbSet<ClientContactChangeLog> ClientContactChangeLog { get; set; }

    private void ConfigureSigningEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SigningPolicy>(entity =>
        {
            entity.ToTable("SigningPolicy");
            entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SignatureMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VerificationMethod).HasMaxLength(50);
            entity.Property(e => e.ConsentText).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.DocumentType).IsUnique();
        });

        modelBuilder.Entity<SigningCase>(entity =>
        {
            entity.ToTable("SigningCase");
            entity.Property(e => e.PublicVerificationId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.DocumentNumber).HasMaxLength(50);
            entity.Property(e => e.SignatureMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProviderCaseRef).HasMaxLength(200);
            entity.Property(e => e.VerificationMethod).HasMaxLength(50);
            entity.Property(e => e.CloseReason).HasMaxLength(1000);
            entity.Property(e => e.SourceFingerprint).HasMaxLength(64);   // migratie 048
            entity.Property(e => e.AmountExclVat).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VatAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AmountInclVat).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.PublicVerificationId).IsUnique();
            entity.HasIndex(e => new { e.DocumentType, e.SourceEntityId, e.Status });
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ClientAccountId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            // Hoogstens één lopend dossier per brondocument (Draft=0, Open=1).
            entity.HasIndex(e => new { e.DocumentType, e.SourceEntityId })
                .IsUnique()
                .HasFilter("[Status] IN (0, 1)")
                .HasDatabaseName("UX_SigningCase_ActivePerSource");

            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningCase_Project");
            entity.HasOne(d => d.ClientAccount).WithMany()
                .HasForeignKey(d => d.ClientAccountId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningCase_ClientAccount");
            // De drie document-verwijzingen zijn "welk bestand speelt welke rol"; de bestanden zelf
            // hangen via SigningDocument.SigningCaseId aan het dossier (cascade daar, NoAction hier
            // om meerdere cascadepaden te vermijden).
            entity.HasOne(d => d.OriginalDocument).WithMany()
                .HasForeignKey(d => d.OriginalDocumentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningCase_OriginalDocument");
            entity.HasOne(d => d.FinalDocument).WithMany()
                .HasForeignKey(d => d.FinalDocumentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningCase_FinalDocument");
            entity.HasOne(d => d.AuditReportDocument).WithMany()
                .HasForeignKey(d => d.AuditReportDocumentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningCase_AuditReportDocument");
        });

        modelBuilder.Entity<SigningDocument>(entity =>
        {
            entity.ToTable("SigningDocument");
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(260);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sha256).IsRequired().HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.StorageFileName).HasMaxLength(260);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => new { e.SigningCaseId, e.Kind });
            entity.HasIndex(e => e.Sha256);
            entity.HasOne(d => d.SigningCase).WithMany(p => p.Documents)
                .HasForeignKey(d => d.SigningCaseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SigningDocument_SigningCase");
        });

        modelBuilder.Entity<SigningParty>(entity =>
        {
            entity.ToTable("SigningParty");
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PhoneMasked).HasMaxLength(40);
            entity.Property(e => e.Capacity).HasMaxLength(200);
            entity.Property(e => e.PartyVerificationId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.SignedIp).HasMaxLength(45);
            entity.Property(e => e.SignedUserAgent).HasMaxLength(500);
            entity.Property(e => e.SignIdempotencyKey).HasMaxLength(100);
            entity.Property(e => e.DeclineReason).HasMaxLength(1000);
            entity.Property(e => e.ProviderPartyRef).HasMaxLength(200);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => new { e.SigningCaseId, e.SortOrder });
            entity.HasIndex(e => e.PartyVerificationId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasOne(d => d.SigningCase).WithMany(p => p.Parties)
                .HasForeignKey(d => d.SigningCaseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SigningParty_SigningCase");
            entity.HasOne(d => d.SignatureImageDocument).WithMany()
                .HasForeignKey(d => d.SignatureImageDocumentId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningParty_SignatureImageDocument");
        });

        modelBuilder.Entity<SigningAccessToken>(entity =>
        {
            entity.ToTable("SigningAccessToken");
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.RevokedReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.SigningPartyId);
            entity.HasIndex(e => e.SessionId);
            entity.HasOne(d => d.SigningParty).WithMany(p => p.AccessTokens)
                .HasForeignKey(d => d.SigningPartyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SigningAccessToken_SigningParty");
        });

        modelBuilder.Entity<SigningVerification>(entity =>
        {
            entity.ToTable("SigningVerification");
            entity.Property(e => e.Method).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ChannelKey).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProviderKey).HasMaxLength(50);
            entity.Property(e => e.DestinationMasked).HasMaxLength(100);
            entity.Property(e => e.CodeHmac).IsRequired().HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.RequestedIp).HasMaxLength(45);
            entity.Property(e => e.ProviderMessageId).HasMaxLength(200);
            entity.Property(e => e.ProviderStatus).HasMaxLength(100);
            entity.Property(e => e.VerifiedIp).HasMaxLength(45);
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => new { e.SigningPartyId, e.Status });
            entity.HasIndex(e => e.ProviderMessageId);
            entity.HasOne(d => d.SigningParty).WithMany(p => p.Verifications)
                .HasForeignKey(d => d.SigningPartyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SigningVerification_SigningParty");
        });

        modelBuilder.Entity<SigningEvent>(entity =>
        {
            entity.ToTable("SigningEvent");
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(60);
            entity.Property(e => e.ActorLabel).HasMaxLength(200);
            entity.Property(e => e.Ip).HasMaxLength(45);
            entity.Property(e => e.IpHash).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserAgentHash).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.DocumentSha256).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.PrevEventHash).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.EventHash).IsRequired().HasMaxLength(64).IsFixedLength();
            entity.HasIndex(e => new { e.SigningCaseId, e.Id });
            entity.HasIndex(e => e.SigningPartyId);
            entity.HasIndex(e => e.EventType);
            // Geen cascade: events overleven bewust elke verwijdering hogerop (de trigger in de
            // migratie weigert DELETE sowieso).
            entity.HasOne(d => d.SigningCase).WithMany(p => p.Events)
                .HasForeignKey(d => d.SigningCaseId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningEvent_SigningCase");
            entity.HasOne(d => d.SigningParty).WithMany()
                .HasForeignKey(d => d.SigningPartyId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_SigningEvent_SigningParty");
        });

        modelBuilder.Entity<ClientContactChangeLog>(entity =>
        {
            entity.ToTable("ClientContactChangeLog");
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Field).IsRequired().HasMaxLength(30);
            entity.Property(e => e.OldValueMasked).HasMaxLength(100);
            entity.Property(e => e.NewValueMasked).HasMaxLength(100);
            entity.Property(e => e.ChangedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => new { e.ClientAccountId, e.ChangedAt });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });
    }
}
