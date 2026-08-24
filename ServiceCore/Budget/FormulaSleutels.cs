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
    // Voeg hier nieuwe sleutels toe zodra een nieuw formule-veld wordt aangemaakt.
    // Voeg ook een corresponderende rij toe in KostprijsFormulaKoppeling.sql.
}
