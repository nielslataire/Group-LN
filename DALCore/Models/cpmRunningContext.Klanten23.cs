#nullable disable
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// Klantenaccounts — voornaam + facturatie-/ondertekenvoorkeuren (migratie 057, design-handoff 23).
/// DbSet + fluent config, aangeroepen vanuit <c>OnModelCreatingPartial</c> in <c>cpmRunningContext.Seeding.cs</c>.
/// </summary>
public partial class cpmRunningContext
{
    public virtual DbSet<ClientAccountInvoiceRecipient> ClientAccountInvoiceRecipient { get; set; }

    private void ConfigureKlanten23Entities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientAccount>(entity =>
        {
            entity.Property(e => e.Forename).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Cellphone).HasMaxLength(50);
            entity.Property(e => e.InvoicingMode).HasDefaultValue((byte)0);
            entity.Property(e => e.PortalInviteRequested).HasDefaultValue(false);

            // NoAction, niet SetNull: migratie 057 zette deze FK op ON DELETE NO ACTION (SQL Server
            // weigerde SetNull hier — "multiple cascade paths" t.o.v. ClientContacts.ClientAccountId,
            // dat al cascade't). ClientAccountTranslator.TranslateBOToEntity ruimt
            // BilledToClientContactId zelf defensief op vóór een mede-eigenaar-rij verwijderd wordt.
            entity.HasOne(d => d.BilledToClientContact).WithMany()
                .HasForeignKey(d => d.BilledToClientContactId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ClientAccount_BilledToContact");
        });

        modelBuilder.Entity<ClientAccountInvoiceRecipient>(entity =>
        {
            entity.ToTable("ClientAccountInvoiceRecipient");
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.DisplayName).HasMaxLength(150);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasIndex(e => e.ClientAccountId);

            entity.HasOne(d => d.ClientAccount).WithMany(p => p.InvoiceRecipients)
                .HasForeignKey(d => d.ClientAccountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ClientAccountInvoiceRecipient_ClientAccount");

            // NoAction, niet SetNull — zelfde reden als FK_ClientAccount_BilledToContact hierboven.
            // HandleInvoiceRecipients vervangt de hele rij-set toch bij elke save, dus dit raakt in de
            // praktijk nooit een bestaande rij die nog verwijst naar een net-verwijderde ClientContacts.
            entity.HasOne(d => d.ClientContact).WithMany()
                .HasForeignKey(d => d.ClientContactId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ClientAccountInvoiceRecipient_ClientContact");
        });
    }
}
