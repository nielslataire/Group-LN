using System.Globalization;
using System.Text;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>Beheer van planningssjablonen: lijst, bewaren vanuit een bestaande planning, verwijderen. Het toepassen zit in <see cref="PlanningService"/>.</summary>
public sealed partial class PlanningSjabloonService : IPlanningSjabloonService
{
    private readonly cpmRunningContext _db;
    public PlanningSjabloonService(cpmRunningContext db) { _db = db; }

    public Task<List<PlanningSjabloonInfo>> Lijst() => LijstVan(_db);

    internal static async Task<List<PlanningSjabloonInfo>> LijstVan(cpmRunningContext db)
    {
        var sjablonen = await db.PlanningSjabloon.AsNoTracking().Where(s => s.IsActief).OrderBy(s => s.Naam)
            .Include(s => s.Blokken).ThenInclude(b => b.Taken).ToListAsync();
        return sjablonen.Select(s => new PlanningSjabloonInfo
        {
            Id = s.Id,
            Naam = s.Naam,
            Beschrijving = s.Beschrijving,
            AantalBlokken = s.Blokken.Count,
            AantalTaken = s.Blokken.Sum(b => b.Taken.Count),
            HeeftVerdiepingen = s.Blokken.Any(b => b.Herhaling == 1),
            HeeftKelder = s.Blokken.Any(b => string.Equals(b.VoorwaardeVariabele, "kelder", StringComparison.OrdinalIgnoreCase)),
            HeeftDak = s.Blokken.Any(b => string.Equals(b.VoorwaardeVariabele, "dak", StringComparison.OrdinalIgnoreCase)),
            HeeftEstafette = s.Blokken.Any(b => b.Taken.Any(t => t.Estafette)),
        }).ToList();
    }

