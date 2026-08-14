#nullable disable
using System;

namespace DALCore.Models;

public partial class Vacature
{
    public int Id { get; set; }

    public string Titel { get; set; }

    public string Slug { get; set; }

    public string Categorie { get; set; }

    public string Locatie { get; set; }

    public string Dienstverband { get; set; }

    public string KorteBeschrijving { get; set; }

    public string Beschrijving { get; set; }

    public bool IsGepubliceerd { get; set; }

    public int SortOrder { get; set; }

    public DateTime AangemaaktOp { get; set; }

    public DateTime GewijzigdOp { get; set; }
}
