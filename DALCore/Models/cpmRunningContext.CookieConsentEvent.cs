using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<CookieConsentEvent> CookieConsentEvent { get; set; }

    private void ConfigureCookieConsentEventEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CookieConsentEvent>(entity =>
        {
            entity.ToTable("CookieConsentEvent");
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(20);
        });
    }
}