    public async Task<PlanningSjabloonBewaarUitkomst> BewaarVanPlanning(int projectId, int? planningId, string naam, string? beschrijving, string? userId)
    {
        naam = (naam ?? "").Trim();
        if (naam.Length == 0) return new PlanningSjabloonBewaarUitkomst { Fout = "Geef het sjabloon een naam." };
        if (naam.Length > 200) naam = naam[..200];
        if (await _db.PlanningSjabloon.AnyAsync(s => s.IsActief && s.Naam == naam))
            return new PlanningSjabloonBewaarUitkomst { Fout = "Er bestaat al een sjabloon met deze naam." };

        var hoofdId = await _db.BouwPlanning.Where(b => b.ProjectId == projectId && b.IsHoofd).Select(b => (int?)b.Id).FirstOrDefaultAsync();
        var gekozen = planningId is null or 0 ? hoofdId : planningId;
        var plan = gekozen == null ? null : await _db.BouwPlanning.FirstOrDefaultAsync(b => b.Id == gekozen && b.ProjectId == projectId);
        var isHoofd = plan == null || plan.IsHoofd;

        var secties = await _db.PlanningSectie.AsNoTracking()
            .Where(s => s.ProjectId == projectId && s.IsActief && (isHoofd ? (s.BouwPlanningId == null || s.BouwPlanningId == hoofdId) : s.BouwPlanningId == gekozen))
            .ToListAsync();
        if (secties.Count == 0) return new PlanningSjabloonBewaarUitkomst { Fout = "Deze planning heeft nog geen fases of taken om als sjabloon te bewaren." };
        var sectieIds = secties.Select(s => s.Id).ToList();
        var taken = await _db.PlanningTaak.AsNoTracking().Where(t => sectieIds.Contains(t.PlanningSectieId)).ToListAsync();
        var takenIds = taken.Select(t => t.Id).ToList();
        var links = await _db.PlanningAfhankelijkheid.AsNoTracking().Where(l => takenIds.Contains(l.OpvolgerTaakId)).ToListAsync();

        // Fases in boomvolgorde (een deelfase komt direct na haar ouder). Deelfases worden zelfstandige blokken.
        var geordend = new List<PlanningSectie>();
        void Voeg(int? ouder)
        {
            foreach (var s in secties.Where(x => x.ParentSectieId == ouder).OrderBy(x => x.Volgorde).ThenBy(x => x.Id))
            {
                geordend.Add(s);
                Voeg(s.Id);
            }
        }
        Voeg(null);
        foreach (var s in secties.Where(s => !geordend.Contains(s))) geordend.Add(s); // wees met ontbrekende ouder

        var gebruikt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string Code(string naamVoorCode)
        {
            var basis = Slug(naamVoorCode);
            var code = basis; var n = 2;
            while (!gebruikt.Add(code)) code = basis + "_" + n++;
            return code;
        }

        var sjabloon = new PlanningSjabloon { Naam = naam, Beschrijving = string.IsNullOrWhiteSpace(beschrijving) ? null : beschrijving.Trim(), IsActief = true, CreatedDate = DateTime.UtcNow, CreatedByUserId = userId };
        var taakCode = new Dictionary<int, string>();
        var sjabloonTaak = new Dictionary<int, PlanningSjabloonTaak>();
        var volgorde = 10;
        foreach (var s in geordend)
        {
            var blok = new PlanningSjabloonBlok { Code = Code(s.Naam), Naam = s.Naam, Volgorde = volgorde, Herhaling = 0 };
            volgorde += 10;
            var tv = 10;
            foreach (var t in taken.Where(t => t.PlanningSectieId == s.Id).OrderBy(t => t.Volgorde).ThenBy(t => t.Id))
            {
                var st = new PlanningSjabloonTaak { Code = Code(t.Naam), Naam = t.Naam, Beschrijving = t.Beschrijving, Duur = t.Duur, Volgorde = tv };
                tv += 10;
                blok.Taken.Add(st);
                taakCode[t.Id] = st.Code!;
                sjabloonTaak[t.Id] = st;
            }
            sjabloon.Blokken.Add(blok);
        }

        var overgeslagen = 0;
        foreach (var l in links)
        {
            if (!sjabloonTaak.TryGetValue(l.OpvolgerTaakId, out var op)) continue;
            if (!taakCode.TryGetValue(l.VoorgangerTaakId, out var vc)) { overgeslagen++; continue; } // voorganger in een andere planning
            op.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = vc, Type = l.Type, WachtDagen = l.WachtDagen });
        }

        _db.PlanningSjabloon.Add(sjabloon);
        await _db.SaveChangesAsync();
        return new PlanningSjabloonBewaarUitkomst
        {
            Ok = true, SjabloonId = sjabloon.Id, Blokken = sjabloon.Blokken.Count, Taken = sjabloon.Blokken.Sum(b => b.Taken.Count), Overgeslagen = overgeslagen,
        };
    }

    public async Task<string?> Verwijder(int id)
    {
        var s = await _db.PlanningSjabloon.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return "Het sjabloon bestaat niet meer.";
        var gebruikt = await _db.BouwPlanning.Where(b => b.SjabloonId == id).Select(b => b.ProjectId).Distinct().CountAsync();
        if (gebruikt > 0) return $"Dit sjabloon is gebruikt in {gebruikt} {(gebruikt == 1 ? "project" : "projecten")} en kan niet verwijderd worden. Zet het op inactief.";
        _db.PlanningSjabloon.Remove(s);
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>"Ruwbouw klaar (dak)" → "RUWBOUW_KLAAR_DAK": hoofdletters, cijfers en underscores, maximaal 40 tekens.</summary>
    internal static string Slug(string tekst)
    {
        var norm = (tekst ?? "").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in norm)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToUpperInvariant(ch));
            else if (sb.Length > 0 && sb[^1] != '_') sb.Append('_');
        }
        var s = sb.ToString().Trim('_');
        if (s.Length == 0) s = "ITEM";
        if (char.IsDigit(s[0])) s = "N" + s;
        return s.Length > 40 ? s[..40].TrimEnd('_') : s;
    }
}
