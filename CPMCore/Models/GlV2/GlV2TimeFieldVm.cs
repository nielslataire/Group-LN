namespace CPMCore.Models.GlV2
{
    /// <summary>Weergavemodel van de gedeelde gl-v2-tijdveldpartial (Views/Shared/GlV2/_TimeField).</summary>
    public class GlV2TimeFieldVm
    {
        public string Name { get; set; } = "";
        public string Id { get; set; } = "";
        public string? Label { get; set; }
        public bool Required { get; set; }
        /// <summary>De waarde als "HH:mm" (24 uur), of leeg.</summary>
        public string Value { get; set; } = "";
        /// <summary>Stap van de keuzelijst in minuten. Typen mag altijd op de minuut.</summary>
        public int StepMinutes { get; set; } = 15;
        /// <summary>Eerste en laatste tijd van de keuzelijst ("HH:mm"). De pagina-JS kan de lijst vervangen (GlV2Time.setItems).</summary>
        public string ListFrom { get; set; } = "07:00";
        public string ListTo { get; set; } = "20:00";
        public string Placeholder { get; set; } = "uu:mm";
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
