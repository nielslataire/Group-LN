using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

public class TrajectInstantiationService : ITrajectInstantiationService
{
    private readonly cpmRunningContext _db;
    private readonly ITrajectSjabloonService _sjablonen;
    private readonly IProjectDossierService _dossiers;

    private const string AnkerProjectCreated = "PROJECT_CREATED";

    public TrajectInstantiationService(cpmRunningContext db, ITrajectSjabloonService sjablonen, IProjectDossierService dossiers)
    {
        _db = db;
        _sjablonen = sjablonen;
        _dossiers = dossiers;
    }

    public async Task<Projecttraject> Instantiate(TrajectInstantiatieBO dto, string? userId)
    {
        if (await _db.Projecttraject.AnyAsync(t => t.ProjectId == dto.ProjectId))
            throw new InvalidOperationException($"Project {dto.ProjectId} heeft al een traject.");

        var project = await _db.Project.FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId)
                      ?? throw new InvalidOperationException($"Project {dto.ProjectId} niet gevonden.");

        TrajectSjabloon? sjabloon = dto.TrajectSjabloonId is int sid and > 0
            ? await _sjablonen.GetById(sid, includeDetails: true)
            : await _sjablonen.GetStandaardVoorProjectType(project.ProjectType);

        if (sjabloon == null)
            throw new InvalidOperationException("Geen (standaard)sjabloon gevonden om het traject uit op te bouwen.");

        var unitIds = await _db.Units.Where(u => u.ProjectId == dto.ProjectId)
            .OrderBy(u => u.Name).Select(u => u.Id).ToListAsync();

        var anchorBase = dto.Startdatum
                         ?? project.StartDateConstruction
                         ?? DateOnly.FromDateTime(DateTime.Today);

        var traject = new Projecttraject
        {
            ProjectId = dto.ProjectId,
            TrajectSjabloonId = sjabloon.Id,
            Naam = string.IsNullOrWhiteSpace(dto.Naam) ? (sjabloon.Naam ?? "Projecttraject") : dto.Naam!.Trim(),
            Status = (int)TrajectStatus.Lopend,
            GestartOp = DateOnly.FromDateTime(DateTime.Today),
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };

        // Fases
        var faseByCode = new Dictionary<string, ProjecttrajectFase>(StringComparer.OrdinalIgnoreCase);
        foreach (var sf in sjabloon.Fases.OrderBy(f => f.Volgorde))
        {
            var fase = new ProjecttrajectFase
            {
                SjabloonFaseId = sf.Id,
                Naam = sf.Naam,
                Code = sf.Code,
                Volgorde = sf.Volgorde,
                KleurCode = sf.KleurCode,
                Status = (int)FaseStatus.Gepland
            };
            traject.Fases.Add(fase);
            if (!string.IsNullOrWhiteSpace(sf.Code)) faseByCode[sf.Code] = fase;
        }
        var eerste = traject.Fases.OrderBy(f => f.Volgorde).FirstOrDefault();
        if (eerste != null) eerste.Status = (int)FaseStatus.Actief;

        // Mijlpalen
        var alleSjabloonMijlpalen = sjabloon.Fases.SelectMany(f => f.Mijlpalen).OrderBy(m => m.Volgorde).ToList();
        foreach (var sm in alleSjabloonMijlpalen)
        {
            var faseCode = sjabloon.Fases.First(f => f.Id == sm.TrajectSjabloonFaseId).Code;
            faseByCode.TryGetValue(faseCode ?? "", out var fase);

            if (sm.Scope == (int)MijlpaalScope.PerEenheid)
            {
                foreach (var uid in unitIds)
                    traject.Mijlpalen.Add(BuildMijlpaal(sm, fase, uid, userId));
            }
            else
            {
                traject.Mijlpalen.Add(BuildMijlpaal(sm, fase, null, userId));
            }
        }

        ResolveDoeldatums(traject.Mijlpalen.ToList(), alleSjabloonMijlpalen, anchorBase);
        SetFasePlanning(traject);

        _db.Projecttraject.Add(traject);
        await _db.SaveChangesAsync();

