#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

public partial class Vacature
{
    public int Id { get; set; }

    public string Titel { get; set; }

    public string Slug { get; set; }

    public string Categorie { get; set; }

    public string Locatie { get; set; }

    public string Dienstverband { get; set; }

    public string Opleiding { get; set; }

    public string Start { get; set; }

    public string KorteBeschrijving { get; set; }

    public string Beschrijving { get; set; }

    public string VideoBestand { get; set; }

    public string VideoPosterBestand { get; set; }

    public bool IsGepubliceerd { get; set; }

    public int SortOrder { get; set; }

    public DateTime AangemaaktOp { get; set; }

    public DateTime GewijzigdOp { get; set; }

    public virtual ICollection<VacatureTaak> TaakItems { get; set; } = new List<VacatureTaak>();

    public virtual ICollection<VacatureVereiste> VereisteItems { get; set; } = new List<VacatureVereiste>();

    public virtual ICollection<VacatureVoordeel> VoordeelItems { get; set; } = new List<VacatureVoordeel>();

    public virtual ICollection<VacatureSollicitatieStap> SollicitatieStapItems { get; set; } = new List<VacatureSollicitatieStap>();
}
