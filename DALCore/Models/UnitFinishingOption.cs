#nullable disable
using System.Collections.Generic;

namespace DALCore.Models;

public partial class UnitFinishingOption
{
    public int Id { get; set; }

    public int UnitId { get; set; }

    public string Name { get; set; }

    public int SortOrder { get; set; }

    public bool IsDefault { get; set; }

    public virtual Units Unit { get; set; }

    public virtual ICollection<UnitConstructionValue> UnitConstructionValues { get; set; } = new List<UnitConstructionValue>();
}
