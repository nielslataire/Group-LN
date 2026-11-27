using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>Beheer van planningssjablonen in de editor: lijst, ophalen, bewaren met validatie, proefdraai, dupliceren en (de)activeren.</summary>
public sealed partial class PlanningSjabloonService
{
    private static readonly string[] Types = { "FS", "SS", "FF", "SF" };

    public async Task<List<PlanningSjabloonBeheerRij>> BeheerLijst()
    {
        var sjablonen = await _db.PlanningSjabloon.AsNoTracking().OrderByDescending(s => s.IsActief).ThenBy(s => s.Naam)
            .Include(s => s.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).ToListAsync();

        var gebruikt = await (from b in _db.BouwPlanning.AsNoTracking()
                              where b.SjabloonId != null
                              join p in _db.Project.AsNoTracking() on b.ProjectId equals p.ProjectId
                              select new { Id = b.SjabloonId!.Value, p.ProjectName }).ToListAsync();
        var perSjabloon = gebruikt.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Select(x => x.ProjectName ?? "").Where(n => n.Length > 0).Distinct().OrderBy(n => n).ToList());

        var gebruikerIds = sjablonen.Select(s => s.ModifiedByUserId ?? s.CreatedByUserId).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
        var namen = gebruikerIds.Count == 0 ? new Dictionary<string, string>()
            : (await _db.AspNetUsers1.AsNoTracking().Where(u => gebruikerIds.Contains(u.Id)).Select(u => new { u.Id, u.Forename, u.Name }).ToListAsync())
                .ToDictionary(u => u.Id, u => string.IsNullOrWhiteSpace(u.Forename) ? (u.Name ?? "") : u.Forename);

