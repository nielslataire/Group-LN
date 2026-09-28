using FacadeCore;

namespace CPMCore.Models.Projecten;

/// <summary>Projecten/ChangeOrderSignV2 — wijzigingsopdracht digitaal ter ondertekening sturen.</summary>
public class ChangeOrderSignVm
{
    public int ChangeOrderId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public int ClientAccountId { get; set; }
    public string ClientName { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly Date { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public decimal TotalExcl { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalIncl { get; set; }
    public bool CanWrite { get; set; }
    /// <summary>Bestaande ondertekening (na een eerste verzending) — null als nog niet verstuurd.</summary>
    public ChangeOrderSigningStatus? Status { get; set; }
    public List<SigningSignerDto> SuggestedSigners { get; set; } = new();
    public string ConsentText { get; set; } = "";
    public string BackUrl { get; set; } = "";
}
