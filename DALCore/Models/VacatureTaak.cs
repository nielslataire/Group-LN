#nullable disable
using System;

namespace DALCore.Models;

public partial class VacatureTaak
{
    public int Id { get; set; }

    public int VacatureId { get; set; }

    public int SortOrder { get; set; }

    public string Tekst { get; set; }

    public virtual Vacature Vacature { get; set; }
}