        return sjablonen.Select(s =>
        {
            var heeftVerdiepingen = s.Blokken.Any(b => b.Herhaling >= 1);
            var verdiepingen = heeftVerdiepingen ? 2 : 0;
            var duur = 0;
            try { duur = PlanningSjabloonProef.Rekenen(s, 1, Math.Max(1, verdiepingen)).Werkdagen; } catch { /* een kapot sjabloon blijft in de lijst staan */ }
            var door = s.ModifiedByUserId ?? s.CreatedByUserId;
            perSjabloon.TryGetValue(s.Id, out var projecten);
            return new PlanningSjabloonBeheerRij
            {
                Id = s.Id, Naam = s.Naam, Beschrijving = s.Beschrijving, IsActief = s.IsActief,
                AantalBlokken = s.Blokken.Count, AantalTaken = s.Blokken.Sum(b => b.Taken.Count),
                HeeftVerdiepingen = heeftVerdiepingen,
                HeeftKelder = PlanningSjabloonUitrol.GebruiktVariabele(s, "kelder"),
                HeeftDak = PlanningSjabloonUitrol.GebruiktVariabele(s, "dak"),
                HeeftTerras = PlanningSjabloonUitrol.GebruiktVariabele(s, "terras"),
                HeeftGevel = PlanningSjabloonUitrol.GebruiktVariabele(s, "gevel"),
                HeeftEstafette = s.Blokken.Any(b => b.Taken.Any(t => t.Estafette)),
                Duur = duur, Verdiepingen = verdiepingen,
                ReageertOp = s.ReageertOp, ReageertOpTekst = PlanningSjabloonReageertOp.Tekst(s.ReageertOp),
                GebruiktIn = (projecten ?? new List<string>()).Take(2).ToList(), GebruiktAantal = (projecten ?? new List<string>()).Count,
                GewijzigdOp = s.ModifiedDate ?? s.CreatedDate,
                GewijzigdDoor = door != null && namen.TryGetValue(door, out var naam) ? naam : null,
            };
        }).ToList();
    }

    public async Task<PlanningSjabloonBewerk?> Haal(int id)
    {
        var s = await _db.PlanningSjabloon.AsNoTracking()
            .Include(x => x.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return null;
        return new PlanningSjabloonBewerk
        {
            Id = s.Id,
            Naam = s.Naam ?? "",
            Beschrijving = s.Beschrijving,
            IsActief = s.IsActief,
            ReageertOp = s.ReageertOp,
            Blokken = s.Blokken.OrderBy(b => b.Volgorde).ThenBy(b => b.Id).Select(b => new PlanningSjabloonBewerkBlok
            {
                Code = b.Code,
                Naam = b.Naam ?? "",
                Herhaling = b.Herhaling,
                VolgtVorigeVerdieping = b.VolgtVorigeVerdieping,
                VoorwaardeVariabele = b.VoorwaardeVariabele,
                VoorwaardeWaarde = b.VoorwaardeWaarde,
                Taken = b.Taken.OrderBy(t => t.Volgorde).ThenBy(t => t.Id).Select(t => new PlanningSjabloonBewerkTaak
                {
                    Code = t.Code,
                    Naam = t.Naam ?? "",
                    Beschrijving = t.Beschrijving,
                    Duur = t.Duur,
                    Estafette = t.Estafette,
                    EstafetteWacht = t.EstafetteWacht,
                    Voorwaarde = string.IsNullOrWhiteSpace(t.Voorwaarde) ? null : t.Voorwaarde,
                    BouwstaatId = t.BouwstaatId,
                    ActivityId = t.ActivityId,
                    Voorgangers = t.Voorgangers.OrderBy(v => v.Id).Select(v => new PlanningSjabloonBewerkVoorganger
                    {
                        Code = v.VoorgangerCode ?? "",
                        Type = Types[Math.Clamp((int)v.Type, 0, 3)],
                        WachtDagen = v.WachtDagen,
                    }).ToList(),
                }).ToList(),
            }).ToList(),
        };
    }

    /// <summary>
    /// Zet het document uit de editor om naar entiteiten en controleert het: codes uniek, voorgangers bestaan, geen verwijzing naar zichzelf of het eigen blok,
    /// geen kring, voorwaarde geldig. Zuiver (geen database). Met <paramref name="tijdelijkeIds"/> krijgen blokken en taken een id voor een proefdraai.
    /// </summary>
    internal static PlanningSjabloon Bouw(PlanningSjabloonBewerk dto, List<string> fouten, bool tijdelijkeIds = false)
    {
        var sjabloon = new PlanningSjabloon { Naam = (dto.Naam ?? "").Trim(), IsActief = dto.IsActief };
        var nummer = 0;

        // Codes: een lege code wordt uit de naam gemaakt, een opgegeven code wordt netjes gemaakt; dubbels zijn een fout.
        var gebruikt = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string Maak(string? code, string naamVoorCode)
        {
            var gewenst = string.IsNullOrWhiteSpace(code) ? Slug(naamVoorCode) : Slug(code);
            if (gebruikt.Add(gewenst)) return gewenst;
            if (!string.IsNullOrWhiteSpace(code)) { fouten.Add($"De code {gewenst} komt twee keer voor. Een code moet uniek zijn in het sjabloon."); return gewenst; }
            var n = 2; string c;
            do { c = gewenst + "_" + n++; } while (!gebruikt.Add(c));
            return c;
        }

        if (dto.Blokken.Count == 0) fouten.Add("Een sjabloon heeft minstens één blok nodig.");

        var codeNaarTaken = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase); // blok- of taakcode -> taakcodes
        var voorgangersPerTaak = new Dictionary<string, List<PlanningSjabloonBewerkVoorganger>>(StringComparer.OrdinalIgnoreCase);
        var taakCodes = new List<string>();
        var bv = 10;
        foreach (var b in dto.Blokken)
        {
            var bnaam = (b.Naam ?? "").Trim();
            if (bnaam.Length == 0) { fouten.Add("Elk blok heeft een naam nodig."); continue; }
            if (bnaam.Length > 200) bnaam = bnaam[..200];
            var bcode = Maak(b.Code, bnaam);

            var herhaling = b.Herhaling == 1 ? (byte)1 : (b.Herhaling == PlanningSjabloonUitrol.PerBouwlaagEenFase ? PlanningSjabloonUitrol.PerBouwlaagEenFase : (byte)0);
            string? variabele = null, waarde = null;
            var vv = (b.VoorwaardeVariabele ?? "").Trim().ToLowerInvariant();
            var vw = (b.VoorwaardeWaarde ?? "").Trim().ToLowerInvariant();
            if (vv.Length > 0)
            {
                if (vv == "kelder" && vw == "ja") { variabele = vv; waarde = vw; }
                else if (vv == "dak" && (vw == "hellend" || vw == "plat")) { variabele = vv; waarde = vw; }
                else if (vv == "terras" && vw == "ja") { variabele = vv; waarde = vw; }
                else fouten.Add($"Blok “{bnaam}”: de voorwaarde is ongeldig (kies kelder, hellend dak, plat dak of terrassen of luifels).");
            }

            var blok = new PlanningSjabloonBlok
            {
                Id = tijdelijkeIds ? ++nummer : 0,
                Code = bcode, Naam = bnaam, Volgorde = bv, Herhaling = herhaling,
                VolgtVorigeVerdieping = herhaling == 1 && b.VolgtVorigeVerdieping,
                VoorwaardeVariabele = variabele, VoorwaardeWaarde = waarde,
            };
            bv += 10;
            var tv = 10;
            var eigen = new List<string>();
            foreach (var t in b.Taken)
            {
                var tnaam = (t.Naam ?? "").Trim();
                if (tnaam.Length == 0) { fouten.Add($"Blok “{bnaam}”: elke taak heeft een naam nodig."); continue; }
                if (tnaam.Length > 200) tnaam = tnaam[..200];
                if (t.Duur < 0 || t.Duur > 3650) fouten.Add($"Taak “{tnaam}”: de duur moet tussen 0 en 3650 werkdagen liggen.");
                if (t.EstafetteWacht < 0 || t.EstafetteWacht > 3650) fouten.Add($"Taak “{tnaam}”: de wachttijd van de estafette moet 0 of meer dagen zijn.");
                var tcode = Maak(t.Code, tnaam);
                var tvoorwaarde = SchoneTaakVoorwaarde(t.Voorwaarde, tnaam, fouten);
                blok.Taken.Add(new PlanningSjabloonTaak
                {
                    Voorwaarde = tvoorwaarde,
                    BouwstaatId = t.BouwstaatId, ActivityId = t.ActivityId,
                    Id = tijdelijkeIds ? ++nummer : 0,
                    Code = tcode, Naam = tnaam,
                    Beschrijving = string.IsNullOrWhiteSpace(t.Beschrijving) ? null : t.Beschrijving.Trim(),
                    Duur = Math.Clamp(t.Duur, 0, 3650), Volgorde = tv,
                    Estafette = t.Estafette, EstafetteWacht = t.Estafette ? Math.Clamp(t.EstafetteWacht, 0, 3650) : 0,
                });
                tv += 10;
                eigen.Add(tcode);
                taakCodes.Add(tcode);
                voorgangersPerTaak[tcode] = t.Voorgangers ?? new();
                codeNaarTaken[tcode] = new List<string> { tcode };
            }
            codeNaarTaken[bcode] = eigen;
            sjabloon.Blokken.Add(blok);
        }

        // Voorgangers: de code moet bestaan, niet naar de taak zelf of haar eigen blok wijzen.
        var rand = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var blok in sjabloon.Blokken)
            foreach (var taak in blok.Taken)
            {
                var gewenst = voorgangersPerTaak.TryGetValue(taak.Code!, out var lijst) ? lijst : new();
                rand[taak.Code!] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var gezien = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var v in gewenst)
                {
                    var vc = (v.Code ?? "").Trim();
                    if (vc.Length == 0) continue;
                    // "EERSTE:CODE" = dezelfde taak in de eerste bouwlaag; de verwijzende taak mag overal staan, de doeltaak moet in een blok per bouwlaag zitten.
                    if (vc.StartsWith("eerste:", StringComparison.OrdinalIgnoreCase))
                    {
                        var ec = Slug(vc["eerste:".Length..].Trim());
                        var etype = (v.Type ?? "FS").Trim().ToUpperInvariant();
                        var eIndex = Array.IndexOf(Types, etype);
                        if (eIndex < 0) { fouten.Add($"Taak “{taak.Naam}”: het type {etype} bestaat niet (kies FS, SS, FF of SF)."); continue; }
                        if (!codeNaarTaken.TryGetValue(ec, out var edoel)) { fouten.Add($"Taak “{taak.Naam}”: voorganger {ec} bestaat niet in dit sjabloon."); continue; }
                        var eBlok = sjabloon.Blokken.FirstOrDefault(b => b.Taken.Any(x => string.Equals(x.Code, ec, StringComparison.OrdinalIgnoreCase)));
                        if (eBlok == null || eBlok.Herhaling < 1)
                        { fouten.Add($"Taak “{taak.Naam}”: “eerste bouwlaag” kan enkel verwijzen naar een taak in een blok dat per bouwlaag terugkomt."); continue; }
                        if (!gezien.Add("eerste:" + ec)) continue;
                        if (v.WachtDagen < -3650 || v.WachtDagen > 3650) { fouten.Add($"Taak “{taak.Naam}”: de wachttijd bij {ec} is te groot."); continue; }
                        taak.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "EERSTE:" + ec, Type = (byte)eIndex, WachtDagen = v.WachtDagen });
                        continue;
                    }
                    // "VORIGE:CODE" = dezelfde taak in de vorige bouwlaag; enkel voor een taak in een blok dat per bouwlaag herhaald wordt.
                    var vorigeBouwlaag = vc.StartsWith("vorige:", StringComparison.OrdinalIgnoreCase);
                    if (vorigeBouwlaag) vc = vc["vorige:".Length..].Trim();
                    vc = Slug(vc);
                    var type = (v.Type ?? "FS").Trim().ToUpperInvariant();
                    var typeIndex = Array.IndexOf(Types, type);
                    if (typeIndex < 0) { fouten.Add($"Taak “{taak.Naam}”: het type {type} bestaat niet (kies FS, SS, FF of SF)."); continue; }
                    if (!codeNaarTaken.TryGetValue(vc, out var doel)) { fouten.Add($"Taak “{taak.Naam}”: voorganger {vc} bestaat niet in dit sjabloon."); continue; }
                    if (vorigeBouwlaag)
                    {
                        var doelBlok = sjabloon.Blokken.FirstOrDefault(b => b.Taken.Any(x => string.Equals(x.Code, vc, StringComparison.OrdinalIgnoreCase)));
                        if (doelBlok == null || doelBlok.Herhaling < 1 || blok.Herhaling < 1)
                        { fouten.Add($"Taak “{taak.Naam}”: “vorige bouwlaag” kan enkel verwijzen naar een taak in een blok dat per bouwlaag terugkomt."); continue; }
                        if (!gezien.Add("vorige:" + vc)) continue;
                        if (v.WachtDagen < -3650 || v.WachtDagen > 3650) { fouten.Add($"Taak “{taak.Naam}”: de wachttijd bij {vc} is te groot."); continue; }
                        // Niet in de kringcontrole: een verwijzing naar de vorige bouwlaag kan nooit een kring in dezelfde bouwlaag vormen.
                        taak.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "VORIGE:" + vc, Type = (byte)typeIndex, WachtDagen = v.WachtDagen });
                        continue;
                    }
                    if (string.Equals(vc, taak.Code, StringComparison.OrdinalIgnoreCase) || string.Equals(vc, blok.Code, StringComparison.OrdinalIgnoreCase))
                    { fouten.Add($"Taak “{taak.Naam}” kan niet op zichzelf of op haar eigen blok wachten."); continue; }
                    if (!gezien.Add(vc)) continue;
                    if (v.WachtDagen < -3650 || v.WachtDagen > 3650) { fouten.Add($"Taak “{taak.Naam}”: de wachttijd bij {vc} is te groot."); continue; }
                    taak.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = vc, Type = (byte)typeIndex, WachtDagen = v.WachtDagen });
                    foreach (var d in doel) rand[taak.Code!].Add(d);
                }
            }

        // Kringverwijzing: een taak die (via anderen) op zichzelf wacht kan nooit ingepland worden.
        var toestand = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 1 = bezig, 2 = klaar
        string? kring = null;
        bool Bezoek(string code)
        {
            toestand[code] = 1;
            foreach (var volgende in rand.TryGetValue(code, out var set) ? set : new HashSet<string>())
            {
                if (toestand.TryGetValue(volgende, out var st)) { if (st == 1) { kring = volgende; return true; } continue; }
                if (Bezoek(volgende)) return true;
            }
            toestand[code] = 2;
            return false;
        }
        foreach (var c in taakCodes)
            if (!toestand.ContainsKey(c) && Bezoek(c)) break;
        if (kring != null)
        {
            var naamKring = sjabloon.Blokken.SelectMany(b => b.Taken).FirstOrDefault(t => string.Equals(t.Code, kring, StringComparison.OrdinalIgnoreCase))?.Naam ?? kring;
            fouten.Add($"De voorgangers vormen een kring bij “{naamKring}”: taken wachten op elkaar en kunnen nooit starten.");
        }
        return sjabloon;
    }

    private static readonly Dictionary<string, string[]> TaakVoorwaardeWaarden = new()
    {
        ["kelder"] = new[] { "ja", "nee" }, ["dak"] = new[] { "hellend", "plat" }, ["terras"] = new[] { "ja", "nee" }, ["gevel"] = new[] { "pleister", "hout", "aluminium" }, ["laag"] = new[] { "eerste", "laatste", "boven", "eenmalig" }, ["grond"] = new[] { "ja", "nee" },
    };

    /// <summary>Controleert de voorwaarde van een taak ("laag:eerste,kelder:nee"): enkel bekende variabelen en waarden. Een ongeldige voorwaarde is een fout, geen stille "altijd".</summary>
    private static string? SchoneTaakVoorwaarde(string? voorwaarde, string taaknaam, List<string> fouten)
    {
        if (string.IsNullOrWhiteSpace(voorwaarde)) return null;
        var delen = new List<string>();
        foreach (var (naam, waarde) in PlanningSjabloonUitrol.TaakVoorwaarden(voorwaarde))
        {
            if (!TaakVoorwaardeWaarden.TryGetValue(naam, out var geldig) || !geldig.Contains(waarde))
            {
                fouten.Add($"Taak “{taaknaam}”: de voorwaarde “{naam}:{waarde}” is ongeldig.");
                continue;
            }
            var deel = naam + ":" + waarde;
            if (!delen.Contains(deel)) delen.Add(deel);
        }
        return delen.Count == 0 ? null : string.Join(",", delen);
    }

    public async Task<PlanningSjabloonBewerkUitkomst> Bewaar(PlanningSjabloonBewerk dto, string? userId)
    {
        var uit = new PlanningSjabloonBewerkUitkomst();
        var fouten = uit.Fouten;
        var naam = (dto.Naam ?? "").Trim();
        if (naam.Length == 0) fouten.Add("Geef het sjabloon een naam.");
        else if (naam.Length > 200) fouten.Add("De naam is te lang (maximaal 200 tekens).");
        else if (await _db.PlanningSjabloon.AnyAsync(s => s.Id != dto.Id && s.Naam == naam))
            fouten.Add("Er bestaat al een sjabloon met deze naam.");
        if (!PlanningSjabloonReageertOp.Geldig(dto.ReageertOp)) fouten.Add("“Reageert op” heeft een onbekende waarde.");

        var nieuw = Bouw(dto, fouten);
        await ControleerBouwstaatEnActiviteit(dto, fouten);
        if (fouten.Count > 0) return uit;

        PlanningSjabloon opslag;
        if (dto.Id > 0)
        {
            var bestaand = await _db.PlanningSjabloon.Include(s => s.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);
            if (bestaand == null) { fouten.Add("Het sjabloon bestaat niet meer."); return uit; }
            foreach (var b in bestaand.Blokken.ToList())
            {
                foreach (var t in b.Taken.ToList()) _db.PlanningSjabloonVoorganger.RemoveRange(t.Voorgangers);
                _db.PlanningSjabloonTaak.RemoveRange(b.Taken);
            }
            _db.PlanningSjabloonBlok.RemoveRange(bestaand.Blokken);
            opslag = bestaand;
        }
        else
        {
            opslag = new PlanningSjabloon { CreatedDate = DateTime.UtcNow, CreatedByUserId = userId };
            _db.PlanningSjabloon.Add(opslag);
        }
        opslag.ModifiedDate = DateTime.UtcNow;
        opslag.ModifiedByUserId = userId;
        opslag.Naam = naam;
        opslag.Beschrijving = string.IsNullOrWhiteSpace(dto.Beschrijving) ? null : dto.Beschrijving.Trim();
        opslag.IsActief = dto.IsActief;
        opslag.ReageertOp = string.IsNullOrEmpty(dto.ReageertOp) ? null : dto.ReageertOp;
        foreach (var b in nieuw.Blokken) opslag.Blokken.Add(b);
        await _db.SaveChangesAsync();
        uit.Ok = true;
        uit.SjabloonId = opslag.Id;
        return uit;
    }

    public PlanningSjabloonProefUitkomst Proef(PlanningSjabloonProefVerzoek verzoek)
    {
        var dto = verzoek.Sjabloon ?? new PlanningSjabloonBewerk();
        var fouten = new List<string>();
        var s = Bouw(dto, fouten, tijdelijkeIds: true);

        PlanningSjabloonProefUitkomst uit;
        try { uit = PlanningSjabloonProef.Rekenen(s, verzoek.Woningen, verzoek.Verdiepingen, verzoek.Kelder); }
        catch { uit = new PlanningSjabloonProefUitkomst(); }

        // Rood: wat het opslaan blokkeert. De naam en "reageert op" zijn geen zaak van de proefdraai.
        foreach (var f in fouten) uit.Meldingen.Insert(0, new PlanningSjabloonProefMelding { Ernst = "rood", Titel = f });
        // Goud: taken zonder voorganger starten op dag 1 van het project (de allereerste taak mag dat).
        var eerste = s.Blokken.SelectMany(b => b.Taken).FirstOrDefault();
        foreach (var t in s.Blokken.SelectMany(b => b.Taken))
            if (t.Voorgangers.Count == 0 && !ReferenceEquals(t, eerste))
                uit.Meldingen.Add(new PlanningSjabloonProefMelding { Ernst = "goud", Titel = t.Naam + " heeft geen voorganger", Tekst = "start op dag 1 van het project" });
        return uit;
    }

    public async Task<(int? Id, string? Fout)> Dupliceer(int id, string? userId)
    {
        var bron = await _db.PlanningSjabloon.AsNoTracking().Include(s => s.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).FirstOrDefaultAsync(s => s.Id == id);
        if (bron == null) return (null, "Het sjabloon bestaat niet meer.");
        var bestaande = await _db.PlanningSjabloon.Select(s => s.Naam).ToListAsync();
        var naam = (bron.Naam ?? "Sjabloon") + " (kopie)";
        for (var n = 2; bestaande.Contains(naam, StringComparer.OrdinalIgnoreCase); n++) naam = $"{bron.Naam} (kopie {n})";
        if (naam.Length > 200) naam = naam[..200];

        var kopie = new PlanningSjabloon { Naam = naam, Beschrijving = bron.Beschrijving, IsActief = true, ReageertOp = null, CreatedDate = DateTime.UtcNow, CreatedByUserId = userId, ModifiedByUserId = userId, ModifiedDate = DateTime.UtcNow };
        foreach (var b in bron.Blokken.OrderBy(x => x.Volgorde))
        {
            var nb = new PlanningSjabloonBlok { Code = b.Code, Naam = b.Naam, Volgorde = b.Volgorde, Herhaling = b.Herhaling, VolgtVorigeVerdieping = b.VolgtVorigeVerdieping, VoorwaardeVariabele = b.VoorwaardeVariabele, VoorwaardeWaarde = b.VoorwaardeWaarde };
            foreach (var t in b.Taken.OrderBy(x => x.Volgorde))
            {
                var nt = new PlanningSjabloonTaak { Code = t.Code, Naam = t.Naam, Beschrijving = t.Beschrijving, Duur = t.Duur, Volgorde = t.Volgorde, Estafette = t.Estafette, EstafetteWacht = t.EstafetteWacht, Voorwaarde = t.Voorwaarde, BouwstaatId = t.BouwstaatId, ActivityId = t.ActivityId };
                foreach (var v in t.Voorgangers) nt.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = v.VoorgangerCode, Type = v.Type, WachtDagen = v.WachtDagen });
                nb.Taken.Add(nt);
            }
            kopie.Blokken.Add(nb);
        }
        _db.PlanningSjabloon.Add(kopie);
        await _db.SaveChangesAsync();
        return (kopie.Id, null);
    }

    public async Task<string?> ZetActief(int id, bool actief, string? userId)
    {
        var s = await _db.PlanningSjabloon.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return "Het sjabloon bestaat niet meer.";
        s.IsActief = actief;
        s.ModifiedDate = DateTime.UtcNow;
        s.ModifiedByUserId = userId;
        await _db.SaveChangesAsync();
        return null;
    }
}

