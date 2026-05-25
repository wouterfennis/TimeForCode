using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal sealed class BrowserFixture : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;

        public IBrowserContext Context { get; private set; } = null!;
        public IPage Page { get; private set; } = null!;
        public string BaseUrl { get; private set; } = string.Empty;

        [BeforeScenario]
        public async Task InitialiseAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            Context = await _browser.NewContextAsync();
            Page = await Context.NewPageAsync();
        }

        [AfterScenario]
        public async Task TeardownAsync()
        {
            await DisposeAsync();
        }

        public void SetBaseUrl(string url)
        {
            BaseUrl = url.TrimEnd('/');
        }

        public async ValueTask DisposeAsync()
        {
            if (Page != null)
            {
                await Page.CloseAsync();
            }

            if (Context != null)
            {
                await Context.CloseAsync();
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
            }

            _playwright?.Dispose();
        }
    }
}