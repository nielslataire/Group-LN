using BOCore;
using DALCore.Models;

namespace CPMCore.Helpers;

/// <summary>Weergave-afleidingen voor dossiers (design-handoff 31): het label en de stap (x van 6) van een
/// nutsaanvraag volgen uit de laatst ingevulde datum — de status zet je nooit zelf, behalve Geannuleerd.
/// Eén bron voor de matrix (31a), de lijst (31b), het detail (31d) en de melding op het traject (30a).</summary>
public static class DossierWeergave
{
    public const int NutsStappen = 6;

    public static readonly string[] NutsStapLabels =
        { "Aanvraag", "Offerte", "Goedgekeurd", "Uitvoering", "Uitgevoerd", "Overdracht" };

    public static readonly string[] NutsStatusLabels =
        { "Aanvraag verstuurd", "Offerte ontvangen", "Offerte goedgekeurd", "Uitvoering gevraagd", "Uitgevoerd", "Overgedragen" };

    /// <summary>0 (nog niets gebeurd) … 6 (overgedragen). Stap 6 vraagt dat de checklist "Keuring &amp; overdracht"
    /// volledig is afgevinkt (en niet leeg is).</summary>
    public static int NutsStap(ProjectNutsAansluiting? nuts, IEnumerable<ProjectDossierSubstap>? checklist)
    {
        if (nuts == null) return 0;
        int stap = 0;
        if (nuts.AanvraagVerstuurdOp.HasValue) stap = 1;
        if (nuts.OfferteOntvangenOp.HasValue) stap = 2;
        if (nuts.OfferteGoedgekeurdOp.HasValue) stap = 3;
        if (nuts.UitvoeringGevraagdOp.HasValue) stap = 4;
        if (nuts.UitgevoerdOp.HasValue) stap = 5;
        if (stap == 5)
        {
            var open = ChecklistItems(checklist).ToList();
            if (open.Count > 0 && open.All(s => s.Status == (int)DossierSubstapStatus.Afgerond || s.Status == (int)DossierSubstapStatus.NietVanToepassing)) stap = 6;
        }
        return stap;
    }

    /// <summary>De checklist "Keuring &amp; overdracht": alle opvolgstappen die niet aan een werkstroomdatum hangen.</summary>
    public static IEnumerable<ProjectDossierSubstap> ChecklistItems(IEnumerable<ProjectDossierSubstap>? substappen)
    {
        var datumCodes = new HashSet<string>(new[] { "AANVRAAG", "OFFERTE_ONTVANGEN", "OFFERTE_GOEDGEKEURD", "UITVOERINGSDATUM_DOORGEGEVEN", "UITGEVOERD" }, StringComparer.OrdinalIgnoreCase);
        return (substappen ?? Enumerable.Empty<ProjectDossierSubstap>())
            .Where(s => string.IsNullOrEmpty(s.Code) || !datumCodes.Contains(s.Code))
            .OrderBy(s => s.Volgorde);
    }

    /// <summary>Statuslabel van een dossier: voor een nutsaanvraag de laatste stap, voor een dossier met checklist de laatst
    /// afgeronde stap, anders de gewone status ("Aangevraagd" heet bij een ander dossier "Ingediend", design 31b).</summary>
    public static string StatusLabel(ProjectDossier d)
    {
        if (d.Status == (int)DossierStatus.Geannuleerd) return "Geannuleerd";
        if (d.DossierKind == (int)DossierKind.NutsAansluiting)
        {
            var stap = NutsStap(d.NutsAansluiting, d.Substappen);
            return stap == 0 ? "Nieuw" : NutsStatusLabels[stap - 1];
        }
        if (d.DossierKind == (int)DossierKind.Omgevingsvergunning && d.Status != (int)DossierStatus.Afgehandeld)
        {
            var laatste = d.Substappen?.Where(s => s.Status == (int)DossierSubstapStatus.Afgerond).OrderByDescending(s => s.Volgorde).FirstOrDefault();
            if (laatste != null) return laatste.Naam;
        }
        return d.Status == (int)DossierStatus.Aangevraagd ? "Ingediend" : ((DossierStatus)d.Status).GetDisplayName();
    }

    public static bool IsOpen(ProjectDossier d) => d.Status != (int)DossierStatus.Afgehandeld && d.Status != (int)DossierStatus.Geannuleerd;

    /// <summary>De "verwacht"-datum die telt: bij een nutsaanvraag zonder offerte de verwachte offertedatum, anders de verwachte afhandeling.</summary>
    public static DateOnly? Verwacht(ProjectDossier d)
    {
        if (d.DossierKind == (int)DossierKind.NutsAansluiting && d.NutsAansluiting != null && !d.NutsAansluiting.OfferteOntvangenOp.HasValue
            && d.NutsAansluiting.VerwachteOfferteDatum.HasValue)
            return d.NutsAansluiting.VerwachteOfferteDatum;
        return d.VerwachteAfhandelingDatum;
    }

    public static bool IsTeLaat(ProjectDossier d, DateOnly vandaag) => IsOpen(d) && Verwacht(d) is DateOnly v && v < vandaag;

