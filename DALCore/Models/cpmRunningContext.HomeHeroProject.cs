using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<HomeHeroProject> HomeHeroProject { get; set; }

    private void ConfigureHomeHeroProjectEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HomeHeroProject>(entity =>
        {
            entity.ToTable("HomeHeroProject");
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HomeHeroProject_Project");
        });
    }
}
