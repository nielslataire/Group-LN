using GroupLN.MarketData.Infrastructure.TitleResolution;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.TitleResolution;

public class ProjectNameRegexExtractorTests
{
    [Theory]
    [InlineData("het project Weylerhof in Brugge", "Weylerhof")]
    [InlineData("Het project Karmel, 15 appartementen", "Karmel")]
    public void TryExtract_HetProject_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Theory]
    [InlineData("Woonproject De Berk te Beernem", "De Berk")]
    [InlineData("woonproject Karmel met 20 units", "Karmel")]
    public void TryExtract_Woonproject_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Theory]
    [InlineData("Residentie Zuidlaan, modern wonen", "Zuidlaan")]
    [InlineData("residentie De Berk in Gent", "De Berk")]
    public void TryExtract_Residentie_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Theory]
    [InlineData("Nieuwbouwproject Weylerhof, 8730 Beernem", "Weylerhof")]
    [InlineData("nieuwbouwproject Green Valley te Gent", "Green Valley")]
    public void TryExtract_Nieuwbouwproject_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Theory]
    [InlineData("'t Molenhof — nieuwbouw appartementen", "Molenhof")]
    [InlineData("'t Kapelhof, Brugge", "Kapelhof")]
    public void TryExtract_TijdeNaam_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Theory]
    [InlineData("Project De Nieuwe Sluis, Brugge", "De Nieuwe Sluis")]
    [InlineData("project Keiberg, 30 wooneenheden", "Keiberg")]
    public void TryExtract_Project_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Theory]
    [InlineData("Appartementsproject Vossenslag in Kortrijk", "Vossenslag")]
    public void TryExtract_Appartementsproject_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Theory]
    [InlineData("Ontwikkelingsproject Villa Rustica te Knokke", "Villa Rustica")]
    public void TryExtract_Ontwikkelingsproject_ReturnsName(string text, string expected)
    {
        var result = ProjectNameRegexExtractor.TryExtract(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.ProjectName);
    }

    [Fact]
    public void TryExtract_NullInput_ReturnsNull()
    {
        var result = ProjectNameRegexExtractor.TryExtract(null);
        Assert.Null(result);
    }

    [Fact]
    public void TryExtract_NoPattern_ReturnsNull()
    {
        var result = ProjectNameRegexExtractor.TryExtract("Mooi appartement te koop in het centrum");
        Assert.Null(result);
    }

    [Fact]
    public void TryExtractFromMultiple_FirstMatchWins()
    {
        var result = ProjectNameRegexExtractor.TryExtractFromMultiple(
            null,
            "geen match hier",
            "Residentie Karmel in Brugge");
        Assert.NotNull(result);
        Assert.Equal("Karmel", result.ProjectName);
    }

    [Fact]
    public void TryExtractFromMultiple_AllNull_ReturnsNull()
    {
        var result = ProjectNameRegexExtractor.TryExtractFromMultiple(null, null, "gewone tekst zonder patroon");
        Assert.Null(result);
    }
}
