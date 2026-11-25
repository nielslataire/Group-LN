#nullable disable
using System;
using Microsoft.EntityFrameworkCore;

namespace DALCore.Models;

/// <summary>Een projectsjabloon (migratie 156): een bundel van een planningssjabloon, een trajectsjabloon en een schijvensjabloon met standaardantwoorden.</summary>
public partial class ProjectSjabloon
{
    public int Id { get; set; }
    public string Naam { get; set; }
    public string Beschrijving { get; set; }

    // Verwijzingen zonder foreign key: een verwijderd sjabloon laat een lege keuze achter.
    public int? PlanningSjabloonId { get; set; }
    public int? TrajectSjabloonId { get; set; }
    public int? SchijfSjabloonId { get; set; }
    /// <summary>Schijvensjabloon voor betalingsgroepen met enkel parkings en bergingen (migratie 160). Leeg = die groepen krijgen het gewone schijvensjabloon.</summary>
    public int? SchijfSjabloonBijgebouwId { get; set; }

    /// <summary>Planningssjabloon voor het project zelf (de hoofdplanning, bv. de wegenis van een verkaveling; migratie 164). Leeg = geen.</summary>
    public int? PlanningSjabloonProjectId { get; set; }
    /// <summary>Bouwstaatcode waarna de woningen starten (bv. WEGENIS_VERHARDING); leeg = onmiddellijk (migratie 164).</summary>
    public string WoningenStartNa { get; set; }

    public int StandaardVerdiepingen { get; set; } = 1;
    public bool StandaardKelder { get; set; }

    /// <summary>"hellend", "plat" of leeg.</summary>
    public string StandaardDak { get; set; }

    public bool IsActief { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string CreatedByUserId { get; set; }
    public string ModifiedByUserId { get; set; }
}

public partial class cpmRunningContext
{
    public virtual DbSet<ProjectSjabloon> ProjectSjabloon { get; set; }

    private void ConfigureProjectSjabloonEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectSjabloon>(entity =>
        {
            entity.ToTable("ProjectSjabloon");
            entity.Property(e => e.Naam).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Beschrijving).HasMaxLength(1000);
            entity.Property(e => e.StandaardDak).HasMaxLength(20);
            entity.Property(e => e.WoningenStartNa).HasMaxLength(60);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.Property(e => e.ModifiedByUserId).HasMaxLength(450);
        });
    }
}
