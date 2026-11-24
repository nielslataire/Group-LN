using System.Text.RegularExpressions;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>Beheer van de standaardlijst bouwstaten (migratie 158): wat een planning kan opleveren en waar schijven en traject naar verwijzen.</summary>
public sealed class BouwstaatService : IBouwstaatService
{
    private static readonly Regex CodePatroon = new("^[A-Z][A-Z0-9_]{1,59}$", RegexOptions.Compiled);

    private readonly cpmRunningContext _db;
    public BouwstaatService(cpmRunningContext db) { _db = db; }

    public async Task<List<BouwstaatRij>> Lijst()
    {
        var lijst = await _db.Bouwstaat.AsNoTracking().Where(b => b.ProjectId == null).OrderBy(b => b.Volgorde).ThenBy(b => b.Naam).ToListAsync();
        var gebruik = await Gebruik();
        return lijst.Select(b => new BouwstaatRij
        {
            Id = b.Id, Code = b.Code, Naam = b.Naam, Groep = b.Groep, Volgorde = b.Volgorde, Bereik = b.Bereik,
            PerBouwlaag = b.PerBouwlaag, IsActief = b.IsActief, ProjectId = b.ProjectId,
            GebruiktIn = gebruik.TryGetValue(b.Id, out var n) ? n : 0,
        }).ToList();
    }

    /// <summary>Per bouwstaat het aantal verwijzingen vanuit sjabloontaken, planningtaken, fases en schijven.</summary>
    private async Task<Dictionary<int, int>> Gebruik()
    {
        var totaal = new Dictionary<int, int>();
        void Tel(IEnumerable<int?> ids)
        {
            foreach (var id in ids.Where(i => i.HasValue).Select(i => i!.Value))
                totaal[id] = totaal.TryGetValue(id, out var n) ? n + 1 : 1;
        }
        Tel(await _db.PlanningSjabloonTaak.AsNoTracking().Where(t => t.BouwstaatId != null).Select(t => t.BouwstaatId).ToListAsync());
        Tel(await _db.PlanningTaak.AsNoTracking().Where(t => t.BouwstaatId != null).Select(t => t.BouwstaatId).ToListAsync());
        Tel(await _db.PlanningSectie.AsNoTracking().Where(t => t.BouwstaatId != null).Select(t => t.BouwstaatId).ToListAsync());
        Tel(await _db.SchijfSjabloonSchijf.AsNoTracking().Where(t => t.BouwstaatId != null).Select(t => t.BouwstaatId).ToListAsync());
        Tel(await _db.InvoicingPaymentStages.AsNoTracking().Where(t => t.BouwstaatId != null).Select(t => t.BouwstaatId).ToListAsync());
        return totaal;
    }

    public async Task<BouwstaatBewerk?> Haal(int id)
    {
        var b = await _db.Bouwstaat.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == null);
        return b == null ? null : new BouwstaatBewerk
        {
            Id = b.Id, Code = b.Code, Naam = b.Naam, Groep = b.Groep, Volgorde = b.Volgorde, Bereik = b.Bereik,
            PerBouwlaag = b.PerBouwlaag, IsActief = b.IsActief,
        };
    }

    public async Task<BouwstaatUitkomst> Bewaar(BouwstaatBewerk invoer)
    {
        var uit = new BouwstaatUitkomst();
        var naam = (invoer.Naam ?? "").Trim();
        var groep = string.IsNullOrWhiteSpace(invoer.Groep) ? null : invoer.Groep.Trim();
        var code = (invoer.Code ?? "").Trim().ToUpperInvariant();

        if (naam.Length == 0) uit.Fouten.Add("Geef een naam in.");
        else if (naam.Length > 200) uit.Fouten.Add("De naam mag maximaal 200 tekens hebben.");
        if (groep != null && groep.Length > 100) uit.Fouten.Add("De groep mag maximaal 100 tekens hebben.");
        if (invoer.Bereik > 1) uit.Fouten.Add("Kies of de bouwstaat voor het gebouw of per eenheid geldt.");

        Bouwstaat? entiteit = null;
        if (invoer.Id != 0)
        {
            entiteit = await _db.Bouwstaat.FirstOrDefaultAsync(x => x.Id == invoer.Id && x.ProjectId == null);
            if (entiteit == null) { uit.Fouten.Add("Deze bouwstaat bestaat niet meer."); return uit; }
        }
        else
        {
            if (!CodePatroon.IsMatch(code)) uit.Fouten.Add("De code bestaat uit hoofdletters, cijfers en underscores (bv. OPSTORT_DAK) en begint met een letter.");
            else if (await _db.Bouwstaat.AnyAsync(x => x.Code == code && x.ProjectId == null)) uit.Fouten.Add($"De code {code} bestaat al.");
        }
        if (uit.Fouten.Count > 0) return uit;

        if (entiteit == null)
        {
            entiteit = new Bouwstaat { Code = code };
            _db.Bouwstaat.Add(entiteit);
        }
        entiteit.Naam = naam;
        entiteit.Groep = groep;
        entiteit.Volgorde = invoer.Volgorde;
        entiteit.Bereik = invoer.Bereik;
        entiteit.PerBouwlaag = invoer.PerBouwlaag;
        entiteit.IsActief = invoer.IsActief;
        await _db.SaveChangesAsync();
        uit.Ok = true;
        uit.Id = entiteit.Id;
        return uit;
    }

    public async Task<string?> Verwijder(int id)
    {
        var b = await _db.Bouwstaat.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == null);
        if (b == null) return "Deze bouwstaat bestaat niet meer.";
        var gebruik = await Gebruik();
        if (gebruik.TryGetValue(id, out var n) && n > 0)
            return $"Deze bouwstaat wordt nog {n} keer gebruikt (taken, fases of schijven). Zet ze op inactief in plaats van te verwijderen.";
        _db.Bouwstaat.Remove(b);
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<BouwstaatKeuzes> Keuzes()
    {
        var uit = new BouwstaatKeuzes();
        uit.Bouwstaten = (await _db.Bouwstaat.AsNoTracking().Where(b => b.ProjectId == null).OrderBy(b => b.Volgorde).ThenBy(b => b.Naam).ToListAsync())
            .Select(b => new BouwstaatKeuze { Id = b.Id, Naam = b.Naam, Groep = b.Groep, Code = b.Code, IsActief = b.IsActief }).ToList();
        var activiteiten = await _db.Activity.AsNoTracking().Include(a => a.Group).ToListAsync();
        uit.Activiteiten = activiteiten
            .OrderBy(a => a.Group?.Lot ?? 9999).ThenBy(a => a.Omschrijving)
            .Select(a => new ActiviteitKeuze { Id = a.ActivityId, Naam = a.Omschrijving ?? "", Groep = a.Group == null ? null : (a.Group.Lot.HasValue ? ((int)a.Group.Lot.Value) + " " : "") + a.Group.Name })
            .ToList();
        return uit;
    }

    public async Task<string?> ZetActief(int id, bool actief)
    {
        var b = await _db.Bouwstaat.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == null);
        if (b == null) return "Deze bouwstaat bestaat niet meer.";
        b.IsActief = actief;
        await _db.SaveChangesAsync();
        return null;
    }
}
