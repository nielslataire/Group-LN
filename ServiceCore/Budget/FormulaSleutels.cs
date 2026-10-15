namespace ServiceCore.Budget;
public static class FormulaSleutels {
    public const string NacalcRuwbouwBasis           = "ruwbouw_basis";
    public const string BovenbouwGevelmetselwerk      = "bovenbouw_gevelmetselwerk";
    public const string BovenbouwGipsblokken          = "bovenbouw_gipsblokken";
    public const string BovenbouwTerras               = "bovenbouw_terras";
    public const string OnderbouwBerlinerwanden       = "onderbouw_berlinerwanden";
    public const string OnderbouwSecanpalen           = "onderbouw_secanpalen";
    public const string OnderbouwFunderingen          = "onderbouw_funderingen";
    public const string OnderbouwGrondwerken          = "onderbouw_grondwerken";
    public const string OnderbouwOnderschoeiingen     = "onderbouw_onderschoeiingen";
    // ── Dakwerken ────────────────────────────────────────────────────────────
    public const string DakwerkenPlatdak              = "dakwerken_platdak";
    public const string DakwerkenGroendak             = "dakwerken_groendak";
    public const string DakwerkenDaktimmerwerk        = "dakwerken_daktimmerwerk";
    public const string DakwerkenDakoversteken        = "dakwerken_dakoversteken";
    public const string DakwerkenOnderkantDoorrit     = "dakwerken_onderkantdoorrit";
    public const string DakwerkenHellendDakBedekking  = "dakwerken_hellenddak_bedekking";
    public const string DakwerkenVeluxen              = "dakwerken_veluxen";
    // ── Gevelsluiting ────────────────────────────────────────────────────────
    public const string GevelsluitingBuitenschrijnwerk = "gevelsluiting_buitenschrijnwerk";
    public const string GevelsluitingBallustrades      = "gevelsluiting_ballustrades";
    public const string GevelsluitingZichtschermen     = "gevelsluiting_zichtschermen";
    public const string GevelsluitingLeien             = "gevelsluiting_leien";
    // ── Poorten ──────────────────────────────────────────────────────────────
    public const string PoortSectionaal                = "poorten_sectionaalpoort";
    public const string PoortKantelpoort               = "poorten_kantelpoort";
    public const string Sleutelcombinatie               = "sleutelcombinatie";
    // ── Algemeen ─────────────────────────────────────────────────────────────
    public const string AlgemeenAfbraak                 = "algemeen_afbraak";
    // ── Vaste activiteiten (tabel Activity) van de Algemeen-posten van het budget, voor de koppeling met contracten en facturen ─
    // (zie ook BudgetAlgemeneActiviteit hieronder)
    // Voeg hier nieuwe sleutels toe zodra een nieuw formule-veld wordt aangemaakt.
    // Voeg ook een corresponderende rij toe in KostprijsFormulaKoppeling.sql.
}

/// <summary>ActivityID's van de algemene posten die op de Parameters-pagina of als formule in het budget staan (opgegeven door Niels, 13/10/2026).</summary>
public static class BudgetAlgemeneActiviteit {
    public const int Afbraak                = 172;
    public const int Veiligheidscoordinatie = 165;
    public const int Epb                    = 166;
    public const int Opmeting               = 168;
    public const int Sondering              = 182;
    public const int Decennale              = 142;   // tienjarige verzekering (groep Verzekeringen)
    public const int Ventilatieverslaggeving = 278;
    public const int Architect              = 162;
    public const int Ingenieur              = 163;
    public const int Projectcoordinatie     = 277;
    public const int Abr                    = 142;   // zelfde activiteit als Decennale: worden in het totaalbudget samen geteld
    public const int FinancieringWetBreyne  = 294;
    public const int FinancieringStraightloan = 295;  // gebouw én grond
    /// <summary>
    /// EPB, veiligheidscoordinatie en ventilatieverslaggeving staan als activiteit in stap 6, maar horen niet in de bouwkost waarop de
    /// percentages van Parameters (erelonen, verzekeringen, financiering, onvoorzien) berekend worden. Ze blijven wel in het totaalbudget.
    /// </summary>
    public static readonly int[] BuitenPercentageBasis = { Epb, Veiligheidscoordinatie, Ventilatieverslaggeving };
    // Onvoorzien is een reserve in het totaalbudget en krijgt bewust geen activiteit.
}
