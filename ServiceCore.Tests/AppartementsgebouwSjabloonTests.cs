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
        B_BOVENBOUW.Taken.Add(new() { Code = "GEVELMETSELWERK", Naam = "Gevelmetselwerk", Duur = 40, Voorwaarde = "laag:eenmalig", Voorgangers = { new() { Code = "EERSTE:METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "PREDALLEN_DAK", Naam = "Predallen dak", Duur = 4, Voorwaarde = "laag:eenmalig,dak:plat", Voorgangers = { new() { Code = "METSELWERK_RUWBOUW" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "OPSTORT_DAK", Naam = "Opstort dak", Duur = 3, Voorwaarde = "laag:eenmalig,dak:plat", Voorgangers = { new() { Code = "PREDALLEN_DAK" } } });
        B_BOVENBOUW.Taken.Add(new() { Code = "HELLINGSSCHAPE", Naam = "Hellingsschape plat dak", Duur = 5, Voorwaarde = "laag:eenmalig,dak:plat", Voorgangers = { new() { Code = "OPSTORT_DAK" } } });
        B_DAK.Taken.Add(new() { Code = "DAKOPSTANDEN", Naam = "Dak- en liftopstanden", Duur = 5, Voorwaarde = "dak:plat", Voorgangers = { new() { Code = "HELLINGSSCHAPE" } } });
        B_DAK.Taken.Add(new() { Code = "DAKBEDEKKING_PLAT", Naam = "Platte dakbedekking", Duur = 15, Voorwaarde = "dak:plat", Voorgangers = { new() { Code = "DAKOPSTANDEN" } } });
        B_DAK.Taken.Add(new() { Code = "DAKSTRUCTUUR", Naam = "Dakstructuur", Duur = 15, Voorwaarde = "dak:hellend", Voorgangers = { new() { Code = "METSELWERK_RUWBOUW" } } });
        B_DAK.Taken.Add(new() { Code = "DAKBEDEKKING_HELLEND", Naam = "Dakbedekking hellend dak", Duur = 10, Voorwaarde = "dak:hellend", Voorgangers = { new() { Code = "DAKSTRUCTUUR" } } });
        B_GEVEL.Taken.Add(new() { Code = "BUITENSCHRIJNWERK", Naam = "Plaatsen buitenschrijnwerk en glas", Duur = 20, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" } } });
        B_TECHNIEKEN.Taken.Add(new() { Code = "LEIDINGEN_ELEKTRA", Naam = "Plaatsen leidingen elektriciteit", Duur = 25, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" }, new() { Code = "BUITENSCHRIJNWERK" } } });
        B_TECHNIEKEN.Taken.Add(new() { Code = "LEIDINGEN_SANITAIR", Naam = "Plaatsen leidingen sanitair en verwarming", Duur = 25, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" }, new() { Code = "BUITENSCHRIJNWERK" } } });
        B_TECHNIEKEN.Taken.Add(new() { Code = "LIFT", Naam = "Plaatsen lift", Duur = 15, Voorwaarde = null, Voorgangers = { new() { Code = "B_DAK" } } });
        B_AFWERKING.Taken.Add(new() { Code = "PLEISTER", Naam = "Pleisterwerken", Duur = 25, Voorwaarde = null, Voorgangers = { new() { Code = "LEIDINGEN_ELEKTRA" }, new() { Code = "LEIDINGEN_SANITAIR" }, new() { Code = "BUITENSCHRIJNWERK" } } });
        B_AFWERKING.Taken.Add(new() { Code = "ISOLATIE", Naam = "Vloerisolatie", Duur = 5, Voorwaarde = null, Voorgangers = { new() { Code = "PLEISTER" } } });
        B_AFWERKING.Taken.Add(new() { Code = "CHAPE", Naam = "Plaatsen chape", Duur = 8, Voorwaarde = null, Voorgangers = { new() { Code = "ISOLATIE" } } });
        B_AFWERKING.Taken.Add(new() { Code = "TEGELWERK", Naam = "Vloeren en wandtegels", Duur = 20, Voorwaarde = null, Voorgangers = { new() { Code = "CHAPE" } } });
        B_AFWERKING.Taken.Add(new() { Code = "PARKET", Naam = "Parket", Duur = 10, Voorwaarde = null, Voorgangers = { new() { Code = "TEGELWERK" } } });
        B_AFWERKING.Taken.Add(new() { Code = "BINNENSCHRIJNWERK", Naam = "Binnenschrijnwerk", Duur = 15, Voorwaarde = null, Voorgangers = { new() { Code = "PARKET" } } });
        B_AFWERKING.Taken.Add(new() { Code = "KEUKEN", Naam = "Plaatsen keuken", Duur = 10, Voorwaarde = null, Voorgangers = { new() { Code = "PARKET" } } });
        B_AFWERKING.Taken.Add(new() { Code = "SCHILDERWERK", Naam = "Schilderwerken", Duur = 20, Voorwaarde = null, Voorgangers = { new() { Code = "BINNENSCHRIJNWERK" }, new() { Code = "KEUKEN" }, new() { Code = "LIFT" } } });
        B_OMGEVING.Taken.Add(new() { Code = "OMGEVINGSWERKEN", Naam = "Omgevingswerken", Duur = 30, Voorwaarde = null, Voorgangers = { new() { Code = "B_AFWERKING" } } });
        B_OPLEVERING.Taken.Add(new() { Code = "OPKUIS", Naam = "Opkuis", Duur = 5, Voorwaarde = null, Voorgangers = { new() { Code = "B_OMGEVING" } } });
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
        var rol = PlanningSjabloonUitrol.Rol(s, new SjabloonVariabelen(bouwlagen, kelder, dak));
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
}