    /// <summary>Voortgang als (gedaan, totaal) voor de segmentenbalk: nuts = stap x van 6, anders de checklist, anders de status (x van 4).</summary>
    public static (int Gedaan, int Totaal) Voortgang(ProjectDossier d)
    {
        if (d.DossierKind == (int)DossierKind.NutsAansluiting) return (NutsStap(d.NutsAansluiting, d.Substappen), NutsStappen);
        var stappen = d.Substappen?.ToList() ?? new List<ProjectDossierSubstap>();
        if (stappen.Count > 0) return (stappen.Count(s => s.Status == (int)DossierSubstapStatus.Afgerond), stappen.Count);
        return ((DossierStatus)d.Status) switch
        {
            DossierStatus.Aangevraagd => (2, 4),
            DossierStatus.InBehandeling => (3, 4),
            DossierStatus.Afgehandeld => (4, 4),
            _ => (0, 4)
        };
    }

    /// <summary>Korte duur "17 d." / "3 w." / "2 m." / "1 j." voor "te laat"-regels in de matrix.</summary>
    public static string KorteDuur(int dagen) =>
        dagen < 14 ? dagen + " d." : dagen < 60 ? (dagen / 7) + " w." : dagen < 365 ? (dagen / 30) + " m." : (dagen / 365) + " j.";

    /// <summary>"17 dagen" / "3 weken" / "2 maanden" / "1 jaar" — voor titels van meldingen ("Offerte 1 jaar te laat").</summary>
    public static string LangeDuur(int dagen) => dagen switch
    {
        < 14 => dagen == 1 ? "1 dag" : dagen + " dagen",
        < 60 => dagen / 7 + " weken",
        < 365 => dagen / 30 == 1 ? "1 maand" : dagen / 30 + " maanden",
        _ => dagen / 365 == 1 ? "1 jaar" : dagen / 365 + " jaar"
    };

    public static string TypeNaam(ProjectDossier d)
    {
        if (d.DossierKind == (int)DossierKind.NutsAansluiting) return "Nutsaansluiting";
        return Enum.IsDefined(typeof(DossierKind), d.DossierKind) ? ((DossierKind)d.DossierKind).GetDisplayName() : "Dossier";
    }

    public static string NutsIcon(int nutsType) => (NutsType)nutsType switch
    {
        NutsType.Elektriciteit => "ph-lightning",
        NutsType.Gas => "ph-flame",
        NutsType.Water => "ph-drop",
        NutsType.Riolering => "ph-toilet",
        NutsType.Proximus or NutsType.Telenet or NutsType.Telecom => "ph-wifi-high",
        _ => "ph-plug"
    };

    public static string KindIcon(ProjectDossier d) => d.DossierKind switch
    {
        (int)DossierKind.NutsAansluiting => NutsIcon(d.NutsAansluiting?.NutsType ?? 0),
        (int)DossierKind.Omgevingsvergunning => "ph-folder-open",
        (int)DossierKind.Attest => "ph-seal-check",
        (int)DossierKind.Keuring => "ph-clipboard-text",
        _ => "ph-file-text"
    };

    /// <summary>Groep voor de typefilter (31b): nuts · vergunning · attest · keuring · vrij · overig.</summary>
    public static string Groep(ProjectDossier d) => d.DossierKind switch
    {
        (int)DossierKind.NutsAansluiting => "nuts",
        (int)DossierKind.Omgevingsvergunning => "vergunning",
        (int)DossierKind.Attest => "attest",
        (int)DossierKind.Keuring => "keuring",
        (int)DossierKind.Vrij => "vrij",
        _ => "overig"
    };

    /// <summary>De regel onder de statuschip in de matrix (31a): wat is de volgende stap / wat is er mis.</summary>
    public static (string Tekst, bool Te_laat) MatrixRegel(ProjectDossier d, DateOnly vandaag)
    {
        var n = d.NutsAansluiting;
        var stap = NutsStap(n, d.Substappen);
        string D(DateOnly? x) => x?.ToString("dd/MM/yyyy") ?? "";
        if (d.Status == (int)DossierStatus.Geannuleerd) return ("geannuleerd", false);
        if (n == null) return ("", false);
        if (stap <= 1 && !n.OfferteOntvangenOp.HasValue && n.VerwachteOfferteDatum is DateOnly v)
        {
            var dagen = vandaag.DayNumber - v.DayNumber;
            return dagen > 0 ? ($"offerte verwacht {D(v)} · {KorteDuur(dagen)} te laat", true) : ($"offerte verwacht {D(v)}", false);
        }
        return stap switch
        {
            0 => ("nog niet aangevraagd", false),
            1 => ($"aangevraagd {D(n.AanvraagVerstuurdOp)}", false),
            2 => ($"offerte ontvangen {D(n.OfferteOntvangenOp)} · goedkeuring nodig", false),
            3 => ($"goedgekeurd {D(n.OfferteGoedgekeurdOp)} · uitvoering aanvragen", false),
            4 => ($"uitvoering gevraagd {D(n.UitvoeringGevraagdOp)}", false),
            5 => ($"uitgevoerd {D(n.UitgevoerdOp)} · keuring & overdracht", false),
            _ => ("overgedragen", false)
        };
    }
}