public sealed partial class PlanningSjabloonService
{
    /// <summary>Een gekozen bouwstaat of activiteit moet bestaan (migratie 158); een inactieve bouwstaat mag blijven staan waar ze al gekozen was, maar wordt niet nieuw aangeboden.</summary>
    private async Task ControleerBouwstaatEnActiviteit(PlanningSjabloonBewerk dto, List<string> fouten)
    {
        var taken = dto.Blokken.SelectMany(b => b.Taken).ToList();
        var bouwstaten = taken.Where(t => t.BouwstaatId.HasValue).Select(t => t.BouwstaatId!.Value).Distinct().ToList();
        var activiteiten = taken.Where(t => t.ActivityId.HasValue).Select(t => t.ActivityId!.Value).Distinct().ToList();
        if (bouwstaten.Count > 0)
        {
            var bestaand = await _db.Bouwstaat.Where(b => bouwstaten.Contains(b.Id)).Select(b => b.Id).ToListAsync();
            if (bestaand.Count != bouwstaten.Count) fouten.Add("Een gekozen bouwstaat bestaat niet meer. Kies opnieuw.");
        }
        if (activiteiten.Count > 0)
        {
            var bestaand = await _db.Activity.Where(a => activiteiten.Contains(a.ActivityId)).Select(a => a.ActivityId).ToListAsync();
            if (bestaand.Count != activiteiten.Count) fouten.Add("Een gekozen activiteit bestaat niet meer. Kies opnieuw.");
        }
    }
}
