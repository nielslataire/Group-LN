using GroupLN.MarketData.Infrastructure.Crawlers;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Crawlers;

public class ZimmoStickerTests
{
    [Theory]
    [InlineData("Project - 80% beschikbaar", 20)]
    [InlineData("Project - 100% beschikbaar", 0)]
    [InlineData("Project - 35% verkocht", 35)]
    [InlineData("Projet - 60% disponible", 40)]
    [InlineData("Nieuw", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("Project", null)]
    public void Leest_verkoopgraad_uit_projectlabel(string? sticker, int? verwacht)
    {
        var result = ZimmoCrawler.ParseStickerSoldPercentage(sticker);
        Assert.Equal(verwacht.HasValue ? (decimal?)verwacht.Value : null, result);
    }
}
