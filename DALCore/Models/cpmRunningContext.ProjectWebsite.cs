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
    public virtual DbSet<ProjectWebsiteComfortItem> ProjectWebsiteComfortItem { get; set; }
    public virtual DbSet<ProjectWebsiteFaq> ProjectWebsiteFaq { get; set; }
    public virtual DbSet<ProjectWebsiteBuyStep> ProjectWebsiteBuyStep { get; set; }
    public virtual DbSet<ProjectWebsiteGuideItem> ProjectWebsiteGuideItem { get; set; }
    public virtual DbSet<ProjectWebsitePoi> ProjectWebsitePoi { get; set; }
    public virtual DbSet<ProjectWebsiteTravel> ProjectWebsiteTravel { get; set; }

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
            entity.Property(e => e.ComfortTitle).HasMaxLength(250);
            entity.Property(e => e.ComfortSubtitle).HasMaxLength(600);
            entity.Property(e => e.BuyTitle).HasMaxLength(250);
            entity.Property(e => e.ContactTitle).HasMaxLength(250);
            entity.Property(e => e.ContactText).HasMaxLength(600);
            entity.Property(e => e.LocationTitle).HasMaxLength(250);
            entity.Property(e => e.LocationMunicipality).HasMaxLength(120);
            entity.Property(e => e.LocationLat).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.LocationLng).HasColumnType("decimal(9, 6)");
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

        modelBuilder.Entity<ProjectWebsiteComfortItem>(entity =>
        {
            entity.ToTable("ProjectWebsiteComfortItem");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Text).HasMaxLength(500);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteComfortItem_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteComfortItem_Project");
        });

        modelBuilder.Entity<ProjectWebsiteFaq>(entity =>
        {
            entity.ToTable("ProjectWebsiteFaq");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Question).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Answer).IsRequired();
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteFaq_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteFaq_Project");
        });

        modelBuilder.Entity<ProjectWebsiteBuyStep>(entity =>
        {
            entity.ToTable("ProjectWebsiteBuyStep");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Text).HasMaxLength(400);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteBuyStep_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteBuyStep_Project");
        });

        modelBuilder.Entity<ProjectWebsiteGuideItem>(entity =>
        {
            entity.ToTable("ProjectWebsiteGuideItem");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Text).HasMaxLength(600);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteGuideItem_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteGuideItem_Project");
        });

        modelBuilder.Entity<ProjectWebsitePoi>(entity =>
        {
            entity.ToTable("ProjectWebsitePoi");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(12).IsUnicode(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Lat).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Lng).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Distance).HasMaxLength(30);
            entity.Property(e => e.TimeText).HasMaxLength(40);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsitePoi_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsitePoi_Project");
        });

        modelBuilder.Entity<ProjectWebsiteTravel>(entity =>
        {
            entity.ToTable("ProjectWebsiteTravel");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Place).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Duration).IsRequired().HasMaxLength(40);
            entity.HasIndex(e => new { e.ProjectId, e.SortOrder }, "IX_ProjectWebsiteTravel_Project_Sort");
            entity.HasOne(d => d.Project).WithMany().HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ProjectWebsiteTravel_Project");
        });
    }
}
