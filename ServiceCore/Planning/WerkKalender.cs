namespace ServiceCore.Planning;

/// <summary>
/// De werkkalender van een project: één set regels voor de deadline van de oplevering
/// (<c>ProjectService.GetFinalConstructionDay</c>) én de bouwplanning, zodat die nooit uit elkaar lopen.
/// Een werkdag is maandag t/m vrijdag, geen feestdag/bouwverlof (<c>VacationDays</c>, bedrijf + project)
/// en geen verletdag (<c>BadWeatherDays</c> van het weerstation van het project).
/// </summary>
public sealed class WerkKalender
{
    private readonly HashSet<DateOnly> _verlof;
    private readonly HashSet<DateOnly> _verlet;

    /// <param name="verlof">Feestdagen en bouwverlof.</param>
    /// <param name="verlet">Verletdagen (leeg als het project geen weerstation heeft).</param>
    public WerkKalender(IEnumerable<DateOnly>? verlof = null, IEnumerable<DateOnly>? verlet = null)
    {
        _verlof = new HashSet<DateOnly>(verlof ?? Array.Empty<DateOnly>());
        _verlet = new HashSet<DateOnly>(verlet ?? Array.Empty<DateOnly>());
    }

    public static bool IsWeekend(DateOnly d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public bool IsVerlof(DateOnly d) => _verlof.Contains(d);

    public bool IsVerlet(DateOnly d) => _verlet.Contains(d);

    /// <summary>Werkdag, met of zonder verlet te tellen als niet-werkdag.</summary>
    public bool IsWerkdag(DateOnly d, bool metVerlet = true)
        => !IsWeekend(d) && !_verlof.Contains(d) && !(metVerlet && _verlet.Contains(d));

    /// <summary>Eerste werkdag op of na <paramref name="d"/>.</summary>
    public DateOnly OpOfNa(DateOnly d, bool metVerlet = true)
    {
        while (!IsWerkdag(d, metVerlet)) d = d.AddDays(1);
        return d;
    }

    /// <summary>Laatste werkdag op of voor <paramref name="d"/>.</summary>
    public DateOnly OpOfVoor(DateOnly d, bool metVerlet = true)
    {
        while (!IsWerkdag(d, metVerlet)) d = d.AddDays(-1);
        return d;
    }

    /// <summary>
    /// <paramref name="dagen"/> werkdagen verder (negatief = terug); de startdag zelf telt niet mee, 0 geeft dezelfde dag.
    /// Zelfde regel als de oude <c>AddWorkDays</c>.
    /// </summary>
    public DateOnly Voeg(DateOnly datum, int dagen, bool metVerlet = true)
    {
        if (dagen == 0) return datum;
        var stap = dagen < 0 ? -1 : 1;
        var rest = Math.Abs(dagen);
        while (rest > 0)
        {
            datum = datum.AddDays(stap);
            if (IsWerkdag(datum, metVerlet)) rest--;
        }
        return datum;
    }

    /// <summary>Aantal werkdagen na <paramref name="van"/> tot en met <paramref name="tot"/> (0 als tot niet later is).</summary>
    public int Tel(DateOnly van, DateOnly tot, bool metVerlet = true)
    {
        if (tot <= van) return 0;
        var n = 0;
        for (var d = van.AddDays(1); d <= tot; d = d.AddDays(1))
            if (IsWerkdag(d, metVerlet)) n++;
        return n;
    }

    /// <summary>
    /// Einddatum van een taak die op <paramref name="start"/> (een werkdag) begint en <paramref name="duur"/> werkdagen duurt.
    /// Een duur van 0 (mijlpaal) eindigt dezelfde dag.
    /// </summary>
    public DateOnly Einde(DateOnly start, int duur, bool metVerlet = true)
        => duur <= 1 ? start : Voeg(start, duur - 1, metVerlet);

    /// <summary>Feestdagen en bouwverlof tussen twee datums (beide inbegrepen), weekends niet meegeteld.</summary>
    public IEnumerable<DateOnly> VerlofDagen(DateOnly van, DateOnly tot)
        => _verlof.Where(d => d >= van && d <= tot && !IsWeekend(d)).OrderBy(d => d);

    /// <summary>Verletdagen tussen twee datums (beide inbegrepen) die geen weekend of verlof zijn: enkel die kosten echt een werkdag.</summary>
    public IEnumerable<DateOnly> VerletDagen(DateOnly van, DateOnly tot)
        => _verlet.Where(d => d >= van && d <= tot && !IsWeekend(d) && !_verlof.Contains(d)).OrderBy(d => d);
}
