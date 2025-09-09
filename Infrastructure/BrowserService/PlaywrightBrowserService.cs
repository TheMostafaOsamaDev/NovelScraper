using Microsoft.Playwright;
using NovelScraper.Domain.Interfaces;
using NovelScraper.Infrastructure.Interfaces;

namespace NovelScraper.Infrastructure.BrowserService;

public class PlaywrightBrowserService : IBrowserInfrastructure
{
    private static IBrowser? _browser;
    private IBrowserContext? _context;
    public IPage? CurrentPage { get; private set; }
    private const int DEFAULT_TIMEOUT = 60000;

    public async Task InitializeAsync(bool headless = false)
    {
        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = new[] { "--start-maximized" }
        });

        // Create a context with modified timeout settings
        _context = await _browser.NewContextAsync();
        
        _context.SetDefaultTimeout(DEFAULT_TIMEOUT);
        _context.SetDefaultNavigationTimeout(DEFAULT_TIMEOUT);

        // Disable CSS, images, fonts, media, etc.
        await _context.RouteAsync("**/*", async route =>
        {
            var req = route.Request;

            // Allow only HTML / JSON / plain text
            if (req.ResourceType is "document" or "xhr" or "fetch")
            {
                await route.ContinueAsync();
            }
            else
            {
                await route.AbortAsync();
            }
        });
    }

    public void CheckIsInitialized()
    {
        if (_browser == null || _context == null)
        {
            throw new InvalidOperationException(
                "Browser service is not initialized. Please call InitializeAsync() first.");
        }
    }

    public async Task CreateNewPageAsync(string url)
    {
        CheckIsInitialized();

        this.CurrentPage = await _context!.NewPageAsync();
        await this.CurrentPage.GotoAsync(url);
    }

    public Task<IPage> GetPageAsync()
    {
        CheckIsInitialized();
        return _context!.NewPageAsync();
    }
}