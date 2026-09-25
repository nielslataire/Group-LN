using FacadeCore;

namespace CPMCore.Models.Signing;

/// <summary>Model van de publieke ondertekenpagina.</summary>
public class SigningPageVm
{
    public string Token { get; set; } = "";
    public SigningInfo Info { get; set; } = new();
    public string ConsentText { get; set; } = "";
    public bool HasAmounts { get; set; }
    public string Description { get; set; } = "";
    public DateOnly Date { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public decimal TotalExcl { get; set; }
    public decimal TotalIncl { get; set; }
}
