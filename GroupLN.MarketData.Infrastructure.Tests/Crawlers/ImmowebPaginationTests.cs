using GroupLN.MarketData.Infrastructure.Crawlers;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Crawlers;

/// <summary>
/// Tests voor ExtractTotalPagesFromHtml — detectie van het totale aantal zoekpagina's.
/// </summary>
public class ImmowebPaginationTests
{
    // ══════════════════════════════════════════════════════════════════════════
    // Geen paginering aanwezig → 1 pagina
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractTotalPages_NoPaginationElement_Returns1()
    {
        const string html = """
            <html><body>
              <ul class="results">
                <li>Resultaat 1</li>
              </ul>
            </body></html>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(1, result);
    }

    [Fact]
    public void ExtractTotalPages_EmptyHtml_Returns1()
    {
        var result = ImmowebCrawler.ExtractTotalPagesFromHtml("");

        Assert.Equal(1, result);
    }

    [Fact]
    public void ExtractTotalPages_NullHtml_Returns1()
    {
        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(null!);

        Assert.Equal(1, result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1 pagina — paginering aanwezig maar alleen pagina 1
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractTotalPages_SinglePagePagination_Returns1()
    {
        const string html = """
            <ul class="pagination">
              <li class="pagination__item pagination__item--active"><a aria-label="Pagina 1" href="?page=1">1</a></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(1, result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Meerdere pagina's
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractTotalPages_FivePages_Returns5()
    {
        const string html = """
            <ul class="pagination">
              <li class="pagination__item pagination__item--active"><a href="?page=1">1</a></li>
              <li class="pagination__item"><a href="?page=2">2</a></li>
              <li class="pagination__item"><a href="?page=3">3</a></li>
              <li class="pagination__item"><a href="?page=4">4</a></li>
              <li class="pagination__item"><a href="?page=5">5</a></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(5, result);
    }

    [Fact]
    public void ExtractTotalPages_PaginationWithEllipsis_ReturnsLastNumber()
    {
        // Typisch Immoweb-formaat: 1, 2, ..., 10
        const string html = """
            <ul class="pagination">
              <li class="pagination__item pagination__item--active"><a href="?page=1">1</a></li>
              <li class="pagination__item"><a href="?page=2">2</a></li>
              <li class="pagination__item pagination__item--ellipsis"><span>...</span></li>
              <li class="pagination__item"><a href="?page=10">10</a></li>
              <li class="pagination__item pagination__item--next"><a aria-label="Volgende pagina" href="?page=2">&gt;</a></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(10, result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 333 pagina's
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractTotalPages_333Pages_Returns333()
    {
        const string html = """
            <ul class="pagination">
              <li class="pagination__item pagination__item--active"><a href="?page=1">1</a></li>
              <li class="pagination__item"><a href="?page=2">2</a></li>
              <li class="pagination__item pagination__item--ellipsis"><span>...</span></li>
              <li class="pagination__item"><a href="?page=333">333</a></li>
              <li class="pagination__item pagination__item--next"><a aria-label="Volgende pagina" href="?page=2">&gt;</a></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(333, result);
    }

    [Fact]
    public void ExtractTotalPages_333Pages_ReturnsMaxNotFirst()
    {
        // Controle: max wordt teruggegeven, niet het eerste getal
        const string html = """
            <ul class="pagination">
              <li><a href="?page=1">1</a></li>
              <li><a href="?page=2">2</a></li>
              <li><span>...</span></li>
              <li><a href="?page=333">333</a></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(333, result);
        Assert.NotEqual(1, result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Foutieve pagination — aanwezig maar niet parseerbaar → null (fallback)
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractTotalPages_PaginationWithOnlyTextItems_ReturnsNull()
    {
        // Paginering aanwezig maar bevat alleen navigatietekst, geen cijfers
        const string html = """
            <ul class="pagination">
              <li class="pagination__item--prev"><a aria-label="Vorige pagina">&lt;</a></li>
              <li class="pagination__item--next"><a aria-label="Volgende pagina">&gt;</a></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractTotalPages_EmptyPaginationUl_ReturnsNull()
    {
        const string html = """
            <ul class="pagination">
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractTotalPages_PaginationWithEllipsisOnly_ReturnsNull()
    {
        const string html = """
            <ul class="pagination">
              <li><span>...</span></li>
              <li><span>...</span></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Null(result);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Randgevallen
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExtractTotalPages_PaginationInsideLargerHtml_DetectedCorrectly()
    {
        // De <ul class="pagination"> zit in een grotere HTML-pagina
        const string html = """
            <html><body>
              <div class="search-results">
                <div class="result-item">...</div>
              </div>
              <nav class="pagination-nav">
                <ul class="pagination">
                  <li><a href="?page=1">1</a></li>
                  <li><a href="?page=2">2</a></li>
                  <li><span>...</span></li>
                  <li><a href="?page=25">25</a></li>
                </ul>
              </nav>
            </body></html>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(25, result);
    }

    [Fact]
    public void ExtractTotalPages_NavigationButtonsIgnored_OnlyNumbersCount()
    {
        // "Volgende pagina" tekst mag het resultaat niet beïnvloeden
        const string html = """
            <ul class="pagination">
              <li><a>Vorige</a></li>
              <li class="active"><a href="?page=1">1</a></li>
              <li><a href="?page=2">2</a></li>
              <li><a href="?page=3">3</a></li>
              <li><a>Volgende pagina</a></li>
            </ul>
            """;

        var result = ImmowebCrawler.ExtractTotalPagesFromHtml(html);

        Assert.Equal(3, result);
    }
}
