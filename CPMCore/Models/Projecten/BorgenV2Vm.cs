using CPMCore.Models.GlV2;
using CPMCore.Services.Borg;

namespace CPMCore.Models.Projecten;

/// <summary>gl-v2 — Nacalculatie › Borgen &amp; inhoudingen (design-handoff 42i, 42j).</summary>
public class BorgenV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    /// <summary>"borgen" of "inhoudingen".</summary>
    public string Tab { get; set; } = "borgen";
    public BorgOverzicht Overzicht { get; set; } = new();
    public List<GlV2KpiItemVm> Kpis { get; set; } = new();
}
