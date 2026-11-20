using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>Beheer en uitrekenen van schijvensjablonen (migratie 155). Het toepassen gebeurt in het scherm van een betalingsgroep.</summary>
public sealed class SchijfSjabloonService : ISchijfSjabloonService
{
    private readonly cpmRunningContext _db;
    public SchijfSjabloonService(cpmRunningContext db) { _db = db; }

    public async Task<List<SchijfSjabloonBeheerRij>> BeheerLijst()
    {
        var lijst = await _db.SchijfSjabloon.AsNoTracking().Include(s => s.Schijven).OrderBy(s => s.Naam).ToListAsync();
        return lijst.Select(s => new SchijfSjabloonBeheerRij
        {
            Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, AantalSchijven = s.Schijven.Count,
            HeeftVerdiepingen = s.Schijven.Any(x => x.PerVerdieping), IsActief = s.IsActief, GewijzigdOp = s.ModifiedDate ?? s.CreatedDate,
        }).ToList();
    }

    public async Task<List<SchijfSjabloonKeuze>> Keuzes()
    {
        var lijst = await _db.SchijfSjabloon.AsNoTracking().Include(s => s.Schijven).Where(s => s.IsActief && s.Schijven.Any()).OrderBy(s => s.Naam).ToListAsync();
        return lijst.Select(s => new SchijfSjabloonKeuze { Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, HeeftVerdiepingen = s.Schijven.Any(x => x.PerVerdieping) }).ToList();
    }

    public async Task<SchijfSjabloonBewerk?> Haal(int id)
    {
        var s = await _db.SchijfSjabloon.AsNoTracking().Include(x => x.Schijven).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return null;
        return new SchijfSjabloonBewerk
        {
            Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, MinimumPercentage = s.MinimumPercentage, IsActief = s.IsActief,
            Schijven = s.Schijven.OrderBy(x => x.Volgorde).ThenBy(x => x.Id).Select(x => new SchijfSjabloonSchijfBewerk
            {
                Naam = x.Naam, PlanningCode = x.PlanningCode, VastPercentage = x.VastPercentage, Gewicht = x.Gewicht, PerVerdieping = x.PerVerdieping,
            }).ToList(),
        };
    }

    private static List<string> Controleer(SchijfSjabloonBewerk s)
    {
        var fouten = new List<string>();
        if (string.IsNullOrWhiteSpace(s.Naam)) fouten.Add("Geef het sjabloon een naam.");
        if (s.MinimumPercentage < 0m || s.MinimumPercentage > 25m) fouten.Add("Het minimum per schijf ligt tussen 0 en 25 %.");
        if (s.Schijven.Count == 0) fouten.Add("Een sjabloon heeft minstens één schijf.");
        for (var i = 0; i < s.Schijven.Count; i++)
        {
            var x = s.Schijven[i];
            var nr = $"Schijf {i + 1}: ";
            if (string.IsNullOrWhiteSpace(x.Naam)) fouten.Add(nr + "vul de omschrijving in.");
            else if (x.Naam.Trim().Length > 250) fouten.Add(nr + "de omschrijving is te lang.");
            if (x.VastPercentage is decimal v && (v <= 0m || v > 100m)) fouten.Add(nr + "een vast percentage ligt tussen 0 en 100.");
            if (x.VastPercentage == null && x.Gewicht <= 0m) fouten.Add(nr + "het gewicht moet groter dan 0 zijn.");
            if (!string.IsNullOrWhiteSpace(x.PlanningCode) && x.PlanningCode.Trim().Length > 100) fouten.Add(nr + "de code is te lang.");
        }
        if (fouten.Count == 0)
        {
            var vast = s.Schijven.Where(x => x.VastPercentage != null).Sum(x => x.VastPercentage!.Value);
            var gewogen = s.Schijven.Count(x => x.VastPercentage == null);
            if (gewogen == 0 && Math.Abs(vast - 100m) > 0.005m) fouten.Add($"Zonder schijven met een gewicht moeten de vaste percentages samen 100 % zijn (nu {vast:0.##} %).");
            if (gewogen > 0 && vast >= 100m) fouten.Add("De vaste percentages zijn samen minstens 100 %: er blijft niets over voor de schijven met een gewicht.");
        }
        return fouten;
    }

