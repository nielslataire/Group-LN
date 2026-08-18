#nullable disable
using System;

namespace DALCore.Models;

public partial class VacatureSollicitatieStap
{
    public int Id { get; set; }

    public int VacatureId { get; set; }

    public int SortOrder { get; set; }

    public string Titel { get; set; }

    public string Tekst { get; set; }

    public virtual Vacature Vacature { get; set; }
}
