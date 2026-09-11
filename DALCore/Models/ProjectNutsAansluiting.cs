#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Getypeerde companion van <see cref="ProjectDossier"/> voor het dossierkind NutsAansluiting.</summary>
public partial class ProjectNutsAansluiting
{
    public int Id { get; set; }

    public int ProjectDossierId { get; set; }

    public int? UnitId { get; set; }

    /// <summary>BOCore.NutsType.</summary>
    public int NutsType { get; set; }

    public int? NetbeheerderCompanyId { get; set; }

    public string Ean { get; set; }

    public string Meternummer { get; set; }

    public string GevraagdVermogen { get; set; }

    public DateOnly? AanvraagVerstuurdOp { get; set; }

    public int? KeuringDocId { get; set; }

    public decimal? AansluitkostRaming { get; set; }

    public decimal? AansluitkostDefinitief { get; set; }

    /// <summary>Koppeling naar de bestaande nutskosten-afrekening zodra die er is.</summary>
    public int? AfrekeningLink { get; set; }

    public virtual ProjectDossier ProjectDossier { get; set; }

    public virtual Units Unit { get; set; }

    public virtual CompanyInfo NetbeheerderCompany { get; set; }

    public virtual ProjectDocs KeuringDoc { get; set; }

    public virtual ConnectionSettlement Afrekening { get; set; }
}
