namespace CPMCore.Models.GlV2
{
    /// <summary>Weergavemodel van de gedeelde gl-v2-datumveldpartial (Views/Shared/GlV2/_DateField).</summary>
    public class GlV2DateFieldVm
    {
        public string Name { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Label { get; set; }
        public bool Required { get; set; }
        public string IsoValue { get; set; } = "";
        public string DisplayValue { get; set; } = "";
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
        /// <summary>Toont bovenaan de kalender de snelknoppen Vandaag, Morgen en Volgende maandag (bv. een afspraak plannen). De voettekst houdt dan enkel Wissen.</summary>
        public bool QuickDates { get; set; }
        /// <summary>Dagen vóór vandaag grijs in de kalender (ze blijven kiesbaar). Voor velden die over de toekomst gaan.</summary>
        public bool DimPast { get; set; }
    }
}
