using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using DALCore.Models;
using Xunit;

/// <summary>Het startsjabloon Appartementsgebouw uit _migrations/158f (blokken, taken, voorwaarden, voorgangers): bewaart zonder fouten en rolt zonder waarschuwingen uit voor elke combinatie van kelder en dak.</summary>
public class AppartementsgebouwSjabloonTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private static PlanningSjabloonBewerk Bouw()
    {
        var dto = new PlanningSjabloonBewerk { Naam = "Appartementsgebouw" };
        var B_VOORBEREIDING = new PlanningSjabloonBewerkBlok { Code = "B_VOORBEREIDING", Naam = "Voorbereiding", Herhaling = 0 }; dto.Blokken.Add(B_VOORBEREIDING);
        var B_ONDERBOUW = new PlanningSjabloonBewerkBlok { Code = "B_ONDERBOUW", Naam = "Onderbouw", Herhaling = 0 }; dto.Blokken.Add(B_ONDERBOUW);
        var B_BOVENBOUW = new PlanningSjabloonBewerkBlok { Code = "B_BOVENBOUW", Naam = "Bovenbouw", Herhaling = 2 }; dto.Blokken.Add(B_BOVENBOUW);
        var B_DAK = new PlanningSjabloonBewerkBlok { Code = "B_DAK", Naam = "Dak", Herhaling = 0 }; dto.Blokken.Add(B_DAK);
        var B_GEVEL = new PlanningSjabloonBewerkBlok { Code = "B_GEVEL", Naam = "Gevel en buitenschrijnwerk", Herhaling = 0 }; dto.Blokken.Add(B_GEVEL);
        var B_TECHNIEKEN = new PlanningSjabloonBewerkBlok { Code = "B_TECHNIEKEN", Naam = "Technieken", Herhaling = 0 }; dto.Blokken.Add(B_TECHNIEKEN);
        var B_AFWERKING = new PlanningSjabloonBewerkBlok { Code = "B_AFWERKING", Naam = "Afwerking", Herhaling = 0 }; dto.Blokken.Add(B_AFWERKING);
        var B_OMGEVING = new PlanningSjabloonBewerkBlok { Code = "B_OMGEVING", Naam = "Omgevingswerken", Herhaling = 0 }; dto.Blokken.Add(B_OMGEVING);
        var B_OPLEVERING = new PlanningSjabloonBewerkBlok { Code = "B_OPLEVERING", Naam = "Oplevering", Herhaling = 0 }; dto.Blokken.Add(B_OPLEVERING);
        B_VOORBEREIDING.Taken.Add(new() { Code = "AFBRAAK", Naam = "Afbraak bestaande gebouwen", Duur = 10, Voorwaarde = null });
        B_VOORBEREIDING.Taken.Add(new() { Code = "LANDMETER_UITZETTEN", Naam = "Uitzetten door landmeter", Duur = 2, Voorwaarde = null, Voorgangers = { new() { Code = "AFBRAAK" } } });
        B_VOORBEREIDING.Taken.Add(new() { Code = "WERF_INRICHTEN", Naam = "Werf inrichten", Duur = 5, Voorwaarde = null, Voorgangers = { new() { Code = "AFBRAAK" } } });
        B_ONDERBOUW.Taken.Add(new() { Code = "UITGRAVEN_KELDER", Naam = "Uitgraven kelder", Duur = 15, Voorwaarde = "kelder:ja", Voorgangers = { new() { Code = "B_VOORBEREIDING" } } });
        B_ONDERBOUW.Taken.Add(new() { Code = "FUNDERINGEN", Naam = "Funderingen", Duur = 15, Voorwaarde = null, Voorgangers = { new() { Code = "B_VOORBEREIDING" }, new() { Code = "UITGRAVEN_KELDER" } } });
        B_ONDERBOUW.Taken.Add(new() { Code = "RIOLERING", Naam = "Rioleringen", Duur = 10, Voorwaarde = null, Voorgangers = { new() { Code = "FUNDERINGEN" } } });
        B_ONDERBOUW.Taken.Add(new() { Code = "VLOERPLAAT_KELDER", Naam = "Gieten vloerplaat kelder", Duur = 5, Voorwaarde = "kelder:ja", Voorgangers = { new() { Code = "FUNDERINGEN" } } });
        B_ONDERBOUW.Taken.Add(new() { Code = "WANDEN_KELDER", Naam = "Kelderwanden", Duur = 20, Voorwaarde = "kelder:ja", Voorgangers = { new() { Code = "VLOERPLAAT_KELDER" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "VLOERPLAAT_GLVL", Naam = "Vloerplaat", Duur = 5, Voorwaarde = "grond:ja", Voorgangers = { new() { Code = "B_ONDERBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "PREDALLEN", Naam = "Predallen", Duur = 4, Voorwaarde = "grond:nee", Voorgangers = { new() { Code = "B_ONDERBOUW" }, new() { Code = "VORIGE:METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "OPSTORT", Naam = "Opstort", Duur = 3, Voorwaarde = "grond:nee", Voorgangers = { new() { Code = "PREDALLEN" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "METSELWERK_RUWBOUW", Naam = "Metselwerk", Duur = 20, Voorwaarde = null, Voorgangers = { new() { Code = "VLOERPLAAT_GLVL" }, new() { Code = "OPSTORT" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "GIPSBLOKKEN", Naam = "Gipsblokken", Duur = 10, Voorwaarde = null, Voorgangers = { new() { Code = "METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "TERRAS_LUIFEL", Naam = "Terrassen en luifels", Duur = 5, Voorwaarde = "terras:ja", Voorgangers = { new() { Code = "METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "DORPELS", Naam = "Dorpels", Duur = 8, Voorwaarde = "laag:eenmalig", Voorgangers = { new() { Code = "METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "GEVELMETSELWERK", Naam = "Gevelmetselwerk", Duur = 40, Voorwaarde = "laag:eenmalig,gevel:steen", Voorgangers = { new() { Code = "EERSTE:METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "PREDALLEN_DAK", Naam = "Predallen dak", Duur = 4, Voorwaarde = "laag:eenmalig,dak:plat", Voorgangers = { new() { Code = "METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "OPSTORT_DAK", Naam = "Opstort dak", Duur = 3, Voorwaarde = "laag:eenmalig,dak:plat", Voorgangers = { new() { Code = "PREDALLEN_DAK" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "HELLINGSSCHAPE", Naam = "Hellingsschape plat dak", Duur = 5, Voorwaarde = "laag:eenmalig,dak:plat", Voorgangers = { new() { Code = "OPSTORT_DAK" } } });
        B_DAK.Taken.Add(new() { Code = "DAKOPSTANDEN", Naam = "Dak- en liftopstanden", Duur = 5, Voorwaarde = "dak:plat", Voorgangers = { new() { Code = "HELLINGSSCHAPE" } } });
        B_DAK.Taken.Add(new() { Code = "DAKBEDEKKING_PLAT", Naam = "Platte dakbedekking", Duur = 15, Voorwaarde = "dak:plat", Voorgangers = { new() { Code = "DAKOPSTANDEN" } } });
        B_DAK.Taken.Add(new() { Code = "DAKSTRUCTUUR", Naam = "Dakstructuur", Duur = 15, Voorwaarde = "dak:hellend", Voorgangers = { new() { Code = "METSELWERK_RUWBOUW" } } });
        B_DAK.Taken.Add(new() { Code = "DAKBEDEKKING_HELLEND", Naam = "Dakbedekking hellend dak", Duur = 10, Voorwaarde = "dak:hellend", Voorgangers = { new() { Code = "DAKSTRUCTUUR" } } });
        B_GEVEL.Taken.Add(new() { Code = "BUITENSCHRIJNWERK", Naam = "Plaatsen buitenschrijnwerk en glas", Duur = 20, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" } } });
        B_GEVEL.Taken.Add(new() { Code = "GEVELBEPLEISTERING", Naam = "Gevelbepleistering", Duur = 20, Voorwaarde = "gevel:pleister", Voorgangers = { new() { Code = "B_BOVENBOUW" } } });
        B_GEVEL.Taken.Add(new() { Code = "GEVELBEKLEDING_HOUT", Naam = "Houten gevelbekleding", Duur = 15, Voorwaarde = "gevel:hout", Voorgangers = { new() { Code = "B_BOVENBOUW" }, new() { Code = "GEVELBEPLEISTERING" } } });
        B_GEVEL.Taken.Add(new() { Code = "GEVELBEKLEDING_ALU", Naam = "Aluminium gevelbekleding", Duur = 15, Voorwaarde = "gevel:aluminium", Voorgangers = { new() { Code = "B_BOVENBOUW" }, new() { Code = "GEVELBEPLEISTERING" } } });
        B_TECHNIEKEN.Taken.Add(new() { Code = "LEIDINGEN_ELEKTRA", Naam = "Plaatsen leidingen elektriciteit", Duur = 25, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" }, new() { Code = "BUITENSCHRIJNWERK" } } });
        B_TECHNIEKEN.Taken.Add(new() { Code = "LEIDINGEN_SANITAIR", Naam = "Plaatsen leidingen sanitair en verwarming", Duur = 25, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" }, new() { Code = "BUITENSCHRIJNWERK" } } });
        B_TECHNIEKEN.Taken.Add(new() { Code = "LIFT", Naam = "Plaatsen lift", Duur = 15, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" } } });
        B_AFWERKING.Taken.Add(new() { Code = "PLEISTER", Naam = "Pleisterwerken", Duur = 25, DuurPerEenheid = 2m, Voorwaarde = null, Voorgangers = { new() { Code = "LEIDINGEN_ELEKTRA" }, new() { Code = "LEIDINGEN_SANITAIR" }, new() { Code = "BUITENSCHRIJNWERK" } } });
        B_AFWERKING.Taken.Add(new() { Code = "ISOLATIE", Naam = "Vloerisolatie", Duur = 5, DuurPerEenheid = 0.5m, Voorwaarde = null, Voorgangers = { new() { Code = "PLEISTER" } } });
        B_AFWERKING.Taken.Add(new() { Code = "CHAPE", Naam = "Plaatsen chape", Duur = 8, DuurPerEenheid = 1m, Voorwaarde = null, Voorgangers = { new() { Code = "ISOLATIE" } } });
        B_AFWERKING.Taken.Add(new() { Code = "TEGELWERK", Naam = "Vloeren en wandtegels", Duur = 20, DuurPerEenheid = 2m, Voorwaarde = null, Voorgangers = { new() { Code = "CHAPE" } } });
        B_AFWERKING.Taken.Add(new() { Code = "PARKET", Naam = "Parket", Duur = 10, DuurPerEenheid = 1m, Voorwaarde = null, Voorgangers = { new() { Code = "CHAPE" } } });
        B_AFWERKING.Taken.Add(new() { Code = "BINNENSCHRIJNWERK", Naam = "Binnenschrijnwerk", Duur = 15, DuurPerEenheid = 1m, Voorwaarde = null, Voorgangers = { new() { Code = "PARKET" } } });
        B_AFWERKING.Taken.Add(new() { Code = "KEUKEN", Naam = "Plaatsen keuken", Duur = 10, DuurPerEenheid = 1m, Voorwaarde = null, Voorgangers = { new() { Code = "PARKET" }, new() { Code = "TEGELWERK" } } });
        B_AFWERKING.Taken.Add(new() { Code = "SCHILDERWERK", Naam = "Schilderwerken", Duur = 20, DuurPerEenheid = 2m, Voorwaarde = null, Voorgangers = { new() { Code = "BINNENSCHRIJNWERK" }, new() { Code = "KEUKEN" }, new() { Code = "LIFT" } } });
        B_AFWERKING.Taken.Add(new() { Code = "AFWERKING_ELEKTRA", Naam = "Afwerking elektriciteit (schakelaars, armaturen)", Duur = 1, DuurPerEenheid = 1m, Voorwaarde = null, Voorgangers = { new() { Code = "TEGELWERK" }, new() { Code = "PARKET" } } });
        B_AFWERKING.Taken.Add(new() { Code = "SANITAIRE_TOESTELLEN", Naam = "Plaatsen sanitaire toestellen", Duur = 1, DuurPerEenheid = 1m, Voorwaarde = null, Voorgangers = { new() { Code = "TEGELWERK" }, new() { Code = "PARKET" } } });
        B_AFWERKING.Taken.Add(new() { Code = "VERWARMINGSTOESTELLEN", Naam = "Plaatsen verwarmingstoestellen", Duur = 1, DuurPerEenheid = 2m, Voorwaarde = null, Voorgangers = { new() { Code = "TEGELWERK" }, new() { Code = "PARKET" } } });
        B_OMGEVING.Taken.Add(new() { Code = "OMGEVINGSWERKEN", Naam = "Omgevingswerken", Duur = 30, Voorwaarde = null, Voorgangers = { new() { Code = "B_AFWERKING" } } });
        B_OPLEVERING.Taken.Add(new() { Code = "OPKUIS", Naam = "Opkuis", Duur = 5, Voorwaarde = null, Voorgangers = { new() { Code = "B_OMGEVING" }, new() { Code = "GEVELMETSELWERK" }, new() { Code = "GEVELBEPLEISTERING" }, new() { Code = "GEVELBEKLEDING_HOUT" }, new() { Code = "GEVELBEKLEDING_ALU" } } });
        B_OPLEVERING.Taken.Add(new() { Code = "VOORLOPIGE_OPLEVERING", Naam = "Voorlopige oplevering (sleutels)", Duur = 1, Voorwaarde = null, Voorgangers = { new() { Code = "OPKUIS" } } });
        return dto;
    }

    [Theory]
    [InlineData(1, false, null)]
    [InlineData(4, false, "plat")]
    [InlineData(4, true, "plat")]
    [InlineData(5, true, "hellend")]
    [InlineData(5, false, "hellend")]
    public async Task Het_sjabloon_bewaart_en_rolt_zonder_waarschuwingen_uit(int bouwlagen, bool kelder, string? dak)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        var sjab = new PlanningSjabloonService(db);
        var uit = await sjab.Bewaar(Bouw(), "u");
        Assert.True(uit.Ok, string.Join("; ", uit.Fouten));
        var s = await db.PlanningSjabloon.Include(x => x.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).SingleAsync();
        var rol = PlanningSjabloonUitrol.Rol(s, new SjabloonVariabelen(bouwlagen, kelder, dak, null, new[] { "steen" }));
        Assert.Empty(rol.Waarschuwingen);
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        Assert.Equal(taken.Count, taken.Select(t => t.Sleutel).Distinct().Count());
        // het gevelmetselwerk wacht op het metselwerk van de EERSTE bouwlaag
        var gevel = taken.Single(t => t.Naam == "Gevelmetselwerk");
        var eerste = taken.First(t => t.Code == "METSELWERK_RUWBOUW");
        Assert.Contains(gevel.Voorgangers, v => v.Sleutel == eerste.Sleutel);
        Assert.Equal(kelder, taken.Any(t => t.Code == "WANDEN_KELDER"));
        Assert.Equal(dak == "plat", taken.Any(t => t.Code == "DAKBEDEKKING_PLAT"));
        Assert.Equal(dak == "hellend", taken.Any(t => t.Code == "DAKSTRUCTUUR"));
    }

    [Fact]
    public void Het_voorbeeld_toont_het_dak_en_de_bouwlagen_en_telt_het_gevelmetselwerk_niet_vanaf_dag_1()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        var sjab = new PlanningSjabloonService(db);
        var proef = sjab.Proef(new PlanningSjabloonProefVerzoek { Sjabloon = Bouw(), Woningen = 1, Verdiepingen = 3 });
        Assert.Empty(proef.Meldingen);
        Assert.Contains(proef.Blokken, b => b.Code == "B_DAK");                         // het daktype staat op de taken, niet op het blok
        var boven = proef.Blokken.Single(b => b.Code == "B_BOVENBOUW");
        Assert.True(boven.Start > 1, "Bovenbouw start na de onderbouw, niet op dag 1");
        Assert.Equal(new[] { "Gelijkvloers", "1ste verdieping", "2de verdieping", "Eenmalig" }, boven.Delen.Select(d => d.Naam));
        // het gevelmetselwerk (40 d) start na het metselwerk van de eerste bouwlaag en niet vanaf dag 1
        Assert.True(boven.Delen.Single(d => d.Naam == "Eenmalig").Start > boven.Delen[0].Start);
    }

    private static async Task<SjabloonUitrolling> Rol(params string[] gevels)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        var uit = await new PlanningSjabloonService(db).Bewaar(Bouw(), "u");
        Assert.True(uit.Ok, string.Join("; ", uit.Fouten));
        var s = await db.PlanningSjabloon.Include(x => x.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).SingleAsync();
        return PlanningSjabloonUitrol.Rol(s, new SjabloonVariabelen(3, true, "plat", null, gevels));
    }

    private static List<string> Bovenbouw(SjabloonUitrolling rol) => rol.Fases.Single(f => f.Naam == "Bovenbouw").Taken.Where(t => t.Code is not ("GEVELBEPLEISTERING" or "GEVELBEKLEDING_HOUT" or "GEVELBEKLEDING_ALU")).Select(t => t.Sleutel).ToList();

    [Fact]
    public async Task Tegelwerk_en_parket_lopen_parallel_na_de_chape_en_de_keuken_wacht_op_beide()
    {
        var rol = await Rol();
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        var chape = taken.Single(t => t.Code == "CHAPE"); var tegel = taken.Single(t => t.Code == "TEGELWERK"); var parket = taken.Single(t => t.Code == "PARKET");
        Assert.Equal(chape.Sleutel, Assert.Single(tegel.Voorgangers).Sleutel);
        Assert.Equal(chape.Sleutel, Assert.Single(parket.Voorgangers).Sleutel);          // niet meer na het tegelwerk
        var keuken = taken.Single(t => t.Code == "KEUKEN");
        Assert.Contains(keuken.Voorgangers, v => v.Sleutel == tegel.Sleutel);
        Assert.Contains(keuken.Voorgangers, v => v.Sleutel == parket.Sleutel);
    }

    [Fact]
    public async Task Elektriciteit_sanitaire_en_verwarmingstoestellen_lopen_tegelijk_na_de_vloerafwerking_en_de_omgeving_wacht_op_alle_drie()
    {
        var rol = await Rol();
        Assert.Empty(rol.Waarschuwingen);
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        var drie = new[] { "AFWERKING_ELEKTRA", "SANITAIRE_TOESTELLEN", "VERWARMINGSTOESTELLEN" }.Select(c => taken.Single(t => t.Code == c)).ToList();
        var tegel = taken.Single(t => t.Code == "TEGELWERK"); var parket = taken.Single(t => t.Code == "PARKET");
        foreach (var t in drie)
        {
            Assert.Contains(t.Voorgangers, v => v.Sleutel == tegel.Sleutel);
            Assert.Contains(t.Voorgangers, v => v.Sleutel == parket.Sleutel);
            Assert.DoesNotContain(t.Voorgangers, v => drie.Any(o => o.Sleutel == v.Sleutel));   // onderling geen volgorde
        }
        var omgeving = taken.Single(t => t.Code == "OMGEVINGSWERKEN");
        foreach (var t in drie) Assert.Contains(omgeving.Voorgangers, v => v.Sleutel == t.Sleutel);
    }

    [Theory]
    [InlineData(1, 1, 1, 2)]
    [InlineData(12, 12, 12, 24)]
    [InlineData(7, 7, 7, 14)]
    public async Task De_techniekafwerking_duurt_dagen_per_eenheid_maal_het_aantal_eenheden(int eenheden, int elektra, int sanitair, int verwarming)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        var uit = await new PlanningSjabloonService(db).Bewaar(Bouw(), "u");
        Assert.True(uit.Ok, string.Join("; ", uit.Fouten));
        var s = await db.PlanningSjabloon.Include(x => x.Blokken).ThenInclude(b => b.Taken).ThenInclude(t => t.Voorgangers).SingleAsync();
        var rol = PlanningSjabloonUitrol.Rol(s, new SjabloonVariabelen(3, true, "plat", null, null, eenheden));
        int Duur(string code) => rol.Fases.SelectMany(f => f.Taken).First(t => t.Code == code).Duur;
        Assert.Equal(new[] { elektra, sanitair, verwarming }, new[] { Duur("AFWERKING_ELEKTRA"), Duur("SANITAIRE_TOESTELLEN"), Duur("VERWARMINGSTOESTELLEN") });
        Assert.Equal(20, Duur("METSELWERK_RUWBOUW"));   // taken zonder "per eenheid" houden hun vaste duur
        // de hele afwerking is per eenheid: pleister 2, isolatie 0,5 (naar boven afgerond), chape 1, tegelwerk 2, parket 1, binnenschrijnwerk 1, keuken 1, schilderwerk 2
        Assert.Equal(new[] { 2 * eenheden, (int)Math.Ceiling(0.5 * eenheden), eenheden, 2 * eenheden, eenheden, eenheden, eenheden, 2 * eenheden },
            new[] { "PLEISTER", "ISOLATIE", "CHAPE", "TEGELWERK", "PARKET", "BINNENSCHRIJNWERK", "KEUKEN", "SCHILDERWERK" }.Select(Duur));
    }

    [Fact]
    public async Task Zonder_keuze_is_er_geen_enkele_gevelafwerking_en_de_opkuis_verliest_geen_voorganger()
    {
        var rol = await Rol();
        Assert.Empty(rol.Waarschuwingen);
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        Assert.DoesNotContain(taken, t => t.Code is "GEVELMETSELWERK" or "GEVELBEPLEISTERING" or "GEVELBEKLEDING_HOUT" or "GEVELBEKLEDING_ALU");
        Assert.Contains(taken, t => t.Code == "OPKUIS");
    }

    [Fact]
    public async Task Het_gevelmetselwerk_is_optioneel_en_loopt_mee_met_de_bovenbouw()
    {
        var rol = await Rol("steen");
        Assert.Empty(rol.Waarschuwingen);
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        var steen = taken.Single(t => t.Code == "GEVELMETSELWERK");
        Assert.Contains(rol.Fases.Single(f => f.Naam == "Bovenbouw").Taken, t => t == steen);
        Assert.DoesNotContain(taken, t => t.Code is "GEVELBEPLEISTERING" or "GEVELBEKLEDING_HOUT" or "GEVELBEKLEDING_ALU");
        Assert.Contains(taken.Single(t => t.Code == "OPKUIS").Voorgangers, v => v.Sleutel == steen.Sleutel);
    }

    [Fact]
    public async Task Pleister_hout_en_aluminium_starten_pas_als_de_hele_bovenbouw_af_is()
    {
        var rol = await Rol("steen", "hout", "pleister", "aluminium");
        Assert.Empty(rol.Waarschuwingen);
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        var boven = Bovenbouw(rol);
        Assert.Contains(taken.Single(t => t.Code == "GEVELMETSELWERK").Sleutel, boven);   // ook het gevelmetselwerk hoort bij de bovenbouw
        var pleister = taken.Single(t => t.Code == "GEVELBEPLEISTERING");
        var hout = taken.Single(t => t.Code == "GEVELBEKLEDING_HOUT");
        var alu = taken.Single(t => t.Code == "GEVELBEKLEDING_ALU");
        foreach (var g in new[] { pleister, hout, alu })
            foreach (var sleutel in boven) Assert.Contains(g.Voorgangers, v => v.Sleutel == sleutel);
        // hout en aluminium komen na de pleister
        foreach (var bekleding in new[] { hout, alu }) Assert.Contains(bekleding.Voorgangers, v => v.Sleutel == pleister.Sleutel);
        // de opkuis wacht op alle gekozen gevelafwerkingen
        var opkuis = taken.Single(t => t.Code == "OPKUIS");
        foreach (var g in new[] { pleister, hout, alu }) Assert.Contains(opkuis.Voorgangers, v => v.Sleutel == g.Sleutel);
    }

    [Fact]
    public async Task Hout_zonder_pleister_en_zonder_stenen_gevel_wacht_enkel_op_de_bovenbouw()
    {
        var rol = await Rol("hout");
        Assert.Empty(rol.Waarschuwingen);
        var taken = rol.Fases.SelectMany(f => f.Taken).ToList();
        Assert.DoesNotContain(taken, t => t.Code is "GEVELBEPLEISTERING" or "GEVELMETSELWERK");
        var hout = taken.Single(t => t.Code == "GEVELBEKLEDING_HOUT");
        Assert.Equal(Bovenbouw(rol).OrderBy(x => x), hout.Voorgangers.Select(v => v.Sleutel).OrderBy(x => x));
    }

    [Fact]
    public void Onbekende_en_dubbele_keuzes_worden_opgeschoond_in_de_vaste_volgorde()
    {
        Assert.Equal(new[] { "steen", "pleister", "hout", "aluminium" }, GevelAfwerkingen.Schoon(new[] { "Aluminium", "baksteen", "hout", "steen", "pleister", "hout" }));
    }
}
