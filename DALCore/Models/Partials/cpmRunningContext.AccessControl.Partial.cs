using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

public partial class cpmRunningContext
{
    internal void ConfigureAccessControl(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectUserAccess>(entity =>
        {
            entity.ToTable("ProjectUserAccess");
            entity.HasKey(e => e.Id);


            entity.HasIndex(e => new { e.ProjectId, e.UserId }, "UX_ProjectUserAccess_Project_User")
                .IsUnique();

            entity.HasOne(d => d.Project)
                .WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectUserAccess_Project");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProjectUserAccess_Users");
        });

        modelBuilder.Entity<UserCompanyAccess>(entity =>
        {
            entity.ToTable("UserCompanyAccess");
            entity.HasKey(e => e.Id);


            entity.HasIndex(e => new { e.UserId, e.CompanyId }, "UX_UserCompanyAccess_User_Company")
                .IsUnique();

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserCompanyAccess_CompanyInfo");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserCompanyAccess_Users");
        });
    }
}
