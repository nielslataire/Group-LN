using System.Text.Json.Nodes;
using FacadeCore;
using ServiceCore.Planning;
using Xunit;

/// <summary>Het AI-voorstel bij een projectsjabloon: wat de AI te zien krijgt, hoe zijn antwoord gecontroleerd wordt en wat er nooit naar hem toe gaat.</summary>
public class ProjectSjabloonAiTests
{
    private static ProjectSjabloonAiContext Context() => new()
    {
        AantalEenheden = 12, MaxWerkdagen = 420, HeeftPlanning = false, HeeftTraject = true, HeeftSchijven = false,
        Keuzes =
        {
            new ProjectSjabloonAiKeuze { Id = 7, Naam = "Appartementsgebouw", Beschrijving = "Gebouw met meerdere appartementen", Bouwlagen = 3, HeeftBouwlagen = true, HeeftKelder = true, HeeftDak = true, HeeftTerras = true, Planning = "11 blokken, 38 taken: Voorbereiding, Ruwbouw", Traject = "9 mijlpalen", Schijven = "10 schijven" },
            new ProjectSjabloonAiKeuze { Id = 9, Naam = "Woning", Bouwlagen = 2, Kelder = false, Dak = "hellend", HeeftBouwlagen = false },
        },
    };

    private static JsonNode Antwoord(string json) => JsonNode.Parse(json)!;

    [Fact]
    public void Een_gekozen_sjabloon_uit_de_lijst_en_de_antwoorden_komen_door_en_aannames_blijven_zichtbaar()
    {
        var v = ProjectSjabloonAiVerwerker.Verwerk(Antwoord("""
            {"sjabloonId":7,"sjabloonReden":"Meerdere appartementen in één gebouw.","verdiepingen":4,"terrassen":[1,3,3],"kelder":true,"dak":"plat","eenPlanningVoorAlles":true,
             "aannames":["Terrassen enkel bij de afdek van het gelijkvloers en de 2de verdieping."],"vragen":["Komt er een lift?"],"toelichting":"Gebouw met vier bouwlagen."}
            """), Context());
        Assert.True(v.Ok);
        Assert.Equal(7, v.SjabloonId);
        Assert.Equal(4, v.Antwoorden.Verdiepingen);
        Assert.True(v.Antwoorden.Kelder);
        Assert.Equal("plat", v.Antwoorden.Dak);
        Assert.Equal(new[] { 1, 3 }, v.Antwoorden.Terrassen);   // dubbele waarde weg
        Assert.True(v.Antwoorden.EenPlanningVoorAlles);
        Assert.Single(v.Aannames);
        Assert.Equal(new[] { "Komt er een lift?" }, v.Vragen);
    }

    [Fact]
    public void Een_sjabloon_dat_niet_in_de_lijst_staat_wordt_genegeerd()
    {
        var v = ProjectSjabloonAiVerwerker.Verwerk(Antwoord("""{"sjabloonId":999,"verdiepingen":2,"eenPlanningVoorAlles":false,"toelichting":"x"}"""), Context());
        Assert.True(v.Ok);
        Assert.Null(v.SjabloonId);
        Assert.Contains(v.Aannames, a => a.Contains("niet bestaat"));

        var geen = ProjectSjabloonAiVerwerker.Verwerk(Antwoord("""{"sjabloonId":null,"sjabloonReden":"Er is geen sjabloon voor een garagebox.","verdiepingen":1,"eenPlanningVoorAlles":false,"toelichting":"x"}"""), Context());
        Assert.Null(geen.SjabloonId);
        Assert.Equal("Er is geen sjabloon voor een garagebox.", geen.SjabloonReden);
    }

    [Fact]
    public void Wat_niet_gezegd_werd_krijgt_geen_stille_waarde_maar_een_aanname_en_alles_wordt_begrensd()
    {
        var v = ProjectSjabloonAiVerwerker.Verwerk(Antwoord("""
            {"sjabloonId":9,"verdiepingen":99,"terrassen":[0,2,30,-1],"kelder":null,"dak":"zadel","eenPlanningVoorAlles":false,
             "vragen":["a","b","c","d","e"],"toelichting":"x"}
            """), Context());
        Assert.Equal(20, v.Antwoorden.Verdiepingen);
        Assert.Equal(new[] { 2 }, v.Antwoorden.Terrassen);   // enkel bouwlagen die bestaan
        Assert.False(v.Antwoorden.Kelder);
        Assert.Null(v.Antwoorden.Dak);                       // onbekende waarde
        Assert.Contains(v.Aannames, a => a.StartsWith("Kelder niet vermeld"));
        Assert.Contains(v.Aannames, a => a.StartsWith("Dak niet vermeld"));
        Assert.Equal(3, v.Vragen.Count);

        // Zonder aantal bouwlagen: dat van het sjabloon, met een aanname.
        var zonder = ProjectSjabloonAiVerwerker.Verwerk(Antwoord("""{"sjabloonId":9,"eenPlanningVoorAlles":false,"toelichting":"x"}"""), Context());
        Assert.Equal(2, zonder.Antwoorden.Verdiepingen);
        Assert.Contains(zonder.Aannames, a => a.StartsWith("Aantal bouwlagen niet vermeld"));
    }

    [Fact]
    public void Wat_naar_de_AI_gaat_bevat_de_sjablonen_en_aantallen_maar_geen_namen()
    {
        var bericht = ProjectSjabloonAiVerwerker.BouwBericht("Appartementsgebouw met vier bouwlagen.", Context());
        Assert.Contains("id 7: Appartementsgebouw", bericht);
        Assert.Contains("12 eenheden", bericht);
        Assert.Contains("uitvoeringstermijn 420 werkdagen", bericht);
        Assert.Contains("terrassen of luifels per bouwlaag", bericht);
        Assert.DoesNotContain("Woning 1", bericht);
        Assert.Contains("Appartementsgebouw met vier bouwlagen.", bericht);
    }

    [Theory]
    [InlineData("Appartementsgebouw met 12 appartementen, 4 bouwlagen, kelder en een plat dak van 450 m2.", 0)]
    [InlineData("Contact: jan@voorbeeld.be", 1)]
    [InlineData("Bel 0475 12 34 56 voor info", 1)]
    [InlineData("Gelegen aan de Kerkstraat 12 in het centrum", 1)]
    [InlineData("Budget € 450.000 voor de ruwbouw", 1)]
    [InlineData("BTW BE0123.456.789", 1)]
    public void Persoonlijke_gegevens_in_de_beschrijving_worden_herkend(string tekst, int minstens)
        => Assert.Equal(minstens == 0, ProjectSjabloonAiVerwerker.ControleerPrivacy(tekst).Count == 0);   // een btw-nummer wordt ook als nummer herkend: telt dus soms dubbel
}
