using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    public virtual DbSet<ProjectWebsite> ProjectWebsite { get; set; }
    public virtual DbSet<ProjectWebsiteKpi> ProjectWebsiteKpi { get; set; }
    public virtual DbSet<ProjectWebsiteLot> ProjectWebsiteLot { get; set; }
    public virtual DbSet<ProjectWebsiteStoryItem> ProjectWebsiteStoryItem { get; set; }
    public virtual DbSet<ProjectWebsiteQuote> ProjectWebsiteQuote { get; set; }
    public virtual DbSet<ProjectWebsiteDetail> ProjectWebsiteDetail { get; set; }

    private void ConfigureProjectWebsiteEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectWebsite>(entity =>
        {
            entity.ToTable("ProjectWebsite");
            entity.HasKey(e => e.ProjectId);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Subtitle).HasMaxLength(250);
            entity.Property(e => e.AerialImageName).HasMaxLength(260);
            entity.Property(e => e.HomesTitle).HasMaxLength(200);
            entity.Property(e => e.StoryTitle).HasMaxLength(250);
            entity.Property(e => e.ArchEyebrow).HasMaxLength(200);
            entity.Property(e => e.ArchTitle).HasMaxLength(250);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime2(0)");
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectWebsite_Project");
        });

        modelBuilder.Entity<ProjectWebsiteKpi>(entity =>
        {
            entity.ToTable("ProjectWebsiteKpi");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteKpi_Project_Sort");
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectWebsiteKpi_Project");
        });

        modelBuilder.Entity<ProjectWebsiteLot>(entity =>
        {
            entity.ToTable("ProjectWebsiteLot");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Polygon).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.LabelX).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.LabelY).HasColumnType("decimal(6, 2)");
            entity.HasIndex(e => e.UnitId, "UQ_ProjectWebsiteLot_Unit").IsUnique();
            entity.HasIndex(e => e.ProjectId, "IX_ProjectWebsiteLot_Project");
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectWebsiteLot_Project");
            entity.HasOne(d => d.Unit).WithMany()
                .HasForeignKey(d => d.UnitId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectWebsiteLot_Unit");
        });

        modelBuilder.Entity<ProjectWebsiteStoryItem>(entity =>
        {
            entity.ToTable("ProjectWebsiteStoryItem");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ImageName).HasMaxLength(260);
            entity.Property(e => e.Text).HasMaxLength(1000);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteStoryItem_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteStoryItem_Project");
        });

        modelBuilder.Entity<ProjectWebsiteQuote>(entity =>
        {
            entity.ToTable("ProjectWebsiteQuote");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired().HasMaxLength(600);
            entity.Property(e => e.Person).HasMaxLength(200);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteQuote_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteQuote_Project");
        });

        modelBuilder.Entity<ProjectWebsiteDetail>(entity =>
        {
            entity.ToTable("ProjectWebsiteDetail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ImageName).HasMaxLength(260);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Text).HasMaxLength(1000);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteDetail_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteDetail_Project");
        });
    }
}
