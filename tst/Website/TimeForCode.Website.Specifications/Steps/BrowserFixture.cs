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
            BaseUrl = WebsiteTestSettings.WebsiteBaseUrl;

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

        public async ValueTask DisposeAsync()
        {
            await (Page?.CloseAsync() ?? Task.CompletedTask);
            await (Context?.CloseAsync() ?? Task.CompletedTask);
            await (_browser?.CloseAsync() ?? Task.CompletedTask);
            _playwright?.Dispose();
        }
    }
}