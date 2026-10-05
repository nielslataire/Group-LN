namespace CPMCore.Configuration;

/// <summary>Sectie "GlV2PdfCompany" in appsettings — de bedrijfsgegevens voor de voet van een
/// gl-v2-document (DESIGN.md "PDF-documenten (gl-v2)"). <c>VatNumber</c>/<c>Iban</c>/<c>Phone</c>
/// staan bewust niet in de code: niemand mag een btw-nummer of rekeningnummer op een PDF naar een
/// klant verzinnen. Laat een veld leeg tot de echte waarde hier ingevuld is; de voet laat een lege
/// regel dan gewoon weg in plaats van iets fout te tonen.</summary>
public class GlV2PdfCompanyOptions
{
    public string Name { get; set; } = "Group LN";
    public string? Tagline { get; set; }
    public string Street { get; set; } = "Klaverdries 53";
    public string PostalCity { get; set; } = "9031 Drongen";
    public string? Phone { get; set; }
    public string Email { get; set; } = "info@groupln.be";
    public string? Website { get; set; } = "www.groupln.be";
    public string? LegalForm { get; set; }
    public string? VatNumber { get; set; }
    public string? Iban { get; set; }
}
