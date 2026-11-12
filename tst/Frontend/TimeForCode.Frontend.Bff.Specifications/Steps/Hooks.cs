using Reqnroll;
using Reqnroll.BoDi;
using TimeForCode.Frontend.Bff.Specifications.Mocking;

namespace TimeForCode.Frontend.Bff.Specifications.Steps
{
    [Binding]
    public sealed class Hooks(ScenarioContext scenarioContext, IObjectContainer objectContainer)
    {
        private static readonly Lazy<BffWebApplicationFactory> Factory = new(() => new BffWebApplicationFactory());

        [BeforeScenario(Order = 0)]
        public void CreateBrowser()
        {
            bool isLive = scenarioContext.ScenarioInfo.Tags.Contains(LiveHost.Tag, StringComparer.OrdinalIgnoreCase);
            Uri? liveAddress = LiveHost.ConfiguredBaseAddress;
            if (isLive && liveAddress is null)
            {
                Assert.Inconclusive($"Needs a live stack: set {LiveHost.BaseUrlVariable} to run this @{LiveHost.Tag} scenario.");
            }

            HttpClient client = isLive
                ? new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false }) { BaseAddress = liveAddress, Timeout = TimeSpan.FromSeconds(30) }
                : Factory.Value.CreateBrowser();
            objectContainer.RegisterInstanceAs(new Browser(client), dispose: true);
        }

        [AfterTestRun]
        public static void DisposeFactory()
        {
            if (Factory.IsValueCreated)
            {
                Factory.Value.Dispose();
            }
        }
    }
}