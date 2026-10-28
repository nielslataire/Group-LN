using System.Globalization;

namespace CPMCore.Services.MijnTaken;

/// <summary>Opmaak voor de Mijn taken-pagina's (Nederlandstalig).</summary>
public static class MtFormat
{
    private static readonly CultureInfo Nl = new("nl-BE");

    /// <summary>Minuten als "1u 30m", "45 min" of "0 min".</summary>
    public static string Duur(int minuten)
    {
        if (minuten < 60) return minuten + " min";
        var u = minuten / 60; var m = minuten % 60;
        return m == 0 ? $"{u}u" : $"{u}u {m:00}m";
    }

    public static string Schatting(int? minuten) => minuten == null ? "" : minuten < 60 ? minuten + "m" : minuten % 60 == 0 ? minuten / 60 + "u" : $"{minuten / 60}u{minuten % 60:00}";

    public static string Uur(DateTime d) => d.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Datum(DateOnly d) => d.ToString("dd/MM", Nl);

    public static string DatumLang(DateOnly d) => d.ToString("dd/MM/yyyy", Nl);

    /// <summary>"37 dagen te laat", "vandaag", "morgen", "over 4 dagen".</summary>
    public static string Rel(int dagen) => dagen switch
    {
        0 => "vandaag",
        1 => "morgen",
        -1 => "1 dag te laat",
        < 0 => -dagen + " dagen te laat",
        _ => "over " + dagen + " dagen"
    };

    public static string Hh(string? kleur, string alpha) => (kleur ?? "#9aa898") + alpha;

    public static string Maand(DateOnly d) => Nl.DateTimeFormat.GetMonthName(d.Month);

    // ── Kalender (Outlook-achtig): afspraken en geplande taken in één baan, overlappende items naast elkaar ──

    public static string Px(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>Een item in de kalender: een afspraak uit Outlook of een geplande taak.</summary>
    public sealed class KalItem
    {
        public bool Afspraak { get; set; }
        /// <summary>Graph-id van een afspraak uit Outlook.</summary>
        public string? EventId { get; set; }
        /// <summary>Een afspraak die je zelf mag wijzigen (jij bent de organisator).</summary>
        public bool Bewerkbaar { get; set; }
        public bool Deelnemers { get; set; }
        public int? TaakId { get; set; }
        public string Titel { get; set; } = "";
        public string Project { get; set; } = "";
        /// <summary>Afspraak: het gekoppelde project en het voorstel van CPM (nog te bevestigen).</summary>
        public int? ProjectId { get; set; }
        public int? VoorstelProjectId { get; set; }
        public string? VoorstelProject { get; set; }
        public string Kleur { get; set; } = "#9aa898";
        public DateTime Start { get; set; }
        public DateTime Einde { get; set; }
        /// <summary>Echt begin en einde, ook als het blok in de kalender op het zichtbare uurbereik is afgekapt.</summary>
        public DateTime EchtStart { get; set; }
        public DateTime EchtEinde { get; set; }
        public string? Plaats { get; set; }
        public string? Link { get; set; }
        public bool Online { get; set; }
        public bool Vrij { get; set; }
        public int Kol { get; set; }
        public int Kols { get; set; } = 1;
    }

    /// <summary>Voegt afspraken en geplande taken van één dag samen, begrenst ze op het zichtbare uurbereik en deelt ze in kolommen in
    /// (zoals Outlook): items die elkaar overlappen komen naast elkaar en delen de breedte.</summary>
    public static List<KalItem> Kalender(IEnumerable<CPMCore.Services.AgendaAfspraak> afspraken, IEnumerable<CPMCore.Models.MijnTaken.MtBlok> taken, DateTime dag, int uurVan, int uurTot)
    {
        var van = dag.Date.AddHours(uurVan); var tot = dag.Date.AddHours(uurTot);
        var lijst = new List<KalItem>();
        foreach (var a in afspraken)
            lijst.Add(new KalItem { Afspraak = true, EventId = a.Id, Bewerkbaar = a.IsOrganisator && !string.IsNullOrEmpty(a.Id), Deelnemers = a.HeeftDeelnemers, Titel = a.Titel, Start = a.Start, Einde = a.Einde, Plaats = a.Plaats, Link = a.Link, Online = a.Online, Vrij = a.ToonAls == "free", Kleur = "#2F4F7A", ProjectId = a.ProjectId, Project = a.ProjectNaam ?? "", VoorstelProjectId = a.VoorstelProjectId, VoorstelProject = a.VoorstelProjectNaam });
        foreach (var b in taken)
            lijst.Add(new KalItem { TaakId = b.TaakId, Titel = b.Titel, Project = b.Project, Kleur = b.Kleur, Start = b.Start, Einde = b.Einde });
        lijst = lijst.Where(i => i.Einde > van && i.Start < tot).ToList();
        foreach (var i in lijst) { i.EchtStart = i.Start; i.EchtEinde = i.Einde; if (i.Start < van) i.Start = van; if (i.Einde > tot) i.Einde = tot; }

        // Clusters van elkaar overlappende items; binnen een cluster krijgt elk item de eerste vrije kolom.
        var gesorteerd = lijst.OrderBy(i => i.Start).ThenByDescending(i => i.Einde).ToList();
        var cluster = new List<KalItem>(); var kolEinde = new List<DateTime>(); var clusterEinde = DateTime.MinValue;
        void Sluit() { foreach (var c in cluster) c.Kols = Math.Max(1, kolEinde.Count); cluster.Clear(); kolEinde.Clear(); }
        foreach (var i in gesorteerd)
        {
            // Een kort item neemt visueel minstens 25 minuten in; reken daarmee zodat korte items niet onder elkaar verdwijnen.
            var zichtEinde = i.Einde < i.Start.AddMinutes(25) ? i.Start.AddMinutes(25) : i.Einde;
            if (cluster.Count > 0 && i.Start >= clusterEinde) { Sluit(); clusterEinde = DateTime.MinValue; }
            var k = kolEinde.FindIndex(e => e <= i.Start);
            if (k < 0) { kolEinde.Add(zichtEinde); k = kolEinde.Count - 1; } else kolEinde[k] = zichtEinde;
            i.Kol = k; cluster.Add(i);
            if (zichtEinde > clusterEinde) clusterEinde = zichtEinde;
        }
        Sluit();
        return gesorteerd;
    }
}
