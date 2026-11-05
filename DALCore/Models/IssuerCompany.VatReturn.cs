#nullable disable
namespace DALCore.Models;

/// <summary>Handmatige aanvulling op de gegenereerde IssuerCompany (migratie 127_IssuerCompanyBtwAangifte.sql).</summary>
public partial class IssuerCompany
{
    /// <summary>"Monthly" (maandelijks), "Quarterly" (per kwartaal) of null (nog niet ingesteld). Zie <c>VatReturnFrequencies</c>.</summary>
    public string VatReturnFrequency { get; set; }
}
