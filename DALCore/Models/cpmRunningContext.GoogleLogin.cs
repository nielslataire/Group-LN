using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>
/// Fluent config voor de Google-login (migratie 060_GoogleLogin.sql), aangeroepen vanuit
/// <c>OnModelCreatingPartial</c> in <c>cpmRunningContext.Seeding.cs</c>.
/// </summary>
public partial class cpmRunningContext
{
    private void ConfigureGoogleLoginEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Users>(entity =>
        {
            entity.Property(e => e.GoogleSubjectId).HasMaxLength(255);
            entity.HasIndex(e => e.GoogleSubjectId, "UX_Users_GoogleSubjectId")
                  .IsUnique()
                  .HasFilter("([GoogleSubjectId] IS NOT NULL)");
        });
    }
}
