using BOCore;

namespace CPMCore.Models.Projecten;

/// <summary>
/// De vier (+1) "fasen" waarin een project op de portfoliokaarten en in de
/// projectenlijst wordt ingedeeld. De volgorde van de enum-waarden is meteen
/// de standaard sorteervolgorde van Projecten/Index: van begin naar einde.
/// </summary>
public enum ProjectPhaseBucket
{
    InUitvoering = 0,
    Opstart      = 1,
    Afgewerkt    = 2,
    Opgeleverd   = 3,
    Stopgezet    = 4,
}

/// <summary>
/// Eén bron van waarheid voor "in welke fase zit dit project" — gebruikt door
/// zowel de sortering in <c>ProjectenController.Index</c>/<c>LoadMoreProjects</c>
/// als het status-chipje op <c>_ProjectWerfCard</c>. Zo kunnen sortering en
/// chip niet meer uit elkaar lopen.
/// </summary>
public static class ProjectPhase
{
    /// <summary>
    /// Bepaalt de fase-bucket. Een terminale projectstatus (Stopgezet /
    /// Opgeleverd) wint altijd van de voortgangsfase, want die laatste kan
    /// verouderd zijn (bv. een stopgezet project met nog een oude
    /// voortgangsberekening op fase "Opstart").
    /// </summary>
    public static ProjectPhaseBucket Resolve(ProjectBO project, ProjectVoortgangBO voortgang)
    {
        var statusId = project?.Status?.Id ?? 0;

        if (statusId == (int)ProjectStatusType.Stopgezet) return ProjectPhaseBucket.Stopgezet;
        if (statusId == (int)ProjectStatusType.Opgeleverd) return ProjectPhaseBucket.Opgeleverd;

        if (voortgang != null)
        {
            return voortgang.Fase switch
            {
                VoortgangFase.InUitvoering => ProjectPhaseBucket.InUitvoering,
                VoortgangFase.Eindfase     => ProjectPhaseBucket.InUitvoering,
                VoortgangFase.Afgewerkt    => ProjectPhaseBucket.Afgewerkt,
                _                          => ProjectPhaseBucket.Opstart,
            };
        }

        return statusId == (int)ProjectStatusType.Uitvoering
            ? ProjectPhaseBucket.InUitvoering
            : ProjectPhaseBucket.Opstart;
    }

    /// <summary>Sorteersleutel: lager = vroeger in de lijst.</summary>
    public static int SortKey(ProjectBO project, ProjectVoortgangBO voortgang)
        => (int)Resolve(project, voortgang);

    /// <summary>
    /// Klasse + label voor het status-chipje op de werfkaart. Voor niet-terminale
    /// projecten zonder voortgangsberekening blijft de echte projectstatusnaam
    /// staan (Ontwerp, Bouwaanvraag, Voorverkoop ...); anders de fase-woordenschat.
    /// </summary>
    public static (string Cls, string Label) Chip(ProjectBO project, ProjectVoortgangBO voortgang)
    {
        switch (Resolve(project, voortgang))
        {
            case ProjectPhaseBucket.Stopgezet:
                return ("sc-rood", "Stopgezet");
            case ProjectPhaseBucket.Opgeleverd:
                return ("sc-donker", "Opgeleverd");
            case ProjectPhaseBucket.Afgewerkt:
                return ("sc-donker", "Afgewerkt");
            case ProjectPhaseBucket.InUitvoering:
                if (voortgang == null)
                    return ("sc-groen", string.IsNullOrWhiteSpace(project?.Status?.Name) ? "In uitvoering" : project.Status.Name);
                return ("sc-groen", voortgang.Fase == VoortgangFase.Eindfase ? "Eindfase" : "In uitvoering");
            default: // Opstart
                if (voortgang == null)
                    return ("sc-geel", string.IsNullOrWhiteSpace(project?.Status?.Name) ? "Opstart" : project.Status.Name);
                return ("sc-geel", voortgang.Fase == VoortgangFase.InVoorbereiding ? "In voorbereiding" : "Opstart");
        }
    }
}
