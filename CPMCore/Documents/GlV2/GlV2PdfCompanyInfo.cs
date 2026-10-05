namespace CPMCore.Documents.GlV2
{
    /// <summary>Bedrijfsgegevens voor de kop/voet van een gl-v2-document (design-handoff punt 35a
    /// "voet... wisselt mee met het uitgevende bedrijf"). Vandaag gevuld vanuit
    /// <see cref="GlV2PdfCompanyOptions"/> (sectie "GlV2PdfCompany" in appsettings), niet
    /// hardcoded: <c>VatNumber</c>/<c>Iban</c>/<c>Phone</c> staan nergens elders in deze repo
    /// vastgelegd en worden daarom, tot iemand ze invult, gewoon weggelaten in de voet in plaats
    /// van een verzonnen nummer te tonen op een document dat naar een klant gaat.</summary>
    public sealed class GlV2PdfCompanyInfo
    {
        public string Name { get; init; } = "";
        /// <summary>Enkel voor een dochter-/zustermerk (design-handoff: "a part of Group LN" onder
        /// het BCO-logo); leeg voor Group LN zelf.</summary>
        public string? Tagline { get; init; }
        public string Street { get; init; } = "";
        public string PostalCity { get; init; } = "";
        public string? Phone { get; init; }
        public string Email { get; init; } = "";
        public string? Website { get; init; }
        /// <summary>Bv. "Group LN NV · RPR Gent" — zonder btw-nummer, dat komt apart.</summary>
        public string? LegalForm { get; init; }
        public string? VatNumber { get; init; }
        public string? Iban { get; init; }
        public byte[]? LogoBytes { get; init; }
    }
}