        foreach (var m in traject.Mijlpalen)
        {
            _db.MijlpaalHistoriek.Add(new MijlpaalHistoriek
            {
                MijlpaalId = m.Id,
                Actie = (int)MijlpaalHistoriekActie.Aangemaakt,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Opmerking = "Aangemaakt bij instantiatie uit sjabloon"
            });
        }
        await _db.SaveChangesAsync();

        return traject;
    }

    public async Task<int> SyncMissing(int projecttrajectId, string? userId)
    {
        var traject = await _db.Projecttraject
            .Include(t => t.Fases)
            .Include(t => t.Mijlpalen).ThenInclude(m => m.Triggers)
            .FirstOrDefaultAsync(t => t.Id == projecttrajectId)
            ?? throw new InvalidOperationException($"Projecttraject {projecttrajectId} niet gevonden.");

        if (traject.TrajectSjabloonId is not int sid)
            return 0;

        var sjabloon = await _sjablonen.GetById(sid, includeDetails: true);
        if (sjabloon == null) return 0;

        var unitIds = await _db.Units.Where(u => u.ProjectId == traject.ProjectId)
            .OrderBy(u => u.Name).Select(u => u.Id).ToListAsync();

        var faseByCode = traject.Fases
            .Where(f => !string.IsNullOrWhiteSpace(f.Code))
            .ToDictionary(f => f.Code!, StringComparer.OrdinalIgnoreCase);

        // ontbrekende fases toevoegen
        foreach (var sf in sjabloon.Fases.OrderBy(f => f.Volgorde))
        {
            if (string.IsNullOrWhiteSpace(sf.Code) || faseByCode.ContainsKey(sf.Code)) continue;
            var fase = new ProjecttrajectFase
            {
                ProjecttrajectId = traject.Id,
                SjabloonFaseId = sf.Id,
                Naam = sf.Naam,
                Code = sf.Code,
                Volgorde = sf.Volgorde,
                KleurCode = sf.KleurCode,
                Status = (int)FaseStatus.Gepland
            };
            traject.Fases.Add(fase);
            faseByCode[sf.Code] = fase;
        }

        var bestaandeProjectCodes = new HashSet<string>(
            traject.Mijlpalen.Where(m => m.UnitId == null && !string.IsNullOrWhiteSpace(m.Code)).Select(m => m.Code!),
            StringComparer.OrdinalIgnoreCase);
        var bestaandePerUnit = new HashSet<(string Code, int UnitId)>(
            traject.Mijlpalen.Where(m => m.UnitId != null && !string.IsNullOrWhiteSpace(m.Code))
                             .Select(m => (m.Code!.ToUpperInvariant(), m.UnitId!.Value)));

        var anchorBase = traject.GestartOp ?? DateOnly.FromDateTime(DateTime.Today);
        var alleSjabloonMijlpalen = sjabloon.Fases.SelectMany(f => f.Mijlpalen).OrderBy(m => m.Volgorde).ToList();
        var sjabloonById = alleSjabloonMijlpalen.ToDictionary(x => x.Id);
        var sjabloonByCode = alleSjabloonMijlpalen
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Bestaande mijlpalen bijwerken met sjabloon-eigenschappen die nog niet ingevuld zijn
        // (bindings + verantwoordelijke rol zijn sjabloon-gestuurd; handmatige waarden blijven behouden).
        int bijgewerkt = 0;
        foreach (var m in traject.Mijlpalen)
        {
            TrajectSjabloonMijlpaal? sm = null;
            if (m.SjabloonMijlpaalId is int smid) sjabloonById.TryGetValue(smid, out sm);
            if (sm == null && !string.IsNullOrWhiteSpace(m.Code)) sjabloonByCode.TryGetValue(m.Code!, out sm);
            if (sm == null) continue;

            bool changed = false;
            if (m.BronBinding == null && sm.BronBinding != null)
            {
                m.BronBinding = sm.BronBinding;
                m.BronParam = sm.BronParam;
                changed = true;
            }
            if (m.VerantwoordelijkeRol == null && sm.VerantwoordelijkeRol != null)
            {
                m.VerantwoordelijkeRol = sm.VerantwoordelijkeRol;
                changed = true;
            }
            if (m.SjabloonMijlpaalId == null)
            {
                m.SjabloonMijlpaalId = sm.Id;
                changed = true;
            }

            var bestaandeSjabloonTriggerIds = new HashSet<int>(
                m.Triggers.Where(t => t.SjabloonTriggerId != null).Select(t => t.SjabloonTriggerId!.Value));
            foreach (var st in sm.Triggers)
            {
                if (bestaandeSjabloonTriggerIds.Contains(st.Id)) continue;
                m.Triggers.Add(BuildTrigger(st));
                changed = true;
            }

            if (changed) { m.ModifiedByUserId = userId; m.ModifiedDate = DateTime.UtcNow; bijgewerkt++; }
        }

        var toegevoegd = new List<Mijlpaal>();
        foreach (var sm in alleSjabloonMijlpalen)
        {
            var faseCode = sjabloon.Fases.First(f => f.Id == sm.TrajectSjabloonFaseId).Code;
            faseByCode.TryGetValue(faseCode ?? "", out var fase);
            var code = sm.Code ?? "";

            if (sm.Scope == (int)MijlpaalScope.PerEenheid)
            {
                foreach (var uid in unitIds)
                {
                    if (bestaandePerUnit.Contains((code.ToUpperInvariant(), uid))) continue;
                    var m = BuildMijlpaal(sm, fase, uid, userId);
                    m.ProjecttrajectId = traject.Id;
                    traject.Mijlpalen.Add(m);
                    toegevoegd.Add(m);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(code) && bestaandeProjectCodes.Contains(code)) continue;
                var m = BuildMijlpaal(sm, fase, null, userId);
                m.ProjecttrajectId = traject.Id;
                traject.Mijlpalen.Add(m);
                toegevoegd.Add(m);
            }
        }

        if (toegevoegd.Count == 0)
        {
            if (_db.ChangeTracker.HasChanges()) await _db.SaveChangesAsync(); // enkel nieuwe fases / bijgewerkte bindings
            bijgewerkt += await _dossiers.RelinkVergunningMijlpalen(traject.ProjectId, userId);
            return bijgewerkt;
        }

        ResolveDoeldatums(traject.Mijlpalen.ToList(), alleSjabloonMijlpalen, anchorBase);

        await _db.SaveChangesAsync();
        foreach (var m in toegevoegd)
        {
            _db.MijlpaalHistoriek.Add(new MijlpaalHistoriek
            {
                MijlpaalId = m.Id,
                Actie = (int)MijlpaalHistoriekActie.Aangemaakt,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Opmerking = "Toegevoegd via sjabloon-synchronisatie"
            });
        }
        await _db.SaveChangesAsync();

        // Nieuwe/bestaande vergunning-mijlpalen die nog aan geen dossier hangen (bv. dossier was al
        // aangemaakt vóór deze mijlpalen bestonden) alsnog koppelen aan een bestaand vergunningsdossier.
        var herkoppeld = await _dossiers.RelinkVergunningMijlpalen(traject.ProjectId, userId);

        return toegevoegd.Count + bijgewerkt + herkoppeld;
    }

    private static Mijlpaal BuildMijlpaal(TrajectSjabloonMijlpaal sm, ProjecttrajectFase? fase, int? unitId, string? userId)
    {
        var m = new Mijlpaal
        {
            SjabloonMijlpaalId = sm.Id,
            ProjecttrajectFase = fase,
            UnitId = unitId,
            Code = sm.Code,
            Naam = sm.Naam,
            Volgorde = sm.Volgorde,
            MijlpaalType = sm.MijlpaalType,
            Status = (int)MijlpaalStatus.Open,
            VerantwoordelijkeRol = sm.VerantwoordelijkeRol,
            BronBinding = sm.BronBinding,
            BronParam = sm.BronParam,
            IsVerplicht = sm.IsVerplicht,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        foreach (var st in sm.Triggers)
            m.Triggers.Add(BuildTrigger(st));
        return m;
    }

    private static MijlpaalTrigger BuildTrigger(TrajectSjabloonMijlpaalTrigger st) => new()
    {
        SjabloonTriggerId = st.Id,
        TriggerEvent = st.TriggerEvent,
        TriggerActie = st.TriggerActie,
        OffsetDagen = st.OffsetDagen,
        ActieParametersJson = st.ActieParametersJson,
        MagProjectWijzigen = st.MagProjectWijzigen,
        IsActief = st.IsActief
    };

    /// <summary>
    /// Berekent <see cref="Mijlpaal.DoeldatumBerekend"/> uit ankercode + offset. Een anker verwijst naar de
    /// <c>Code</c> van een andere mijlpaal (of <c>PROJECT_CREATED</c> voor de basisdatum). Bij per-eenheid
    /// mijlpalen wordt eerst de anker-mijlpaal van dezelfde eenheid gezocht, dan de projectniveau-versie.
    /// Meerdere passes lossen ketens op; niet-opgeloste ankers vallen terug op de basisdatum + offset.
    /// </summary>
    private static void ResolveDoeldatums(List<Mijlpaal> mijlpalen, List<TrajectSjabloonMijlpaal> sjabloon, DateOnly anchorBase)
    {
        var ankerBySjabloonId = sjabloon.ToDictionary(s => s.Id, s => (s.DoeldatumAnkerCode, s.DoeldatumOffsetDagen));
        var byCode = mijlpalen.Where(m => !string.IsNullOrWhiteSpace(m.Code))
                              .ToLookup(m => m.Code!, StringComparer.OrdinalIgnoreCase);

        DateOnly? Effectief(Mijlpaal m) => m.Doeldatum ?? m.DoeldatumBerekend;

        Mijlpaal? AnkerVoor(Mijlpaal m, string ankerCode)
        {
            var kandidaten = byCode[ankerCode].ToList();
            if (kandidaten.Count == 0) return null;
            if (m.UnitId != null)
                return kandidaten.FirstOrDefault(k => k.UnitId == m.UnitId)
                       ?? kandidaten.FirstOrDefault(k => k.UnitId == null)
                       ?? kandidaten[0];
            return kandidaten.FirstOrDefault(k => k.UnitId == null) ?? kandidaten[0];
        }

        for (int pass = 0; pass < mijlpalen.Count + 2; pass++)
        {
            bool changed = false;
            foreach (var m in mijlpalen)
            {
                if (m.DoeldatumBerekend != null || m.Doeldatum != null) continue;
                if (m.SjabloonMijlpaalId is not int smid || !ankerBySjabloonId.TryGetValue(smid, out var anker)) continue;

                var (ankerCode, offset) = anker;
                int off = offset ?? 0;

                if (string.IsNullOrWhiteSpace(ankerCode) || ankerCode.Equals(AnkerProjectCreated, StringComparison.OrdinalIgnoreCase))
                {
                    m.DoeldatumBerekend = anchorBase.AddDays(off);
                    changed = true;
                    continue;
                }

                var ankerM = AnkerVoor(m, ankerCode);
                if (ankerM != null && Effectief(ankerM) is DateOnly basis)
                {
                    m.DoeldatumBerekend = basis.AddDays(off);
                    changed = true;
                }
            }
            if (!changed) break;
        }

        foreach (var m in mijlpalen)
        {
            if (m.DoeldatumBerekend != null || m.Doeldatum != null) continue;
            if (m.SjabloonMijlpaalId is int smid && ankerBySjabloonId.TryGetValue(smid, out var anker))
                m.DoeldatumBerekend = anchorBase.AddDays(anker.DoeldatumOffsetDagen ?? 0);
            else
                m.DoeldatumBerekend = anchorBase;
        }
    }

    private static void SetFasePlanning(Projecttraject traject)
    {
        foreach (var fase in traject.Fases)
        {
            var datums = traject.Mijlpalen
                .Where(m => m.ProjecttrajectFase == fase)
                .Select(m => m.Doeldatum ?? m.DoeldatumBerekend)
                .Where(d => d != null)
                .Select(d => d!.Value)
                .ToList();
            if (datums.Count == 0) continue;
            fase.StartGepland = datums.Min();
            fase.EindGepland = datums.Max();
        }
    }
}
