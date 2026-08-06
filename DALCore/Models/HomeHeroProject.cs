#nullable disable
using System;

namespace DALCore.Models;

public partial class HomeHeroProject
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Kicker { get; set; }

    public string Titel { get; set; }

    public string Tekst { get; set; }

    public string ProjectTitelOverride { get; set; }

    public DateTime GewijzigdOp { get; set; }

    public virtual Project Project { get; set; }
}
