using Microsoft.Playwright;
using Reqnroll;

namespace TimeForCode.Website.Specifications.Steps
{
    [Binding]
    internal sealed class BrowserFixture : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private readonly ScenarioContext _scenarioContext;

        public IBrowserContext Context { get; private set; } = null!;
        public IPage Page { get; private set; } = null!;
        public string BaseUrl { get; private set; } = string.Empty;

        public BrowserFixture(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

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
            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
            Page = await Context.NewPageAsync();
        }

        [AfterScenario]
        public async Task TeardownAsync()
        {
            var tracesDir = Path.Combine("TestResults", "traces");
            Directory.CreateDirectory(tracesDir);
            var safeName = string.Concat(_scenarioContext.ScenarioInfo.Title.Split(Path.GetInvalidFileNameChars()));
            var tracePath = Path.Combine(tracesDir, $"{safeName}.zip");
            await (Context?.Tracing.StopAsync(new TracingStopOptions { Path = tracePath }) ?? Task.CompletedTask);
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