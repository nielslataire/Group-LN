#nullable disable
using Microsoft.EntityFrameworkCore;
namespace DALCore.Models;
public partial class cpmRunningContext {
    public virtual DbSet<KmIndexType> KmIndexTypes { get; set; }
    public virtual DbSet<KostprijsMateriaal> KostprijsMaterialen { get; set; }
    public virtual DbSet<KostprijsFormulaKoppeling> KostprijsFormulaKoppelingen { get; set; }
    public virtual DbSet<ProjectKostprijs> ProjectKostprijzen { get; set; }
    public virtual DbSet<BouwkostPercentageGroep> BouwkostPercentageGroepen { get; set; }
    public virtual DbSet<BouwkostPercentage> BouwkostPercentages { get; set; }
    private void ConfigureKostprijsMaterialenEntities(ModelBuilder modelBuilder) {
        modelBuilder.Ignore<KostprijsCategorie>();
        modelBuilder.Entity<KmIndexType>(entity => {
            entity.ToTable("KmIndexType");
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(100);
        });
        modelBuilder.Entity<KostprijsMateriaal>(entity => {
            entity.ToTable("KostprijsMateriaal");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Eenheid).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Toepassingsgebied).HasMaxLength(50);
            entity.Property(e => e.ReferentiePrijs).HasColumnType("decimal(19,4)");
            entity.Property(e => e.ReferentieDatum).HasColumnType("date");
            entity.HasOne(d => d.Categorie).WithMany()
                .HasForeignKey(d => d.CategorieId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KostprijsMateriaal_ActivityGroep");
            entity.HasOne(d => d.IndexType).WithMany(p => p.Materialen)
                .HasForeignKey(d => d.KmIndexTypeId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KostprijsMateriaal_IndexType");
        });
        modelBuilder.Entity<ProjectKostprijs>(entity => {
            entity.ToTable("ProjectKostprijs");
            entity.Property(e => e.CategorieNaam).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Eenheid).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IndexTypeCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Prijs).HasColumnType("decimal(19,4)");
            entity.Property(e => e.ReferentieDatum).HasColumnType("date");
            entity.Property(e => e.SnapshotDatum).HasDefaultValueSql("(sysutcdatetime())");
            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectKostprijs_Project");
            entity.HasOne(d => d.KostprijsMateriaal).WithMany(p => p.ProjectKostprijzen)
                .HasForeignKey(d => d.KostprijsMateriaalId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProjectKostprijs_Materiaal");
        });
        modelBuilder.Entity<BouwkostPercentageGroep>(entity => {
            entity.ToTable("BouwkostPercentageGroep");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Volgorde).HasDefaultValue(0);
        });
        modelBuilder.Entity<KostprijsFormulaKoppeling>(entity => {
            entity.ToTable("KostprijsFormulaKoppeling");
            entity.Property(e => e.Sleutel).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Omschrijving).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Sleutel).IsUnique();
            entity.HasOne(d => d.Materiaal).WithMany()
                .HasForeignKey(d => d.MateriaalId).OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_FormulaKoppeling_Materiaal");
        });
        modelBuilder.Entity<BouwkostPercentage>(entity => {
            entity.ToTable("BouwkostPercentage");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Percentage).HasColumnType("decimal(7,4)");
            entity.Property(e => e.Volgorde).HasDefaultValue(0);
            entity.HasOne(d => d.Groep).WithMany(p => p.Percentages)
                .HasForeignKey(d => d.GroepId).OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BouwkostPercentage_Groep");
        });
    }
}
