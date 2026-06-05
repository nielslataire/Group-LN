using Microsoft.Playwright;

namespace GroupLN.MarketData.Infrastructure.Browser;

public class PlaywrightBrowserService : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IPage> NewPageAsync(string? userAgent = null)
    {
        await _lock.WaitAsync();
        try
        {
            _playwright ??= await Playwright.CreateAsync();
            _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
            });
        }
        finally
        {
            _lock.Release();
        }

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            Locale = "nl-BE",
            TimezoneId = "Europe/Brussels"
        });

        return await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