    public async Task<SchijfSjabloonBewerkUitkomst> Bewaar(SchijfSjabloonBewerk s, string? userId)
    {
        var fouten = Controleer(s);
        var naam = (s.Naam ?? "").Trim();
        if (fouten.Count == 0 && await _db.SchijfSjabloon.AnyAsync(x => x.Id != s.Id && x.Naam == naam))
            fouten.Add("Er bestaat al een schijvensjabloon met deze naam.");
        if (fouten.Count > 0) return new SchijfSjabloonBewerkUitkomst { Fouten = fouten };

        SchijfSjabloon entiteit;
        if (s.Id > 0)
        {
            entiteit = await _db.SchijfSjabloon.Include(x => x.Schijven).FirstOrDefaultAsync(x => x.Id == s.Id)
                       ?? throw new InvalidOperationException("Het sjabloon bestaat niet meer.");
            _db.SchijfSjabloonSchijf.RemoveRange(entiteit.Schijven);
            entiteit.Schijven.Clear();
            entiteit.ModifiedDate = DateTime.UtcNow; entiteit.ModifiedByUserId = userId;
        }
        else
        {
            entiteit = new SchijfSjabloon { CreatedDate = DateTime.UtcNow, CreatedByUserId = userId };
            _db.SchijfSjabloon.Add(entiteit);
        }
        entiteit.Naam = naam;
        entiteit.Beschrijving = string.IsNullOrWhiteSpace(s.Beschrijving) ? null : s.Beschrijving.Trim();
        entiteit.MinimumPercentage = s.MinimumPercentage;
        entiteit.IsActief = s.IsActief;
        var volgorde = 10;
        foreach (var x in s.Schijven)
        {
            entiteit.Schijven.Add(new SchijfSjabloonSchijf
            {
                Volgorde = volgorde, Naam = x.Naam.Trim(), PlanningCode = string.IsNullOrWhiteSpace(x.PlanningCode) ? null : x.PlanningCode.Trim(),
                VastPercentage = x.VastPercentage, Gewicht = x.Gewicht > 0 ? x.Gewicht : 1m, PerVerdieping = x.PerVerdieping,
            });
            volgorde += 10;
        }
        await _db.SaveChangesAsync();
        return new SchijfSjabloonBewerkUitkomst { Ok = true, Id = entiteit.Id };
    }

    public async Task<string?> Verwijder(int id)
    {
        var s = await _db.SchijfSjabloon.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return "Het sjabloon bestaat niet meer.";
        _db.SchijfSjabloon.Remove(s);      // de schijven gaan mee (cascade); betalingsgroepen die het gebruikten houden hun eigen schijven
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<(int? Id, string? Fout)> Dupliceer(int id, string? userId)
    {
        var bron = await Haal(id);
        if (bron == null) return (null, "Het sjabloon bestaat niet meer.");
        var basis = bron.Naam + " (kopie)";
        var naam = basis;
        for (var i = 2; await _db.SchijfSjabloon.AnyAsync(x => x.Naam == naam); i++) naam = basis + " " + i;
        bron.Id = 0; bron.Naam = naam; bron.IsActief = false;
        var uitkomst = await Bewaar(bron, userId);
        return uitkomst.Ok ? (uitkomst.Id, null) : (null, string.Join(" ", uitkomst.Fouten));
    }

    public async Task<string?> ZetActief(int id, bool actief, string? userId)
    {
        var s = await _db.SchijfSjabloon.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return "Het sjabloon bestaat niet meer.";
        s.IsActief = actief; s.ModifiedDate = DateTime.UtcNow; s.ModifiedByUserId = userId;
        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<SchijfVoorbeeld?> Voorbeeld(int id, int verdiepingen)
    {
        var s = await _db.SchijfSjabloon.AsNoTracking().Include(x => x.Schijven).FirstOrDefaultAsync(x => x.Id == id && x.IsActief);
        if (s == null || s.Schijven.Count == 0) return null;
        var invoer = s.Schijven.OrderBy(x => x.Volgorde).ThenBy(x => x.Id)
            .Select(x => new SchijfInvoer(x.Naam, x.PlanningCode, x.VastPercentage, x.Gewicht, x.PerVerdieping)).ToList();
        return SchijfSjabloonUitrol.Rol(s.MinimumPercentage, invoer, Math.Max(1, Math.Min(verdiepingen, 20)));
    }
}
