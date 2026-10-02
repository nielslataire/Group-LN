using GroupLN.MarketData.Infrastructure.Crawlers;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Crawlers;

/// <summary>
/// De overzichts-URL moet exact overeenkomen met wat Zimmo in zijn sitemap publiceert,
/// en mag geen patroon bevatten dat in robots.txt als Disallow staat.
/// </summary>
public class ZimmoProjectOverviewUrlTests
{
    [Theory]
    [InlineData("brugge", "8000", "https://www.zimmo.be/nl/brugge-8000/te-koop/nieuwbouwproject/")]
    [InlineData("erpe-mere", "9420", "https://www.zimmo.be/nl/erpe-mere-9420/te-koop/nieuwbouwproject/")]
    [InlineData("de panne", "8660", "https://www.zimmo.be/nl/de%20panne-8660/te-koop/nieuwbouwproject/")]
    [InlineData(" Koksijde ", "8670", "https://www.zimmo.be/nl/koksijde-8670/te-koop/nieuwbouwproject/")]
    public void Bouwt_de_sitemap_url_van_de_gemeente(string slug, string postcode, string verwacht)
    {
        Assert.Equal(verwacht, ZimmoSearchUrlBuilder.BuildProjectOverviewUrl(slug, postcode));
    }

    [Theory]
    [InlineData("brugge", "8000")]
    [InlineData("de panne", "8660")]
    public void Bevat_geen_door_robots_txt_verboden_patronen(string slug, string postcode)
    {
        var url = ZimmoSearchUrlBuilder.BuildProjectOverviewUrl(slug, postcode);

        Assert.DoesNotContain("?search=", url);
        Assert.DoesNotContain("/zoeken/", url);
        Assert.DoesNotContain("/filter", url);
    }
}
