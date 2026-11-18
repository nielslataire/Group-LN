using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FacadeCore;

namespace ServiceCore.Planning;

/// <summary>
/// Leest een MS Project-XML-bestand (Bestand › Opslaan als › XML-indeling) in tot regels voor de planningimport.
/// Samenvattingstaken worden fases, de andere taken en mijlpalen blijven taken. Resources en kosten worden genegeerd, duren in uren worden
/// afgerond naar werkdagen, "zo laat mogelijk" wordt "zo vroeg mogelijk", voorgangers die niet bestaan en kringen worden weggelaten.
/// </summary>
public static class MsProjectXmlParser
{
    public const int MaxRijen = 5000;

    private sealed class Bron
    {
        public int Uid, Niveau;
        public string Wbs = "", Naam = "", Notitie = "";
        public bool IsFase, IsMijlpaal;
        public int Duur, Voortgang, ConstraintType;
        public double Uren;
        public DateTime? Start, Einde, ConstraintDatum, Deadline, ActueelStart, ActueelEinde, BasisStart, BasisEinde;
        public List<(int Uid, byte Type, int Wacht, bool Procent)> Links = new();
        public PlanningImportRij Rij = new();
    }

    public static PlanningImportBestand Lees(Stream stream, string bestandsnaam)
    {
        var uitkomst = new PlanningImportBestand { Bestandsnaam = bestandsnaam };
        XDocument doc;
        try { doc = XDocument.Load(stream); }
        catch (Exception) { uitkomst.Fout = "Dit bestand is geen geldige XML. Sla het in MS Project op via Bestand › Opslaan als › XML-indeling (*.xml)."; return uitkomst; }

        var wortel = doc.Root;
        if (wortel == null || wortel.Name.LocalName != "Project") { uitkomst.Fout = "Dit is geen MS Project-XML. Sla het in MS Project op via Bestand › Opslaan als › XML-indeling (*.xml)."; return uitkomst; }

        static XElement? Kind(XElement e, string naam) => e.Elements().FirstOrDefault(x => x.Name.LocalName == naam);
        static string? Tekst(XElement e, string naam) => Kind(e, naam)?.Value?.Trim();
        static int Getal(XElement e, string naam, int standaard = 0) => int.TryParse(Tekst(e, naam), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : standaard;
        static DateTime? Datum(XElement e, string naam) => DateTime.TryParse(Tekst(e, naam), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

        uitkomst.Projectnaam = Tekst(wortel, "Title") is { Length: > 0 } t ? t : Tekst(wortel, "Name");
        uitkomst.ProjectStart = Iso(Datum(wortel, "StartDate"));
        var minutenPerDag = Getal(wortel, "MinutesPerDay", 480);
        if (minutenPerDag <= 0) minutenPerDag = 480;

        var takenEl = Kind(wortel, "Tasks");
        if (takenEl == null) { uitkomst.Fout = "Er staan geen taken in dit bestand."; return uitkomst; }

        var bronnen = new List<Bron>();
        var overgeslagen = 0;
        foreach (var el in takenEl.Elements().Where(x => x.Name.LocalName == "Task"))
        {
            var uid = Getal(el, "UID", -1);
            if (uid <= 0) continue;                         // UID 0 = de samenvatting van het hele project
            if (Tekst(el, "Active") == "0") { overgeslagen++; continue; }
            var naam = Tekst(el, "Name");
            if (string.IsNullOrWhiteSpace(naam)) continue;  // lege regels in MS Project

            var b = new Bron
            {
                Uid = uid, Naam = naam!, Niveau = Math.Max(1, Getal(el, "OutlineLevel", 1)),
                Wbs = Tekst(el, "WBS") is { Length: > 0 } w ? w : (Tekst(el, "OutlineNumber") ?? ""),
                IsFase = Tekst(el, "Summary") == "1", IsMijlpaal = Tekst(el, "Milestone") == "1",
                Voortgang = Math.Clamp(Getal(el, "PercentComplete"), 0, 100), ConstraintType = Getal(el, "ConstraintType"),
                Start = Datum(el, "Start"), Einde = Datum(el, "Finish"), ConstraintDatum = Datum(el, "ConstraintDate"), Deadline = Datum(el, "Deadline"),
                ActueelStart = Datum(el, "ActualStart"), ActueelEinde = Datum(el, "ActualFinish"), Notitie = Tekst(el, "Notes") ?? "",
            };
            b.Uren = ParseUren(Tekst(el, "Duration"));
            b.Duur = (int)Math.Round(b.Uren * 60.0 / minutenPerDag, MidpointRounding.AwayFromZero);
            if (b.Uren > 0 && b.Duur == 0 && !b.IsMijlpaal) b.Duur = 1;
            if (b.IsMijlpaal) b.Duur = 0;

            // Basisplanning 0
            var basis = el.Elements().Where(x => x.Name.LocalName == "Baseline").FirstOrDefault(x => Getal(x, "Number") == 0);
            if (basis != null) { b.BasisStart = Datum(basis, "Start"); b.BasisEinde = Datum(basis, "Finish"); }

            foreach (var l in el.Elements().Where(x => x.Name.LocalName == "PredecessorLink"))
            {
                var pu = Getal(l, "PredecessorUID", -1);
                if (pu <= 0) continue;
                var type = Getal(l, "Type", 1) switch { 0 => (byte)2 /*FF*/, 1 => (byte)0 /*FS*/, 2 => (byte)3 /*SF*/, 3 => (byte)1 /*SS*/, _ => (byte)0 };
                var procent = Getal(l, "LagFormat", 7) is 19 or 20;     // % van de duur van de voorganger: niet te vertalen
                var lag = (int)Math.Round(Getal(l, "LinkLag") / 10.0 / minutenPerDag, MidpointRounding.AwayFromZero);
                b.Links.Add((pu, type, procent ? 0 : lag, procent));
            }
            bronnen.Add(b);
            if (bronnen.Count > MaxRijen) { uitkomst.Fout = $"Dit bestand heeft meer dan {MaxRijen} regels. Splits het eerst op in MS Project."; return uitkomst; }
        }
        if (bronnen.Count == 0) { uitkomst.Fout = "Er staan geen taken in dit bestand."; return uitkomst; }

        // Een samenvattingstaak zonder onderliggende regels is gewoon een taak; een regel met onderliggende regels is een fase.
        for (var i = 0; i < bronnen.Count; i++)
            bronnen[i].IsFase = i + 1 < bronnen.Count && bronnen[i + 1].Niveau > bronnen[i].Niveau;

        var perUid = new Dictionary<int, int>();
        for (var i = 0; i < bronnen.Count; i++) perUid[bronnen[i].Uid] = i;

        List<Bron> Bladeren(int index)
        {
            var uit = new List<Bron>();
            for (var j = index + 1; j < bronnen.Count && bronnen[j].Niveau > bronnen[index].Niveau; j++)
                if (!bronnen[j].IsFase) uit.Add(bronnen[j]);
            return uit;
        }

        if (overgeslagen > 0) uitkomst.Meldingen.Add(overgeslagen == 1 ? "1 inactieve taak is overgeslagen." : $"{overgeslagen} inactieve taken zijn overgeslagen.");

        // Resources en toewijzingen: welke resource(s) bij welke taak horen.
        var resNaam = new Dictionary<int, string>();
        var resEl = Kind(wortel, "Resources");
        if (resEl != null)
            foreach (var re in resEl.Elements().Where(x => x.Name.LocalName == "Resource"))
            {
                var ru = Getal(re, "UID", -1);
                var rn = Tekst(re, "Name");
                if (ru > 0 && !string.IsNullOrWhiteSpace(rn)) resNaam[ru] = rn!;
            }
        var resPerTaak = new Dictionary<int, List<string>>();
        var asEl = Kind(wortel, "Assignments");
        if (asEl != null)
            foreach (var ae in asEl.Elements().Where(x => x.Name.LocalName == "Assignment"))
            {
                var tu = Getal(ae, "TaskUID", -1);
                var ru = Getal(ae, "ResourceUID", -1);
                if (tu <= 0 || !resNaam.TryGetValue(ru, out var rnm)) continue;
                if (!resPerTaak.TryGetValue(tu, out var lijstRes)) resPerTaak[tu] = lijstRes = new();
                if (!lijstRes.Contains(rnm)) lijstRes.Add(rnm);
            }

        var afhankelijkheden = new HashSet<(int Van, int Naar)>();
        var perBlad = new Dictionary<int, List<(int Van, byte Type, int Wacht)>>();
        for (var i = 0; i < bronnen.Count; i++)
        {
            var b = bronnen[i];
            var r = b.Rij;
            r.Uid = b.Uid; r.Wbs = b.Wbs; r.Naam = b.Naam.Length > 300 ? b.Naam[..300] : b.Naam; r.Niveau = b.Niveau;
            r.Soort = b.IsFase ? "fase" : (b.IsMijlpaal ? "mijlpaal" : "taak");
            r.Duur = b.IsFase ? 0 : b.Duur;
            r.Start = Iso(b.Start); r.Einde = Iso(b.Einde); r.Voortgang = b.Voortgang;
            r.Deadline = Iso(b.Deadline); r.WerkelijkStart = Iso(b.ActueelStart); r.WerkelijkEinde = Iso(b.ActueelEinde);
            r.BasisStart = Iso(b.BasisStart); r.BasisEinde = Iso(b.BasisEinde);
            r.Notitie = string.IsNullOrWhiteSpace(b.Notitie) ? null : (b.Notitie.Length > 2000 ? b.Notitie[..2000] : b.Notitie);
            var meldingen = new List<string>();
            if (!b.IsFase && resPerTaak.TryGetValue(b.Uid, out var taakRes)) r.Resources = taakRes.ToList();
            if (!b.IsFase)
            {
                var (type, datum, tekst) = Beperking(b);
                r.Beperking = type; r.BeperkingDatum = datum;
                if (tekst != null) meldingen.Add(tekst);
                var dagen = b.Uren * 60.0 / minutenPerDag;
                if (!b.IsMijlpaal && Math.Abs(dagen - Math.Round(dagen)) > 0.01)
                    meldingen.Add($"Duur {b.Uren.ToString("0.##", CultureInfo.InvariantCulture)} uur is afgerond naar {b.Duur} werkdag{(b.Duur == 1 ? "" : "en")}.");
            }

            // Voorgangers: naar een fase verwijzen betekent op al haar taken wachten; een fase als opvolger geldt voor haar eerste taak.
            var bladeren = b.IsFase ? Bladeren(i) : new List<Bron> { b };
            if (b.IsFase && b.Links.Count > 0) meldingen.Add(bladeren.Count > 0 ? "Voorganger van een fase geldt voor haar eerste taak." : "Een fase zonder taken kan geen voorganger hebben.");
            foreach (var (pu, type, wacht, procent) in b.Links)
            {
                if (!perUid.TryGetValue(pu, out var vi)) { meldingen.Add($"Voorganger {pu} bestaat niet en is weggelaten."); continue; }
                if (procent) meldingen.Add("Wachttijd in % is niet te vertalen: genomen als 0 werkdagen.");
                var voor = bronnen[vi];
                var voorBladeren = voor.IsFase ? Bladeren(vi) : new List<Bron> { voor };
                var doel = bladeren.FirstOrDefault();
                if (doel == null) continue;
                foreach (var vb in voorBladeren)
                {
                    if (vb.Uid == doel.Uid) { meldingen.Add("Een taak kan niet op zichzelf wachten: weggelaten."); continue; }
                    if (Bereikbaar(perBlad, vb.Uid, doel.Uid)) { meldingen.Add($"De voorganger {vb.Wbs} zou een kring maken en is weggelaten."); continue; }
                    if (!afhankelijkheden.Add((vb.Uid, doel.Uid))) continue;
                    if (!perBlad.TryGetValue(doel.Uid, out var lijst)) perBlad[doel.Uid] = lijst = new();
                    lijst.Add((vb.Uid, type, wacht));
                }
            }
            if (meldingen.Count > 0) { r.Status = "waarschuwing"; r.Melding = string.Join(" ", meldingen.Distinct()); }
        }
        foreach (var b in bronnen)
            if (perBlad.TryGetValue(b.Uid, out var links))
                b.Rij.Voorgangers = links.Select(l => new PlanningImportVoorganger { Uid = l.Van, Type = TypeNaam(l.Type), Wacht = l.Wacht }).ToList();

        uitkomst.Rijen = bronnen.Select(b => b.Rij).ToList();
        uitkomst.Resources = uitkomst.Rijen.SelectMany(r => r.Resources).GroupBy(n => n, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new PlanningImportResource { Naam = g.First(), AantalTaken = g.Count() }).OrderBy(x => x.Naam, StringComparer.CurrentCultureIgnoreCase).ToList();
        uitkomst.AantalFases = uitkomst.Rijen.Count(r => r.Soort == "fase");
        uitkomst.AantalMijlpalen = uitkomst.Rijen.Count(r => r.Soort == "mijlpaal");
        uitkomst.AantalTaken = uitkomst.Rijen.Count(r => r.Soort == "taak");
        uitkomst.AantalAfhankelijkheden = afhankelijkheden.Count;
        uitkomst.AantalAandachtspunten = uitkomst.Rijen.Count(r => r.Status == "waarschuwing");
        if (uitkomst.AantalTaken + uitkomst.AantalMijlpalen == 0) { uitkomst.Fout = "Er staan wel fases maar geen taken in dit bestand."; return uitkomst; }
        uitkomst.Ok = true;
        return uitkomst;
    }

    private static string TypeNaam(byte t) => t switch { 1 => "SS", 2 => "FF", 3 => "SF", _ => "FS" };

    /// <summary>Zou een voorganger <paramref name="van"/> voor <paramref name="doel"/> een kring maken? Ja als <paramref name="doel"/> al (indirect) voorganger van <paramref name="van"/> is.</summary>
    private static bool Bereikbaar(Dictionary<int, List<(int Van, byte Type, int Wacht)>> voorgangers, int van, int doel)
    {
        var gezien = new HashSet<int>();
        var stapel = new Stack<int>();
        stapel.Push(van);
        while (stapel.Count > 0)
        {
            var huidig = stapel.Pop();
            if (huidig == doel) return true;
            if (!gezien.Add(huidig) || !voorgangers.TryGetValue(huidig, out var lijst)) continue;
            foreach (var l in lijst) stapel.Push(l.Van);
        }
        return false;
    }

    private static (byte Type, string? Datum, string? Melding) Beperking(Bron b)
    {
        (byte, string?, string?) Met(byte t) => b.ConstraintDatum == null ? (0, null, "Beperking zonder datum is weggelaten.") : (t, Iso(b.ConstraintDatum), null);
        return b.ConstraintType switch
        {
            1 => (0, null, "“Zo laat mogelijk” is vervangen door “zo vroeg mogelijk”."),
            2 => Met(1),   // moet beginnen op
            3 => Met(6),   // moet eindigen op
            4 => Met(2),   // niet eerder beginnen dan
            5 => Met(3),   // niet later beginnen dan
            6 => Met(4),   // niet eerder eindigen dan
            7 => Met(5),   // niet later eindigen dan
            _ => (0, null, null),
        };
    }

    /// <summary>"PT48H0M0S" → 48 uur. Ook P1DT0H0M0S (een dag telt als 24 uur).</summary>
    internal static double ParseUren(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return 0;
        var m = Regex.Match(iso, @"^P(?:(\d+(?:\.\d+)?)D)?(?:T(?:(\d+(?:\.\d+)?)H)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)S)?)?$", RegexOptions.CultureInvariant);
        if (!m.Success) return 0;
        double G(int i) => m.Groups[i].Success ? double.Parse(m.Groups[i].Value, CultureInfo.InvariantCulture) : 0;
        return G(1) * 24 + G(2) + G(3) / 60 + G(4) / 3600;
    }

    private static string? Iso(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
