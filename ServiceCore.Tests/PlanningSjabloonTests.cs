using System.Text.Json;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Planningssjablonen: uitrollen (voorwaarden, verdiepingen), bewaren vanuit een planning en toepassen met estafette (migratie 146).</summary>
public class PlanningSjabloonTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;

    private static (PlanningService Svc, PlanningSjabloonService Sjab, TestDb Db) Maak()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling", StartDateConstruction = new DateOnly(2026, 11, 16) }); // maandag
        for (var i = 1; i <= 3; i++) { db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 10 + i, Name = "Woning " + i, ProjectId = ProjectId }); db.Units.Add(new Units { Id = 10 + i, Name = "Woning " + i, ProjectId = ProjectId, PaymentGroupId = 10 + i }); }
        db.SaveChanges();
        return (new PlanningService(db), new PlanningSjabloonService(db), db);
    }

    private static PlanningSjabloon Voorbeeld()
    {
        var s = new PlanningSjabloon { Id = 1, Naam = "Woning", IsActief = true };
        var fundering = new PlanningSjabloonBlok { Id = 1, Code = "FUND", Naam = "Fundering", Volgorde = 10 };
        fundering.Taken.Add(new PlanningSjabloonTaak { Id = 1, BlokId = 1, Code = "GRAAF", Naam = "Uitgraven", Duur = 2, Volgorde = 10, Estafette = true });
        fundering.Taken.Add(new PlanningSjabloonTaak { Id = 2, BlokId = 1, Code = "BETON", Naam = "Betonneren", Duur = 3, Volgorde = 20 });
        fundering.Taken.ElementAt(1).Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "GRAAF" });
        var kelder = new PlanningSjabloonBlok { Id = 2, Code = "KELDER", Naam = "Kelder", Volgorde = 20, VoorwaardeVariabele = "kelder", VoorwaardeWaarde = "ja" };
        kelder.Taken.Add(new PlanningSjabloonTaak { Id = 3, BlokId = 2, Code = "KELDERMUUR", Naam = "Kelderwanden", Duur = 4, Volgorde = 10 });
        kelder.Taken.ElementAt(0).Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "BETON" });
        var ruwbouw = new PlanningSjabloonBlok { Id = 3, Code = "RUW", Naam = "Ruwbouw", Volgorde = 30, Herhaling = 1, VolgtVorigeVerdieping = true };
        ruwbouw.Taken.Add(new PlanningSjabloonTaak { Id = 4, BlokId = 3, Code = "METSEL", Naam = "Metselwerk", Duur = 5, Volgorde = 10 });
        ruwbouw.Taken.Add(new PlanningSjabloonTaak { Id = 5, BlokId = 3, Code = "VLOER", Naam = "Vloer", Duur = 2, Volgorde = 20 });
        ruwbouw.Taken.ElementAt(0).Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "FUND" });        // verwijst naar een blok
        ruwbouw.Taken.ElementAt(1).Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "METSEL" });      // zelfde verdieping
        var dak = new PlanningSjabloonBlok { Id = 4, Code = "DAKHELLEND", Naam = "Dak", Volgorde = 40, VoorwaardeVariabele = "dak", VoorwaardeWaarde = "hellend" };
        dak.Taken.Add(new PlanningSjabloonTaak { Id = 6, BlokId = 4, Code = "DAKSPANT", Naam = "Dakspant", Duur = 3, Volgorde = 10 });
        dak.Taken.ElementAt(0).Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "VLOER" });           // herhaald blok: de laatste verdieping
        dak.Taken.ElementAt(0).Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "BESTAATNIET" });
        foreach (var b in new[] { fundering, kelder, ruwbouw, dak }) s.Blokken.Add(b);
        return s;
    }

    [Fact]
    public void Een_blok_met_voorwaarde_valt_weg_en_een_blok_per_verdieping_wordt_herhaald()
    {
        var zonder = PlanningSjabloonUitrol.Rol(Voorbeeld(), new SjabloonVariabelen(1, false, null));
        Assert.Equal(new[] { "Fundering", "Ruwbouw" }, zonder.Fases.Select(f => f.Naam));   // geen kelder, geen hellend dak, 1 verdieping = geen suffix

        var met = PlanningSjabloonUitrol.Rol(Voorbeeld(), new SjabloonVariabelen(2, true, "hellend"));
        Assert.Equal(new[] { "Fundering", "Kelder", "Ruwbouw gelijkvloers", "Ruwbouw 1ste verdieping", "Dak" }, met.Fases.Select(f => f.Naam));

        var plat = PlanningSjabloonUitrol.Rol(Voorbeeld(), new SjabloonVariabelen(1, false, "plat"));
        Assert.DoesNotContain(plat.Fases, f => f.Naam == "Dak");
    }

    [Fact]
    public void Voorgangers_via_codes_wijzen_naar_de_juiste_verdieping_en_de_volgende_verdieping_volgt_de_vorige()
    {
        var uit = PlanningSjabloonUitrol.Rol(Voorbeeld(), new SjabloonVariabelen(2, true, "hellend"));
        var v1 = uit.Fases.Single(f => f.Naam == "Ruwbouw gelijkvloers");
        var v2 = uit.Fases.Single(f => f.Naam == "Ruwbouw 1ste verdieping");

        // Vloer volgt metselwerk van dezelfde verdieping.
        Assert.Equal("4#1", Assert.Single(v1.Taken[1].Voorgangers).Sleutel);
        Assert.Equal("4#2", Assert.Single(v2.Taken[1].Voorgangers).Sleutel);
        // Metselwerk wacht op het hele blok Fundering (2 taken) en op verdieping 1 (alle taken) als het verdieping 2 is.
        Assert.Equal(new[] { "1", "2" }, v1.Taken[0].Voorgangers.Select(x => x.Sleutel).OrderBy(x => x));
        Assert.Contains(v2.Taken[0].Voorgangers, x => x.Sleutel == "4#1");
        Assert.Contains(v2.Taken[0].Voorgangers, x => x.Sleutel == "5#1");

        // Het dak wacht op de vloer van de laatste verdieping; de onbekende code is een waarschuwing.
        var dak = uit.Fases.Single(f => f.Naam == "Dak").Taken[0];
        Assert.Equal("5#2", Assert.Single(dak.Voorgangers).Sleutel);
        Assert.Contains(uit.Waarschuwingen, w => w.Contains("BESTAATNIET"));
    }

    [Fact]
    public void Een_voorganger_naar_een_blok_dat_niet_meedoet_wordt_stilzwijgend_overgeslagen()
    {
        var uit = PlanningSjabloonUitrol.Rol(Voorbeeld(), new SjabloonVariabelen(1, false, null));
        Assert.Empty(uit.Waarschuwingen);
        var metsel = uit.Fases.Single(f => f.Naam == "Ruwbouw").Taken[0];
        Assert.Equal(2, metsel.Voorgangers.Count);
    }

    private static Dictionary<string, JsonElement> V(params (string Naam, object? Waarde)[] velden)
        => velden.ToDictionary(v => v.Naam, v => JsonSerializer.SerializeToElement(v.Waarde));

    [Fact]
    public async Task Een_planning_bewaren_als_sjabloon_geeft_codes_en_voorgangers_en_laat_voorgangers_uit_andere_plannings_weg()
    {
        var (svc, sjab, db) = Maak();
        var s = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", Versie = 0, Velden = V(("naam", "Ruwbouw"), ("duur", 5)) }, "u");
        Assert.True(s.Ok, s.Fout);
        s = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", Versie = s.Staat!.Versie, Velden = V(("naam", "Ruwbouw"), ("duur", 2), ("voorgangers", "1.1")) }, "u");
        Assert.True(s.Ok, s.Fout);

        var u = await sjab.BewaarVanPlanning(ProjectId, null, "Standaard", null, "u");
        Assert.True(u.Ok, u.Fout);
        Assert.Equal(2, u.Taken);

        var sj = await db.PlanningSjabloon.Include(x => x.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).SingleAsync();
        var blok = Assert.Single(sj.Blokken);
        Assert.Equal("FASE_1", blok.Code);
        Assert.Equal(new[] { "RUWBOUW", "RUWBOUW_2" }, blok.Taken.OrderBy(t => t.Volgorde).Select(t => t.Code));
        Assert.Equal("RUWBOUW", Assert.Single(blok.Taken.Single(t => t.Code == "RUWBOUW_2").Voorgangers).VoorgangerCode);

        Assert.False((await sjab.BewaarVanPlanning(ProjectId, null, "Standaard", null, "u")).Ok);   // naam bestaat al
        Assert.Single(await sjab.Lijst());
    }

    [Fact]
    public async Task Een_sjabloon_toepassen_op_drie_woningen_maakt_drie_plannings_met_estafette_in_de_gekozen_volgorde()
    {
        var (svc, _, db) = Maak();
        db.PlanningSjabloon.Add(Voorbeeld());
        await db.SaveChangesAsync();

        var c = new PlanningCommando
        {
            Type = "sjabloonToepassen", Versie = 0,
            Toepassing = new PlanningSjabloonToepassing
            {
                SjabloonId = 1,
                Planningen = new List<PlanningToepassingPlanning>
                {
                    new() { EenheidIds = { 13 }, Verdiepingen = 1 },   // woning 3 eerst
                    new() { EenheidIds = { 11 }, Verdiepingen = 1 },
                    new() { EenheidIds = { 12 }, Verdiepingen = 2, Kelder = true },
                },
            },
        };
        var u = await svc.Voer(ProjectId, c, "u");
        Assert.True(u.Ok, u.Fout);

        Assert.Equal(new[] { "Woning 3", "Woning 1", "Woning 2" }, (await db.BouwPlanning.Where(p => !p.IsHoofd).OrderBy(p => p.Volgorde).ToListAsync()).Select(p => p.Naam));
        Assert.Equal(u.NieuwePlanningId, u.Staat!.ActiefPlanningId);
        Assert.Equal("Woning 3", u.Staat.Planningen.Single(p => p.Id == u.NieuwePlanningId).Naam);

        // Uitgraven heeft de estafettevlag: woning 1 wacht op woning 3, woning 2 op woning 1. Betonneren heeft ze niet.
        var graven = await db.PlanningTaak.Where(t => t.Naam == "Uitgraven").Include(t => t.PlanningSectie).ToListAsync();
        var perPlanning = graven.ToDictionary(t => db.BouwPlanning.Single(p => p.Id == t.PlanningSectie.BouwPlanningId).Naam, t => t.Id);
        var links = await db.PlanningAfhankelijkheid.ToListAsync();
        Assert.Contains(links, l => l.VoorgangerTaakId == perPlanning["Woning 3"] && l.OpvolgerTaakId == perPlanning["Woning 1"]);
        Assert.Contains(links, l => l.VoorgangerTaakId == perPlanning["Woning 1"] && l.OpvolgerTaakId == perPlanning["Woning 2"]);
        var w2Id = (await db.BouwPlanning.SingleAsync(p => p.Naam == "Woning 2")).Id;
        foreach (var l in links.Where(l => l.VoorgangerTaakId == perPlanning["Woning 2"]))   // geen volgende woning: alles blijft binnen woning 2
            Assert.Equal(w2Id, (await db.PlanningTaak.Include(t => t.PlanningSectie).SingleAsync(t => t.Id == l.OpvolgerTaakId)).PlanningSectie.BouwPlanningId);
        var betonneren = await db.PlanningTaak.Where(t => t.Naam == "Betonneren").Select(t => t.Id).ToListAsync();
        Assert.DoesNotContain(links, l => betonneren.Contains(l.VoorgangerTaakId) && betonneren.Contains(l.OpvolgerTaakId));

        // Woning 2 heeft een kelder en twee verdiepingen, de anderen niet.
        var w2 = await svc.Laad(ProjectId, (await db.BouwPlanning.SingleAsync(p => p.Naam == "Woning 2")).Id);
        Assert.Contains(w2.Rijen, r => r.Soort == "fase" && r.Naam == "Kelder");
        Assert.Contains(w2.Rijen, r => r.Soort == "fase" && r.Naam == "Ruwbouw 1ste verdieping");
        var w1 = await svc.Laad(ProjectId, (await db.BouwPlanning.SingleAsync(p => p.Naam == "Woning 1")).Id);
        Assert.DoesNotContain(w1.Rijen, r => r.Soort == "fase" && r.Naam == "Kelder");

        // De woningen na elkaar: het einde van woning 2 ligt na het einde van woning 3.
        var einde3 = u.Staat.Planningen.Single(p => p.Naam == "Woning 3").Einde;
        var einde2 = w2.Planningen.Single(p => p.Naam == "Woning 2").Einde;
        Assert.True(string.CompareOrdinal(einde2, einde3) > 0);
    }

    [Fact]
    public async Task Een_bouwdeel_met_een_planning_of_van_een_ander_project_wordt_geweigerd()
    {
        var (svc, _, db) = Maak();
        db.PlanningSjabloon.Add(Voorbeeld());
        db.Units.Add(new Units { Id = 99, Name = "Elders", ProjectId = 2 });
        await db.SaveChangesAsync();

        var vreemd = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = 1, Planningen = { new() { EenheidIds = { 99 } } } } }, "u");
        Assert.False(vreemd.Ok);

        var eerste = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = 1, Planningen = { new() { EenheidIds = { 11 } } } } }, "u");
        Assert.True(eerste.Ok, eerste.Fout);
        var dubbel = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = eerste.Staat!.Versie, Toepassing = new() { SjabloonId = 1, Planningen = { new() { EenheidIds = { 11 } } } } }, "u");
        Assert.False(dubbel.Ok);
        Assert.Contains("zit al in een planning", dubbel.Fout);
    }

    [Fact]
    public async Task Eenheden_uit_dezelfde_betalingsgroep_kunnen_elk_een_eigen_planning_of_samen_een_planning_hebben()
    {
        var (svc, _, db) = Maak();
        db.PlanningSjabloon.Add(Voorbeeld());
        db.Units.Add(new Units { Id = 21, Name = "Appartement A", ProjectId = ProjectId, PaymentGroupId = 11 });
        db.Units.Add(new Units { Id = 22, Name = "Appartement B", ProjectId = ProjectId, PaymentGroupId = 11 });
        await db.SaveChangesAsync();

        // Woning 1 (groep 11) en twee appartementen (ook groep 11): één planning voor Woning 1, één gedeelde voor beide appartementen.
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = 1, Planningen = { new() { EenheidIds = { 11 } }, new() { EenheidIds = { 21, 22 } } } } }, "u");
        Assert.True(u.Ok, u.Fout);
        var gedeeld = u.Staat!.Planningen.Single(p => p.Naam == "Appartement A e.a.");
        Assert.Equal(new[] { 21, 22 }, gedeeld.EenheidIds.OrderBy(i => i).ToArray());
        Assert.Equal(new[] { 11 }, u.Staat.Planningen.Single(p => p.Naam == "Woning 1").EenheidIds.ToArray());
        Assert.Equal(3, u.Staat.Eenheden.Count(e => e.PlanningId != null && e.Id is 11 or 21 or 22));
    }

    [Fact]
    public async Task Een_planning_met_taken_wordt_enkel_na_bevestiging_door_een_ander_sjabloon_vervangen()
    {
        var (svc, db2, db) = Maak();
        db.PlanningSjabloon.Add(Voorbeeld());
        await db.SaveChangesAsync();
        var eerste = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = 1, Planningen = { new() { EenheidIds = { 11 } } } } }, "u");
        Assert.True(eerste.Ok, eerste.Fout);
        var planningId = eerste.NieuwePlanningId!.Value;
        var aantal = await db.PlanningTaak.CountAsync();
        Assert.True(aantal > 0);

        var zonder = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = eerste.Staat!.Versie, Toepassing = new() { SjabloonId = 1, Planningen = { new() { PlanningId = planningId } } } }, "u");
        Assert.False(zonder.Ok);
        Assert.Contains("vervangen", zonder.Fout);

        var met = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = eerste.Staat.Versie, Toepassing = new() { SjabloonId = 1, Planningen = { new() { PlanningId = planningId, Vervang = true } } } }, "u");
        Assert.True(met.Ok, met.Fout);
        Assert.Equal(aantal, await db.PlanningTaak.CountAsync());
        Assert.Single(met.Staat!.Planningen.Where(p => !p.IsHoofd));
        Assert.Equal(new[] { 11 }, met.Staat.Planningen.Single(p => !p.IsHoofd).EenheidIds.ToArray());
    }

    [Fact]
    public async Task Een_planning_uit_een_sjabloon_kan_hernoemd_en_verwijderd_worden()
    {
        var (svc, _, db) = Maak();
        db.PlanningSjabloon.Add(Voorbeeld());
        await db.SaveChangesAsync();
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = 1, Planningen = { new() { EenheidIds = { 11 } }, new() { EenheidIds = { 12 } } } } }, "u");
        Assert.True(u.Ok, u.Fout);
        var id = u.NieuwePlanningId!.Value;

        var naam = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningWijzig", Versie = u.Staat!.Versie, PlanningId = id, Velden = V(("naam", "Huis Peeters"), ("eenheden", new[] { 11 })) }, "u");
        Assert.True(naam.Ok, naam.Fout);
        Assert.Contains(naam.Staat!.Planningen, p => p.Naam == "Huis Peeters");

        var weg = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningVerwijder", Versie = naam.Staat.Versie, PlanningId = id }, "u");
        Assert.True(weg.Ok, weg.Fout);
        Assert.DoesNotContain(weg.Staat!.Planningen, p => p.Id == id);
    }

    private static PlanningSjabloonBewerk Editor(string naam = "Eengezinswoning")
    {
        var b1 = new PlanningSjabloonBewerkBlok { Naam = "Fundering" };
        b1.Taken.Add(new() { Naam = "Uitgraven", Duur = 2, Estafette = true, EstafetteWacht = 1 });
        b1.Taken.Add(new() { Naam = "Betonneren", Duur = 3, Voorgangers = { new() { Code = "UITGRAVEN", Type = "SS", WachtDagen = 1 } } });
        var b2 = new PlanningSjabloonBewerkBlok { Naam = "Ruwbouw", Herhaling = 1, VolgtVorigeVerdieping = true, VoorwaardeVariabele = "dak", VoorwaardeWaarde = "hellend" };
        b2.Taken.Add(new() { Naam = "Metselwerk", Duur = 5, Voorgangers = { new() { Code = "FUNDERING" } } });
        return new PlanningSjabloonBewerk { Naam = naam, Blokken = { b1, b2 } };
    }

    [Fact]
    public async Task Een_eenmalige_taak_kan_wachten_op_de_taak_van_de_eerste_bouwlaag()
    {
        var (_, sjab, db) = Maak();
        var dto = new PlanningSjabloonBewerk { Naam = "Gebouw" };
        var boven = new PlanningSjabloonBewerkBlok { Naam = "Bovenbouw", Herhaling = 2 };
        boven.Taken.Add(new() { Code = "METSELWERK", Naam = "Metselwerk", Duur = 5, Voorgangers = { new() { Code = "VORIGE:METSELWERK" } } });
        boven.Taken.Add(new() { Code = "GEVEL", Naam = "Gevelmetselwerk", Duur = 8, Voorwaarde = "laag:eenmalig", Voorgangers = { new() { Code = "EERSTE:METSELWERK" } } });
        dto.Blokken.Add(boven);
        var uit = await sjab.Bewaar(dto, "u");
        Assert.True(uit.Ok, string.Join("; ", uit.Fouten));
        var terug = (await sjab.Haal(uit.SjabloonId))!;
        Assert.Equal("EERSTE:METSELWERK", terug.Blokken[0].Taken[1].Voorgangers.Single().Code);

        var rol = PlanningSjabloonUitrol.Rol(db.PlanningSjabloon.Include(s => s.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).Single(s => s.Id == uit.SjabloonId), new SjabloonVariabelen(3, false, null));
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        var gevel = taken.Single(t => t.Naam == "Gevelmetselwerk");
        var eerste = taken.Single(t => t.Naam == "Metselwerk gelijkvloers");
        Assert.Equal(eerste.Sleutel, Assert.Single(gevel.Voorgangers).Sleutel);   // niet het metselwerk van de bovenste bouwlaag
    }

    [Fact]
    public async Task Bouwstaat_en_activiteit_van_een_sjabloontaak_worden_bewaard_en_komen_op_de_planningtaak()
    {
        var (svc, sjab, db) = Maak();
        var bouwstaat = new Bouwstaat { Code = "FUNDERING_KLAAR", Naam = "Fundering klaar" };
        db.Bouwstaat.Add(bouwstaat);
        db.Activity.Add(new Activity { ActivityId = 77, Omschrijving = "Ruwbouw", GroupId = 1 });
        await db.SaveChangesAsync();

        var dto = Editor();
        dto.Blokken[0].Taken[0].BouwstaatId = bouwstaat.Id;
        dto.Blokken[0].Taken[0].ActivityId = 77;
        var uit = await sjab.Bewaar(dto, "u");
        Assert.True(uit.Ok, string.Join("; ", uit.Fouten));

        var terug = (await sjab.Haal(uit.SjabloonId))!;
        Assert.Equal(bouwstaat.Id, terug.Blokken[0].Taken[0].BouwstaatId);
        Assert.Equal(77, terug.Blokken[0].Taken[0].ActivityId);
        Assert.Null(terug.Blokken[0].Taken[1].BouwstaatId);

        var toegepast = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = uit.SjabloonId, Planningen = { new() { EenheidIds = { 11 }, Verdiepingen = 1 } } } }, "u");
        Assert.True(toegepast.Ok, toegepast.Fout);
        var taak = await db.PlanningTaak.SingleAsync(t => t.Code == "UITGRAVEN");
        Assert.Equal(bouwstaat.Id, taak.BouwstaatId);
        Assert.Equal(77, taak.ActivityId);
        Assert.Null((await db.PlanningTaak.SingleAsync(t => t.Code == "BETONNEREN")).BouwstaatId);
    }

    [Fact]
    public async Task Een_onbestaande_bouwstaat_wordt_geweigerd_bij_het_bewaren()
    {
        var (_, sjab, _) = Maak();
        var dto = Editor();
        dto.Blokken[0].Taken[0].BouwstaatId = 9999;
        var uit = await sjab.Bewaar(dto, "u");
        Assert.False(uit.Ok);
        Assert.Contains(uit.Fouten, f => f.Contains("bouwstaat"));
    }

    [Fact]
    public async Task De_editor_bewaart_een_sjabloon_met_codes_en_leest_het_terug()
    {
        var (_, sjab, _) = Maak();
        var uit = await sjab.Bewaar(Editor(), "u");
        Assert.True(uit.Ok, string.Join("; ", uit.Fouten));

        var terug = await sjab.Haal(uit.SjabloonId);
        Assert.NotNull(terug);
        Assert.Equal(new[] { "FUNDERING", "RUWBOUW" }, terug!.Blokken.Select(b => b.Code));
        var uitgraven = terug.Blokken[0].Taken[0];
        Assert.Equal("UITGRAVEN", uitgraven.Code);
        Assert.True(uitgraven.Estafette);
        Assert.Equal(1, uitgraven.EstafetteWacht);
        var beton = terug.Blokken[0].Taken[1];
        Assert.Equal("SS", beton.Voorgangers.Single().Type);
        Assert.Equal(1, beton.Voorgangers.Single().WachtDagen);
        Assert.Equal(1, terug.Blokken[1].Herhaling);
        Assert.Equal("dak", terug.Blokken[1].VoorwaardeVariabele);
        Assert.Contains(await sjab.BeheerLijst(), r => r.Id == uit.SjabloonId && r.HeeftVerdiepingen && r.HeeftDak && r.HeeftEstafette && !r.HeeftKelder);
    }

    [Fact]
    public async Task Opnieuw_bewaren_vervangt_de_blokken_en_het_sjabloon_blijft_toepasbaar()
    {
        var (svc, sjab, db) = Maak();
        var eerste = await sjab.Bewaar(Editor(), "u");
        var dto = (await sjab.Haal(eerste.SjabloonId))!;
        dto.Naam = "Herdoopt";
        dto.Blokken.RemoveAt(1);
        dto.Blokken[0].Taken[1].Duur = 7;
        var tweede = await sjab.Bewaar(dto, "u");
        Assert.True(tweede.Ok, string.Join("; ", tweede.Fouten));
        Assert.Equal(eerste.SjabloonId, tweede.SjabloonId);
        Assert.Equal(1, await db.PlanningSjabloon.CountAsync());
        Assert.Equal(2, await db.PlanningSjabloonTaak.CountAsync());
        Assert.Equal(1, await db.PlanningSjabloonVoorganger.CountAsync());

        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = eerste.SjabloonId, Planningen = { new() { EenheidIds = { 11 } }, new() { EenheidIds = { 12 } } } } }, "u");
        Assert.True(u.Ok, u.Fout);
    }

    [Fact]
    public async Task De_editor_weigert_onbekende_voorgangers_kringen_dubbele_namen_en_lege_namen()
    {
        var (_, sjab, _) = Maak();
        Assert.True((await sjab.Bewaar(Editor(), "u")).Ok);

        var dubbel = await sjab.Bewaar(Editor(), "u");
        Assert.False(dubbel.Ok);
        Assert.Contains(dubbel.Fouten, f => f.Contains("bestaat al een sjabloon"));

        var onbekend = Editor("Andere");
        onbekend.Blokken[0].Taken[0].Voorgangers.Add(new() { Code = "NIETS" });
        var u1 = await sjab.Bewaar(onbekend, "u");
        Assert.False(u1.Ok);
        Assert.Contains(u1.Fouten, f => f.Contains("NIETS"));

        var kring = Editor("Kring");
        kring.Blokken[0].Taken[0].Voorgangers.Add(new() { Code = "BETONNEREN" }); // uitgraven wacht op betonneren, dat wacht op uitgraven
        var u2 = await sjab.Bewaar(kring, "u");
        Assert.False(u2.Ok);
        Assert.Contains(u2.Fouten, f => f.Contains("kring"));

        var zelf = Editor("Zelf");
        zelf.Blokken[0].Taken[0].Voorgangers.Add(new() { Code = "FUNDERING" }); // eigen blok
        Assert.False((await sjab.Bewaar(zelf, "u")).Ok);

        var leeg = Editor("");
        Assert.False((await sjab.Bewaar(leeg, "u")).Ok);

        var voorwaarde = Editor("Voorwaarde");
        voorwaarde.Blokken[1].VoorwaardeWaarde = "rond";
        Assert.False((await sjab.Bewaar(voorwaarde, "u")).Ok);
    }

    [Fact]
    public void De_proefdraai_rekent_werkdagen_per_blok_en_toont_de_estafette()
    {
        var (_, sjab, _) = Maak();
        var uit = sjab.Proef(new PlanningSjabloonProefVerzoek { Sjabloon = Editor(), Woningen = 1, Verdiepingen = 2 });
        Assert.DoesNotContain(uit.Meldingen, m => m.Ernst == "rood");
        // Uitgraven 2d, Betonneren 3d (SS+1), Metselwerk 5d per verdieping (na de fundering)
        var fundering = uit.Blokken.Single(b => b.Code == "FUNDERING");
        Assert.Equal(1, fundering.Start);
        Assert.True(fundering.Duur >= 3);
        Assert.True(uit.Werkdagen > fundering.Einde);
        Assert.Contains(uit.Taken, t => t.Code == "METSELWERK" && t.Start > fundering.Einde - 1);

        var drie = sjab.Proef(new PlanningSjabloonProefVerzoek { Sjabloon = Editor(), Woningen = 3, Verdiepingen = 2 });
        Assert.Equal(3, drie.Woningen.Count);
        Assert.True(drie.WerkdagenAlle > drie.Werkdagen);             // uitgraven wacht op de vorige woning
        Assert.True(drie.Woningen[1].Start > drie.Woningen[0].Start);
        Assert.True(drie.Woningen[2].Einde > drie.Woningen[1].Einde);

        var zonderEstafette = Editor();
        zonderEstafette.Blokken[0].Taken[0].Estafette = false;
        var parallel = sjab.Proef(new PlanningSjabloonProefVerzoek { Sjabloon = zonderEstafette, Woningen = 3, Verdiepingen = 2 });
        Assert.Equal(parallel.Werkdagen, parallel.WerkdagenAlle);    // alle woningen tegelijk
    }

    [Fact]
    public void De_proefdraai_meldt_kringen_onbekende_codes_en_taken_zonder_voorganger()
    {
        var (_, sjab, _) = Maak();
        var kring = Editor();
        kring.Blokken[0].Taken[0].Voorgangers.Add(new() { Code = "BETONNEREN" });
        var uit = sjab.Proef(new PlanningSjabloonProefVerzoek { Sjabloon = kring });
        Assert.Contains(uit.Meldingen, m => m.Ernst == "rood" && m.Titel.Contains("kring"));

        var onbekend = Editor();
        onbekend.Blokken[1].Taken[0].Voorgangers.Add(new() { Code = "NERGENS" });
        Assert.Contains(sjab.Proef(new PlanningSjabloonProefVerzoek { Sjabloon = onbekend }).Meldingen, m => m.Ernst == "rood" && m.Titel.Contains("NERGENS"));

        var los = Editor();
        los.Blokken[1].Taken[0].Voorgangers.Clear();
        Assert.Contains(sjab.Proef(new PlanningSjabloonProefVerzoek { Sjabloon = los }).Meldingen, m => m.Ernst == "goud" && m.Titel.Contains("Metselwerk"));
    }

    [Fact]
    public async Task Dupliceren_activeren_en_verwijderen_van_een_sjabloon_in_gebruik()
    {
        var (svc, sjab, db) = Maak();
        var bron = await sjab.Bewaar(Editor(), "u");
        var (kopieId, fout) = await sjab.Dupliceer(bron.SjabloonId, "u");
        Assert.Null(fout);
        var kopie = await sjab.Haal(kopieId!.Value);
        Assert.Equal("Eengezinswoning (kopie)", kopie!.Naam);
        Assert.Equal(2, kopie.Blokken.Count);
        Assert.Equal("SS", kopie.Blokken[0].Taken[1].Voorgangers.Single().Type);
        Assert.Equal(2, (await sjab.BeheerLijst()).Count);

        Assert.Null(await sjab.ZetActief(kopieId.Value, false, "u"));
        Assert.False((await sjab.Haal(kopieId.Value))!.IsActief);
        Assert.DoesNotContain(await sjab.Lijst(), s => s.Id == kopieId.Value);        // inactief wordt niet aangeboden bij het toepassen

        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "sjabloonToepassen", Versie = 0, Toepassing = new() { SjabloonId = bron.SjabloonId, Planningen = { new() { EenheidIds = { 11 } } } } }, "u");
        Assert.True(u.Ok, u.Fout);
        var rij = (await sjab.BeheerLijst()).Single(r => r.Id == bron.SjabloonId);
        Assert.Equal(1, rij.GebruiktAantal);
        Assert.Equal("Verkaveling", rij.GebruiktIn.Single());
        Assert.True(rij.Duur > 0);

        Assert.Contains("kan niet verwijderd", await sjab.Verwijder(bron.SjabloonId) ?? "");
        Assert.Null(await sjab.Verwijder(kopieId.Value));                              // ongebruikt: mag wel
    }

    [Fact]
    public async Task Reageert_op_wordt_bewaard_en_een_onbekende_waarde_geweigerd()
    {
        var (_, sjab, _) = Maak();
        var dto = Editor();
        dto.ReageertOp = "traject-start";
        var uit = await sjab.Bewaar(dto, "u");
        Assert.True(uit.Ok, string.Join("; ", uit.Fouten));
        Assert.Equal("traject-start", (await sjab.Haal(uit.SjabloonId))!.ReageertOp);
        Assert.Equal("Traject: start der werken", (await sjab.BeheerLijst()).Single().ReageertOpTekst);

        var fout = Editor("Andere");
        fout.ReageertOp = "iets-anders";
        Assert.False((await sjab.Bewaar(fout, "u")).Ok);
    }

    /// <summary>
    /// Appartementsgebouw zoals Niels het ziet: alles van de ruwbouw zit in één blok per bouwlaag (gelijkvloers, 1ste verdieping, ...), met voorwaarden per taak.
    /// De afdek van een bouwlaag is de vloerplaat van de volgende; met een kelder is de afdek van de kelder de vloerplaat van het gelijkvloers.
    /// </summary>
    private static PlanningSjabloon Gebouw()
    {
        var s = new PlanningSjabloon { Id = 1, Naam = "Gebouw", IsActief = true };
        PlanningSjabloonTaak T(int id, int blok, string code, string naam, int duur, int volgorde, string? voorwaarde = null, params (string Code, byte Type, int Wacht)[] voorgangers)
        {
            var t = new PlanningSjabloonTaak { Id = id, BlokId = blok, Code = code, Naam = naam, Duur = duur, Volgorde = volgorde, Voorwaarde = voorwaarde };
            foreach (var v in voorgangers) t.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = v.Code, Type = v.Type, WachtDagen = v.Wacht });
            return t;
        }
        var voor = new PlanningSjabloonBlok { Id = 1, Code = "B_VOORBEREIDING", Naam = "Voorbereiding", Volgorde = 1 };
        voor.Taken.Add(T(1, 1, "WERF_UITZETTEN", "Werf uitzetten", 3, 1));
        var kelder = new PlanningSjabloonBlok { Id = 2, Code = "B_KELDER", Naam = "Ruwbouw kelder", Volgorde = 2, VoorwaardeVariabele = "kelder", VoorwaardeWaarde = "ja" };
        kelder.Taken.Add(T(2, 2, "AFDEK_KELDER", "Afdek kelder (= vloerplaat gelijkvloers)", 15, 1, null, ("WERF_UITZETTEN", 0, 0)));
        var ruw = new PlanningSjabloonBlok { Id = 3, Code = "B_RUWBOUW", Naam = "Ruwbouw", Volgorde = 3, Herhaling = 1 };
        ruw.Taken.Add(T(3, 3, "VLOERPLAAT_GLVL", "Gieten vloerplaat gelijkvloers", 2, 1, "laag:eerste,kelder:nee", ("WERF_UITZETTEN", 0, 0)));
        ruw.Taken.Add(T(4, 3, "METSELWERK_RUWBOUW", "Metselwerk", 15, 2, null, ("VLOERPLAAT_GLVL", 0, 0), ("AFDEK_KELDER", 0, 0), ("VORIGE:AFDEK", 0, 0)));
        ruw.Taken.Add(T(5, 3, "AFDEK", "Afdek (= vloerplaat volgende verdieping)", 10, 3, null, ("METSELWERK_RUWBOUW", 0, 0)));
        ruw.Taken.Add(T(6, 3, "TERRAS_LUIFEL", "Terrassen en luifels", 5, 4, "terras:ja", ("AFDEK", 0, 0)));
        ruw.Taken.Add(T(7, 3, "GEVELMETSELWERK", "Gevelmetselwerk", 40, 5, "laag:laatste"));   // één taak, hangt aan niets
        var dak = new PlanningSjabloonBlok { Id = 4, Code = "B_DAK", Naam = "Dak", Volgorde = 4 };
        dak.Taken.Add(T(8, 4, "DAKBEDEKKING", "Dakbedekking", 15, 1, null, ("AFDEK", 0, 0)));
        foreach (var b in new[] { voor, kelder, ruw, dak }) s.Blokken.Add(b);
        return s;
    }

    private static UitgerolTaak Taak(SjabloonUitrolling uit, string fase, string naamBegin)
        => uit.Fases.Single(f => f.Naam == fase).Taken.Single(t => t.Naam.StartsWith(naamBegin));

    /// <summary>
    /// Zoals Niels het wil: kelder = fase "Ruwbouw - Onderbouw", en één fase "Ruwbouw - Bovenbouw" met per bouwlaag predallen, opstort en metselwerk.
    /// Zonder kelder is het gelijkvloers gewoon een vloerplaat; bij een plat dak komen er predallen en opstort van het dak bij; het gevelmetselwerk is één taak.
    /// </summary>
    private static PlanningSjabloon Bovenbouw()
    {
        var s = new PlanningSjabloon { Id = 1, Naam = "Bovenbouw", IsActief = true };
        PlanningSjabloonTaak T(int id, int blok, string code, string naam, int duur, int volgorde, string? voorwaarde = null, params (string Code, byte Type, int Wacht)[] voorgangers)
        {
            var t = new PlanningSjabloonTaak { Id = id, BlokId = blok, Code = code, Naam = naam, Duur = duur, Volgorde = volgorde, Voorwaarde = voorwaarde };
            foreach (var v in voorgangers) t.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = v.Code, Type = v.Type, WachtDagen = v.Wacht });
            return t;
        }
        var voor = new PlanningSjabloonBlok { Id = 1, Code = "B_VOORBEREIDING", Naam = "Voorbereiding", Volgorde = 1 };
        voor.Taken.Add(T(1, 1, "WERF_UITZETTEN", "Werf uitzetten", 3, 1));
        var onder = new PlanningSjabloonBlok { Id = 2, Code = "B_KELDER", Naam = "Ruwbouw - Onderbouw", Volgorde = 2, VoorwaardeVariabele = "kelder", VoorwaardeWaarde = "ja" };
        onder.Taken.Add(T(2, 2, "WANDEN_KELDER", "Wanden kelder", 25, 1, null, ("WERF_UITZETTEN", 0, 0)));
        var boven = new PlanningSjabloonBlok { Id = 3, Code = "B_BOVENBOUW", Naam = "Ruwbouw - Bovenbouw", Volgorde = 3, Herhaling = PlanningSjabloonUitrol.PerBouwlaagEenFase };
        boven.Taken.Add(T(3, 3, "VLOERPLAAT_GLVL", "Vloerplaat", 2, 1, "grond:ja", ("WERF_UITZETTEN", 0, 0)));
        boven.Taken.Add(T(4, 3, "PREDALLEN", "Predallen", 3, 2, "grond:nee", ("VORIGE:METSELWERK_RUWBOUW", 0, 0), ("WANDEN_KELDER", 0, 0)));
        boven.Taken.Add(T(5, 3, "OPSTORT", "Opstort", 2, 3, "grond:nee", ("PREDALLEN", 0, 0)));
        boven.Taken.Add(T(6, 3, "METSELWERK_RUWBOUW", "Metselwerk", 15, 4, null, ("VLOERPLAAT_GLVL", 0, 0), ("OPSTORT", 0, 0)));
        boven.Taken.Add(T(7, 3, "TERRAS_LUIFEL", "Terrassen en luifels", 5, 5, "terras:ja", ("METSELWERK_RUWBOUW", 0, 0)));
        boven.Taken.Add(T(8, 3, "PREDALLEN_DAK", "Predallen dak", 3, 6, "laag:eenmalig,dak:plat", ("VORIGE:METSELWERK_RUWBOUW", 0, 0)));
        boven.Taken.Add(T(9, 3, "OPSTORT_DAK", "Opstort dak", 2, 7, "laag:eenmalig,dak:plat", ("PREDALLEN_DAK", 0, 0)));
        boven.Taken.Add(T(10, 3, "GEVELMETSELWERK", "Gevelmetselwerk", 40, 8, "laag:eenmalig"));
        var dak = new PlanningSjabloonBlok { Id = 4, Code = "B_DAK", Naam = "Dak", Volgorde = 4 };
        dak.Taken.Add(T(11, 4, "DAKBEDEKKING", "Dakbedekking", 15, 1, null, ("METSELWERK_RUWBOUW", 0, 0), ("OPSTORT_DAK", 0, 0)));
        foreach (var b in new[] { voor, onder, boven, dak }) s.Blokken.Add(b);
        return s;
    }

    [Fact]
    public async Task Bovenbouw_toepassen_maakt_een_deelfase_per_bouwlaag_en_het_gevelmetselwerk_rechtstreeks_in_de_fase()
    {
        var (svc, _, db) = Maak();
        db.PlanningSjabloon.Add(Bovenbouw());
        await db.SaveChangesAsync();
        var u = await svc.Voer(ProjectId, new PlanningCommando
        {
            Type = "sjabloonToepassen", Versie = 0,
            Toepassing = new PlanningSjabloonToepassing { SjabloonId = 1, Planningen = new List<PlanningToepassingPlanning> { new() { EenheidIds = { 11 }, Verdiepingen = 3, Kelder = true, Dak = "hellend" } } },
        }, "u");
        Assert.True(u.Ok, u.Fout);

        var secties = await db.PlanningSectie.ToListAsync();
        var boven = secties.Single(s => s.Naam == "Ruwbouw - Bovenbouw");
        var delen = secties.Where(s => s.ParentSectieId == boven.Id).OrderBy(s => s.Volgorde).ToList();
        Assert.Equal(new[] { "Gelijkvloers", "1ste verdieping", "2de verdieping" }, delen.Select(s => s.Naam));
        var taken = await db.PlanningTaak.ToListAsync();
        Assert.Equal(new[] { "Predallen gelijkvloers", "Opstort gelijkvloers", "Metselwerk gelijkvloers" }, taken.Where(t => t.PlanningSectieId == delen[0].Id).OrderBy(t => t.Volgorde).Select(t => t.Naam));
        Assert.Equal(new[] { "Gevelmetselwerk" }, taken.Where(t => t.PlanningSectieId == boven.Id).Select(t => t.Naam));
        // De volgorde in de fase: eerst de deelfases per bouwlaag, daarna het gevelmetselwerk.
        Assert.True(delen.All(s => s.Volgorde < taken.Single(t => t.Naam == "Gevelmetselwerk").Volgorde));
    }

    [Fact]
    public void Bij_meerdere_bouwlagen_staan_de_taken_per_bouwlaag_in_een_deelfase_en_de_eenmalige_taken_rechtstreeks_in_de_fase()
    {
        var uit = PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(2, true, "plat"));
        var taken = uit.Fases.Single(f => f.Naam == "Ruwbouw - Bovenbouw").Taken;
        Assert.Equal(new string?[] { "Gelijkvloers", "Gelijkvloers", "Gelijkvloers", "1ste verdieping", "1ste verdieping", "1ste verdieping", null, null, null }, taken.Select(t => t.Subfase));
        Assert.All(PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(1, false, "hellend")).Fases.SelectMany(f => f.Taken), t => Assert.Null(t.Subfase));   // één bouwlaag: geen tussenniveau
    }

    [Fact]
    public void Bovenbouw_is_een_fase_met_per_bouwlaag_predallen_opstort_en_metselwerk()
    {
        var uit = PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(3, true, "hellend"));
        Assert.Empty(uit.Waarschuwingen);
        Assert.Equal(new[] { "Voorbereiding", "Ruwbouw - Onderbouw", "Ruwbouw - Bovenbouw", "Dak" }, uit.Fases.Select(f => f.Naam));
        Assert.Equal(new[]
        {
            "Predallen gelijkvloers", "Opstort gelijkvloers", "Metselwerk gelijkvloers",
            "Predallen 1ste verdieping", "Opstort 1ste verdieping", "Metselwerk 1ste verdieping",
            "Predallen 2de verdieping", "Opstort 2de verdieping", "Metselwerk 2de verdieping",
            "Gevelmetselwerk",
        }, uit.Fases.Single(f => f.Naam == "Ruwbouw - Bovenbouw").Taken.Select(t => t.Naam));
        Assert.Equal("B_BOVENBOUW", uit.Fases.Single(f => f.Naam == "Ruwbouw - Bovenbouw").Code);
    }

    [Fact]
    public void Predallen_wachten_op_het_metselwerk_van_de_vorige_bouwlaag_en_de_kelder_en_geen_afdek_nodig()
    {
        var uit = PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(3, true, "hellend"));
        string Sleutel(string naam) => uit.Fases.SelectMany(f => f.Taken).Single(t => t.Naam == naam).Sleutel;
        UitgerolTaak T(string naam) => uit.Fases.SelectMany(f => f.Taken).Single(t => t.Naam == naam);
        // Gelijkvloers wacht op de wanden van de kelder, hoger op het metselwerk eronder.
        Assert.Equal(new[] { T("Wanden kelder").Sleutel }, T("Predallen gelijkvloers").Voorgangers.Select(v => v.Sleutel));
        Assert.Contains(Sleutel("Metselwerk gelijkvloers"), T("Predallen 1ste verdieping").Voorgangers.Select(v => v.Sleutel));   // (wacht ook op de kelder: overbodig maar onschadelijk)
        Assert.Equal(new[] { Sleutel("Opstort 2de verdieping") }, T("Metselwerk 2de verdieping").Voorgangers.Select(v => v.Sleutel));
        Assert.Equal(new[] { Sleutel("Predallen 2de verdieping") }, T("Opstort 2de verdieping").Voorgangers.Select(v => v.Sleutel));
        // Het gevelmetselwerk hangt aan niets.
        Assert.Empty(T("Gevelmetselwerk").Voorgangers);
        // Het dak wacht op het metselwerk van de bovenste bouwlaag.
        Assert.Equal(new[] { Sleutel("Metselwerk 2de verdieping") }, T("Dakbedekking").Voorgangers.Select(v => v.Sleutel));
    }

    [Fact]
    public void Zonder_kelder_is_het_gelijkvloers_gewoon_een_vloerplaat()
    {
        var uit = PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(2, false, "hellend"));
        Assert.DoesNotContain(uit.Fases, f => f.Naam == "Ruwbouw - Onderbouw");
        var boven = uit.Fases.Single(f => f.Naam == "Ruwbouw - Bovenbouw").Taken.Select(t => t.Naam).ToList();
        Assert.Equal(new[] { "Vloerplaat gelijkvloers", "Metselwerk gelijkvloers", "Predallen 1ste verdieping", "Opstort 1ste verdieping", "Metselwerk 1ste verdieping", "Gevelmetselwerk" }, boven);
        var vloerplaat = uit.Fases.SelectMany(f => f.Taken).Single(t => t.Naam == "Vloerplaat gelijkvloers");
        Assert.Equal(vloerplaat.Sleutel, Assert.Single(uit.Fases.SelectMany(f => f.Taken).Single(t => t.Naam == "Metselwerk gelijkvloers").Voorgangers).Sleutel);
    }

    [Fact]
    public void Bij_een_plat_dak_komen_predallen_en_opstort_van_het_dak_na_de_bovenste_bouwlaag()
    {
        var plat = PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(2, true, "plat"));
        var taken = plat.Fases.Single(f => f.Naam == "Ruwbouw - Bovenbouw").Taken;
        Assert.Equal(new[] { "Predallen dak", "Opstort dak", "Gevelmetselwerk" }, taken.Skip(6).Select(t => t.Naam));
        var metselBoven = taken.Single(t => t.Naam == "Metselwerk 1ste verdieping");
        Assert.Equal(metselBoven.Sleutel, Assert.Single(taken.Single(t => t.Naam == "Predallen dak").Voorgangers).Sleutel);
        // De dakbedekking wacht op de opstort van het dak.
        Assert.Contains(plat.Fases.Single(f => f.Naam == "Dak").Taken[0].Voorgangers, v => v.Sleutel == taken.Single(t => t.Naam == "Opstort dak").Sleutel);
        Assert.DoesNotContain(PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(2, true, "hellend")).Fases.SelectMany(f => f.Taken), t => t.Naam.EndsWith(" dak") && t.Naam != "Dakbedekking");
    }

    [Fact]
    public void Terrassen_zitten_in_de_fase_bovenbouw_bij_de_gekozen_bouwlagen()
    {
        var uit = PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(3, true, "hellend", new[] { 1, 3 }));
        var terrassen = uit.Fases.Single(f => f.Naam == "Ruwbouw - Bovenbouw").Taken.Where(t => t.Naam.StartsWith("Terrassen")).Select(t => t.Naam);
        Assert.Equal(new[] { "Terrassen en luifels gelijkvloers", "Terrassen en luifels 2de verdieping" }, terrassen);
    }

    [Fact]
    public void Een_bouwlaag_geeft_geen_toevoeging_aan_de_naam_van_de_taken()
    {
        var uit = PlanningSjabloonUitrol.Rol(Bovenbouw(), new SjabloonVariabelen(1, false, "hellend"));
        Assert.Equal(new[] { "Vloerplaat", "Metselwerk", "Gevelmetselwerk" }, uit.Fases.Single(f => f.Naam == "Ruwbouw - Bovenbouw").Taken.Select(t => t.Naam));
    }

    [Fact]
    public void Bouwlagen_heten_gelijkvloers_en_dan_1ste_2de_verdieping()
    {
        Assert.Equal("gelijkvloers", PlanningSjabloonUitrol.BouwlaagNaam(1));
        Assert.Equal("1ste verdieping", PlanningSjabloonUitrol.BouwlaagNaam(2));
        Assert.Equal("2de verdieping", PlanningSjabloonUitrol.BouwlaagNaam(3));
        Assert.Equal("8ste verdieping", PlanningSjabloonUitrol.BouwlaagNaam(9));
    }

    [Fact]
    public void Alles_van_de_ruwbouw_zit_in_een_fase_per_bouwlaag_en_taken_met_een_voorwaarde_komen_enkel_waar_ze_horen()
    {
        // Met kelder, 3 bouwlagen, terrassen bij de afdek van het gelijkvloers en de 2de verdieping.
        var uit = PlanningSjabloonUitrol.Rol(Gebouw(), new SjabloonVariabelen(3, true, null, new[] { 1, 3 }));
        Assert.Empty(uit.Waarschuwingen);
        Assert.Equal(new[] { "Voorbereiding", "Ruwbouw kelder", "Ruwbouw gelijkvloers", "Ruwbouw 1ste verdieping", "Ruwbouw 2de verdieping", "Dak" }, uit.Fases.Select(f => f.Naam));
        // Geen aparte fase voor de vloerplaat gelijkvloers of de terrassen. Met een kelder is de afdek van de kelder de vloerplaat: geen eigen taak.
        Assert.Equal(new[] { "Metselwerk", "Afdek (= vloerplaat volgende verdieping)", "Terrassen en luifels" }, uit.Fases.Single(f => f.Naam == "Ruwbouw gelijkvloers").Taken.Select(t => t.Naam));
        Assert.Equal(new[] { "Metselwerk", "Afdek (= vloerplaat volgende verdieping)" }, uit.Fases.Single(f => f.Naam == "Ruwbouw 1ste verdieping").Taken.Select(t => t.Naam));
        // Gevelmetselwerk: één taak, enkel in de bovenste bouwlaag, zonder voorgangers.
        var gevel = uit.Fases.SelectMany(f => f.Taken).Where(t => t.Naam == "Gevelmetselwerk").ToList();
        Assert.Single(gevel);
        Assert.Empty(gevel[0].Voorgangers);
        Assert.Contains(gevel[0], uit.Fases.Single(f => f.Naam == "Ruwbouw 2de verdieping").Taken);
    }

    [Fact]
    public void Met_een_kelder_is_de_afdek_van_de_kelder_de_vloerplaat_van_het_gelijkvloers_en_zonder_kelder_komt_er_een_eigen_vloerplaat()
    {
        var met = PlanningSjabloonUitrol.Rol(Gebouw(), new SjabloonVariabelen(2, true, null));
        var metsel = Taak(met, "Ruwbouw gelijkvloers", "Metselwerk");
        Assert.Equal("2", Assert.Single(metsel.Voorgangers).Sleutel);                   // de afdek van de kelder
        Assert.DoesNotContain(met.Fases.SelectMany(f => f.Taken), t => t.Naam.StartsWith("Gieten vloerplaat"));

        var zonder = PlanningSjabloonUitrol.Rol(Gebouw(), new SjabloonVariabelen(2, false, null));
        Assert.DoesNotContain(zonder.Fases, f => f.Naam == "Ruwbouw kelder");
        var vloerplaat = Taak(zonder, "Ruwbouw gelijkvloers", "Gieten vloerplaat gelijkvloers");
        Assert.Equal("1", Assert.Single(vloerplaat.Voorgangers).Sleutel);                // de werf
        Assert.Equal(vloerplaat.Sleutel, Assert.Single(Taak(zonder, "Ruwbouw gelijkvloers", "Metselwerk").Voorgangers).Sleutel);
        Assert.Empty(zonder.Waarschuwingen);
    }

    [Fact]
    public void De_afdek_van_een_bouwlaag_is_de_vloerplaat_van_de_volgende_en_een_terras_houdt_de_volgende_bouwlaag_niet_op()
    {
        var uit = PlanningSjabloonUitrol.Rol(Gebouw(), new SjabloonVariabelen(3, true, null, new[] { 1 }));
        var afdekGl = Taak(uit, "Ruwbouw gelijkvloers", "Afdek");
        var terrasGl = Taak(uit, "Ruwbouw gelijkvloers", "Terrassen");
        var metsel1 = Taak(uit, "Ruwbouw 1ste verdieping", "Metselwerk");
        // Het metselwerk van de 1ste verdieping wacht op de afdek van het gelijkvloers (de vloerplaat) en niet op het terras.
        Assert.Contains(metsel1.Voorgangers, v => v.Sleutel == afdekGl.Sleutel);
        Assert.DoesNotContain(metsel1.Voorgangers, v => v.Sleutel == terrasGl.Sleutel);
        // Het terras hangt aan de afdek van zijn eigen bouwlaag.
        Assert.Equal(afdekGl.Sleutel, Assert.Single(terrasGl.Voorgangers).Sleutel);
        // Het dak wacht op de afdek van de bovenste bouwlaag.
        var afdekBoven = Taak(uit, "Ruwbouw 2de verdieping", "Afdek");
        Assert.Equal(afdekBoven.Sleutel, Assert.Single(uit.Fases.Single(f => f.Naam == "Dak").Taken[0].Voorgangers).Sleutel);
    }

    [Fact]
    public void Terrassen_komen_enkel_bij_de_gekozen_bouwlagen_en_zonder_keuze_valt_de_taak_weg()
    {
        var geen = PlanningSjabloonUitrol.Rol(Gebouw(), new SjabloonVariabelen(3, false, null));
        Assert.DoesNotContain(geen.Fases.SelectMany(f => f.Taken), t => t.Naam.StartsWith("Terrassen"));

        var enkel = PlanningSjabloonUitrol.Rol(Gebouw(), new SjabloonVariabelen(3, false, null, new[] { 3, 9 }));
        var terrassen = enkel.Fases.Where(f => f.Taken.Any(t => t.Naam.StartsWith("Terrassen"))).Select(f => f.Naam).ToList();
        Assert.Equal(new[] { "Ruwbouw 2de verdieping" }, terrassen);   // bouwlaag 9 bestaat niet en doet niet mee
    }

    [Fact]
    public void Bij_een_gebouw_met_een_bouwlaag_is_die_laag_zowel_eerste_als_laatste()
    {
        var uit = PlanningSjabloonUitrol.Rol(Gebouw(), new SjabloonVariabelen(1, false, null));
        var ruw = uit.Fases.Single(f => f.Naam == "Ruwbouw");   // geen toevoeging bij één bouwlaag
        Assert.Contains(ruw.Taken, t => t.Naam.StartsWith("Gieten vloerplaat gelijkvloers"));
        Assert.Contains(ruw.Taken, t => t.Naam == "Gevelmetselwerk");
    }

    [Fact]
    public async Task Een_voorwaarde_per_taak_wordt_gecontroleerd_bij_het_bewaren_en_blijft_bewaard()
    {
        var (_, sjab, _) = Maak();
        PlanningSjabloonBewerk Doc(string voorwaarde) => new()
        {
            Naam = "Met voorwaarden",
            Blokken =
            {
                new PlanningSjabloonBewerkBlok { Naam = "Ruwbouw", Code = "RUW", Herhaling = 1, Taken =
                {
                    new PlanningSjabloonBewerkTaak { Naam = "Vloerplaat", Code = "VLOER", Duur = 2, Voorwaarde = voorwaarde },
                    new PlanningSjabloonBewerkTaak { Naam = "Metselwerk", Code = "METSEL", Duur = 5, Voorgangers = { new PlanningSjabloonBewerkVoorganger { Code = "VORIGE:METSEL" } } },
                } },
            },
        };
        var goed = await sjab.Bewaar(Doc("laag:eerste,kelder:nee"), "u");
        Assert.True(goed.Ok, string.Join(" ", goed.Fouten));
        var terug = (await sjab.Haal(goed.SjabloonId))!;
        Assert.Equal("laag:eerste,kelder:nee", terug.Blokken[0].Taken[0].Voorwaarde);
        Assert.Equal("VORIGE:METSEL", terug.Blokken[0].Taken[1].Voorgangers[0].Code);

        var fout = await sjab.Bewaar(Doc("laag:ergens"), "u");
        Assert.False(fout.Ok);
        Assert.Contains(fout.Fouten, f => f.Contains("laag:ergens"));
    }
}
